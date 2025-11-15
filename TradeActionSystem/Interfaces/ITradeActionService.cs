namespace TradeActionSystem.Interfaces
{
    public interface ITradeActionService : IHostedService
    {
        public bool Sell(string Ticker, int Quantity, string UniqueID);
        public bool Buy(string Ticker, int Quantity, string UniqueID);

    }
}