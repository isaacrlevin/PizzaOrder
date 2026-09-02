using PizzaOrder;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("PizzaOrders")
                       ?? "Host=localhost;Port=55432;Database=pizza_orders;Username=postgres;Password=postgres";
builder.Services.AddSingleton(new OrderDatabase(connectionString));

var app = builder.Build();
var database = app.Services.GetRequiredService<OrderDatabase>();
await database.InitializeAsync();

app.MapGet("/orders", async (string customerName) =>
{
    if (string.IsNullOrWhiteSpace(customerName))
        return Results.BadRequest("A customer name is required.");

    return Results.Ok(await database.GetOrdersAsync(customerName));
});

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
