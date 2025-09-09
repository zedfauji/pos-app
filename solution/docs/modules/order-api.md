# OrderApi Module

- Project: `solution/backend/OrderApi/`
- Entry point: `Program.cs`
- Technologies: ASP.NET Core 8, Controllers, DI, Swagger (Development), Npgsql
- Database: PostgreSQL 17 (Cloud SQL via Unix socket in Production)

## Configuration

`appsettings.json` section `Postgres`:

```json
{
  "Postgres": {
    "InstanceConnectionName": "bola8pos:northamerica-south1:pos-app-1",
    "Username": "posapp",
    "Password": "Campus_66",
    "Database": "posapp",
    "LocalConnectionString": "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=posapp",
    "CloudRunSocketConnectionString": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Username=posapp;Password=Campus_66;Database=posapp;SslMode=Disable"
  }
}
```

Program selects Local in Development and CloudRun socket in Production.

## Schema Initialization

- Hosted service: `Services/DatabaseInitializer`
- Ensures schema `ord`:
  - `orders`, `order_items`, `order_logs`

## Pricing, Validation, and Logs

- Pricing and validation is performed in `Services/OrderService` using `Repositories/OrderRepository` helpers:
  - Items: validates availability, snapshots price/vendor, computes modifier deltas from `menu.modifier_options`, calculates profit.
  - Combos: validates combo and its child items availability, snapshots combo price and vendor sum.
  - Logs: `order_logs` capture create/add/update/delete/close with old/new JSON.
  - Totals: `RecalculateTotalsAsync` recomputes subtotal, profit_total, and total from items.

## Endpoints

- Health: `GET /health` → 200 OK
- DB Debug: `GET /debug/db` → `{ configured, canConnect, serverVersion, database, user, error? }`
- Orders:
  - `POST /api/orders` (Create order)
  - `GET /api/orders/{orderId}` (Get order)
  - `GET /api/orders/by-session/{sessionId}?includeHistory={bool}`
  - `POST /api/orders/{orderId}/items` (Add items)
  - `PUT /api/orders/{orderId}/items/{orderItemId}` (Update item)
  - `DELETE /api/orders/{orderId}/items/{orderItemId}` (Delete item)
  - `POST /api/orders/{orderId}/close` (Close order)
- Logs & Totals:
  - `GET /api/orders/{orderId}/logs?page=&pageSize=`
  - `POST /api/orders/{orderId}/logs/recalculate`

## Deployment

Use Cloud Run deploy script:

```
./solution/backend/deploy-order-cloudrun.ps1 -ProjectId bola8pos -Region northamerica-south1 -ServiceName magidesk-order -CloudSqlInstance bola8pos:northamerica-south1:pos-app-1
```

- Dockerfile: `solution/backend/OrderApi/Dockerfile` (copied to solution root during build)
- Cloud SQL is attached with `--add-cloudsql-instances`; app uses Unix socket connection in Production.
