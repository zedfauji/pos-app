# MenuApi Module

- Project: `solution/backend/MenuApi/`
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

## Schema Initialization & Seed

- Hosted service: `Services/DatabaseInitializer`
- Ensures schema `menu`:
  - `menu_items`, `modifiers`, `modifier_options`, `menu_item_modifiers`, `combos`, `combo_items`, `menu_history`
- Seeds if empty:
  - Items: Coffee, Classic Burger, French Fries
  - Modifier: Size with options (Small/Medium/Large) linked to Coffee
  - Combo: Burger Combo with Burger + Fries + Coffee

## Endpoints

- Health: `GET /health` → 200 OK
- DB Debug: `GET /debug/db` → `{ configured, canConnect, serverVersion, database, user, error? }`
- Menu Items:
  - `GET /api/menu/items` (query: q, category, group, availableOnly, page, pageSize)
  - `GET /api/menu/items/{id}`
  - `POST /api/menu/items`
  - `PUT /api/menu/items/{id}`
  - `DELETE /api/menu/items/{id}`
  - `POST /api/menu/items/{id}/restore`
- Combos:
  - `GET /api/menu/combos` (query: q, availableOnly, page, pageSize)
  - `GET /api/menu/combos/{id}`
  - `POST /api/menu/combos`
  - `PUT /api/menu/combos/{id}`
  - `DELETE /api/menu/combos/{id}`
- History:
  - `GET /api/menu/history?EntityType={menu_item|combo|modifier}&EntityId={id}&page=&pageSize=`
- Modifiers CRUD:
  - `GET /api/menu/modifiers` (query: q, page, pageSize)
  - `GET /api/menu/modifiers/{id}`
  - `POST /api/menu/modifiers`
  - `PUT /api/menu/modifiers/{id}`
  - `DELETE /api/menu/modifiers/{id}`

## Deployment

Use Cloud Run deploy script:

```
./solution/backend/deploy-menu-cloudrun.ps1 -ProjectId bola8pos -Region northamerica-south1 -ServiceName magidesk-menu -CloudSqlInstance bola8pos:northamerica-south1:pos-app-1
```

- Dockerfile: `solution/backend/MenuApi/Dockerfile` (copied to solution root during build)
- Cloud SQL is attached with `--add-cloudsql-instances`; app uses Unix socket connection in Production.
