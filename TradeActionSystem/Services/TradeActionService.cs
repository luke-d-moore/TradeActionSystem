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
        public async Task<decimal> BuyAsync(string Ticker, int Quantity, decimal OriginalPrice)
        {
            if (!await Validate(Ticker, Quantity, nameof(BuyAsync))) return 0m;
            var currentPrice = await _pricingService.GetPriceFromTicker(Ticker);
            if (currentPrice == 0m)
            {
                _logger.LogError($"Failed To Get Current Price for Ticker {Ticker}, Action : {nameof(BuyAsync)}");
                throw new Exception($"Failed To Get Current Price for Ticker {Ticker}");
            }

            var Difference = OriginalPrice - currentPrice; // for buy this is positive as original > current

            return Difference * Quantity;
        }
        public async Task<decimal> SellAsync(string Ticker, int Quantity, decimal OriginalPrice)
        {
            if(!await Validate(Ticker, Quantity, nameof(SellAsync))) return 0m;
            var currentPrice = await _pricingService.GetPriceFromTicker(Ticker);
            if (currentPrice == 0m)
            {
                _logger.LogError($"Failed To Get Current Price for Ticker {Ticker}, Action : {nameof(SellAsync)}");
                throw new Exception($"Failed To Get Current Price for Ticker {Ticker}");
            }

            var Difference = currentPrice - OriginalPrice; // for sell this is negative as original < current

            return Difference * Quantity;
        }
    }
}
