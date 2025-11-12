using System.Collections.Concurrent;
using System.Globalization;
using TradeActionSystem.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using TradeActionSystem.Dtos;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Diagnostics;
using System.Xml;
using System;

namespace TradeActionSystem.Services
{
    public class TradeActionService : TradeActionServiceBase, ITradeActionService
    {
        private readonly ILogger<TradeActionService> _logger;
        private IPricingService _pricingService;
        private ConcurrentDictionary<string,decimal> _prices;
        private const int _checkRate = 500;
        private const int _networkRecoveryInterval = 10;
        private readonly string _queueName;
        private readonly string _hostName;
        private readonly HashSet<string> _allowedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Buy", "Sell" };
        public ConcurrentDictionary<string, decimal> Prices
        {
            get { return _prices; }
            set { _prices = value; }
        }
        public TradeActionService(ILogger<TradeActionService> logger, IPricingService pricingService, IConfiguration configuration) 
            : base(logger)
        {
            _logger = logger;
            _pricingService = pricingService;
            _prices = new ConcurrentDictionary<string,decimal>();
            _queueName = configuration["RabbitMQQueue"];
            _hostName = configuration["ConnectionHostName"];

        }
        private async Task<IDictionary<string, decimal>> GetPrices()
        {
            return (await _pricingService.GetPrices().ConfigureAwait(false));
        }
        private bool Validate(string Ticker, int Quantity, string Action)
        {
            if (!_prices.Keys.Contains(Ticker, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError($"Invalid Ticker : {Ticker}, Action : {Action}");
                throw new ArgumentException("Invalid Ticker", "ticker");
            }
            if (Quantity <= 0)
            {
                _logger.LogError($"Invalid Quantity : {Quantity}, Action : {Action}");
                throw new ArgumentException("Quantity must be greater than 0.", "quantity");
            }

            return true;
        }
        public bool Buy(string Ticker, int Quantity)
        {
            if (!Validate(Ticker, Quantity, nameof(Buy))) return false;
            if (_prices.TryGetValue(Ticker, out var price))
            {
                _logger.LogInformation($"Buy {Quantity} of {Ticker} at Price {price}");
                //Execute the Trade
                return true;
            }
            else
            {
                _logger.LogError($"Failed attempt to {nameof(Buy)}, Ticker was : {Ticker}");
                return false;
            }
        }
        public bool Sell(string Ticker, int Quantity)
        {
            if (!Validate(Ticker, Quantity, nameof(Sell))) return false;
            if (_prices.TryGetValue(Ticker, out var price))
            {
                _logger.LogInformation($"Sell {Quantity} of {Ticker} at Price {price}");
                //Execute the Trade
                return true;
            }
            else
            {
                _logger.LogError($"Failed attempt to {nameof(Sell)}, Ticker was : {Ticker}");
                return false;
            }
        }

        private async Task SetPrices(IDictionary<string, decimal> prices)
        {
            foreach(var price in prices)
            {
                if(!_prices.TryAdd(price.Key, price.Value))
                {
                    _prices[price.Key] = price.Value;
                }
            }
        }

        private bool ExecuteTrade(Message message)
        {
            if (message.Action == "Buy")
            {
                return Buy(message.Ticker, message.Quantity);
            }
            else if (message.Action == "Sell")
            {
                return Sell(message.Ticker, message.Quantity);
            }
            return false;
        }

        protected override async Task CheckMessages(CancellationToken cancellationToken)
        {
            var priceUpdateTask = PriceUpdateAsync(cancellationToken);

            var messageProcessingTask = ProcessMessagesAsync(cancellationToken);

            await Task.WhenAll(messageProcessingTask, priceUpdateTask).ConfigureAwait(false);
        }

        private async Task PriceUpdateAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SetPrices(await GetPrices().ConfigureAwait(false)).ConfigureAwait(false);
                    _logger.LogInformation($"Prices updated successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get and set prices.");
                }

