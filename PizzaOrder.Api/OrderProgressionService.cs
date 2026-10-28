namespace PizzaOrder;

/// <summary>Simulates the kitchen and drivers by moving active orders through each status over time.</summary>
internal sealed class OrderProgressionService(OrderDatabase database, ILogger<OrderProgressionService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await database.AdvanceOrdersAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not advance orders.");
            }
        }
    }
}
