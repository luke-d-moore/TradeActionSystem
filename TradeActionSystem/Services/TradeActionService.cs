using System.Collections.Concurrent;
using System.Globalization;
using TradeActionSystem.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using TradeActionSystem.Dtos;
using System.Text.Json;

namespace TradeActionSystem.Services
{
    public class TradeActionService : TradeActionServiceBase, ITradeActionService
    {
        private readonly ILogger<TradeActionService> _logger;
        private IPricingService _pricingService;
        private IDictionary<string,decimal> _prices;
        private const int _checkRate = 500;
        private readonly string _queueName;
        public IDictionary<string, decimal> Prices
        {
            get { return _prices; }
            set { _prices = value; }
        }
        public TradeActionService(ILogger<TradeActionService> logger, IPricingService pricingService, IConfiguration configuration) 
            : base(_checkRate, logger)
        {
            _logger = logger;
            _pricingService = pricingService;
            _prices = new ConcurrentDictionary<string,decimal>();
            _queueName = configuration["RabbitMQQueue"];

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
                _logger.LogInformation($"Buy {Quantity} of {Ticker} at Price {price} at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
                //Execute the Trade
                return true;
            }
            else
            {
                _logger.LogError($"Failed attempt to {nameof(Buy)}, Ticker was : {Ticker} at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
                return false;
            }
        }
        public bool Sell(string Ticker, int Quantity)
        {
            if (!Validate(Ticker, Quantity, nameof(Sell))) return false;
            if (_prices.TryGetValue(Ticker, out var price))
            {
                _logger.LogInformation($"Sell {Quantity} of {Ticker} at Price {price} at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
                //Execute the Trade
                return true;
            }
            else
            {
                _logger.LogError($"Failed attempt to {nameof(Sell)}, Ticker was : {Ticker} at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
                return false;
            }
        }

        protected override async Task<bool> CheckMessages()
        {
            _prices = await GetPrices().ConfigureAwait(false);

            var message = await ReadMessages().ConfigureAwait(false); 

            if (message.Action == "Buy")
            {
                return Buy(message.Ticker, message.Quantity);
            }
            else
            {
                return Sell(message.Ticker, message.Quantity);
            }
        }

        private async Task<Message> ReadMessages()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = await factory.CreateConnectionAsync().ConfigureAwait(false);
            var channel = await connection.CreateChannelAsync().ConfigureAwait(false);

            var consumer = new AsyncEventingBasicConsumer(channel);

            var messageReceivedTaskSource = new TaskCompletionSource<Message>();

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var jsonmessage = Encoding.UTF8.GetString(body);

                _logger.LogInformation($"message : {jsonmessage}");

                var message = JsonSerializer.Deserialize<Message>(jsonmessage);

                messageReceivedTaskSource.SetResult(message);
            };

            await channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: true,
                consumer: consumer);

            return await messageReceivedTaskSource.Task;
        }
    }
}
