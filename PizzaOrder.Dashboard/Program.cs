using PizzaOrder;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var apiUrl = Environment.GetEnvironmentVariable("PizzaOrderApi__BaseUrl") ?? "http://localhost:5150";
using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = TimeSpan.FromSeconds(5) };
await new OrderDashboard(new OrderApiClient(httpClient)).RunAsync();
