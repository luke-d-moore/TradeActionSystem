using TradeActionSystem;
using TradeActionSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;

namespace TradeActionServiceTests
{
    public class TradeActionServiceTests
    {
        private readonly ITradeActionService _tradeActionService;
        private readonly Mock<IPricingService> _pricingService;
        private readonly ILogger<TradeActionService> _tradeActionLogger;

        public TradeActionServiceTests()
        {
            _tradeActionLogger = new Mock<ILogger<TradeActionService>>().Object;
            _pricingService = new Mock<IPricingService>();
            _pricingService.Setup(x => x.GetTickers()).ReturnsAsync(new List<string>() { "IBM" });
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
        public async Task SellAsync_ValidTicker_ReturnsTrueAsync()
        {
            Assert.True(await _tradeActionService.SellAsync("IBM", 5));
        }
        [Fact]
        public async Task BuyAsync_ValidTickerAndQuantity_ReturnsTrueAsync()
        {
            Assert.True(await _tradeActionService.BuyAsync("IBM", 5));
        }
        [Theory, MemberData(nameof(InvalidData))]
        public void BuyAsync_InValidArgumentInputs_ThrowsArgumentException(string ticker, int Quantity)
        {
            // Arrange
            var exceptionType = typeof(ArgumentException);
            // Act and Assert
            Assert.ThrowsAsync(exceptionType, async () => await _tradeActionService.BuyAsync(ticker, Quantity));
        }
        [Theory, MemberData(nameof(InvalidData))]
        public void SellAsync_InValidArgumentInputs_ThrowsArgumentException(string ticker, int Quantity)
        {
            // Arrange
            var exceptionType = typeof(ArgumentException);
            // Act and Assert
            Assert.ThrowsAsync(exceptionType, async () => await _tradeActionService.SellAsync(ticker, Quantity));
        }
    }
}