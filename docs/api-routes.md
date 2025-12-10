# API Routes

Base path: `https://<host>/`
All routes below are relative to the base path.

## InventoryApi

- Health
  - GET    `/health`
- DB
  - GET    `/api/db/probe`
  - POST   `/api/db/apply-schema`
- Items
  - GET    `/api/inventory/items` (query: `vendorId?`, `categoryId?`, `isMenuAvailable?`, `search?`)
  - GET    `/api/inventory/items/{id}`
  - POST   `/api/inventory/items`
  - PUT    `/api/inventory/items/{id}`
  - DELETE `/api/inventory/items/{id}`
- Stock
  - POST   `/api/inventory/items/{id}/adjust` (body: `{ delta, reason, userId? }`)
  - POST   `/api/inventory/items/adjust-by-sku` (body: `{ sku, delta, reason, userId? }`)
  - POST   `/api/inventory/availability/check` (body: `[{ id, quantity }]`)
  - POST   `/api/inventory/availability/check-by-sku` (body: `[{ sku, quantity }]`)
- Reports
  - GET    `/api/reports/low-stock?threshold={n}`
  - GET    `/api/reports/margins?from=YYYY-MM-DD&to=YYYY-MM-DD`

## SettingsApi (separate project)

- Settings
  - GET    `/api/settings/frontend?host={host?}`
  - PUT    `/api/settings/frontend?host={host?}`
  - GET    `/api/settings/backend?host={host?}`
  - PUT    `/api/settings/backend?host={host?}`
  - GET    `/api/settings/app?host={host?}`
  - PUT    `/api/settings/app?host={host?}`
  - GET    `/api/settings/frontend/defaults`
  - GET    `/api/settings/backend/defaults`
  - GET    `/api/settings/app/defaults`
  - GET    `/api/settings/audit?host={host?}&limit={n}`

## TablesApi (separate project)

- Health
  - GET    `/health`

- Tables
  - GET    `/tables`
  - GET    `/tables/{label}`
  - GET    `/tables/counts`
  - POST   `/tables/{label}/start`
  - POST   `/tables/{label}/stop`
  - POST   `/tables/{label}/items`
  - GET    `/tables/{label}/items`
  - POST   `/tables/{label}/force-free`
  - POST   `/tables/{fromLabel}/move?to={toLabel}`

- Sessions
  - GET    `/sessions/active`
  - POST   `/sessions/enforce` (uses env `SETTINGSAPI_BASEURL` to read app defaults)

- Settings (rate)
  - GET    `/settings/rate` → `{ ratePerMinute }`
  - PUT    `/settings/rate` (body is a number)
