using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TradeActionSystem.Interfaces;

namespace TradeActionSystem.Services
{
    public class MessageConsumerService : IMessageConsumerService
    {
        private readonly ILogger<MessageConsumerService> _logger;
        private const int _networkRecoveryInterval = 10;
        private readonly string _queueName;
        private readonly string _hostName;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IMessageHandlerService _messageHandler;
        public string HostName
        {
            get => _hostName;
        }
        public string QueueName
        {
            get => _queueName;
        }
        public MessageConsumerService(ILogger<MessageConsumerService> logger, IConfiguration configuration, IConnectionFactory connectionFactory, IMessageHandlerService messageHandler) 
        { 
            _logger = logger;
            _queueName = configuration["RabbitMQQueue"];
            _hostName = configuration["ConnectionHostName"];
            _connectionFactory = connectionFactory;
            _messageHandler = messageHandler;
        }
        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consumer service starting...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
                    using var channel = await connection.CreateChannelAsync(null,cancellationToken).ConfigureAwait(false);

                    await channel.BasicQosAsync(0, 1, false);
                    //Only send one message at a time.
                    //Do not send me the next message until I explicitly acknowledge that I have finished processing the previous one.

                    var processCompletionSource = new TaskCompletionSource<bool>();

                    cancellationToken.Register(() => processCompletionSource.SetResult(true));

                    var consumer = new AsyncEventingBasicConsumer(channel);

                    consumer.ReceivedAsync += async (model, eventArgs) =>
                    {
                        await HandleReceivedMessage(eventArgs.DeliveryTag, eventArgs.Body.ToArray(), channel);
                    };

                    var consumerTag = await channel.BasicConsumeAsync(
                        queue: QueueName,
                        autoAck: false,
                        consumer: consumer);

                    _logger.LogInformation($"Consumer started with tag: {consumerTag}");

                    // Await the TaskCompletionSource task. This task finishes ONLY when
                    // cancellationToken.IsCancellationRequested becomes true.
                    await processCompletionSource.Task.ConfigureAwait(false);

                    _logger.LogInformation("Consumer stopping due to cancellation request or channel closure");

                    if (consumerTag != null && connection.IsOpen)
                    {
                        _logger.LogInformation($"Cancel consumer {consumerTag}");
                        await channel.BasicCancelAsync(consumerTag).ConfigureAwait(false);
                    }
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError("Message consumption cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to connect to RabbitMQ. Retrying in {_networkRecoveryInterval} seconds");
                    await Task.Delay(TimeSpan.FromSeconds(_networkRecoveryInterval), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        private async Task HandleReceivedMessage(ulong deliveryTag, byte[] bytes, IChannel channel)
        {
            string jsonMessage = Encoding.UTF8.GetString(bytes);
            bool requeueOnFailure = false;

            try
            {
                bool success = await _messageHandler.HandleMessageAsync(jsonMessage).ConfigureAwait(false);

                if (success)
                {
                    await channel.BasicAckAsync(deliveryTag, false);
                    _logger.LogInformation("Message acknowledged successfully.");
                }
                else
                {
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
                    _logger.LogWarning($"Message NACKed without requeue: {jsonMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Critical error processing message: {jsonMessage}. NACKing message (requeue: {requeueOnFailure}).");
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: requeueOnFailure);
            }
        }
    }
}
