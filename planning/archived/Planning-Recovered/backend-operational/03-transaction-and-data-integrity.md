# 03 - Transaction & Data Integrity

## Purpose

Define transaction boundaries, locking strategies, and data integrity rules for the OPERATIONAL endpoints.

---

## Transaction Scope

### EndSession Transaction

```
BEGIN TRANSACTION

1. Read session (FOR UPDATE lock)
2. Validate session.status = "active"
3. Calculate totals (read-only from OrderApi)
4. Create bill record (Status = "UNSETTLED")
5. Update session.status = "ended"
6. Update session.endTime
7. Update table.occupied = false

COMMIT TRANSACTION
```

**Atomicity**: All operations succeed or none do.

### PrintPreSettlementReceipt Transaction

```
No transaction required - read-only operation.
Printer failure does NOT affect database state.
```

---

## Locking Strategy

### EndSession

| Resource | Lock Type | Reason |
|----------|-----------|--------|
| sessions table | Row lock (FOR UPDATE) | Prevent concurrent end attempts |
| bills table | Insert lock | Prevent duplicate bill creation |
| tables table | Row lock | Ensure atomic release |

### PrintPreSettlementReceipt

| Resource | Lock Type | Reason |
|----------|-----------|--------|
| None | Read-only | No state changes |

---

## Idempotency Implementation

### EndSession Idempotency

```csharp
// In handler:
var session = await _tableRepo.GetSessionByIdAsync(sessionId);

// Case 1: Already ended (idempotent return)
if (session.Status == "ended")
{
    var existingBill = await _billingRepo.GetBySessionIdAsync(sessionId);
    return new EndSessionResult(
        session.SessionId,
        existingBill.BillingId,
        session.TableId,
        existingBill.TotalAmount,
        "UNSETTLED",
        existingBill.EndTime
    );
}

// Case 2: Already settled (conflict)
if (session.Status == "settled")
    throw new ConflictException("Session already settled");

// Case 3: Proceed with end
```

### PrintPreSettlementReceipt Idempotency

Each call prints a new receipt. This is **intentionally non-idempotent** for operational reasons (customer may request multiple receipts).

---

## Legacy Schema Interaction

### Sessions Table

```sql
-- Existing columns used:
session_id      UUID PRIMARY KEY
billing_id      UUID
table_id        VARCHAR(50)  -- Table label
start_time      TIMESTAMP
end_time        TIMESTAMP    -- Set by EndSession
status          VARCHAR(20)  -- 'active' | 'ended' | 'settled'
server_name     VARCHAR(100)
```

### Bills Table

```sql
-- Existing columns + new status column:
billing_id      UUID PRIMARY KEY
session_id      UUID
table_label     VARCHAR(50)
total_amount    DECIMAL(10,2)
status          VARCHAR(20)  -- 'UNSETTLED' | 'SETTLED' (new distinction)
end_time        TIMESTAMP
items           JSONB
```

### Tables Table

```sql
-- Existing columns:
label           VARCHAR(50) PRIMARY KEY
type            VARCHAR(20)
occupied        BOOLEAN     -- Must be false after EndSession
current_session_id UUID     -- Set to NULL after EndSession
```

---

## Failure Rollback Rules

### EndSession Failures

| Failure Point | Rollback Behavior |
|---------------|-------------------|
| Session not found | No rollback needed (read failed) |
| Bill creation fails | Full rollback (no session change) |
| Session update fails | Full rollback (bill deleted) |
| Table update fails | Full rollback (all reverted) |

### PrintPreSettlementReceipt Failures

| Failure Point | Rollback Behavior |
|---------------|-------------------|
| Session not found | No rollback needed |
| Calculation fails | No rollback needed |
| Printer fails | No rollback needed (return error) |

---

## Concurrency Scenarios

### Scenario 1: Double-Click End Session

```
User clicks "End Session" twice quickly.

Request 1: Gets lock, proceeds with end
Request 2: Waits for lock, finds session already ended
         → Returns idempotent result (200)
```

### Scenario 2: End Session + Navigate to Payments

```
User ends session, then immediately navigates to Payments.

Request 1: EndSession completes
Request 2 (Payments): Finds UNSETTLED bill → displays for settlement
```

### Scenario 3: Concurrent Print Requests

```
Two print requests come in simultaneously.

Both proceed independently → Two receipts printed
(Acceptable behavior)
```

---

## Data Consistency Guarantees

1. **A session cannot be ended if already settled**
2. **An ended session always has an UNSETTLED bill**
3. **An ended session's table is always released**
4. **Bill totals are calculated from authoritative backend data**
