# SettingsApi

The SettingsApi provides hierarchical, multi-tenant settings management for application, frontend, and backend configurations.

## Overview

**Location:** `solution/backend/SettingsApi/`  
**Port:** 5007 (local)  
**Database Schema:** `settings`  
**Framework:** ASP.NET Core 8

## Key Features

- Hierarchical settings (host-specific defaults)
- Multi-tenant support
- Frontend, backend, and app settings
- Settings versioning
- Settings audit trail

## Configuration

**appsettings.json:**
```json
{
  "Postgres": {
    "LocalConnectionString": "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=postgres",
    "CloudRunSocketConnectionString": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Port=5432;Username=posapp;Password=Campus_66;Database=postgres;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

## Database Schema

### settings.app_settings
```sql
CREATE TABLE settings.app_settings (
    setting_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    host TEXT,
    category TEXT NOT NULL,
    key TEXT NOT NULL,
    value JSONB NOT NULL,
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(host, category, key, version)
);
```

### settings.setting_categories
```sql
CREATE TABLE settings.setting_categories (
    category_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL UNIQUE,
    parent_id UUID REFERENCES settings.setting_categories(category_id),
    path TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Endpoints

### Frontend Settings

**GET** `/api/settings/frontend?host={host}`
- **Description:** Get frontend settings for host
- **Query Parameters:**
  - `host` (string, optional) - Host identifier
- **Response:** `200 OK` with `FrontendSettings` | `404 Not Found`

**PUT** `/api/settings/frontend?host={host}`
- **Description:** Update frontend settings
- **Query Parameters:**
  - `host` (string, optional)
- **Request Body:** `FrontendSettings`
- **Response:** `204 No Content` | `400 Bad Request`

### Backend Settings

**GET** `/api/settings/backend?host={host}`
- **Description:** Get backend settings for host
- **Response:** `200 OK` with `BackendSettings` | `404 Not Found`

**PUT** `/api/settings/backend?host={host}`
- **Description:** Update backend settings
- **Request Body:** `BackendSettings`
- **Response:** `204 No Content` | `400 Bad Request`

### App Settings

**GET** `/api/settings/app?host={host}`
- **Description:** Get app settings for host
- **Response:** `200 OK` with `AppSettings` | `404 Not Found`

**PUT** `/api/settings/app?host={host}`
- **Description:** Update app settings
- **Request Body:** `AppSettings`
- **Response:** `204 No Content` | `400 Bad Request`

### Defaults

**GET** `/api/settings/frontend/defaults`
- **Description:** Get default frontend settings
- **Response:** `200 OK` with `FrontendSettings`

**GET** `/api/settings/backend/defaults`
- **Description:** Get default backend settings
- **Response:** `200 OK` with `BackendSettings`

**GET** `/api/settings/app/defaults`
- **Description:** Get default app settings
- **Response:** `200 OK` with `AppSettings`

### Audit

**GET** `/api/settings/audit?host={host}&limit={n}`
- **Description:** Get settings change history
- **Query Parameters:**
  - `host` (string, optional)
  - `limit` (int, optional, default: 100)
- **Response:** `200 OK` with audit log entries

## Hierarchical Settings

Settings support host-specific overrides:
- **Default settings:** Applied when no host-specific setting exists
- **Host-specific:** Override defaults for specific hosts
- **Inheritance:** Host settings inherit from defaults

## Settings Models

### FrontendSettings
```json
{
  "theme": "light|dark",
  "language": "en|es|fr",
  "dateFormat": "MM/dd/yyyy",
  "timeFormat": "12h|24h",
  "currency": "USD",
  "receiptSettings": {
    "printerName": "string",
    "paperWidth": 80,
    "showLogo": true
  }
}
```

### BackendSettings
```json
{
  "apiBaseUrls": {
    "users": "https://...",
    "menu": "https://...",
    "order": "https://..."
  },
  "database": {
    "connectionString": "string",
    "poolSize": 20
  }
}
```

### AppSettings
```json
{
  "businessName": "string",
  "address": "string",
  "phone": "string",
  "taxRate": 0.08,
  "timeRatePerMinute": 2.00
}
```

## Deployment

**Cloud Run Deployment:**
```powershell
.\solution\backend\deploy-settings-cloudrun.ps1 `
    -ProjectId "bola8pos" `
    -Region "northamerica-south1" `
    -ServiceName "magidesk-settings" `
    -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
```

**Service URL:** `https://magidesk-settings-904541739138.northamerica-south1.run.app`

## Related Documentation

- [Configuration Guide](../configuration/appsettings.md)
- [Frontend Settings](../frontend/settings.md)
