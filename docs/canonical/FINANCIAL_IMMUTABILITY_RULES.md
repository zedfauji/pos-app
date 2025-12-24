# Financial Immutability Rules

**Status**: ACTIVE
**Enforced By**: Database Triggers + Audit Service

## 1. Database-Level Invariants

The following tables are **strictly APPEND-ONLY**.
Any attempt to `UPDATE` or `DELETE` rows will result in a database exception.

| Schema | Table | Description |
|:-------|:------|:------------|
| `ord` | `orders` | Core order records. |
| `ord` | `order_items` | Line items. |
| `pay` | `payments` | Monetary transactions. |
| `pay` | `payment_ledger` | Running balance ledger. |
| `billing` | `bills` | Issued bills. |
| `billing` | `bill_items` | Snapshot of billed items. |
| `public` | `shifts` | Shift records (legal boundaries). |
| `audit` | `events` | The audit log itself. |

**Compensating Actions Only**:
- To "undo" an order → Create a VOID transaction.
- To "fix" a payment → Issue a REFUND transaction.
- **NEVER** modify the original record.

## 2. Audit Requirements

All financial actions must generate an `audit.events` record.

### Mandatory Context
- **ActorId**: Who performed the action (User ID or 'system').
- **CorrelationId**: Session ID or Billing ID to trace flow.
- **Before/After State**: JSON snapshots of the entity.

### Events to Log
- `ORDER_CREATED`
- `PAYMENT_RECEIVED`
- `SHIFT_OPENED` / `SHIFT_CLOSED`
- `SESSION_STARTED` / `SESSION_STOPPED`
- `DISCOUNT_APPLIED`

## 3. Precision Rules
- Database: `NUMERIC(18,4)`
- Backend: `decimal` (C#)
- JSON: Strings for values > 15 digits (standard practice, though typically safe for currency).

## 4. Operational Invariants
- A Payment without a Billing ID is INVALID.
- A Bill without a Session ID is INVALID.
- An Order cannot be created without an Open Shift (enforced by `[RequireOpenShift]`).

---
**VIOLATION OF THESE RULES IS A SECURITY INCIDENT.**
