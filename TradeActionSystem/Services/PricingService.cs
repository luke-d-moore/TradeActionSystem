using Serilog;
using System;
using System.Text.Json;

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
        public async Task<decimal> GetPriceFromTicker(string ticker)
        {
            try
            {
                HttpClient client = new HttpClient();

                using (HttpResponseMessage response = await client.GetAsync(_baseURL + "/GetPrice/" + ticker))
                {
                    using (HttpContent content = response.Content)
                    {
                        var json = await content.ReadAsStringAsync();
                        var responseObject = JsonSerializer.Deserialize<GetPriceResponse>(json);
                        decimal? currentPrice = (responseObject?.Prices.Values.FirstOrDefault());
                        return currentPrice.HasValue ? currentPrice.Value : 0m;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPriceFromTicker Failed with the following exception message" + ex.Message);
            }
            return 0m;
        }

        public async Task<IDictionary<string, decimal>> GetPrices()
        {
            try
            {
                HttpClient client = new HttpClient();

                using (HttpResponseMessage response = await client.GetAsync(_baseURL + "/GetAllPrices"))
                {
                    using (HttpContent content = response.Content)
                    {
                        var json = await content.ReadAsStringAsync();
                        var responseObject = JsonSerializer.Deserialize<GetPriceResponse>(json);
                        IDictionary<string, decimal> currentPrices = responseObject.Prices;
                        return currentPrices ?? new Dictionary<string, decimal>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPrices Failed with the following exception message" + ex.Message);
            }
            return new Dictionary<string, decimal>();
        }
        public async Task<IList<string>> GetTickers()
        {
            try
            {
                HttpClient client = new HttpClient();

                using (HttpResponseMessage response = await client.GetAsync(_baseURL + "/GetTickers"))
                {
                    using (HttpContent content = response.Content)
                    {
                        var json = await content.ReadAsStringAsync();
                        var responseObject = JsonSerializer.Deserialize<GetTickersResponse>(json);
                        IList<string> tickers = responseObject?.Tickers;
                        return tickers ?? new List<string>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTickers Failed with the following exception message" + ex.Message);
            }
            return new List<string>();
        }
    }
}
