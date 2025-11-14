using Serilog;
using System;
using System.Globalization;
using System.Text.Json;
using TradeActionSystem.Interfaces;

namespace TradeActionSystem.Services
{
    public class PricingService : IPricingService
    {
        private readonly ILogger<PricingService> _logger;
        private IConfiguration _configuration;
        private string _baseURL;
        private HttpClient _client = new HttpClient();
        public string BaseURL
        {
            get { return _baseURL; }
        }
        public PricingService(ILogger<PricingService> logger, IConfiguration configuration) 
        { 
            _logger = logger;
            _configuration = configuration;
            _baseURL = _configuration["PricingSystemBaseURL"];
        }
        public async Task<IDictionary<string, decimal>> GetPrices()
        {
            var requestUrl = $"{BaseURL}/GetAllPrices";

            _logger.LogInformation($"GetAllPrices Request sent to {requestUrl}");

            try
            {
                using (HttpResponseMessage response = await _client.GetAsync(requestUrl).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    _logger.LogInformation($"GetAllPrices Response received. Response : {json}");

                    var responseObject = JsonSerializer.Deserialize<GetPriceResponse>(json);

                    IDictionary<string, decimal> prices = responseObject?.Prices;

                    return prices ?? new Dictionary<string, decimal>();
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"GetAllPrices Failed due to HTTP request error. Status Code: {ex.StatusCode}");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "GetAllPrices Failed JSON deserialization");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while getting all prices.");
                throw;
            }
        }
    }
}
