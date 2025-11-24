using TradeActionSystem.Interfaces;

namespace TradeActionSystem.Services
{
    public class TradeActionService : TradeActionServiceBase, ITradeActionService
    {
        private readonly ILogger<TradeActionService> _logger;
        private readonly IMessageConsumerService _messageConsumerService;
        private IPricingService _pricingService;
        private const int _delay = 500;

        public TradeActionService(
            ILogger<TradeActionService> logger, 
            IMessageConsumerService messageConsumerService,
            IPricingService pricingService) 
            : base(logger)
        {
            _logger = logger;
            _messageConsumerService = messageConsumerService;
            _pricingService = pricingService;
        }

        protected override async Task CheckMessages(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                if (!_pricingService.GetLatestPrices().Any())
                {
                    _logger.LogInformation("Waiting for Prices before starting the consumer");
                    await Task.Delay(_delay);
                    continue;
                }
                var messageProcessingTask = _messageConsumerService.StartConsumingAsync(cancellationToken);

                await messageProcessingTask.ConfigureAwait(false);
            }
        }

        //DB stuff Idempotent Consumer notes and ideas for next steps

        //Generate a Unique ID(Idempotency Key): The producing system(AutoTradeSystem) must generate a unique,
        //non-repeating identifier(like a GUID) for each trade message.This ID should be included in the message payload.
        //Store Processed IDs: TradeActionSystem must have a mechanism to record every idempotency key
        //it has successfully processed.A simple database table works well for this.
        //Check and Process in a Transaction: The trade execution logic in
        //TradeActionSystem must perform a check and write within a single database transaction.
        //// Inside the ReceivedAsync handler in TradeActionSystem
        //// 1. Get the message and its unique ID
        // var uniqueTradeId = message.UniqueID; // Assuming TradeId is part of the message
        // var deliveryTag = ea.DeliveryTag;
        //try
        //{
        //// Begin a database transaction
        //    using var transaction = dbContext.Database.BeginTransaction();
        //// 2. Check if the ID has already been processed
        //    if (await dbContext.ProcessedTradeIds.AnyAsync(id => id == UniqueID))
        //    {
        //        _logger.LogInformation($"Duplicate message received for trade ID {UniqueID}. Ignoring.");
        //        // Ack the message immediately since it's already processed
        //        channel.BasicAck(deliveryTag, multiple: false);
        //        transaction.Commit();
        //        return; // Exit without processing again
        //    }
        //// 3. Action the trade
        //    await ActionTrade(message); // This contains your Buy/Sell logic
        //// 4. Record the unique ID as processed
        //    dbContext.ProcessedTradeIds.Add(UniqueID);
        //    await dbContext.SaveChangesAsync();
        //// 5. Commit the database transaction and acknowledge the message
        //    transaction.Commit();
        //    channel.BasicAck(deliveryTag, multiple: false);
        //}
        //catch (Exception ex)
        //{
        //// Handle error, roll back transaction, nack the message
        //    _logger.LogError(ex, "Error processing trade {UniqueID}", UniqueID);
        //channel.BasicNack(deliveryTag, multiple: false, requeue: true);
        //// Note: The transaction will be rolled back automatically on exception,
        //// or you can explicitly call transaction.Rollback().
    }
}
