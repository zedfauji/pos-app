# InventoryApi Module

- Project: `solution/backend/InventoryApi/`
- Entry point: `Program.cs`
- Technologies: ASP.NET Core 8, Controllers, DI, Npgsql, Dapper
- Database: PostgreSQL 17 (Cloud SQL via Unix socket in Production)

## Configuration

`appsettings.json` connection config:

```json
{
  "ConnectionStrings": {
    "InventoryDb": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Username=posapp;Password=Campus_66;Database=posapp;SslMode=Disable"
  },
  "Postgres": {
    "LocalConnectionString": "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=posapp",
    "CloudRunSocketConnectionString": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Username=posapp;Password=Campus_66;Database=posapp;SslMode=Disable"
  }
}
```

Program prioritizes `ConnectionStrings:InventoryDb`; falls back to `Postgres` section; then env vars.

## Schema

Defined in `Database/schema.sql` under schema `inventory`:
- `vendors`, `inventory_categories`, `inventory_items`
- `inventory_stock` (current on-hand)
- `inventory_transactions` (audit; enum `transaction_source`)
- `v_items_current` view

Apply via endpoint: `POST /api/db/apply-schema` or run in Cloud SQL Studio.

## Endpoints

- Health: `GET /health`
- DB: `GET /api/db/probe`, `POST /api/db/apply-schema`
- Items:
  - `GET /api/inventory/items` (filters: `vendorId`, `categoryId`, `isMenuAvailable`, `search`)
  - `GET /api/inventory/items/{id}`
  - `POST /api/inventory/items`
  - `PUT /api/inventory/items/{id}`
  - `DELETE /api/inventory/items/{id}`
- Stock:
  - `POST /api/inventory/items/{id}/adjust`
  - `POST /api/inventory/items/adjust-by-sku`
  - `POST /api/inventory/availability/check`
  - `POST /api/inventory/availability/check-by-sku`
- Reports:
  - `GET /api/reports/low-stock?threshold=10`
  - `GET /api/reports/margins?from=YYYY-MM-DD&to=YYYY-MM-DD`

## Behavior & Guarantees

- Row-level locking (`FOR UPDATE`) and transactions prevent negative stock.
- All stock movements are recorded in `inventory_transactions` with source.
- Async/await across all IO; cancellation tokens respected.

## Integration

- MenuApi stores `inventory_item_id` (FK) and may look up by SKU.
- OrderApi checks availability and deducts stock via HTTP calls with retries (Polly).

## Deployment

Use script:

```
./solution/backend/deploy-inventory-cloudrun.ps1 -ProjectId bola8pos -Region northamerica-south1 -ServiceName magidesk-inventory -CloudSqlInstance bola8pos:northamerica-south1:pos-app-1
```

Service URL example: `https://magidesk-inventory-<hash>-<region>.run.app`


