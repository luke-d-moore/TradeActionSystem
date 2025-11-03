namespace TradeActionSystem.Interfaces
{
    public interface IPricingService
    {
        public Task<IList<string>> GetTickers();
    }
}