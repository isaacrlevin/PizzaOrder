using PizzaOrder;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("PizzaOrders")
                       ?? "Host=localhost;Port=55432;Database=pizza_orders;Username=postgres;Password=postgres";
builder.Services.AddSingleton(new OrderDatabase(connectionString));
builder.Services.AddHostedService<OrderProgressionService>();

var app = builder.Build();
var database = app.Services.GetRequiredService<OrderDatabase>();
await database.InitializeAsync();

app.MapGet("/orders", async (string customerName) =>
{
    if (string.IsNullOrWhiteSpace(customerName))
        return Results.BadRequest("A customer name is required.");

    return Results.Ok(await database.GetOrdersAsync(customerName));
});

app.MapGet("/orders/active", async () => Results.Ok(await database.GetActiveOrdersAsync()));

app.MapGet("/orders/completed", async (int? limit) =>
    Results.Ok(await database.GetCompletedOrdersAsync(Math.Clamp(limit ?? 10, 1, 100))));

app.MapGet("/orders/{number}", async (string number) =>
    await database.GetOrderAsync(number) is { } order ? Results.Ok(order) : Results.NotFound());

app.MapPost("/orders", async (Order order) =>
{
    if (string.IsNullOrWhiteSpace(order.Number) ||
        string.IsNullOrWhiteSpace(order.CustomerName) ||
        string.IsNullOrWhiteSpace(order.Address) ||
        order.Items.Count == 0 ||
        order.Items.Any(item => item.Quantity is < 1 or > 10))
        return Results.BadRequest("The order is invalid.");

    await database.SaveAsync(order);
    return Results.Created($"/orders/{Uri.EscapeDataString(order.Number)}", order);
});

app.Run();
