using System.Net.Http.Json;

namespace PizzaOrder;

internal sealed class OrderApiClient(HttpClient httpClient)
{
    public async Task<List<Order>> GetOrdersAsync(string customerName)
    {
        var path = $"orders?customerName={Uri.EscapeDataString(customerName)}";
        return await httpClient.GetFromJsonAsync<List<Order>>(path) ?? [];
    }

    public async Task SaveAsync(Order order)
    {
        using var response = await httpClient.PostAsJsonAsync("orders", order);
        response.EnsureSuccessStatusCode();
    }
}