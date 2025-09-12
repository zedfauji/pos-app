# Complete Payment Flow Analysis: Database Schemas, Endpoints, and Design Flaws

## Overview

This document provides a comprehensive analysis of the MagiDesk Billiard POS payment flow, including complete database schemas, all API endpoints, ID generation patterns, and the critical design flaw causing payment failures.

## 1. Complete Database Schemas

### 1.1 TablesApi Database Schema

#### Table: `public.table_status`
```sql
CREATE TABLE IF NOT EXISTS public.table_status (
    label text PRIMARY KEY,
    type text NOT NULL,
    occupied boolean NOT NULL,
    order_id text NULL,
    start_time timestamptz NULL,
    server text NULL,
    updated_at timestamptz NOT NULL DEFAULT now()
);
```

#### Table: `public.table_sessions`
```sql
CREATE TABLE IF NOT EXISTS public.table_sessions (
    session_id uuid PRIMARY KEY,
    table_label text NOT NULL,
    server_id text NOT NULL,
    server_name text NOT NULL,
    start_time timestamp NOT NULL,
    end_time timestamp NULL,
    status text NOT NULL CHECK (status IN ('active','closed')) DEFAULT 'active',
    items jsonb NOT NULL DEFAULT '[]',
    billing_id uuid NULL
);
```

#### Table: `public.bills`
```sql
CREATE TABLE IF NOT EXISTS public.bills (
    bill_id uuid PRIMARY KEY,
    billing_id text NULL UNIQUE,
    session_id text NULL,
    table_label text NOT NULL,
    server_id text NOT NULL,
    server_name text NOT NULL,
    start_time timestamp NOT NULL,
    end_time timestamp NOT NULL,
    total_time_minutes int NOT NULL,
    items jsonb NOT NULL DEFAULT '[]',
    time_cost numeric NOT NULL DEFAULT 0,
    items_cost numeric NOT NULL DEFAULT 0,
    total_amount numeric NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'awaiting_payment' CHECK (status IN ('awaiting_payment','closed')),
    is_settled boolean NOT NULL DEFAULT false,
    payment_state text NOT NULL DEFAULT 'not-paid' CHECK (payment_state IN ('not-paid','partial-paid','paid','partial-refunded','refunded','cancelled')),
    closed_at timestamp NULL,
    created_at timestamp NOT NULL DEFAULT now()
);
```

#### Table: `public.app_settings`
```sql
CREATE TABLE IF NOT EXISTS public.app_settings(
    key text PRIMARY KEY,
    value text NOT NULL
);
```

#### Table: `public.table_session_moves`
```sql
CREATE TABLE IF NOT EXISTS public.table_session_moves(
    session_id uuid NOT NULL,
    from_label text NOT NULL,
    to_label text NOT NULL,
    moved_at timestamp NOT NULL DEFAULT now()
);
```

### 1.2 PaymentApi Database Schema

#### Table: `pay.payments`
```sql
CREATE TABLE IF NOT EXISTS pay.payments (
    payment_id       bigserial primary key,
    session_id       text not null,
    billing_id       text not null,
    amount_paid      numeric(12,2) not null check (amount_paid >= 0),
    currency         text not null default 'USD',
    payment_method   text not null,
    discount_amount  numeric(12,2) not null default 0.00 check (discount_amount >= 0),
    discount_reason  text null,
    tip_amount       numeric(12,2) not null default 0.00 check (tip_amount >= 0),
    external_ref     text null,
    meta             jsonb null,
    created_by       text null,
    created_at       timestamptz not null default now(),
    CONSTRAINT payments_immutable_billing_id CHECK (billing_id IS NOT NULL AND billing_id != '')
);
```

#### Table: `pay.bill_ledger`
```sql
-- This table is created by PaymentApi for tracking payment totals
-- Schema not explicitly shown in code, but referenced in queries
```

## 2. Complete API Endpoints Analysis

### 2.1 TablesApi Endpoints

#### Session Management
- **`POST /tables/{label}/start`** - Start a new table session
- **`POST /tables/{label}/stop`** - Stop session and create bill
- **`POST /tables/{label}/move/{to}`** - Move session to different table
- **`GET /sessions/active`** - Get all active sessions
- **`GET /debug/sessions`** - Debug endpoint for session inspection

#### Bill Management
- **`GET /bills`** - Get all bills (last 500)
- **`GET /bills/{billId}`** - Get specific bill by ID
- **`GET /bills/by-billing/{billingId}`** - Get bill by billing ID
- **`POST /bills/{billId}/settle`** - Mark bill as settled by bill ID
- **`POST /bills/by-billing/{billingId}/settle`** - Mark bill as settled by billing ID

