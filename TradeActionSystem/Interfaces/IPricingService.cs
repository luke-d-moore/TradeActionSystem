namespace TradeActionSystem.Interfaces
{
    public interface IPricingService : IHostedService
    {
        public IDictionary<string, decimal> GetLatestPrices();
        public decimal GetLatestPriceFromTicker(string Ticker);
        public HashSet<string> GetLatestTickers();
        public Task InitialPricesLoadedTask();
    }
}