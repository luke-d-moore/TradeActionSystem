using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using TradeActionSystem.Dtos;
using TradeActionSystem.Interfaces;

namespace TradeActionSystem.Services
{
    public class MessageHandlerService : IMessageHandlerService
    {
        private readonly ILogger<MessageHandlerService> _logger;
        private IConfiguration _configuration;
        private ITradeExecutionService _tradeExecutionService;
        public MessageHandlerService(ILogger<MessageHandlerService> logger, IConfiguration configuration, ITradeExecutionService tradeExecutionService)
        {
            _logger = logger;
            _configuration = configuration;
            _tradeExecutionService = tradeExecutionService;
        }
        public Task<bool> HandleMessageAsync(string jsonMessage)
        {
            _logger.LogInformation($"Processing message: {jsonMessage}");
            Message message;

            try
            {
                message = JsonSerializer.Deserialize<Message>(jsonMessage);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to deserialize json message {jsonMessage}");
                message = null;
            }

            bool tradeExecuted;

            if (message == null)
            {
                tradeExecuted = false;
            }
            else
            {
                tradeExecuted = _tradeExecutionService.ExecuteTrade(message);
            }

            return Task.FromResult(tradeExecuted);
        }
    }
}
