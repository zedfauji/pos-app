# TablesApi

The TablesApi manages table sessions, table status, and bill generation for billiard and bar tables.

## Overview

**Location:** `solution/backend/TablesApi/`  
**Port:** 5010 (local)  
**Database Schema:** `public`  
**Framework:** ASP.NET Core 8

## Key Features

- Table status management
- Session lifecycle (start/stop)
- Bill generation
- Table transfers
- Session enforcement
- Rate per minute configuration

## Database Schema

### public.table_status
```sql
CREATE TABLE public.table_status (
    label TEXT PRIMARY KEY,
    type TEXT NOT NULL CHECK (type IN ('billiard', 'bar')),
    occupied BOOLEAN NOT NULL DEFAULT false,
    order_id TEXT,
    start_time TIMESTAMPTZ,
    server TEXT,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### public.table_sessions
```sql
CREATE TABLE public.table_sessions (
    session_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    table_label TEXT NOT NULL,
    server_id TEXT NOT NULL,
    server_name TEXT NOT NULL,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    status TEXT NOT NULL CHECK (status IN ('active', 'closed')) DEFAULT 'active',
    items JSONB NOT NULL DEFAULT '[]',
    billing_id UUID,
    last_heartbeat TIMESTAMPTZ
);
```

### public.bills
```sql
CREATE TABLE public.bills (
    bill_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    billing_id UUID UNIQUE,
    session_id UUID NOT NULL REFERENCES public.table_sessions(session_id),
    table_label TEXT NOT NULL,
    server_id TEXT NOT NULL,
    server_name TEXT NOT NULL,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    total_time_minutes INTEGER NOT NULL,
    items JSONB NOT NULL,
    time_cost NUMERIC(18,2) NOT NULL,
    items_cost NUMERIC(18,2) NOT NULL,
    total_amount NUMERIC(18,2) NOT NULL,
    status TEXT NOT NULL CHECK (status IN ('awaiting_payment', 'closed')) DEFAULT 'awaiting_payment',
    is_settled BOOLEAN NOT NULL DEFAULT false,
    payment_state TEXT CHECK (payment_state IN ('not-paid', 'partial', 'paid')) DEFAULT 'not-paid',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    closed_at TIMESTAMPTZ
);
```

## Endpoints

### Tables

**GET** `/tables`
- **Description:** List all tables
- **Response:** `200 OK` with `TableDto[]`

**GET** `/tables/&#123;label&#125;`
- **Description:** Get table by label
- **Response:** `200 OK` with `TableDto` | `404 Not Found`

**GET** `/tables/counts`
- **Description:** Get table counts (available/occupied)
- **Response:** `200 OK` with counts

**POST** `/tables/&#123;label&#125;/start`
- **Description:** Start table session
- **Request Body:**
```json
{
  "serverId": "user-id",
  "serverName": "Server Name"
}
```
- **Response:** `200 OK` with `{ sessionId, billingId }` | `409 Conflict`

**POST** `/tables/&#123;label&#125;/stop`
- **Description:** Stop table session and generate bill
- **Response:** `200 OK` with `BillResult` | `404 Not Found`

**POST** `/tables/&#123;label&#125;/items`
- **Description:** Update session items
- **Request Body:** `Item[]` (JSONB)
- **Response:** `200 OK`

**GET** `/tables/&#123;label&#125;/items`
- **Description:** Get session items
- **Response:** `200 OK` with items array

**POST** `/tables/&#123;label&#125;/force-free`
- **Description:** Force free table (recovery)
- **Response:** `200 OK`

**POST** `/tables/&#123;fromLabel&#125;/move?to=&#123;toLabel&#125;`
- **Description:** Move session between tables
- **Response:** `200 OK` | `400 Bad Request`

### Sessions

**GET** `/sessions/active`
- **Description:** Get all active sessions
- **Response:** `200 OK` with `SessionDto[]`

**POST** `/sessions/enforce`
- **Description:** Enforce session state (uses SettingsApi for defaults)
- **Response:** `200 OK`

### Settings

**GET** `/settings/rate`
- **Description:** Get rate per minute
- **Response:** `200 OK` with `{ ratePerMinute: decimal }`

**PUT** `/settings/rate`
- **Description:** Update rate per minute
- **Request Body:** Rate amount (decimal)
- **Response:** `200 OK`

## Session Lifecycle

### Start Session

1. Check for existing active session
2. Create new session with `billing_id`
3. Update `table_status` to occupied
4. Return `sessionId` and `billingId`

### Stop Session

1. Find active session
2. Calculate duration and costs:
   - Time cost = `ratePerMinute * minutes`
   - Items cost = sum of session items
   - Total = time cost + items cost
3. Create bill record
4. Close session
5. Free table
6. Return bill details

## Bill Generation

Bills include:
- **Time Cost:** Based on session duration and rate per minute
- **Items Cost:** Sum of all items in session
- **Total:** Time + Items
- **Status:** `awaiting_payment` initially

## Integration

### SettingsApi Integration

TablesApi reads default rate from SettingsApi:
- Environment variable: `SETTINGSAPI_BASEURL`
- Falls back to default rate if SettingsApi unavailable

### OrderApi Integration

- Sessions link to orders via `session_id`
- Orders link to bills via `billing_id`

### PaymentApi Integration

- Bills link to payments via `billing_id`
- PaymentApi processes bills generated by TablesApi

## Deployment

**Cloud Run Deployment:**
```powershell
.\solution\backend\deploy-tables-cloudrun.ps1 `
    -ProjectId "bola8pos" `
    -Region "northamerica-south1" `
    -ServiceName "magidesk-tables" `
    -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
```

**Service URL:** `https://magidesk-tables-904541739138.northamerica-south1.run.app`

## Related Documentation

- [Table Management Flow](../features/table-management.md)
- [Payment Flow](../features/payments.md)
- [Order Processing](../backend/order-api.md)
