using TradeActionSystem.Services;
using Serilog;
using TradeActionSystem.Logging;
using TradeActionSystem.Interfaces;
using RabbitMQ.Client;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var configuration = configurationBuilder.Build();

builder.Services.AddSingleton<IConfiguration>(configuration);
LogConfiguration.ConfigureSerilog(configuration);
builder.Services.AddLogging(configure => { configure.AddSerilog(); });

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        HostName = configuration["ConnectionHostName"],
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(
            int.Parse(configuration["NetworkRecoveryIntervalSeconds"] ?? "10"))
    };
    return factory;
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


builder.Services.AddHttpClient();

var app = builder.Build();

app.Run();