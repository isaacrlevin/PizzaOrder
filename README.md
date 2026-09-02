# Slice Station

An interactive pizza ordering system with two .NET projects:

- `PizzaOrder.Console/PizzaOrder.Console.csproj` is the Spectre.Console client and calls the HTTP API.
- `PizzaOrder.Api/PizzaOrder.Api.csproj` owns all PostgreSQL access.

Customers sign in by name, create multiple orders, and browse their complete order history. Orders persist between runs.

## Run

```powershell
docker compose up -d --wait
dotnet run --project PizzaOrder.Api
```

In another terminal, start the console:

```powershell
dotnet run --project PizzaOrder.Console
```

Use the arrow keys and Enter to navigate.

The API targets PostgreSQL on `localhost:55432` by default. To use another database, set
`ConnectionStrings__PizzaOrders` before starting the API:

```powershell
$env:ConnectionStrings__PizzaOrders = "Host=localhost;Port=55432;Database=pizza_orders;Username=postgres;Password=postgres"
dotnet run --project PizzaOrder.Api
```

The console targets `http://localhost:5150` by default. Set `PizzaOrderApi__BaseUrl` to use a different API URL.

Stop the container with `docker compose down`. The named volume keeps order data between container restarts; use
`docker compose down --volumes` when you intentionally want to remove it.
