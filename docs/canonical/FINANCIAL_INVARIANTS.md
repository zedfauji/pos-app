# Financial Invariants

**Status**: ACTIVE  
**Owner**: Risk & Financial Safety Owner  
**Last Reviewed**: 2025-12-22  

---

## Non-Negotiable Rules

### 1. Backend is Financial Authority

All financial calculations MUST happen in the backend.

| Calculation | Where | Type |
|:------------|:------|:-----|
| Order total | Backend | `decimal` |
| Time cost (billiard) | Backend | `decimal` |
| Split payment amounts | Backend | `decimal` |
| Discount application | Backend | `decimal` |
| Tax calculation | Backend | `decimal` |
| Payment validation | Backend | `decimal` |

### 2. Immutable Billing IDs

Once a `billing_id` is created:
- ❌ Cannot be modified
- ❌ Cannot be reused
- ❌ Cannot be deleted
- ✅ Format: UUID v4

### 3. Payment Ledger is Append-Only

```
┌─────────────────┐
│ Initial Bill    │
├─────────────────┤
│ Payment 1       │ ← APPEND
├─────────────────┤
│ Payment 2       │ ← APPEND
├─────────────────┤
│ Refund 1        │ ← APPEND (negative)
└─────────────────┘
```

❌ Cannot DELETE payment records  
❌ Cannot UPDATE payment records  
✅ Refunds create NEW records

### 4. Shift Gating

No financial operation without open shift:

```
[RequireOpenShift]
├── Start Session  → 423 if no shift
├── Place Order    → 423 if no shift
└── Accept Payment → 423 if no shift
```

### 5. Full Payment Enforcement (MVP)

```csharp
if (PaymentMethod == Cash && AmountTendered < TotalDue)
    throw InvalidOperationException("INSUFFICIENT_PAYMENT");
```

Partial payments are NOT supported in MVP.

---

## Data Persistence Rules

### Order Data Survives Restart

```sql
-- BANNED in dev startup:
DROP SCHEMA ord CASCADE;

-- Orders MUST persist for multi-step flows:
-- Place Order → Restart → Pay
```

### Session Data Survives Restart

Active sessions must be recoverable after app crash or restart.

---

## Precision Requirements

| Layer | Type | Precision |
|:------|:-----|:----------|
| Database | `NUMERIC(18,4)` | 4 decimal places |
| Backend | `decimal` | Full .NET precision |
| API JSON | `number` | String for large amounts |
| UI Binding | `double` | Display only |

> [!WARNING]
> UI `double` values are for DISPLAY ONLY.  
> Backend MUST recalculate before persisting.

---

## Audit Trail Requirements

Every financial transaction must record:

1. **Who** - User ID
2. **When** - UTC timestamp
3. **What** - Transaction type
4. **Amount** - Original amount
5. **Shift** - Shift ID for grouping

---

## Validation Chain

```
Client → API Gateway → Controller → Service → Repository → Database
         ↓              ↓           ↓          ↓           ↓
       Format        Auth       Business    SQL        Constraints
       Check         Check      Rules       Validation  (CHECK)
```

All 5 layers MUST validate.

---

**VIOLATION of these rules is a CRITICAL BUG.**
