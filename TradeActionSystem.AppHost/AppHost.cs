var builder = DistributedApplication.CreateBuilder(args);

var rabbitMQConnection = builder.AddConnectionString("my-rabbit");

builder.AddProject<Projects.TradeActionSystem>("tradeactionsystem")
    .WithReference(rabbitMQConnection);

builder.Build().Run();
