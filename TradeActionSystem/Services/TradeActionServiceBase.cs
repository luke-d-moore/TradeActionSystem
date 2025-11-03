namespace TradeActionSystem.Services
{
    public abstract class TradeActionServiceBase : BackgroundService
    {

        private readonly int _checkRate;

        private readonly ILogger<TradeActionServiceBase> _logger;

        protected TradeActionServiceBase(int checkRate, ILogger<TradeActionServiceBase> logger)
        {
            _checkRate = checkRate;
            _logger = logger;
        }
        protected abstract Task CheckMessages();

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trade Action Service is starting.");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckMessages().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "An exception occurred");
                    //throw;
                }

                await Task.Delay(_checkRate, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Trade Action Service is stopping.");
        }
    }
}