                await Task.Delay(_checkRate, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory { 
                HostName = _hostName, 
                AutomaticRecoveryEnabled = true, 
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_networkRecoveryInterval) };

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_prices.Any()) continue; //prevent messages being picked up before currentprices have been retrieved
                try
                {
                    using var connection = await factory.CreateConnectionAsync().ConfigureAwait(false);
                    using var channel = await connection.CreateChannelAsync().ConfigureAwait(false);

                    await channel.BasicQosAsync(0, 1, false);
                    //Only send one message at a time.
                    //Do not send me the next message until I explicitly acknowledge that I have finished processing the previous one.

                    var consumer = new AsyncEventingBasicConsumer(channel);

                    var processCompletionSource = new TaskCompletionSource<bool>();

                    cancellationToken.Register(() => processCompletionSource.SetResult(true));

                    consumer.ReceivedAsync += async (model, eventArgs) => await ProcessMessage(eventArgs, channel);

                    var consumerTag = await channel.BasicConsumeAsync(
                        queue: _queueName,
                        autoAck: false,
                        consumer: consumer);

                    _logger.LogInformation("Consumer started");

                    // Await the TaskCompletionSource task. This task finishes ONLY when
                    // cancellationToken.IsCancellationRequested becomes true.
                    await processCompletionSource.Task.ConfigureAwait(false);

                    _logger.LogInformation("Consumer stopped");

                    if (consumerTag != null && connection.IsOpen)
                    {
                        _logger.LogInformation($"Cancel consumer {consumerTag}");
                        await channel.BasicCancelAsync(consumerTag).ConfigureAwait(false);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to connect to RabbitMQ. Retrying in {_networkRecoveryInterval} seconds");
                    await Task.Delay(TimeSpan.FromSeconds(_networkRecoveryInterval), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessMessage(BasicDeliverEventArgs ea, IChannel channel)
        {
            var deliveryTag = ea.DeliveryTag;
            string jsonmessage = string.Empty;
            try
            {
                var body = ea.Body.ToArray();
                jsonmessage = Encoding.UTF8.GetString(body);

                _logger.LogInformation($"Processing message: {jsonmessage}");

                var message = JsonSerializer.Deserialize<Message>(jsonmessage);

                bool tradeExecuted = false;
                bool requeue = true;

                if (message == null || !_allowedActions.Contains(message.Action))
                {
                    tradeExecuted = false;
                    requeue = false;
                }
                else
                {
                    tradeExecuted = ExecuteTrade(message);
                }

                if (tradeExecuted)
                {
                    await channel.BasicAckAsync(deliveryTag, multiple: false);
                    _logger.LogInformation($"Message acknowledged successfully");
                }
                else
                {
                    _logger.LogError($"Trade execution failed for message : {jsonmessage}. Nack message requeue : {requeue}");
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process message: {jsonmessage}. Nack message without requeue");
                // If deserialization fails, we Nack without requeueing (potentially dead-lettering)
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
            }
        }

        //DB stuff Idempotent Consumer notes and ideas for next steps

        //Generate a Unique ID(Idempotency Key): The producing system(System B) must generate a unique,
        //non-repeating identifier(like a GUID) for each trade message.This ID should be included in the message payload.
        //Store Processed IDs: System C must have a mechanism to record every idempotency key
        //it has successfully processed.A simple database table works well for this.
        //Check and Process in a Transaction: The trade execution logic in
        //TradeActionSystem must perform a check and write within a single database transaction.

        //        // Inside the ReceivedAsync handler in TradeActionSystem

        //        // 1. Get the message and its unique ID
        //        var uniqueTradeId = message.TradeId; // Assuming TradeId is part of the message
        //        var deliveryTag = ea.DeliveryTag;

        //try
        //{
        //    // Begin a database transaction
        //    using var transaction = dbContext.Database.BeginTransaction();

        //    // 2. Check if the ID has already been processed
        //    if (await dbContext.ProcessedTradeIds.AnyAsync(id => id == uniqueTradeId))
        //    {
        //        _logger.LogInformation($"Duplicate message received for trade ID {uniqueTradeId}. Ignoring.");
        //        // Ack the message immediately since it's already processed
        //        channel.BasicAck(deliveryTag, multiple: false);
        //        transaction.Commit();
        //        return; // Exit without processing again
        //    }

        //    // 3. Action the trade
        //    await ActionTrade(message); // This contains your Buy/Sell logic

        //    // 4. Record the unique ID as processed
        //    dbContext.ProcessedTradeIds.Add(uniqueTradeId);
        //    await dbContext.SaveChangesAsync();

        //    // 5. Commit the database transaction and acknowledge the message
        //    transaction.Commit();
        //    channel.BasicAck(deliveryTag, multiple: false);
        //}
        //catch (Exception ex)
        //{
        //    // Handle error, roll back transaction, nack the message
        //    _logger.LogError(ex, "Error processing trade {TradeId}", uniqueTradeId);
        //channel.BasicNack(deliveryTag, multiple: false, requeue: true);
        //    // Note: The transaction will be rolled back automatically on exception,
        //    // or you can explicitly call transaction.Rollback().
    }
}
