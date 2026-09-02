#pragma warning disable ASPIRETERMINAL001

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .WithDataVolume("pizza-orders-data")
    .AddDatabase("PizzaOrders", "pizza_orders");

var api = builder.AddProject("api", "../PizzaOrder.Api/PizzaOrder.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject("console", "../PizzaOrder.Console/PizzaOrder.Console.csproj")
    .WithEnvironment("PizzaOrderApi__BaseUrl", api.GetEndpoint("http"))
    .WaitFor(api)
    .WithTerminal();

builder.Build().Run();