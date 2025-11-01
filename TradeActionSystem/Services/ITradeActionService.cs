namespace TradeActionSystem.Services
{
    public interface ITradeActionService
    {
        public Task<decimal> SellAsync(string Ticker, int Quantity, decimal OriginalPrice);
        public Task<decimal> BuyAsync(string Ticker, int Quantity, decimal OriginalPrice);

    }
}