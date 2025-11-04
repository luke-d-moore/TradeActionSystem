using System.Collections.Concurrent;
using System.Globalization;
using TradeActionSystem.Interfaces;

namespace TradeActionSystem.Services
{
    public class TradeActionService : TradeActionServiceBase, ITradeActionService
    {
        private readonly ILogger<TradeActionService> _logger;
        private IPricingService _pricingService;
        private HashSet<string> _tickers;
        private const int _checkRate = 500;
        public HashSet<string> Tickers
        {
            get { return _tickers; }
            set { _tickers = value; }
        }
        public TradeActionService(ILogger<TradeActionService> logger, IPricingService pricingService) 
            : base(_checkRate, logger)
        {
            _logger = logger;
            _pricingService = pricingService;
            _tickers = new HashSet<string>();
        }
        private async Task<HashSet<string>> GetTickers()
        {
            return (await _pricingService.GetTickers()).ToHashSet();
        }
        private bool Validate(string Ticker, int Quantity, string Action)
        {
            if (!_tickers.Contains(Ticker, StringComparer.OrdinalIgnoreCase))
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
            _logger.LogInformation($"Buy {Quantity} of {Ticker} at {DateTime.Now}");
            //Execute the Trade

            return true;
        }
        public bool Sell(string Ticker, int Quantity)
        {
            if(!Validate(Ticker, Quantity, nameof(Sell))) return false;
            _logger.LogInformation($"Sell {Quantity} of {Ticker} at {DateTime.Now}");
            //Execute the Trade

            return true;
        }

        protected override async Task<bool> CheckMessages()
        {
            if (!_tickers.Any()) _tickers = await GetTickers();
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
