namespace PizzaOrder;

internal enum PizzaSize
{
    Small,
    Medium,
    Large
}

internal sealed record MenuPizza(string Name, string Description, decimal BasePrice)
{
    public override string ToString() => Name;
}

internal sealed record PizzaItem(MenuPizza Pizza, PizzaSize Size, int Quantity)
{
    public decimal UnitPrice => Pizza.BasePrice + Size switch
    {
        PizzaSize.Small => 0m, PizzaSize.Medium => 3m, PizzaSize.Large => 6m, _ => 0m
    };

    public decimal Total => UnitPrice * Quantity;
}

internal sealed class Order
{
    public required string Number { get; init; }
    public required string CustomerName { get; init; }
    public required string Address { get; init; }
    public required List<PizzaItem> Items { get; init; }
    public DateTimeOffset PlacedAt { get; init; } = DateTimeOffset.Now;
    public decimal Subtotal => Items.Sum(item => item.Total);
    public decimal Tax => Math.Round(Subtotal * 0.0825m, 2);
    public decimal DeliveryFee => 3.99m;
    public decimal Total => Subtotal + Tax + DeliveryFee;
}