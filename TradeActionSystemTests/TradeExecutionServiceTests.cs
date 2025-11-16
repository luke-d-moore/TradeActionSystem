using TradeActionSystem;
using TradeActionSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using TradeActionSystem.Interfaces;
using Castle.Core.Configuration;
using System.Collections.Concurrent;
using RabbitMQ.Client;
using TradeActionSystem.Dtos;
using System.Xml;

namespace TradeActionServiceTests
{
    public class TradeExecutionServiceTests
    {
        private readonly ITradeExecutionService _tradeExecutionService;
        private readonly Mock<IPricingService> _pricingService;
        private readonly Mock<ILogger<TradeExecutionService>> _tradeExecutionLogger;
        public TradeExecutionServiceTests()
        {
            _tradeExecutionLogger = new Mock<ILogger<TradeExecutionService>>();
            _pricingService = new Mock<IPricingService>();
            _pricingService.Setup(x => x.GetLatestPrices()).Returns(new Dictionary<string, decimal>() { { "IBM", 100.0m } });
            _tradeExecutionService = new TradeExecutionService(
                _tradeExecutionLogger.Object,
                _pricingService.Object);
        }
        [Fact]
        public void Sell_ValidMessage_ReturnsTrueAsync()
        {
            var message = new Message() { Ticker = "IBM", Action = "Sell", Quantity = 5, UniqueID = "abc" };
            Assert.True(_tradeExecutionService.ExecuteTrade(message));
        }
        [Fact]
        public void Buy_ValidMessage_ReturnsTrueAsync()
        {
            var message = new Message() { Ticker = "IBM", Action = "Buy", Quantity = 5, UniqueID = "abc" };
            Assert.True(_tradeExecutionService.ExecuteTrade(message));
        }
        [Fact]
        public void Buy_AlreadyProcessedTrade_ThrowsArgumentException()
        {
            // Arrange
            var message = new Message() { Ticker = "IBM", Action = "Buy", Quantity = 5, UniqueID = "abc" };
            // Act and Assert
            Assert.True(_tradeExecutionService.ExecuteTrade(message));
            Assert.Throws<ArgumentException>(() => _tradeExecutionService.ExecuteTrade(message));
        }
        [Fact]
        public void Sell_AlreadyProcessedTrade_ThrowsArgumentException()
        {
            // Arrange
            var message = new Message() { Ticker = "IBM", Action = "Sell", Quantity = 5, UniqueID = "abc" };
            // Act and Assert
            Assert.True(_tradeExecutionService.ExecuteTrade(message));
            Assert.Throws<ArgumentException>(() => _tradeExecutionService.ExecuteTrade(message));
        }
        public static IEnumerable<object[]> InvalidData =>
        new List<object[]>
        {
            new object[] { null, 5 , "abc"},
            new object[] { "wrong", 5, "abc"},
            new object[] { "", 5 , "abc" },
            new object[] { "IBM", 0 , "abc"},
            new object[] { "IBM", -5, "abc"}
        };
        [Theory, MemberData(nameof(InvalidData))]
        public void Buy_InValidArgumentInputs_ThrowsArgumentException(string Ticker, int Quantity, string UniqueID)
        {
            // Arrange
            var message = new Message() { Action = "Buy", Quantity = Quantity, Ticker = Ticker, UniqueID = UniqueID };
            // Act and Assert
            Assert.Throws<ArgumentException>(() => _tradeExecutionService.ExecuteTrade(message));
        }
        [Theory, MemberData(nameof(InvalidData))]
        public void Sell_InValidArgumentInputs_ThrowsArgumentException(string Ticker, int Quantity, string UniqueID)
        {
            // Arrange
            var message = new Message() { Action = "Buy", Quantity = Quantity, Ticker = Ticker, UniqueID = UniqueID };
            // Act and Assert
            Assert.Throws<ArgumentException>(() => _tradeExecutionService.ExecuteTrade(message));
        }
        [Fact]
        public void ExecuteTrade_InValidArgumentInputs_ReturnsFalse()
        {
            // Arrange
            var message = new Message() { Ticker = "IBM", Action = "Sell123", Quantity = 5, UniqueID = "abc" };
            // Act and Assert
            Assert.False(_tradeExecutionService.ExecuteTrade(message));
            _tradeExecutionLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Invalid Action")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
        }

    }
}