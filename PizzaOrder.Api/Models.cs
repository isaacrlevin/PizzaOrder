namespace PizzaOrder;

internal enum PizzaSize
{
    Small,
    Medium,
    Large
}

internal enum OrderStatus
{
    Received,
    Preparing,
    Baking,
    Boxing,
    OutForDelivery,
    Delivered
}

internal sealed record MenuPizza(string Name, string Description, decimal BasePrice)
{
    public override string ToString() => Name;
}

internal static class PizzaMenu
{
    public static readonly MenuPizza[] Items =
    [
        new("Margherita", "San Marzano tomato, fresh mozzarella, basil", 11.99m),
        new("Pepperoni", "Cup-and-char pepperoni, mozzarella, tomato", 13.49m),
        new("Garden Verde", "Peppers, mushrooms, olives, red onion", 13.99m),
        new("Firehouse", "Spicy sausage, jalapeño, hot honey", 15.49m),
        new("Truffle Shroom", "Roasted mushrooms, fontina, truffle cream", 16.49m)
    ];
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
    public OrderStatus Status { get; init; } = OrderStatus.Received;
    public DateTimeOffset StatusUpdatedAt { get; init; } = DateTimeOffset.Now;
    public decimal Subtotal => Items.Sum(item => item.Total);
    public decimal Tax => Math.Round(Subtotal * 0.0825m, 2);
    public decimal DeliveryFee => 3.99m;
    public decimal Total => Subtotal + Tax + DeliveryFee;
}
