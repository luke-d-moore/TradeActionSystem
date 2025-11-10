using System.Collections.Concurrent;
using System.Globalization;
using TradeActionSystem.Interfaces;

namespace TradeActionSystem.Services
{
    public class TradeActionService : TradeActionServiceBase, ITradeActionService
    {
        private readonly ILogger<TradeActionService> _logger;
        private IPricingService _pricingService;
        private IDictionary<string,decimal> _prices;
        private const int _checkRate = 500;
        public IDictionary<string, decimal> Prices
        {
            get { return _prices; }
            set { _prices = value; }
        }
        public TradeActionService(ILogger<TradeActionService> logger, IPricingService pricingService) 
            : base(_checkRate, logger)
        {
            _logger = logger;
            _pricingService = pricingService;
            _prices = new ConcurrentDictionary<string,decimal>();
        }
        private async Task<IDictionary<string, decimal>> GetPrices()
        {
            return (await _pricingService.GetPrices());
        }
        private bool Validate(string Ticker, int Quantity, string Action)
        {
            if (!_prices.Keys.Contains(Ticker, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError($"Invalid Ticker : {Ticker}, Action : {Action}");
                throw new ArgumentException("Invalid Ticker", "ticker");
            }
            if (Quantity <= 0)
            {
                _logger.LogError($"Invalid Quantity : {Quantity}, Action : {Action}");
                throw new ArgumentException("Quantity must be greater than 0.", "quantity");
            }

            return true;
        }
        public bool Buy(string Ticker, int Quantity)
        {
            if (!Validate(Ticker, Quantity, nameof(Buy))) return false;
            if (_prices.TryGetValue(Ticker, out var price))
            {
                _logger.LogInformation($"Buy {Quantity} of {Ticker} at Price {price} at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
                //Execute the Trade
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool Sell(string Ticker, int Quantity)
        {
            if (!Validate(Ticker, Quantity, nameof(Sell))) return false;
            if (_prices.TryGetValue(Ticker, out var price))
            {
                _logger.LogInformation($"Sell {Quantity} of {Ticker} at Price {price} at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
                //Execute the Trade
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override async Task<bool> CheckMessages()
        {
            _prices = await GetPrices();
            //Write code here to check messages from the queue
            //and then call the relevant method for buy or sell
            //the message will need a ticker, a quantity and the action buy or sell

            var message = new Message("IBM", 5, DateTime.Now.Millisecond < 500 ? "Buy" : "Sell"); 
            //vary the buy or sell command for testing the logic before we make the queue

            if (message.Action == "Buy")
            {
                return Buy(message.Ticker, message.Quantity);
            }
            else
            {
                return Sell(message.Ticker, message.Quantity);
            }
        }

        //This message class is here as a template of what I will be trying to do with the message queue
        //get a message which has these values and then I can use them to commit the action
        private class Message
        {
            public string Ticker;
            public int Quantity;
            public string Action;
            public Message(string ticker, int quantity, string action)
            {
                Ticker = ticker;
                Quantity = quantity;
                Action = action;
            }
        }
    }
}
