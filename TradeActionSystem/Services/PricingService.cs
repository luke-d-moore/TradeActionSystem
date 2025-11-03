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
        public async Task<IList<string>> GetTickers()
        {
            try
            {
                HttpClient client = new HttpClient();

                using (HttpResponseMessage response = await client.GetAsync(_baseURL + "/GetTickers").ConfigureAwait(false))
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
