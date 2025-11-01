namespace TradeActionSystem.Services
{
    public interface ITradeActionService
    {
        public Task<decimal> Sell(string Ticker, int Quantity, decimal OriginalPrice, decimal CurrentPrice);
        public Task<decimal> Buy(string Ticker, int Quantity, decimal OriginalPrice, decimal CurrentPrice);

    }
}