#### Table Management
- **`GET /tables`** - Get all table statuses
- **`GET /tables/{label}`** - Get specific table status
- **`PUT /tables/{label}`** - Update table status
- **`POST /tables/upsert`** - Upsert single table
- **`POST /tables/bulkUpsert`** - Bulk upsert tables
- **`POST /tables/reset`** - Reset all tables
- **`GET /tables/seed`** - Seed default tables
- **`GET /tables/counts`** - Get table counts by type

#### Settings & Maintenance
- **`GET /settings/rate`** - Get rate per minute
- **`PUT /settings/rate`** - Set rate per minute
- **`POST /sessions/enforce`** - Auto-stop sessions based on settings
- **`GET /health`** - Health check
- **`GET /diagnostics`** - Database diagnostics

#### Session Items
- **`POST /sessions/{sessionId}/items`** - Update session items
- **`POST /tables/{label}/items`** - Update items for active session on table

### 2.2 PaymentApi Endpoints

#### Payment Processing
- **`POST /api/payments`** - Register a payment
- **`GET /api/payments/{billingId}`** - List payments for billing ID
- **`GET /api/payments/{billingId}/ledger`** - Get payment ledger
- **`POST /api/payments/{billingId}/discounts`** - Apply discount

## 3. Complete Flow Analysis: Session Start to Payment

### 3.1 Session Start Flow (`POST /tables/{label}/start`)

#### Step 1: Validation
```sql
-- Check if table already has active session
SELECT COUNT(1) FROM public.table_sessions 
WHERE table_label = @label AND status = 'active'
```

#### Step 2: ID Generation
```csharp
var sessionId = Guid.NewGuid();        // UUID for session
var billingId = Guid.NewGuid();       // UUID for billing
var now = DateTime.UtcNow;            // Current timestamp
```

#### Step 3: Create Session Record
```sql
INSERT INTO public.table_sessions(
    session_id, table_label, server_id, server_name, 
    start_time, status, billing_id
) VALUES(@sid, @label, @srvId, @srvName, @start, 'active', @bid)
```

#### Step 4: Update Table Status
```sql
INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server)
VALUES(@l, 'billiard', true, NULL, @st, @srv)
ON CONFLICT (label) DO UPDATE SET 
    occupied = EXCLUDED.occupied, 
    start_time = EXCLUDED.start_time, 
    server = EXCLUDED.server, 
    updated_at = now()
```

#### Step 5: Response
```json
{
    "session_id": "uuid",
    "billing_id": "uuid", 
    "start_time": "timestamp"
}
```

### 3.2 Session Stop Flow (`POST /tables/{label}/stop`)

#### Step 1: Find Active Session
```sql
SELECT session_id, server_id, server_name, start_time, billing_id 
FROM public.table_sessions
WHERE table_label = @label AND status = 'active' 
ORDER BY start_time DESC LIMIT 1
```

#### Step 2: Calculate Costs
```csharp
var end = DateTime.UtcNow;
var minutes = (int)Math.Max(0, (end - startTime).TotalMinutes);
decimal ratePerMinute = await GetRatePerMinuteAsync(conn, defaultRatePerMinute);
decimal itemsCost = items.Sum(i => i.price * i.quantity);
decimal timeCost = ratePerMinute * minutes;
decimal total = timeCost + itemsCost;
```

#### Step 3: Close Session
```sql
UPDATE public.table_sessions 
SET end_time = @end, status = 'closed' 
WHERE session_id = @sid
```

#### Step 4: Create Bill Record
```sql
INSERT INTO public.bills(
    bill_id, billing_id, session_id, table_label, server_id, server_name,
    start_time, end_time, total_time_minutes, items, time_cost, items_cost, 
    total_amount, status, is_settled, payment_state
) VALUES(
    @bid, @billingId, @sid, @label, @srvId, @srvName,
    @start, @end, @mins, @items, @timeCost, @itemsCost, 
    @total, 'awaiting_payment', false, 'not-paid'
)
```

**Critical Issue**: The `billing_id` is converted from UUID to TEXT:
```csharp
bill.Parameters.AddWithValue("@billingId", (object?)(billingGuid?.ToString()) ?? DBNull.Value);
```

#### Step 5: Free Table
```sql
UPDATE public.table_status 
SET occupied = false, start_time = NULL, server = NULL, updated_at = now() 
WHERE label = @l
```

### 3.3 Payment Processing Flow (`POST /api/payments`)

