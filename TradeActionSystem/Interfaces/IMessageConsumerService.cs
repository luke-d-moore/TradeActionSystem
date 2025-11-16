namespace TradeActionSystem.Interfaces
{
    public interface IMessageConsumerService
    {
        Task StartConsumingAsync(CancellationToken cancellationToken);
    }
}