# Feature Log

This document summarizes the implemented features in the MagiDesk solution, grouped by module. It reflects the current codebase as of the latest commit.

## Core Capabilities

- Backend: ASP.NET Core 8 Web API with Swagger in Development, permissive CORS for local development.
- Frontend: WinUI 3 (.NET 8), MVVM, multiple pages for business flows (Orders, Items, Vendors, Inventory, Cash Flow, Settings, Users, Tables, Dashboard, Billing).
- Shared DTO library to ensure strict API/UI contract.
- Firestore integration for persistence, plus background job pattern for long-running tasks (inventory sync, order finalization, etc.).

## Backend Features

- Vendors Management
  - List vendors, get vendor by ID.
  - Create, update, delete vendor.
  - Export vendor items to JSON/YAML/CSV.
  - Import vendor items from request body (JSON/YAML/CSV).
  - Debug probe endpoints (dump all vendors, vendor detail probe, Heineken sample probe).
  - Nested item CRUD under vendor path.

- Items Management
  - Item CRUD via vendor-scoped endpoints and generic item endpoints requiring `vendorId` query parameter.

- Orders
  - Get all orders.
  - Finalize order (creates background job, returns `202 Accepted` with `jobId`).
  - Save and retrieve cart drafts; list drafts.
  - Retrieve order-related jobs and notifications.

- Inventory
  - Get current inventory snapshot.
  - Launch background job to synchronize product names; returns `202 Accepted` with `jobId`.

- Cash Flow
  - List cash flow history.
  - Create new entry; update existing entry.

- Users & Auth
  - Users CRUD (create requires validation, update supports password change, delete).
  - Auth login endpoint returns session data (`LoginResponse`).
  - On app start, seed a default admin user (username: `admin`, password: `1234`) if missing.

- Settings
  - Frontend and backend settings retrieval and updates by optional `host` (multi-tenant friendly).
  - Separate `SettingsApi` project exposes similar endpoints (app/frontend/backend settings).

## Frontend Features

- Navigation shell with pages:
  - Dashboard, Orders, Order Builder, Billing, Cash Flow, Inventory, Items, Vendors, Users, Settings, Tables, Login, Main shell.
- Dialogs for order building and item/vendor operations.
- API client usage configured via `frontend/appsettings.json`.
- Event handlers and pages wired to backend APIs (see `Views/*.xaml.cs`).

## Sync/Worker Features

- Separate `sync` folder with `SyncService` and `SyncWorker` projects for background processing.
- Uses shared DTOs for consistent data model.

## Observability & Tooling

- Swagger/OpenAPI enabled in Development for `MagiDesk.Backend`.
- Structured logging in controllers for error scenarios.

## Security & Configuration

- Firestore project credentials via `appsettings.json` or `GOOGLE_APPLICATION_CREDENTIALS`.
- Default CORS policy allows any origin/headers/methods for local dev; tighten for production.

## Next Planned Enhancements

- Harden authentication/authorization (JWT or Identity) and remove seed password from code.
- Add validation attributes and problem-details responses.
- Add integration/unit tests for services.
- Expand telemetry and structured logs.

---

## Recent Progress (Settings, Tables, UX)

- __Settings Page Refactor (Frontend)__
  - Replaced horizontal tabs with vertical category list (General, Connections, Billing & Printing, Tables).
  - Added loading spinner and bottom action bar alignment.
  - Added a lightweight toast via `InfoBar` for Save, Print, Defaults actions.

- __Tables Section Enhancements (Frontend)__
  - Added Session Timers inputs (Warn minutes, Auto-stop minutes). Values persisted to `SettingsApi` App settings `Extras` keys:
    - `tables.session.warnMinutes`
    - `tables.session.autoStopMinutes`
  - Added "Rate Per Minute" UI with `NumberBox` and buttons to Load/Save.
  - Load/Save integrates to `TablesApi` endpoints: `GET/PUT /settings/rate`.

- __SettingsApi (Backend)__
  - Added defaults endpoints:
    - `GET /api/settings/frontend/defaults`
    - `GET /api/settings/backend/defaults`
    - `GET /api/settings/app/defaults`
  - Added audit logging on Save for frontend/backend/app sections.
  - Added audit reader endpoint: `GET /api/settings/audit?host=&limit=`.

- __TablesApi (Backend)__
  - Fixed route registration bug that nested endpoints under `/health` (moved braces to root register all routes correctly).
  - Added session move endpoint: `POST /tables/{fromLabel}/move?to=...` with move audit.
  - Added sessions overview: `GET /sessions/active`.
  - Added enforcement: `POST /sessions/enforce` that reads defaults from `SettingsApi` using env `SETTINGSAPI_BASEURL` and auto-stops sessions beyond configured minutes.
  - Added Settings endpoints for rate:
    - `GET /settings/rate` returns `{ ratePerMinute }` from `public.app_settings` key `Tables.RatePerMinute`.
    - `PUT /settings/rate` body is a number; upserts the same key.

- __Deployments__
  - Deployed `SettingsApi` to Cloud Run: `https://magidesk-settings-904541739138.northamerica-south1.run.app`.
  - Deployed `TablesApi` to Cloud Run: `https://magidesk-tables-904541739138.northamerica-south1.run.app`.
  - Cloud SQL instance attached for `TablesApi`; connection via `/cloudsql` socket.

- __Docs__
  - Updated `docs/api-routes.md` to include new `SettingsApi` defaults/audit and all `TablesApi` routes.

