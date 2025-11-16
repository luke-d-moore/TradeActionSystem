namespace TradeActionSystem.Interfaces
{
    public interface IMessageHandlerService
    {
        Task<bool> HandleMessageAsync(string jsonMessage);
    }
}