using TradeActionSystem;
using TradeActionSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using TradeActionSystem.Interfaces;
using System.Collections.Concurrent;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace TradeActionServiceTests
{
    public class PricingServiceTests
    {
        private readonly Mock<ILogger<PricingService>> _priceLogger;
        private readonly Mock<IConfiguration> _configuration;
        public PricingServiceTests()
        {
            _priceLogger = new Mock<ILogger<PricingService>>();
            _configuration = new Mock<IConfiguration>();
            _configuration.SetupGet(x => x[It.Is<string>(s => s == "PricingSystemBaseURL")])
                                      .Returns("http://api.example.com/prices");
        }

        private IHttpClientFactory SetupFactory(HttpResponseMessage httpResponseMessage, bool ShouldFail = false, bool ThrowsException = false)
        {
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            if (!ShouldFail)
            {
                mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(httpResponseMessage);
            }
            else
            {
                mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Throws(ThrowsException ? new Exception() : new HttpRequestException());
            }


            var controlledHttpClient = new HttpClient(mockHandler.Object);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            mockHttpClientFactory.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                                 .Returns(controlledHttpClient);
            return mockHttpClientFactory.Object;
        }
        [Fact]
        public async Task GetPrices_ReturnsPriceSuccessfully()
        {
            // Arrange
            var mockResponseContent = JsonSerializer.Serialize(new GetPriceResponse(true, "", new Dictionary<string, decimal>()));
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var pricingService = new PricingService(
                _priceLogger.Object,
                _configuration.Object,
                SetupFactory(httpResponse)
            );

            // Act
            Assert.IsType<Dictionary<string, decimal>>( await pricingService.GetPrices());
        }

        [Fact]
        public async Task GetPrices_ApiReturnsErrorStatusCode_ThrowsHttpRequestException()
        {
            // Arrange
            var mockResponseContent = JsonSerializer.Serialize(new GetPriceResponse(true, "", new Dictionary<string, decimal>()));
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var pricingService = new PricingService(
                _priceLogger.Object,
                _configuration.Object,
                SetupFactory(httpResponse, true)
            );
            // Act and Assert
            var result = await Assert.ThrowsAsync<HttpRequestException>(async () => await pricingService.GetPrices());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetPrices_ApiReturnsMalformedJson_ThrowsJsonException(string response)
        {
            // Arrange
            var mockResponseContent = response;
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var pricingService = new PricingService(
                _priceLogger.Object,
                _configuration.Object,
                SetupFactory(httpResponse)
            );

            // Act and Assert
            var result = await Assert.ThrowsAsync<JsonException>(async () => await pricingService.GetPrices());
        }

        [Fact]
        public async Task GetPrices_ApiThrowsException_ThrowsException()
        {
            // Arrange
            var mockResponseContent = JsonSerializer.Serialize(new GetPriceResponse(true, "", new Dictionary<string, decimal>()));
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var pricingService = new PricingService(
                _priceLogger.Object,
                _configuration.Object,
                SetupFactory(httpResponse, true, true)
            );
            // Act and Assert
            var result = await Assert.ThrowsAsync<Exception>(async () => await pricingService.GetPrices());
        }
    }
}