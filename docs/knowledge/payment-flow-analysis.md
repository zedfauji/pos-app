# Payment Flow Analysis: Session ID, Billing ID, and Payment Processing

## Overview

This document analyzes the complete payment flow in the MagiDesk Billiard POS system, including how Session IDs and Billing IDs are generated, how payments are processed, and where potential issues exist in the current implementation.

## 1. Session ID Generation

### When Session IDs are Generated

Session IDs are generated when a table session is started via the TablesApi endpoint:

**Endpoint**: `POST /tables/{label}/start`

**Location**: `solution/backend/TablesApi/Program.cs` (lines 505-540)

```csharp
var sessionId = Guid.NewGuid();
var billingId = Guid.NewGuid();
var now = DateTime.UtcNow;

const string insertSql = @"INSERT INTO public.table_sessions(session_id, table_label, server_id, server_name, start_time, status, billing_id)
                           VALUES(@sid, @label, @srvId, @srvName, @start, 'active', @bid)";
```

### Database Schema for Sessions

**Table**: `public.table_sessions`

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

### Key Points:
- **Session ID**: Generated as `Guid.NewGuid()` when session starts
- **Billing ID**: Generated as `Guid.NewGuid()` when session starts
- **Status**: Initially 'active', changes to 'closed' when session ends
- **Relationship**: Each session has one billing_id (nullable)

## 2. Billing ID Generation

### When Billing IDs are Generated

Billing IDs are generated in **two places**:

#### A. During Session Start (TablesApi)
- Generated alongside Session ID when table session begins
- Stored in `table_sessions.billing_id` column
- Used for active sessions

#### B. During Bill Creation (TablesApi)
- Generated when session is closed and bill is created
- Stored in `bills.billing_id` column
- Used for completed bills awaiting payment

**Location**: `solution/backend/TablesApi/Program.cs` (lines 194-200)

```csharp
var billId = Guid.NewGuid();
const string billSql = @"INSERT INTO public.bills(bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount)
                        VALUES(@bid, @label, @srvId, @srvName, @start, @end, @mins, @items, @timeCost, @itemsCost, @total)";
```

### Database Schema for Bills

**Table**: `public.bills`

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

### Key Points:
- **Bill ID**: Primary key for the bill record
- **Billing ID**: Separate field, nullable, unique constraint
- **Session ID**: Links back to the original session
- **Status**: 'awaiting_payment' → 'closed' when settled

## 3. Payment Processing Flow

### Payment API Validation Process

**Location**: `solution/backend/PaymentApi/Services/PaymentService.cs` (lines 44-105)

The Payment API validates billing IDs through the following process:

#### Step 1: Format Validation
```csharp
if (!_idService.IsValidBillingId(req.BillingId))
{
    throw new InvalidOperationException("INVALID_BILLING_ID_FORMAT: Billing ID must follow immutable format");
}
```

#### Step 2: TablesApi Verification
```csharp
var sessionsUrl = $"sessions/active";
using var res = await http.GetAsync(sessionsUrl, ct);

var sessionsJson = await res.Content.ReadAsStringAsync();
var sessions = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonArray>(sessionsJson);

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

### Payment Database Schema

**Table**: `pay.payments`

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

## 4. The Core Problem Identified

### Issue: Payment API Only Validates Active Sessions

The Payment API validation logic has a **critical flaw**:

1. **It only checks `sessions/active` endpoint** - which returns only currently active sessions
2. **Completed bills are not in active sessions** - they exist in the `bills` table with status 'awaiting_payment'
3. **Payment API rejects completed bills** - because they're not found in active sessions

### Evidence from Debug Output:
```
TableRepository: Found 0 active sessions
PaymentService: Billing ID ca81c994-9221-444f-8ec6-a347d3bcd3db not found in active sessions, rejecting payment
```

### The Mismatch:
- **Frontend**: Trying to pay for completed bills (from `bills` table)
- **Payment API**: Only validates against active sessions (from `table_sessions` table)
- **Result**: Payment always fails for completed bills

## 5. Database Relationship Analysis

### Current State:
```
table_sessions (active sessions)
├── session_id (uuid)
├── billing_id (uuid) 
└── status = 'active'

bills (completed bills awaiting payment)
├── bill_id (uuid)
├── billing_id (text, nullable)
├── session_id (text, nullable)
└── status = 'awaiting_payment'
```

### The Problem:
1. **Active sessions** have `billing_id` as UUID
2. **Completed bills** have `billing_id` as TEXT (nullable)
3. **Payment API** expects billing_id to exist in active sessions
4. **Completed bills** are not in active sessions

## 6. Potential Solutions

### Option 1: Modify Payment API Validation
Change Payment API to check both active sessions AND completed bills:

```csharp
// Check active sessions first
var activeSessionsUrl = $"sessions/active";
// Check completed bills if not found in active sessions
var billsUrl = $"bills?billing_id={req.BillingId}";
```

### Option 2: Use Session ID as Billing ID
For completed bills, use the original session_id as the billing_id:

```csharp
var billingId = completeBill?.SessionId ?? _bill.SessionId ?? _bill.BillId.ToString();
```

### Option 3: Create Unified Billing ID System
Ensure billing_id is consistent between active sessions and completed bills.

## 7. Current Frontend Implementation Issues

### EphemeralPaymentPage Billing ID Resolution:
```csharp
// Current approach (failing):
var billingId = activeBillingId ?? activeSessionId ?? _bill.BillId.ToString();

// The issue: activeBillingId and activeSessionId are null for completed bills
// Because GetActiveSessionForTableAsync only returns active sessions
```

### Debug Output Analysis:
```
EphemeralPaymentPage: GetActiveSessionForTableAsync result - SessionId='', BillingId=''
EphemeralPaymentPage: Final - BillingId=ca81c994-9221-444f-8ec6-a347d3bcd3db
```

The frontend correctly falls back to `BillId.ToString()`, but the Payment API rejects it because it's not in active sessions.

## 8. Recommendations

### Immediate Fix:
1. **Modify Payment API** to check completed bills when billing ID not found in active sessions
2. **Add endpoint** in TablesApi to validate billing IDs against completed bills
3. **Update validation logic** to handle both active and completed billing scenarios

### Long-term Solution:
1. **Unify billing ID system** across active sessions and completed bills
2. **Ensure consistency** between session billing_id and bill billing_id
3. **Add proper foreign key relationships** between tables

## 9. API Endpoints Summary

### TablesApi Endpoints:
- `POST /tables/{label}/start` - Creates session with session_id and billing_id
- `POST /tables/{label}/stop` - Closes session, creates bill
- `GET /sessions/active` - Returns active sessions only
- `GET /bills` - Returns completed bills (missing billing_id validation endpoint)

### PaymentApi Endpoints:
- `POST /api/payments` - Processes payment (validates against active sessions only)
- `GET /api/payments/{billingId}/ledger` - Gets payment ledger

### Missing Endpoint:
- `GET /bills/validate/{billingId}` - Validate billing ID against completed bills

## 10. Conclusion

The payment failure is caused by a **design mismatch** between the Payment API validation logic and the actual data flow:

- **Payment API expects**: Billing IDs to exist in active sessions
- **Reality**: Completed bills (awaiting payment) are not in active sessions
- **Result**: All payments for completed bills fail validation

The solution requires either modifying the Payment API validation logic or ensuring billing IDs are consistently available across both active sessions and completed bills.
