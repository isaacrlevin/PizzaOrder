using Spectre.Console;
using Spectre.Console.Rendering;

namespace PizzaOrder;

internal sealed class OrderDashboard(OrderApiClient api)
{
    private const int CompletedCount = 10;
    private static readonly OrderStatus[] Stages = Enum.GetValues<OrderStatus>();

    public async Task RunAsync()
    {
        // The dashboard is a long-running display: if rendering itself fails, report it and start over.
        while (true)
        {
            try
            {
                await RunLiveAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{DateTime.Now:T} Dashboard error, restarting: {ex.Message}");
                await Task.Delay(2000);
            }
        }
    }

    private async Task RunLiveAsync()
    {
        await AnsiConsole.Live(Render([], [], null)).AutoClear(false).StartAsync(async ctx =>
        {
            while (true)
            {
                try
                {
                    var active = await api.GetActiveOrdersAsync();
                    var completed = await api.GetCompletedOrdersAsync(CompletedCount);
                    ctx.UpdateTarget(Render(active, completed, null));
                }
                catch (Exception ex)
                {
                    ctx.UpdateTarget(Render([], [], ex.Message));
                }

                ctx.Refresh();
                await Task.Delay(1000);
            }
        });
    }

    private static IRenderable Render(IReadOnlyList<Order> active, IReadOnlyList<Order> completed, string? error)
    {
        var header = new Panel(new FigletText("ORDER BOARD").Color(Color.Yellow).Centered())
            .Border(BoxBorder.Double).BorderColor(Color.Red);
        var footer = error is null
            ? new Markup($"[grey]Live · refreshed {DateTime.Now:T} · Ctrl+C to exit[/]")
            : new Markup($"[red]⚠ Cannot reach the order API: {Markup.Escape(error)}[/]");
        return new Rows(header, ActiveSection(active), CompletedSection(completed), footer);
    }

    private static IRenderable ActiveSection(IReadOnlyList<Order> active)
    {
        var chart = new BarChart().Width(60).WithMaxValue(Math.Max(1, active.Count)).HideValues();
        foreach (var stage in Stages.Where(stage => stage != OrderStatus.Delivered))
            chart.AddItem($"{OrderStatusDisplay.Icon(stage)} {stage}",
                active.Count(order => order.Status == stage), ParseColor(OrderStatusDisplay.Color(stage)));

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Expand()
            .AddColumn("[bold]Order[/]").AddColumn("[bold]Customer[/]").AddColumn("[bold]Pizzas[/]")
            .AddColumn(new TableColumn("[bold]Total[/]").RightAligned())
            .AddColumn("[bold]Progress[/]").AddColumn("[bold]Status[/]")
            .AddColumn(new TableColumn("[bold]Age[/]").RightAligned());
        foreach (var order in active)
            table.AddRow(Markup.Escape(order.Number), Markup.Escape(order.CustomerName), Pizzas(order),
                $"${order.Total:F2}", ProgressBar(order.Status), StatusLabel(order.Status),
                Elapsed(DateTimeOffset.Now - order.PlacedAt));
        if (active.Count == 0)
            table.AddRow("[grey]No active orders[/]", "", "", "", "", "", "");

        var summary = new Rows(
            new Markup($"[bold yellow]{active.Count}[/] active order{(active.Count == 1 ? "" : "s")}"),
            active.Count == 0 ? new Markup("") : chart);
        return new Panel(new Rows(summary, table)).Header(" 🔥 Active orders ").Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow).Expand();
    }

    private static IRenderable CompletedSection(IReadOnlyList<Order> completed)
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Expand()
            .AddColumn("[bold]Order[/]").AddColumn("[bold]Customer[/]").AddColumn("[bold]Pizzas[/]")
            .AddColumn(new TableColumn("[bold]Total[/]").RightAligned())
            .AddColumn("[bold]Delivered[/]").AddColumn(new TableColumn("[bold]Took[/]").RightAligned());
        foreach (var order in completed)
            table.AddRow(Markup.Escape(order.Number), Markup.Escape(order.CustomerName), Pizzas(order),
                $"${order.Total:F2}", $"[green]✓[/] {order.StatusUpdatedAt.ToLocalTime():T}",
                Elapsed(order.StatusUpdatedAt - order.PlacedAt));
        if (completed.Count == 0)
            table.AddRow("[grey]Nothing delivered yet[/]", "", "", "", "", "");

        return new Panel(table).Header($" ✅ Last {CompletedCount} completed orders ").Border(BoxBorder.Rounded)
            .BorderColor(Color.Green).Expand();
    }

    private static string Pizzas(Order order) => Markup.Escape(string.Join(", ",
        order.Items.Select(item => $"{item.Quantity}× {item.Pizza.Name} ({item.Size.ToString()[0]})")));

    private static string ProgressBar(OrderStatus status)
    {
        var color = OrderStatusDisplay.Color(status);
        var done = (int)status + 1;
        return $"[{color}]{new string('█', done * 2)}[/][grey]{new string('░', (Stages.Length - done) * 2)}[/]";
    }

    private static string StatusLabel(OrderStatus status) =>
        $"[{OrderStatusDisplay.Color(status)}]{OrderStatusDisplay.Icon(status)} {OrderStatusDisplay.Describe(status)}[/]";

    private static string Elapsed(TimeSpan span) =>
        span.TotalMinutes >= 1 ? $"{(int)span.TotalMinutes}m {span.Seconds:D2}s" : $"{Math.Max(0, (int)span.TotalSeconds)}s";

    private static Color ParseColor(string name) => name switch
    {
        "blue" => Color.Blue, "red" => Color.Red, "yellow" => Color.Yellow, "aqua" => Color.Aqua,
        "green" => Color.Green, _ => Color.Grey
    };
}
