namespace TradeActionSystem.Services
{
    public interface ITradeActionService
    {
        public Task<bool> SellAsync(string Ticker, int Quantity);
        public Task<bool> BuyAsync(string Ticker, int Quantity);

    }
}