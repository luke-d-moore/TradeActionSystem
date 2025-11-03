using TradeActionSystem.Services;
using Serilog;
using TradeActionSystem.Logging;
using TradeActionSystem.Interfaces;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var configuration = configurationBuilder.Build();

builder.Services.AddSingleton<IConfiguration>(configuration);
LogConfiguration.ConfigureSerilog(configuration);
builder.Services.AddLogging(configure => { configure.AddSerilog(); });

builder.Services.AddSingleton<ITradeActionService, TradeActionService>();
builder.Services.AddHostedService(p => p.GetRequiredService<ITradeActionService>());
builder.Services.AddSingleton<IPricingService, PricingService>();

var app = builder.Build();

app.Run();