# 04 - Failure & Edge Case Matrix

## Purpose

Document all failure scenarios and edge cases for the OPERATIONAL endpoints, with expected behavior and recovery strategies.

---

## EndSession Failure Matrix

| Scenario | HTTP Code | Response | Recovery |
|----------|-----------|----------|----------|
| Session not found | 404 | `{ "error": "Session not found" }` | User should refresh table list |
| Session already ended | 200 | Return existing EndSessionResult | Idempotent - no action needed |
| Session already settled | 409 | `{ "error": "Session already settled" }` | Cannot re-end - display error |
| Invalid session ID format | 400 | `{ "error": "Invalid session ID" }` | Client validation issue |
| Database connection failed | 500 | `{ "error": "Database error" }` | Retry after delay |
| Order items fetch failed | 500 | `{ "error": "Failed to fetch items" }` | Retry - data integrity issue |
| Bill creation failed | 500 | `{ "error": "Failed to create bill" }` | Full rollback, retry |
| Concurrent modification | 409 | `{ "error": "Session modified" }` | Refresh and retry |

### Edge Case Details

#### Double-Click End Session

**Trigger**: User clicks "End Session" button twice in rapid succession.

**Expected Behavior**:
1. First request acquires row lock on session
2. Second request waits for lock
3. First request completes, commits
4. Second request finds session.status = "ended"
5. Second request returns idempotent 200 with existing result

**No duplicate bills are created.**

#### Network Retry After Timeout

**Trigger**: Client times out, retries request.

**Expected Behavior**:
1. Original request may have completed
2. Retry finds session.status = "ended"
3. Returns idempotent 200

**Safe for retry.**

#### Session Ended But Table Still Shows Occupied

**Trigger**: Race condition between session end and table status update.

**Expected Behavior**:
1. Transaction ensures atomic update
2. If table update fails, full rollback
3. If transaction commits, both are consistent

**Guaranteed consistency.**

---

## PrintPreSettlementReceipt Failure Matrix

| Scenario | HTTP Code | Response | Recovery |
|----------|-----------|----------|----------|
| No active session | 404 | `{ "error": "No active session for table" }` | Refresh table status |
| Invalid table label | 400 | `{ "error": "Invalid table label" }` | Client validation issue |
| Items fetch failed | 500 | `{ "error": "Failed to fetch items" }` | Retry |
| Calculation error | 500 | `{ "error": "Calculation failed" }` | Retry |
| Printer offline | 200* | `{ "success": false, "message": "Printer offline" }` | Manual print later |
| Printer timeout | 200* | `{ "success": false, "message": "Printer timeout" }` | Retry |

*Note: Printer failures return 200 with success=false to distinguish from server errors.

### Edge Case Details

#### Printed After Session Ended

**Trigger**: User prints pre-settlement receipt, then another user ends session.

**Expected Behavior**:
- Print returns 404 if session already ended
- User should refresh and go to Payment Hub

**Clear error message.**

#### Multiple Print Requests

**Trigger**: User clicks print multiple times.

**Expected Behavior**:
- Each request prints a new receipt
- This is intentional (customer may want copies)
- No rate limiting (operational decision)

**Intentionally allowed.**

---

## Concurrency Matrix

| Action 1 | Action 2 | Outcome |
|----------|----------|---------|
| EndSession | EndSession | Second returns idempotent result |
| EndSession | PrintReceipt | Print returns 404 if end completes first |
| EndSession | StopSession (Payment) | StopSession returns 409 (already ended) |
| PrintReceipt | PrintReceipt | Both succeed (multiple receipts) |
| PrintReceipt | EndSession | End proceeds, print may fail with 404 |

---

## Recovery Strategies

### Client-Side Retry Logic

```typescript
async function endSession(sessionId: string): Promise<EndSessionResult> {
    const maxRetries = 3;
    
    for (let i = 0; i < maxRetries; i++) {
        try {
            const result = await api.post(`/tables/${sessionId}/end`);
            return result.data;
        } catch (error) {
            if (error.status === 404) throw error; // Don't retry
            if (error.status === 409) throw error; // Don't retry
            if (i === maxRetries - 1) throw error;
            await delay(1000 * (i + 1)); // Exponential backoff
        }
    }
}
```

### Server-Side Idempotency

```csharp
// Always check current state before proceeding
if (session.Status == "ended")
    return existingResult; // Safe idempotent return

if (session.Status == "settled")
    throw new ConflictException();
```

---

## Monitoring & Alerting

| Metric | Threshold | Alert |
|--------|-----------|-------|
| EndSession failure rate | > 5% | Page on-call |
| EndSession latency p99 | > 2s | Non-urgent investigation |
| PrintReceipt printer failures | > 50% | Check printer connection |
| Concurrent modification conflicts | > 1/hour | Check for UI bugs |
