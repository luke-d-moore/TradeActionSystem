using System.Collections.Concurrent;

namespace TradeActionSystem.Services
{
    public class TradeActionService : ITradeActionService
    {
        private readonly ILogger<TradeActionService> _logger;
        private IPricingService _pricingService;
        private HashSet<string> _tickers;
        public TradeActionService(ILogger<TradeActionService> logger, IPricingService pricingService) 
        {
            _logger = logger;
            _pricingService = pricingService;
            _tickers = new HashSet<string>();
        }
        private async Task<HashSet<string>> GetTickers()
        {
            return (await _pricingService.GetTickers()).ToHashSet();
        }
        private async Task<bool> Validate(string Ticker, int Quantity, string Action)
        {
            if (!_tickers.Any()) _tickers = await GetTickers();
            if (!_tickers.Contains(Ticker, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError($"Invalid Ticker : {Ticker}, Action : {Action}");
                throw new ArgumentException("Invalid Ticker", "ticker");
            }
            if (Quantity <= 0)
            {
                _logger.LogError($"Invalid Quantity : {Quantity}, Action : {Action}");
                throw new ArgumentOutOfRangeException("quantity", Quantity, "Quantity must be greater than 0.");
            }

            return true;
        }
        public async Task<bool> BuyAsync(string Ticker, int Quantity)
        {
            if (!await Validate(Ticker, Quantity, nameof(BuyAsync))) return false;

            //Execute the Trade

            return true;
        }
        public async Task<bool> SellAsync(string Ticker, int Quantity)
        {
            if(!await Validate(Ticker, Quantity, nameof(SellAsync))) return false;

            //Execute the Trade

            return true;
        }
    }
}
