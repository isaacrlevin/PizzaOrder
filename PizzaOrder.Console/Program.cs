namespace PizzaOrder;

internal static class Program
{
    public static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var apiUrl = Environment.GetEnvironmentVariable("PizzaOrderApi__BaseUrl") ?? "http://localhost:5150";
        using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
        await new PizzaCompanyApp(new OrderApiClient(httpClient)).RunAsync();
    }
}