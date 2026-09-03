using Spectre.Console;

namespace PizzaOrder;

internal sealed class PizzaCompanyApp
{
    private readonly OrderApiClient _api;

    public PizzaCompanyApp(OrderApiClient api) => _api = api;

    private static readonly MenuPizza[] Menu =
    [
        new("Margherita", "San Marzano tomato, fresh mozzarella, basil", 11.99m),
        new("Pepperoni", "Cup-and-char pepperoni, mozzarella, tomato", 13.49m),
        new("Garden Verde", "Peppers, mushrooms, olives, red onion", 13.99m),
        new("Firehouse", "Spicy sausage, jalapeño, hot honey", 15.49m),
        new("Truffle Shroom", "Roasted mushrooms, fontina, truffle cream", 16.49m)
    ];

    public async Task RunAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            ShowBrand();
            AnsiConsole.MarkupLine("[grey]Order pizza and revisit every delicious delivery.[/]");
            AnsiConsole.WriteLine();

            var name = PromptForName();
            if (!await RunCustomerDashboardAsync(name))
                return;
        }
    }

    private async Task<bool> RunCustomerDashboardAsync(string name)
    {
        while (true)
        {
            var orders = await _api.GetOrdersAsync(name);
            ShowCustomerSummary(name, orders);
            var action = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices("Create a new order", "View a previous order", "Switch customer", "Exit"));

            switch (action)
            {
                case "Create a new order":
                    await CreateOrderAsync(name, orders.FirstOrDefault()?.Address);
                    break;
                case "View a previous order":
                    ViewPreviousOrder(orders);
                    break;
                case "Switch customer":
                    return true;
                default:
                    return false;
            }
        }
    }

    private async Task CreateOrderAsync(string name, string? previousAddress)
    {
        var order = TakeOrder(name, previousAddress);
        ShowOrderSummary(order);
        if (!AnsiConsole.Confirm("[bold yellow]Place this order?[/]", true))
        {
            AnsiConsole.MarkupLine("\n[grey]No problem — your order was not placed.[/]");
            Pause();
            return;
        }

        await _api.SaveAsync(order);
        AnsiConsole.MarkupLine("[green]✓ Order saved.[/]");
        await RunKitchenAsync(order);
        await RunDeliveryAsync(order);
        ShowReceipt(order);
        Pause();
    }

    private static string PromptForName() => AnsiConsole.Prompt(new TextPrompt<string>("Your [green]name[/]:")
        .Validate(value => string.IsNullOrWhiteSpace(value)
            ? ValidationResult.Error("[red]Please enter your name.[/]")
            : ValidationResult.Success())).Trim();

    private static Order TakeOrder(string name, string? previousAddress)
    {
        AnsiConsole.Write(new Rule("[bold yellow]1 · ORDER[/]").RuleStyle("grey").LeftJustified());
        var addressPrompt = new TextPrompt<string>("Delivery [green]address[/]:")
            .Validate(value => value.Trim().Length < 5
                ? ValidationResult.Error("[red]Please enter a valid address.[/]")
                : ValidationResult.Success());
        if (!string.IsNullOrWhiteSpace(previousAddress))
            addressPrompt.DefaultValue(previousAddress);
        var address = AnsiConsole.Prompt(addressPrompt);
        var items = new List<PizzaItem>();
        do
        {
            ShowMenu();
            var pizza = AnsiConsole.Prompt(new SelectionPrompt<MenuPizza>()
                .Title("Choose your [yellow]pizza[/]").PageSize(5)
                .UseConverter(item => $"{item.Name,-16} [grey]from ${item.BasePrice:F2}[/]").AddChoices(Menu));
            var size = AnsiConsole.Prompt(new SelectionPrompt<PizzaSize>()
                .Title("Choose a [yellow]size[/]")
                .UseConverter(s => s switch
                {
                    PizzaSize.Small => "Small · 10\"", PizzaSize.Medium => "Medium · 12\"  (+$3)",
                    _ => "Large · 16\"  (+$6)"
                })
                .AddChoices(Enum.GetValues<PizzaSize>()));
            var quantity = AnsiConsole.Prompt(new TextPrompt<int>("Quantity:").DefaultValue(1)
                .ValidationErrorMessage("[red]Choose 1–10.[/]").Validate(q => q is >= 1 and <= 10));
            items.Add(new PizzaItem(pizza, size, quantity));
            AnsiConsole.MarkupLine($"[green]✓[/] Added [bold]{quantity}× {Markup.Escape(pizza.Name)}[/]");
        } while (AnsiConsole.Confirm("Add another pizza?", false));

        return new Order
        {
            Number = $"PZ-{DateTime.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(10, 99)}",
            CustomerName = name.Trim(), Address = address.Trim(), Items = items
        };
    }

    private static void ShowCustomerSummary(string name, IReadOnlyCollection<Order> orders)
    {
        AnsiConsole.Write(new Rule($"[bold yellow]{Markup.Escape(name.ToUpperInvariant())}'S DASHBOARD[/]")
            .RuleStyle("grey").LeftJustified());
        if (orders.Count == 0)
            AnsiConsole.MarkupLine("[grey]No previous orders yet. Let's fix that.[/]\n");
        else
            AnsiConsole.MarkupLine($"[green]{orders.Count}[/] previous order{(orders.Count == 1 ? "" : "s")} · " +
                                   $"[yellow]${orders.Sum(order => order.Total):F2}[/] lifetime total\n");
    }

    private static void ViewPreviousOrder(IReadOnlyList<Order> orders)
    {
        if (orders.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]There are no previous orders for this name.[/]");
            Pause();
            return;
        }

        var order = AnsiConsole.Prompt(new SelectionPrompt<Order>()
            .Title("Select an order")
            .PageSize(10)
            .UseConverter(item =>
                $"{item.PlacedAt.ToLocalTime():MMM d, yyyy h:mm tt} · {item.Number} · ${item.Total:F2}")
            .AddChoices(orders));
        ShowOrderSummary(order);
        AnsiConsole.MarkupLine($"[grey]Placed {order.PlacedAt.ToLocalTime():f} · {Markup.Escape(order.Address)}[/]");
        Pause();
    }

    private static void Pause() => AnsiConsole.Prompt(
        new TextPrompt<string>("\n[grey]Press Enter to return to the dashboard[/]").AllowEmpty());

    private static void ShowMenu()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey)
            .AddColumn("[bold]Pizza[/]").AddColumn("[bold]Fresh toppings[/]")
            .AddColumn(new TableColumn("[bold]Small[/]").RightAligned());
        foreach (var item in Menu)
            table.AddRow($"[yellow]{Markup.Escape(item.Name)}[/]", $"[grey]{Markup.Escape(item.Description)}[/]",
                $"${item.BasePrice:F2}");
        AnsiConsole.Write(table);
    }

    private static void ShowOrderSummary(Order order)
    {
        AnsiConsole.Write(new Rule("[bold yellow]ORDER REVIEW[/]").RuleStyle("grey").LeftJustified());
        var table = new Table().Border(TableBorder.Simple).Expand().AddColumn("Item").AddColumn("Size")
            .AddColumn(new TableColumn("Qty").RightAligned()).AddColumn(new TableColumn("Total").RightAligned());
        foreach (var item in order.Items)
            table.AddRow(Markup.Escape(item.Pizza.Name), item.Size.ToString(), item.Quantity.ToString(),
                $"${item.Total:F2}");
        table.AddEmptyRow().AddRow("[grey]Subtotal[/]", "", "", $"${order.Subtotal:F2}")
            .AddRow("[grey]Tax[/]", "", "", $"${order.Tax:F2}")
            .AddRow("[grey]Delivery[/]", "", "", $"${order.DeliveryFee:F2}")
            .AddRow("[bold yellow]TOTAL[/]", "", "", $"[bold yellow]${order.Total:F2}[/]");
        AnsiConsole.Write(new Panel(table).Header($" Order {order.Number} ").Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow));
    }

    private static async Task RunKitchenAsync(Order order)
    {
        AnsiConsole.Write(new Rule("[bold yellow]2 · PREPARE & COOK[/]").RuleStyle("grey").LeftJustified());
        var steps = new (string Icon, string Text, int Delay)[]
        {
            ("🧾", "Order sent to the kitchen", 1200), ("🥣", "Hand-stretching the dough", 1500),
            ("🍅", "Adding sauce and fresh toppings", 1600), ("🔥", "Baking at 750°F", 2000),
            ("🔪", "Slicing and boxing", 1300), ("✅", "Quality check complete", 1000)
        };
        await AnsiConsole.Progress().AutoClear(false).HideCompleted(false)
            .Columns(new SpinnerColumn(), new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
            .StartAsync(async context =>
            {
                foreach (var (icon, text, delay) in steps)
                {
                    var task = context.AddTask($"{icon} [white]{text}[/]");
                    for (var i = 0; i < 10; i++)
                    {
                        await Task.Delay(delay / 10);
                        task.Increment(10);
                    }
                }
            });
        AnsiConsole.MarkupLine(
            $"\n[bold green]Fresh from the oven![/] Order [yellow]{order.Number}[/] is ready for pickup.");
    }

    private static async Task RunDeliveryAsync(Order order)
    {
        AnsiConsole.Write(new Rule("[bold yellow]3 · DELIVERY[/]").RuleStyle("grey").LeftJustified());
        string[] statuses =
        [
            "Driver Maya collected your order", "Leaving the restaurant", "Halfway there", "Turning onto your street",
            "Arrived at your door"
        ];
        await AnsiConsole.Status().Spinner(Spinner.Known.BouncingBar).SpinnerStyle(Style.Parse("yellow"))
            .StartAsync("[yellow]Finding your driver…[/]", async ctx =>
            {
                await Task.Delay(1000);
                foreach (var status in statuses)
                {
                    ctx.Status($"[yellow]🛵 {status}[/]");
                    await Task.Delay(1100);
                }
            });
        AnsiConsole.Write(
            new Panel(new Markup(
                    $"[bold green]DELIVERED[/]\n\nEnjoy your pizza, [yellow]{Markup.Escape(order.CustomerName)}[/]!\n[grey]{Markup.Escape(order.Address)}[/]"))
                .Header(" 🍕 Hot & Fresh ").Border(BoxBorder.Double).BorderColor(Color.Green).Padding(2, 1));
    }

    private static void ShowReceipt(Order order)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]Receipt: {order.Number}  •  Total paid: ${order.Total:F2}[/]");
        AnsiConsole.MarkupLine("[bold yellow]Thanks for choosing Slice Station![/] ⭐⭐⭐⭐⭐");
    }

    private static void ShowBrand() => AnsiConsole.Write(
        new Panel(new FigletText("SLICE STATION").Color(Color.Yellow).Centered())
            .Border(BoxBorder.Double).BorderColor(Color.Red).Padding(1, 0));
}