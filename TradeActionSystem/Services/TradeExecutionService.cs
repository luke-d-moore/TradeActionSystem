using TradeActionSystem.Dtos;
using TradeActionSystem.Interfaces;

namespace TradeActionSystem.Services
{
    public class TradeExecutionService : ITradeExecutionService
    {
        private readonly ILogger<TradeExecutionService> _logger;
        private ITradeExecutionService _tradeExecutionService;
        private IPricingService _pricingService;
        //The ProcessedIds would be stored in the db, but we are using this in memory collection to simulate the db
        private readonly HashSet<string> _processedIds = new HashSet<string>();
        public HashSet<string> ProcessedIds
        {
            get => _processedIds;
        }
        public TradeExecutionService(
            ILogger<TradeExecutionService> logger, 
            IPricingService pricingService)
        {
            _logger = logger;
            _pricingService = pricingService;
        }
        private IDictionary<string, decimal> GetPrices()
        {
            return _pricingService.GetLatestPrices();
        }
        private bool Validate(string Ticker, int Quantity, string Action, string UniqueID)
        {
            if (!GetPrices().Keys.Contains(Ticker, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError($"Invalid Ticker : {Ticker}, Action : {Action}");
                throw new ArgumentException("Invalid Ticker", "ticker");
            }
            if (Quantity <= 0)
            {
                _logger.LogError($"Invalid Quantity : {Quantity}, Action : {Action}");
                throw new ArgumentException("Quantity must be greater than 0.", "quantity");
            }
            if (ProcessedIds.Contains(UniqueID))
            {
                _logger.LogError($"Trade with this UniqueID : {UniqueID} has already been processed");
                throw new ArgumentException($"Trade with this UniqueID : {UniqueID} has already been processed", "UniqueID");
            }

            return true;
        }
        private string GetSuccessLogString(string ticker, int quantity, decimal price, string uniqueID, string action)
        {
            return $"{action} {quantity} of {ticker} at Price : {price}, UniqueID : {uniqueID}";
        }
        private string GetFailLogString(string ticker, string action, string uniqueID)
        {
            return $"Failed attempt to {action}, Ticker was : {ticker}, UniqueID : {uniqueID}";
        }
        public bool Buy(string Ticker, int Quantity, string UniqueID)
        {
            if (!Validate(Ticker, Quantity, nameof(Buy), UniqueID)) return false;
            if (GetPrices().TryGetValue(Ticker, out var price))
            {
                _logger.LogInformation(GetSuccessLogString(Ticker, Quantity, price, UniqueID, nameof(Buy)));
                //Execute the Trade
                ProcessedIds.Add(UniqueID);
                return true;
            }
            else
            {
                _logger.LogError(GetFailLogString(Ticker, nameof(Buy), UniqueID));
                return false;
            }
        }
        public bool Sell(string Ticker, int Quantity, string UniqueID)
        {
            if (!Validate(Ticker, Quantity, nameof(Sell), UniqueID)) return false;
            if (GetPrices().TryGetValue(Ticker, out var price))
            {
                _logger.LogInformation(GetSuccessLogString(Ticker, Quantity, price, UniqueID, nameof(Sell)));
                //Execute the Trade
                ProcessedIds.Add(UniqueID);
                return true;
            }
            else
            {
                _logger.LogError(GetFailLogString(Ticker, nameof(Sell), UniqueID));
                return false;
            }
        }
        public bool ExecuteTrade(Message message)
        {
            if (message.Action == "Buy")
            {
                return Buy(message.Ticker, message.Quantity, message.UniqueID);
            }
            else if (message.Action == "Sell")
            {
                return Sell(message.Ticker, message.Quantity, message.UniqueID);
            }
            else
            {
                _logger.LogError($"Invalid Action : {message.Action}");
                _logger.LogError(GetFailLogString(message.Ticker, message.Action, message.UniqueID));
                return false;
            }
        }
    }
}
