using TradeActionSystem;
using TradeActionSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using TradeActionSystem.Interfaces;
using Castle.Core.Configuration;
using System.Collections.Concurrent;

namespace TradeActionServiceTests
{
    public class TradeActionServiceTests
    {
        private readonly ITradeActionService _tradeActionService;
        private readonly Mock<IPricingService> _pricingService;
        private readonly ILogger<TradeActionService> _tradeActionLogger;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, decimal> _setupPrices = new ConcurrentDictionary<string, decimal>();

        public TradeActionServiceTests()
        {
            _tradeActionLogger = new Mock<ILogger<TradeActionService>>().Object;
            _pricingService = new Mock<IPricingService>();
            _configuration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object;
            _tradeActionService = new TradeActionService(_tradeActionLogger, _pricingService.Object, _configuration);
            _setupPrices.TryAdd("IBM", 300m);
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

        [Fact]
        public async Task Sell_ValidTicker_ReturnsTrueAsync()
        {
            var tradeActionService = (TradeActionService) _tradeActionService;
            tradeActionService.Prices = _setupPrices;
            Assert.True(_tradeActionService.Sell("IBM", 5, "abc"));
        }
        [Fact]
        public async Task Buy_ValidTickerAndQuantity_ReturnsTrueAsync()
        {
            var tradeActionService = (TradeActionService) _tradeActionService;
            tradeActionService.Prices = _setupPrices;
            Assert.True(_tradeActionService.Buy("IBM", 5, "abc"));
        }
        [Theory, MemberData(nameof(InvalidData))]
        public void Buy_InValidArgumentInputs_ThrowsArgumentException(string ticker, int Quantity, string UniqueID)
        {
            // Arrange
            // Act and Assert
            Assert.Throws<ArgumentException>(() => _tradeActionService.Buy(ticker, Quantity, UniqueID));
        }
        [Theory, MemberData(nameof(InvalidData))]
        public void Sell_InValidArgumentInputs_ThrowsArgumentException(string ticker, int Quantity, string UniqueID)
        {
            // Arrange
            // Act and Assert
            Assert.Throws<ArgumentException>(() => _tradeActionService.Sell(ticker, Quantity, UniqueID));
        }
    }
}