var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TradeActionSystem>("tradeactionsystem");

builder.Build().Run();
