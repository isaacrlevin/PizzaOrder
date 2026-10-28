using System.Net;
using System.Net.Http.Json;

namespace PizzaOrder;

internal sealed class OrderApiClient(HttpClient httpClient)
{
    public async Task<List<Order>> GetOrdersAsync(string customerName)
    {
        var path = $"orders?customerName={Uri.EscapeDataString(customerName)}";
        return await httpClient.GetFromJsonAsync<List<Order>>(path) ?? [];
    }

    public async Task<List<Order>> GetActiveOrdersAsync() =>
        await httpClient.GetFromJsonAsync<List<Order>>("orders/active") ?? [];

    public async Task<List<Order>> GetCompletedOrdersAsync(int limit) =>
        await httpClient.GetFromJsonAsync<List<Order>>($"orders/completed?limit={limit}") ?? [];

    public async Task<Order?> GetOrderAsync(string number)
    {
        using var response = await httpClient.GetAsync($"orders/{Uri.EscapeDataString(number)}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task SaveAsync(Order order)
    {
        using var response = await httpClient.PostAsJsonAsync("orders", order);
        response.EnsureSuccessStatusCode();
    }
}
