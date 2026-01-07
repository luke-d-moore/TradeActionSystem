namespace TradeActionSystem.Services
{
    public abstract class PricingServiceBase : BackgroundService
    {
        private readonly ILogger<PricingServiceBase> _logger;
        protected PricingServiceBase(ILogger<PricingServiceBase> logger)
        {
            _logger = logger;
        }
        protected abstract Task UpdatePrices(CancellationToken cancellationToken);

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pricing Service is starting.");
            while (!cancellationToken.IsCancellationRequested)
            {
                await UpdatePrices(cancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation("Pricing Service is stopping.");
        }
    }
}
