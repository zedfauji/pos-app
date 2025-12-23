# 01 - API & Domain Design

## Purpose

Define the API contracts, DTOs, and domain behavior for **OPERATIONAL** session-ending actions that preserve the strict boundary between **OPERATIONAL** (session lifecycle) and **FINANCIAL** (payment settlement).

---

## ⛔ CRITICAL BOUNDARY

These endpoints are **OPERATIONAL ONLY**:
- ❌ NO payment capture
- ❌ NO settlement
- ❌ NO split logic
- ❌ NO discount/tip logic
- ❌ NO PaymentMethod parameter

✅ Backend owns calculations
✅ Backend owns state transitions
✅ Transactions must be atomic
✅ Endpoints must be idempotent

---

## Endpoint 1: End Session

### Contract

```
POST /tables/{sessionId}/end
```

### Purpose

Ends an active session **without** processing payment:
1. Stops the session timer
2. Calculates final bill (backend only)
3. Marks session as **UNSETTLED**
4. Session appears in Payment Hub for later settlement

### Request

```
No body required.
URL Parameter: sessionId (Guid)
```

### Response (Success - 200)

```json
{
  "sessionId": "guid",
  "billingId": "guid",
  "tableLabel": "Bar 8",
  "totalAmount": 45.50,
  "status": "UNSETTLED",
  "endedAt": "2025-12-22T00:46:00Z"
}
```

### Response DTO

```csharp
public record EndSessionResult(
    Guid SessionId,
    Guid BillingId,
    string TableLabel,
    decimal TotalAmount,
    string Status,      // Always "UNSETTLED"
    DateTime EndedAt
);
```

### State Transitions

| Before | After |
|--------|-------|
| Session.Status = "active" | Session.Status = "ended" |
| Bill does not exist | Bill created with Status = "UNSETTLED" |
| Table.Occupied = true | Table.Occupied = false |

### Idempotency

| Scenario | Behavior |
|----------|----------|
| Session already ended | Return existing EndSessionResult (200) |
| Session already SETTLED | Return 409 Conflict |
| Session not found | Return 404 Not Found |

### Error Codes

| Code | Reason |
|------|--------|
| 404 | Session not found |
| 409 | Session already settled (cannot re-end) |
| 400 | Invalid session ID format |

---

## Endpoint 2: Print Pre-Settlement Receipt

### Contract

```
POST /tables/{label}/print-presettlement
```

### Purpose

Prints an **informational** receipt for customer review:
1. Fetches current session and items
2. Calculates preview totals (backend only)
3. Sends to printer
4. Does **NOT** modify any state

### Request

```
No body required.
URL Parameter: label (string) - Table label
```

### Response (Success - 200)

```json
{
  "success": true,
  "message": "Receipt sent to printer",
  "tableLabel": "Bar 8",
  "previewTotal": 45.50
}
```

### Response DTO

```csharp
public record PrintPreSettlementResult(
    bool Success,
    string Message,
    string TableLabel,
    decimal PreviewTotal
);
```

### State Transitions

**NONE** - This is a read-only operation with print side effect.

### Idempotency

| Scenario | Behavior |
|----------|----------|
| Called multiple times | Each call prints a new receipt (allowed) |
| No active session | Return 404 Not Found |
| Printer offline | Return 500 with error message |

### Error Codes

| Code | Reason |
|------|--------|
| 404 | No active session for table |
| 500 | Printer error |

---

## Domain DTOs

### EndSessionResult (New)

```csharp
namespace MagiDesk.Shared.DTOs.Tables;

/// <summary>
/// Result from ending a session (OPERATIONAL - NOT payment).
/// Session becomes UNSETTLED and appears in Payment Hub.
/// </summary>
public record EndSessionResult(
    Guid SessionId,
    Guid BillingId,
    string TableLabel,
    decimal TotalAmount,
    string Status,
    DateTime EndedAt
);
```

### PrintPreSettlementResult (New)

```csharp
namespace MagiDesk.Shared.DTOs.Tables;

/// <summary>
/// Result from printing a pre-settlement receipt.
/// Informational only - no state changes.
/// </summary>
public record PrintPreSettlementResult(
    bool Success,
    string Message,
    string TableLabel,
    decimal PreviewTotal
);
```

---

## Distinction from StopSessionCommand

| Aspect | EndSession (NEW) | StopSession (EXISTING) |
|--------|------------------|------------------------|
| Type | OPERATIONAL | FINANCIAL |
| Purpose | End timer, create unsettled bill | Process payment, settle bill |
| Payment Params | NONE | PaymentMethod, AmountTendered, Tip, Discount |
| Bill Status | UNSETTLED | SETTLED |
| Table Status | Released | Released |
| Triggered From | Table Workspace | Payment Workspace |

---

## Forbidden Implementations

The following are **BANNED** in these endpoints:

```csharp
// ❌ FORBIDDEN in EndSession
command.PaymentMethod  // No payment methods
command.AmountTendered // No money
command.TipAmount      // No tips
command.DiscountAmount // No discounts

// ❌ FORBIDDEN - Setting bill as settled
bill.Status = "SETTLED";
bill.PaymentMethod = ...;

// ❌ FORBIDDEN - Calculating change
var change = amountTendered - total;
```

---

## Summary

These two endpoints serve **OPERATIONAL** purposes only:

1. **End Session**: Freezes time, calculates costs, creates UNSETTLED bill
2. **Print Pre-Settlement Receipt**: Prints informational receipt, no state change

Financial settlement happens ONLY via:
- Payment Hub → Payment Workspace → `StopSessionCommand`
