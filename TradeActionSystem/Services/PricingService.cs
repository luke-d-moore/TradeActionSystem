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
        public PricingService(ILogger<PricingService> logger, IConfiguration configuration) 
        { 
            _logger = logger;
            _configuration = configuration;
            _baseURL = _configuration["PricingSystemBaseURL"];
        }
        public async Task<IDictionary<string, decimal>> GetPrices()
        {
            try
            {
                HttpClient client = new HttpClient();
                _logger.LogInformation($"GetAllPrices Request sent at Time :{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
                using (HttpResponseMessage response = await client.GetAsync(_baseURL + "/GetAllPrices").ConfigureAwait(false))
                {
                    using (HttpContent content = response.Content)
                    {
                        var json = await content.ReadAsStringAsync();
                        var responseObject = JsonSerializer.Deserialize<GetPriceResponse>(json);
                        _logger.LogInformation($"GetAllPrices Response received at Time :{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}, response was : {json}");
                        IDictionary<string, decimal> prices = responseObject?.Prices;
                        return prices ?? new Dictionary<string, decimal>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetAllPrices Failed at Time :{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}, with the following exception message" + ex.Message);
            }
            return new Dictionary<string, decimal>();
        }
    }
}
