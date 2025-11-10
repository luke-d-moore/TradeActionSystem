namespace TradeActionSystem.Interfaces
{
    public interface IPricingService
    {
        public Task<IDictionary<string, decimal>> GetPrices();
    }
}