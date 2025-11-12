namespace TradeActionSystem.Services
{
    public abstract class TradeActionServiceBase : BackgroundService
    {

        private readonly ILogger<TradeActionServiceBase> _logger;

        protected TradeActionServiceBase(ILogger<TradeActionServiceBase> logger)
        {
            _logger = logger;
        }
        protected abstract Task CheckMessages(CancellationToken cancellationToken);

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trade Action Service is starting.");
            
            await CheckMessages(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Trade Action Service is stopping.");
        }
    }
}
