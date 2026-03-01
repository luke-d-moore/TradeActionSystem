using PricingSystem.Protos;
using RabbitMQ.Client;
using Serilog;
using TradeActionSystem.Interfaces;
using TradeActionSystem.Logging;
using TradeActionSystem.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var configuration = configurationBuilder.Build();

LogConfiguration.ConfigureSerilog(configuration);
builder.Services.AddLogging(configure => { configure.AddSerilog(); });

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    return new ConnectionFactory
    {
        HostName = builder.Configuration["ConnectionHostName"] ?? "rabbitmq",
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(
            int.Parse(builder.Configuration["NetworkRecoveryIntervalSeconds"] ?? "10"))
    };
});

builder.Services.AddSingleton<PricingService>();
builder.Services.AddSingleton<TradeActionService>();
builder.Services.AddSingleton<IMessageConsumerService, MessageConsumerService>();
builder.Services.AddSingleton<IMessageHandlerService, MessageHandlerService>();
builder.Services.AddSingleton<ITradeExecutionService, TradeExecutionService>();

builder.Services.AddSingleton<IPricingService>(p => p.GetRequiredService<PricingService>());
builder.Services.AddSingleton<ITradeActionService>(p => p.GetRequiredService<TradeActionService>());

builder.Services.AddHostedService(p => p.GetRequiredService<PricingService>());
builder.Services.AddHostedService(p => p.GetRequiredService<TradeActionService>());


builder.Services.AddGrpcClient<GrpcPricingService.GrpcPricingServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["PricingSystemBaseURL"]);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var app = builder.Build();

app.Run();