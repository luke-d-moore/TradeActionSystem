namespace TradeActionSystem.Services
{
    public interface IPricingService
    {
        public Task<IList<string>> GetTickers();
    }
}