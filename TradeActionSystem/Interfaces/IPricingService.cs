namespace TradeActionSystem.Interfaces
{
    public interface IPricingService : IHostedService
    {
        public IDictionary<string, decimal> GetLatestPrices();
        public decimal GetLatestPriceFromTicker(string Ticker);
        public IList<string> GetLatestTickers();
        public Task InitialPricesLoadedTask();
    }
}