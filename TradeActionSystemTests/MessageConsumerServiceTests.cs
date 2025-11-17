using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using TradeActionSystem.Interfaces;
using TradeActionSystem.Services;

namespace TradeActionServiceTests
{
    public class MessageConsumerServiceTests
    {   
        private readonly Mock<ILogger<MessageConsumerService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConnectionFactory> _mockConnectionFactory;
        private readonly Mock<IMessageHandlerService> _mockMessageHandler;
        private readonly Mock<IConnection> _mockConnection;
        private readonly Mock<IChannel> _mockChannel;
        private readonly MessageConsumerService _service;
        private const string TestQueueName = "testQueue";
        private const string TestHostName = "testHost";

        public MessageConsumerServiceTests()
        {
            _mockLogger = new Mock<ILogger<MessageConsumerService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConnectionFactory = new Mock<IConnectionFactory>();
            _mockMessageHandler = new Mock<IMessageHandlerService>();
            _mockConnection = new Mock<IConnection>();
            _mockChannel = new Mock<IChannel>();

            // Setup configuration mocks
            _mockConfiguration.Setup(c => c["RabbitMQQueue"]).Returns(TestQueueName);
            _mockConfiguration.Setup(c => c["ConnectionHostName"]).Returns(TestHostName);

            // Setup connection factory mocks
            _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
            _mockConnection.Setup(conn => conn.CreateChannelAsync(null,It.IsAny<CancellationToken>())).ReturnsAsync(_mockChannel.Object);
            _mockConnection.SetupGet(conn => conn.IsOpen).Returns(true);

            // Instantiate the service
            _service = new MessageConsumerService(_mockLogger.Object, _mockConfiguration.Object, _mockConnectionFactory.Object, _mockMessageHandler.Object);
        }

        [Fact]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            // Assert
            Assert.Equal(TestQueueName, _service.QueueName);
            Assert.Equal(TestHostName, _service.HostName);
        }

        [Fact]
        public async Task StartConsumingAsync_LogsInformationAndSetsUpConsumer()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await _service.StartConsumingAsync(cts.Token);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Consumer service starting...")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);

            _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockConnection.Verify(conn => conn.CreateChannelAsync(null,It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task StartConsumingAsync_HandlesConnectionFailureAndRetries()
        {
            // Arrange
            var cts = new CancellationTokenSource();

            _mockConnectionFactory.SetupSequence(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                .Throws(new Exception("Connection failed"))
                .ReturnsAsync(_mockConnection.Object);

            _mockConnection.Setup(conn => conn.CreateChannelAsync(null,It.IsAny<CancellationToken>())).ReturnsAsync(_mockChannel.Object);

            _mockChannel.Setup(c => c.BasicConsumeAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IAsyncBasicConsumer>(), It.IsAny<CancellationToken>()))
                .Callback(() => cts.Cancel())
                .ReturnsAsync("consumerTag");

            // Act
            await _service.StartConsumingAsync(cts.Token);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to connect to RabbitMQ. Retrying in")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);

            _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        [Fact]
        public async Task PublishMessage_WhenCancellationTokenIsCancelledDuringConnection_ThrowsOperationCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken ct) =>
                {
                    await Task.Delay(2000, ct);
                    return _mockConnection.Object;
                });

            cts.CancelAfter(500);

            // Act and Assert
            await _service.StartConsumingAsync(cts.Token);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Message consumption cancelled")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        private async Task InvokeHandleReceivedMessage(ulong deliveryTag, byte[] bytes, IChannel channel)
        {
            var method = _service.GetType().GetMethod("HandleReceivedMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(_service, new object[] { deliveryTag, bytes.ToArray(), channel });
            }
        }

        [Fact]
        public async Task HandleReceivedMessage_AcknowledgesOnSuccessfulHandling()
        {
            // Arrange
            var messageBody = Encoding.UTF8.GetBytes("{\"test\":\"message\"}");
            var body = new ReadOnlyMemory<byte>(messageBody).ToArray();
            var deliveryTag = 1UL;

            _mockMessageHandler.Setup(h => h.HandleMessageAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            await InvokeHandleReceivedMessage(deliveryTag, body, _mockChannel.Object);

            // Assert
            _mockMessageHandler.Verify(h => h.HandleMessageAsync(Encoding.UTF8.GetString(messageBody)), Times.Once);
            _mockChannel.Verify(c => c.BasicAckAsync(deliveryTag, false,CancellationToken.None), Times.Once);
            _mockChannel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Message acknowledged successfully.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleReceivedMessage_NacksOnFailedHandling()
        {
            // Arrange
            var messageBody = Encoding.UTF8.GetBytes("{\"test\":\"message\"}");
            var body = new ReadOnlyMemory<byte>(messageBody).ToArray();
            var deliveryTag = 1UL;

            _mockMessageHandler.Setup(h => h.HandleMessageAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act
            await InvokeHandleReceivedMessage(deliveryTag, body, _mockChannel.Object);

            // Assert
            _mockMessageHandler.Verify(h => h.HandleMessageAsync(It.IsAny<string>()), Times.Once);
            _mockChannel.Verify(c => c.BasicNackAsync(deliveryTag, false, false, CancellationToken.None), Times.Once);
            _mockChannel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Message NACKed without requeue")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleReceivedMessage_NacksOnErrorInHandler()
        {
            // Arrange
            var messageBody = Encoding.UTF8.GetBytes("{\"test\":\"message\"}");
            var body = new ReadOnlyMemory<byte>(messageBody).ToArray();
            var deliveryTag = 1UL;
            var handlerException = new Exception("Handler failed");

            _mockMessageHandler.Setup(h => h.HandleMessageAsync(It.IsAny<string>())).ThrowsAsync(handlerException);

            // Act
            await InvokeHandleReceivedMessage(deliveryTag, body, _mockChannel.Object);

            // Assert
            _mockMessageHandler.Verify(h => h.HandleMessageAsync(It.IsAny<string>()), Times.Once);
            _mockChannel.Verify(c => c.BasicNackAsync(deliveryTag, false, false, CancellationToken.None), Times.Once);
            _mockChannel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Critical error processing message")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
    }
}