#### Step 1: Format Validation
```csharp
if (!_idService.IsValidBillingId(req.BillingId))
{
    throw new InvalidOperationException("INVALID_BILLING_ID_FORMAT");
}
```

#### Step 2: TablesApi Validation (THE PROBLEM)
```csharp
var sessionsUrl = $"sessions/active";
using var res = await http.GetAsync(sessionsUrl, ct);

// Check if billing ID exists in any active session
bool billingIdExists = false;
foreach (var session in sessions)
{
    if (sessionObj.TryGetPropertyValue("billingId", out var billingIdNode) &&
        billingIdNode?.ToString() == req.BillingId)
    {
        billingIdExists = true;
        break;
    }
}

if (!billingIdExists)
{
    throw new InvalidOperationException($"BILL_NOT_FOUND: Billing ID {req.BillingId} does not exist in active sessions");
}
```

#### Step 3: Payment Record Creation
```sql
INSERT INTO pay.payments(
    session_id, billing_id, amount_paid, payment_method, 
    discount_amount, tip_amount, external_ref, meta, created_by
) VALUES(@sessionId, @billingId, @amountPaid, @paymentMethod, 
         @discountAmount, @tipAmount, @externalRef, @meta, @createdBy)
```

## 4. The Critical Design Flaw

### 4.1 Root Cause Analysis

The payment failure is caused by a **fundamental architectural mismatch**:

1. **Session Start**: Creates `billing_id` as UUID in `table_sessions`
2. **Session Stop**: Converts `billing_id` from UUID to TEXT in `bills` table
3. **Payment Validation**: Only checks `sessions/active` endpoint (active sessions)
4. **Completed Bills**: Exist in `bills` table, NOT in active sessions
5. **Result**: Payment API rejects all completed bills

### 4.2 Data Type Inconsistency

| Table | Column | Type | Notes |
|-------|--------|------|-------|
| `table_sessions` | `billing_id` | `uuid` | Generated during session start |
| `bills` | `billing_id` | `text` | Converted from UUID during session stop |
| `pay.payments` | `billing_id` | `text` | Expects TEXT format |

### 4.3 Validation Logic Flaw

The Payment API validation logic:
```csharp
// ❌ WRONG: Only checks active sessions
var sessionsUrl = $"sessions/active";

// ✅ CORRECT: Should also check completed bills
var billsUrl = $"bills/by-billing/{req.BillingId}";
```

### 4.4 ID Generation Timeline

```
Session Start:
├── sessionId = Guid.NewGuid() → UUID
├── billingId = Guid.NewGuid() → UUID
└── Stored in table_sessions.billing_id (UUID)

Session Stop:
├── Retrieve billingId from table_sessions (UUID)
├── Convert to string: billingGuid.ToString()
└── Store in bills.billing_id (TEXT)

Payment Processing:
├── Frontend sends billingId as TEXT
├── Payment API checks sessions/active (UUID format)
└── ❌ MISMATCH: TEXT vs UUID comparison fails
```

## 5. Complete Endpoint Trace Analysis

### 5.1 Frontend Payment Flow

1. **Frontend**: Gets bill from `GET /bills` (returns `BillResult`)
2. **Frontend**: Extracts `BillId` (UUID) and `BillingId` (TEXT)
3. **Frontend**: Calls `POST /api/payments` with `BillingId` (TEXT)
4. **Payment API**: Validates against `GET /sessions/active` (UUID format)
5. **Payment API**: ❌ Rejects payment - billing ID not found in active sessions

### 5.2 Debug Output Analysis

```
EphemeralPaymentPage: Final - BillingId=ca81c994-9221-444f-8ec6-a347d3bcd3db
PaymentApiService: Request JSON = {
  "BillingId": "ca81c994-9221-444f-8ec6-a347d3bcd3db"  ← TEXT format
}
PaymentService: Checking if billing ID exists in active sessions
PaymentService: Billing ID ca81c994-9221-444f-8ec6-a347d3bcd3db not found in active sessions
```

The billing ID `ca81c994-9221-444f-8ec6-a347d3bcd3db` is a TEXT format from the `bills` table, but the Payment API is checking against active sessions which have UUID format.

## 6. Database Relationship Issues

### 6.1 Missing Foreign Key Relationships

```sql
-- ❌ MISSING: No foreign key between bills and table_sessions
-- ❌ MISSING: No foreign key between pay.payments and bills
-- ❌ MISSING: No foreign key between pay.payments and table_sessions
```

### 6.2 Data Consistency Issues

