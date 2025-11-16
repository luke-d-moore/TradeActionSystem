namespace TradeActionSystem.Interfaces
{
    public interface IPricingService : IHostedService
    {
        public IDictionary<string, decimal> GetLatestPrices();
        public Task<IDictionary<string, decimal>> GetPrices();
    }
}