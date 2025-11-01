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
        }
        private async Task<HashSet<string>> GetTickers()
        {
            return (await _pricingService.GetTickers()).ToHashSet();
        }
        public async Task<decimal> Buy(string Ticker, int Quantity, decimal OriginalPrice, decimal CurrentPrice)
        {
            var tickers = await GetTickers();
            if (!_tickers.Contains(Ticker, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid Ticker", "ticker");
            }
            if (Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException("quantity", Quantity, "Quantity must be greater than 0.");
            }

            var Difference = OriginalPrice - CurrentPrice; // for buy this is positive as original > current

            return Difference * Quantity;
        }
        public async Task<decimal> Sell(string Ticker, int Quantity, decimal OriginalPrice, decimal CurrentPrice)
        {
            var tickers = await GetTickers();
            if (!_tickers.Contains(Ticker, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid Ticker", "ticker");
            }
            if (Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException("quantity", Quantity, "Quantity must be greater than 0.");
            }

            var Difference = CurrentPrice - OriginalPrice; // for sell this is negative as original < current

            return Difference * Quantity;
        }
    }
}