1. **No Referential Integrity**: `bills.session_id` is TEXT, `table_sessions.session_id` is UUID
2. **No Cascade Updates**: Changes to sessions don't update bills
3. **No Validation**: No constraints ensuring billing_id consistency

## 7. Recommended Solutions

### 7.1 Immediate Fix: Modify Payment API Validation

```csharp
// Add this to PaymentService.cs after checking active sessions
if (!billingIdExists)
{
    // Check completed bills as fallback
    var billsUrl = $"bills/by-billing/{req.BillingId}";
    using var billsRes = await http.GetAsync(billsUrl, ct);
    if (billsRes.IsSuccessStatusCode)
    {
        billingIdExists = true;
    }
}
```

### 7.2 Alternative Fix: Add TablesApi Endpoint

```csharp
// New endpoint in TablesApi
app.MapGet("/bills/validate/{billingId}", async (string billingId) =>
{
    const string sql = "SELECT COUNT(1) FROM public.bills WHERE billing_id = @bid";
    // Return validation result
});
```

### 7.3 Long-term Solution: Unify ID System

1. **Standardize Data Types**: Use consistent UUID or TEXT across all tables
2. **Add Foreign Keys**: Establish proper relationships
3. **Add Constraints**: Ensure data consistency
4. **Update Validation Logic**: Check both active sessions and completed bills

## 8. Complete API Endpoint Summary

### 8.1 TablesApi Endpoints (Complete List)

| Method | Endpoint | Purpose | Database Tables |
|--------|----------|---------|----------------|
| POST | `/tables/{label}/start` | Start session | `table_sessions`, `table_status` |
| POST | `/tables/{label}/stop` | Stop session, create bill | `table_sessions`, `bills`, `table_status` |
| POST | `/tables/{label}/move/{to}` | Move session | `table_sessions`, `table_session_moves`, `table_status` |
| GET | `/sessions/active` | Get active sessions | `table_sessions` |
| GET | `/debug/sessions` | Debug sessions | `table_sessions` |
| GET | `/bills` | Get all bills | `bills` |
| GET | `/bills/{billId}` | Get bill by ID | `bills` |
| GET | `/bills/by-billing/{billingId}` | Get bill by billing ID | `bills` |
| POST | `/bills/{billId}/settle` | Settle bill by ID | `bills` |
| POST | `/bills/by-billing/{billingId}/settle` | Settle bill by billing ID | `bills` |
| GET | `/tables` | Get table statuses | `table_status` |
| GET | `/tables/{label}` | Get table status | `table_status` |
| PUT | `/tables/{label}` | Update table | `table_status` |
| POST | `/tables/upsert` | Upsert table | `table_status` |
| POST | `/tables/bulkUpsert` | Bulk upsert tables | `table_status` |
| POST | `/tables/reset` | Reset tables | `table_status` |
| GET | `/tables/seed` | Seed tables | `table_status` |
| GET | `/tables/counts` | Get table counts | `table_status` |
| GET | `/settings/rate` | Get rate | `app_settings` |
| PUT | `/settings/rate` | Set rate | `app_settings` |
| POST | `/sessions/enforce` | Auto-stop sessions | `table_sessions`, `bills` |
| GET | `/health` | Health check | - |
| GET | `/diagnostics` | Diagnostics | - |
| POST | `/sessions/{sessionId}/items` | Update session items | `table_sessions` |
| POST | `/tables/{label}/items` | Update table items | `table_sessions` |

### 8.2 PaymentApi Endpoints (Complete List)

| Method | Endpoint | Purpose | Database Tables |
|--------|----------|---------|----------------|
| POST | `/api/payments` | Register payment | `pay.payments`, `pay.bill_ledger` |
| GET | `/api/payments/{billingId}` | List payments | `pay.payments` |
| GET | `/api/payments/{billingId}/ledger` | Get ledger | `pay.bill_ledger` |
| POST | `/api/payments/{billingId}/discounts` | Apply discount | `pay.payments`, `pay.bill_ledger` |

## 9. Conclusion

The payment failure is caused by a **critical design flaw** in the Payment API validation logic:

1. **Payment API only validates against active sessions** (`sessions/active`)
2. **Completed bills exist in a separate table** (`bills`) with different data types
3. **No validation against completed bills** during payment processing
4. **Data type inconsistency** between UUID (sessions) and TEXT (bills)

The solution requires either:
- **Modifying Payment API** to check completed bills when not found in active sessions
- **Adding TablesApi endpoint** for billing ID validation against completed bills
- **Unifying the ID system** across all tables for consistency

This analysis provides the complete picture of the payment flow and identifies the exact point of failure in the system architecture.
