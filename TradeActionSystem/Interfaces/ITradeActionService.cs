namespace TradeActionSystem.Interfaces
{
    public interface ITradeActionService : IHostedService
    {
        public bool Sell(string Ticker, int Quantity);
        public bool Buy(string Ticker, int Quantity);

    }
}