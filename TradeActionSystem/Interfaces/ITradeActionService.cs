namespace TradeActionSystem.Interfaces
{
    public interface ITradeActionService : IHostedService
    {
        public Task<bool> SellAsync(string Ticker, int Quantity);
        public Task<bool> BuyAsync(string Ticker, int Quantity);

    }
}