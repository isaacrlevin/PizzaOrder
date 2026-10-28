namespace PizzaOrder;

internal static class OrderStatusDisplay
{
    public static string Icon(OrderStatus status) => status switch
    {
        OrderStatus.Received => "🧾",
        OrderStatus.Preparing => "🥣",
        OrderStatus.Baking => "🔥",
        OrderStatus.Boxing => "📦",
        OrderStatus.OutForDelivery => "🛵",
        _ => "✅"
    };

    public static string Describe(OrderStatus status) => status switch
    {
        OrderStatus.Received => "Order sent to the kitchen",
        OrderStatus.Preparing => "Stretching dough and adding toppings",
        OrderStatus.Baking => "Baking at 750°F",
        OrderStatus.Boxing => "Slicing and boxing",
        OrderStatus.OutForDelivery => "Out for delivery",
        _ => "Delivered"
    };

    public static string Color(OrderStatus status) => status switch
    {
        OrderStatus.Received => "grey",
        OrderStatus.Preparing => "blue",
        OrderStatus.Baking => "red",
        OrderStatus.Boxing => "yellow",
        OrderStatus.OutForDelivery => "aqua",
        _ => "green"
    };
}
