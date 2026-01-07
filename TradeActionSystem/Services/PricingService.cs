using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;
using System.Text.Json;
using TradeActionSystem.Interfaces;
using PricingSystem.Protos;

namespace TradeActionSystem.Services
{
    public class PricingService : PricingServiceBase, IPricingService
    {
        private readonly ILogger<PricingService> _logger;
        private readonly GrpcPricingService.GrpcPricingServiceClient _grpcClient;
        private ConcurrentDictionary<string, decimal> _prices = new ConcurrentDictionary<string, decimal>();
        private readonly TaskCompletionSource<bool> _initialpriceLoad = new();
        private int _retryInterval = 5000;
        public Task InitialPriceLoadTask => _initialpriceLoad.Task;
        public ConcurrentDictionary<string, decimal> Prices => _prices;
        public PricingService(ILogger<PricingService> logger, GrpcPricingService.GrpcPricingServiceClient grpcClient)
            : base(logger)
        {
            _logger = logger;
            _grpcClient = grpcClient;
        }
        protected override async Task UpdatePrices(CancellationToken cancellationToken)
        {
            using var call = _grpcClient.GetLatestPrices(new Empty());

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await foreach (var priceUpdate in call.ResponseStream.ReadAllAsync())
                    {
                        if (Prices.Any() && !_initialpriceLoad.Task.IsCompleted)
                        {
                            _initialpriceLoad.SetResult(true);
                        }
                        Prices[priceUpdate.Symbol] = (decimal)priceUpdate.Price;

                        _logger.LogInformation($"{priceUpdate.Symbol} : {Prices[priceUpdate.Symbol]}");
                    }

                    _logger.LogError("Server connection closed gracefully. Attempting to reconnect in 5s...");
                    await Task.Delay(_retryInterval);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, $"Application shutting down");
                    throw;
                }
                catch (RpcException ex)
                {
                    _logger.LogError($"gRPC Error message: {ex.Message}. Retrying...");
                    await Task.Delay(_retryInterval);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An unexpected error occurred: {ex.Message}");
                }
            }
        }

        public IDictionary<string, decimal> GetLatestPrices()
        {
            return Prices;
        }
        public decimal GetLatestPriceFromTicker(string Ticker)
        {
            return Prices[Ticker];
        }
        public IList<string> GetLatestTickers()
        {
            return Prices.Keys.ToList();
        }

        public Task InitialPricesLoadedTask()
        {
            return InitialPriceLoadTask;
        }
    }
}
