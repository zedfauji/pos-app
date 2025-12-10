# SettingsApi Overview

- Project: `solution/backend/SettingsApi/`
- Purpose: Dedicated service for application, frontend, and backend settings management per host (multi-tenant friendly).
- Entry point: `Program.cs`
- Controller: `Controllers/SettingsController.cs`

## Endpoints

- GET `/api/settings/frontend?host={host?}` → `FrontendSettings` | 404
- PUT `/api/settings/frontend?host={host?}` (body: `FrontendSettings`) → 204 | 400
- GET `/api/settings/backend?host={host?}` → `BackendSettings` | 404
- PUT `/api/settings/backend?host={host?}` (body: `BackendSettings`) → 204 | 400
- GET `/api/settings/app?host={host?}` → `AppSettings` | 404
- PUT `/api/settings/app?host={host?}` (body: `AppSettings`) → 204 | 400

## Notes

- Implements `ISettingsService` for reading/writing settings.
- Use consistent models across backend and this service to avoid drift.
