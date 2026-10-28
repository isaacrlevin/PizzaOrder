using PizzaOrder;

var apiUrl = Environment.GetEnvironmentVariable("PizzaOrderApi__BaseUrl") ?? "http://localhost:5150";
using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
var api = new OrderApiClient(httpClient);

string[] names = ["Ava", "Liam", "Noah", "Mia", "Zoe", "Ethan", "Sofia", "Lucas", "Maya", "Owen", "Nora", "Leo"];
string[] streets = ["Main St", "Oak Ave", "Maple Dr", "Cedar Ln", "Elm St", "Pine Rd", "Lake View Blvd"];

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Order client started. Submitting random orders to {apiUrl}. Ctrl+C to stop.");
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
var nextOrderAt = DateTime.UtcNow;
try
{
    while (await timer.WaitForNextTickAsync(cts.Token))
    {
        if (DateTime.UtcNow < nextOrderAt)
            continue;

        // Randomly spawn 1–2 simulated customers, then wait a random 2–10 seconds.
        for (var i = Random.Shared.Next(1, 3); i > 0; i--)
            _ = SubmitOrderAsync();
        nextOrderAt = DateTime.UtcNow.AddSeconds(Random.Shared.Next(2, 11));
    }
}
catch (OperationCanceledException)
{
}

async Task SubmitOrderAsync()
{
    var order = CreateRandomOrder();
    try
    {
        await api.SaveAsync(order);
        Console.WriteLine($"{DateTime.Now:T} Submitted {order.Number} for {order.CustomerName} (${order.Total:F2})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{DateTime.Now:T} Could not submit {order.Number}: {ex.Message}");
    }
}

Order CreateRandomOrder()
{
    var items = Enumerable.Range(0, Random.Shared.Next(1, 4))
        .Select(_ => new PizzaItem(Random.Shared.GetItems(PizzaMenu.Items, 1)[0],
            Random.Shared.GetItems(Enum.GetValues<PizzaSize>(), 1)[0], Random.Shared.Next(1, 4)))
        .ToList();
    return new Order
    {
        Number = $"PZ-{DateTime.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
        CustomerName = Random.Shared.GetItems(names, 1)[0],
        Address = $"{Random.Shared.Next(100, 999)} {Random.Shared.GetItems(streets, 1)[0]}",
        Items = items
    };
}
