using TradeActionSystem;
using TradeActionSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using TradeActionSystem.Interfaces;

namespace TradeActionServiceTests
{
    public class TradeActionServiceTests
    {
        private readonly ITradeActionService _tradeActionService;
        private readonly Mock<IPricingService> _pricingService;
        private readonly ILogger<TradeActionService> _tradeActionLogger;
        private readonly HashSet<string> _setupTickers = new HashSet<string>() { "IBM" };

        public TradeActionServiceTests()
        {
            _tradeActionLogger = new Mock<ILogger<TradeActionService>>().Object;
            _pricingService = new Mock<IPricingService>();
            _tradeActionService = new TradeActionService(_tradeActionLogger, _pricingService.Object);
        }
        public static IEnumerable<object[]> InvalidData =>
        new List<object[]>
        {
                    new object[] { null, 5 },
                    new object[] { "wrong", 5},
                    new object[] { "", 5 },
                    new object[] { "IBM", 0 },
                    new object[] { "IBM", -5}
        };

        [Fact]
        public async Task Sell_ValidTicker_ReturnsTrueAsync()
        {
            var tradeActionService = (TradeActionService)_tradeActionService;
            tradeActionService.Tickers = _setupTickers;
            Assert.True(_tradeActionService.Sell("IBM", 5));
        }
        [Fact]
        public async Task Buy_ValidTickerAndQuantity_ReturnsTrueAsync()
        {
            var tradeActionService = (TradeActionService) _tradeActionService;
            tradeActionService.Tickers = _setupTickers;
            Assert.True(_tradeActionService.Buy("IBM", 5));
        }
        [Theory, MemberData(nameof(InvalidData))]
        public void Buy_InValidArgumentInputs_ThrowsArgumentException(string ticker, int Quantity)
        {
            // Arrange
            var exceptionType = typeof(ArgumentException);
            // Act and Assert
            Assert.Throws(exceptionType, () => _tradeActionService.Buy(ticker, Quantity));
        }
        [Theory, MemberData(nameof(InvalidData))]
        public void Sell_InValidArgumentInputs_ThrowsArgumentException(string ticker, int Quantity)
        {
            // Arrange
            var exceptionType = typeof(ArgumentException);
            // Act and Assert
            Assert.Throws(exceptionType, () => _tradeActionService.Sell(ticker, Quantity));
        }
    }
}