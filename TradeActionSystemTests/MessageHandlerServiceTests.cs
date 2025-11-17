using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using TradeActionSystem.Dtos;
using TradeActionSystem.Interfaces;
using TradeActionSystem.Services;
using System;

namespace TradeActionServiceTests
{
    public class MessageHandlerServiceTests
    {
        private readonly Mock<ILogger<MessageHandlerService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ITradeExecutionService> _mockTradeExecutionService;
        private readonly MessageHandlerService _service;

        public MessageHandlerServiceTests()
        {
            _mockLogger = new Mock<ILogger<MessageHandlerService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockTradeExecutionService = new Mock<ITradeExecutionService>();

            // Instantiate the service with the mocks
            _service = new MessageHandlerService(_mockLogger.Object, _mockConfiguration.Object, _mockTradeExecutionService.Object);
        }

        [Fact]
        public async Task HandleMessageAsync_ReturnsTrueAndExecutesTrade_ForValidJsonMessage()
        {
            // Arrange
            var messageDto = new Message { Ticker = "AAPL", UniqueID = "abc", Action = "Buy", Quantity = 10 };
            var jsonMessage = JsonSerializer.Serialize(messageDto);
            _mockTradeExecutionService.Setup(s => s.ExecuteTrade(It.IsAny<Message>())).Returns(true);

            // Act
            var result = await _service.HandleMessageAsync(jsonMessage);

            // Assert
            Assert.True(result);
            _mockTradeExecutionService.Verify(s => s.ExecuteTrade(It.Is<Message>(m => m.Ticker == "AAPL")), Times.Once);
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Processing message: {jsonMessage}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ReturnsFalseAndLogsError_ForInvalidJsonMessage()
        {
            // Arrange
            var invalidJsonMessage = "{\"Ticker\":\"AAPL\", \"Quantity\":\"invalid_int\"}";

            // Act
            var result = await _service.HandleMessageAsync(invalidJsonMessage);

            // Assert
            Assert.False(result);
            _mockTradeExecutionService.Verify(s => s.ExecuteTrade(It.IsAny<Message>()), Times.Never);
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Failed to deserialize json message {invalidJsonMessage}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ReturnsFalse_WhenTradeExecutionFails()
        {
            // Arrange
            var messageDto = new Message { Ticker = "AAPL", UniqueID = "abc", Action = "Buy", Quantity = 10 };
            var jsonMessage = JsonSerializer.Serialize(messageDto);
            _mockTradeExecutionService.Setup(s => s.ExecuteTrade(It.IsAny<Message>())).Returns(false);

            // Act
            var result = await _service.HandleMessageAsync(jsonMessage);

            // Assert
            Assert.False(result);
            _mockTradeExecutionService.Verify(s => s.ExecuteTrade(It.IsAny<Message>()), Times.Once);
            // Verify no error logs, only information logs are present
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Never);
        }
    }
}
