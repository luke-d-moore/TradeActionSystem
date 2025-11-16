namespace TradeActionSystem.Services
{
    public abstract class PricingServiceBase : BackgroundService
    {

        private readonly ILogger<PricingServiceBase> _logger;
        private int _checkRate;

        protected PricingServiceBase(ILogger<PricingServiceBase> logger, int checkRate)
        {
            _logger = logger;
            _checkRate = checkRate;
        }
        protected abstract Task UpdatePrices(CancellationToken cancellationToken);

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pricing Service is starting.");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await UpdatePrices(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get and set prices.");
                }

                await Task.Delay(_checkRate, cancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation("Pricing Service is stopping.");
        }
    }
}
