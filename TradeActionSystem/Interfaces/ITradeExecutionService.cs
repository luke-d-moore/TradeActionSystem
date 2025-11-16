using TradeActionSystem.Dtos;

namespace TradeActionSystem.Interfaces
{
    public interface ITradeExecutionService
    {
        public bool ExecuteTrade(Message message);
    }
}