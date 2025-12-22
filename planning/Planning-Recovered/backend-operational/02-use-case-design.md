# 02 - Use Case Design

## Purpose

Define the use cases and domain logic for the two OPERATIONAL endpoints, clearly separating them from FINANCIAL settlement logic.

---

## Use Case 1: EndSessionUseCase

### Responsibilities

1. Validate session exists and is active
2. Freeze session end time (UTC)
3. Fetch all order items from OrderApi
4. Calculate totals via BillingService (backend only)
5. Create bill record with status = "UNSETTLED"
6. Update session status to "ended"
7. Release table (Occupied = false)
8. Return EndSessionResult

### Inputs

| Parameter | Type | Source | Required |
|-----------|------|--------|----------|
| SessionId | Guid | URL path | Yes |

### Outputs

```csharp
record EndSessionResult(
    Guid SessionId,
    Guid BillingId,
    string TableLabel,
    decimal TotalAmount,
    string Status,      // "UNSETTLED"
    DateTime EndedAt
);
```

### Side Effects

| Effect | Description |
|--------|-------------|
| Session.Status | Updated from "active" to "ended" |
| Session.EndTime | Set to current UTC time |
| Bill Record | Created with Status = "UNSETTLED" |
| Table.Occupied | Set to false |

### Forbidden Behavior

```csharp
// ❌ FORBIDDEN - Payment processing
bill.PaymentMethod = ...;
bill.AmountTendered = ...;
bill.Status = "SETTLED";

// ❌ FORBIDDEN - Financial calculations
var change = tendered - total;
var tip = ...;
var discount = ...;

// ❌ FORBIDDEN - Payment validation
if (amountTendered < total) throw ...;
```

### Command Definition

```csharp
namespace MagiDesk.Core.Commands;

/// <summary>
/// OPERATIONAL command: Ends a session without processing payment.
/// Creates an UNSETTLED bill that appears in Payment Hub.
/// </summary>
public record EndSessionCommand(Guid SessionId);

public record EndSessionResult(
    Guid SessionId,
    Guid BillingId,
    string TableLabel,
    decimal TotalAmount,
    string Status,
    DateTime EndedAt
);
```

### Handler Pseudocode

```csharp
public async Task<EndSessionResult> HandleAsync(EndSessionCommand cmd)
{
    // 1. Fetch session
    var session = await _tableRepo.GetSessionByIdAsync(cmd.SessionId);
    if (session == null) throw new NotFoundException();
    
    // 2. Check idempotency - if already ended, return existing result
    if (session.Status == "ended")
    {
        var existingBill = await _billingRepo.GetBySessionIdAsync(cmd.SessionId);
        return new EndSessionResult(...); // Return existing
    }
    
    // 3. Check if already settled (conflict)
    if (session.Status == "settled")
        throw new ConflictException("Session already settled");
    
    // 4. Mark as ended
    var endTime = DateTime.UtcNow;
    
    // 5. Fetch items
    var items = await _orderService.GetOrderItemsForSessionAsync(cmd.SessionId);
    
    // 6. Calculate totals (BACKEND ONLY)
    var (timeCost, itemCost, total) = _billingService.CalculateTotal(
        session.StartTime, endTime, items);
    
    // 7. Create UNSETTLED bill
    var bill = new BillRecord
    {
        BillingId = session.BillingId ?? Guid.NewGuid(),
        SessionId = cmd.SessionId,
        TableLabel = session.TableId,
        TotalAmount = total,
        Status = "UNSETTLED",  // Key difference from StopSession
        EndTime = endTime,
        Items = items
    };
    await _billingRepo.CreateBillAsync(bill);
    
    // 8. Update session
    await _tableRepo.EndSessionAsync(cmd.SessionId, endTime);
    
    // 9. Return result
    return new EndSessionResult(
        cmd.SessionId,
        bill.BillingId,
        session.TableId,
        total,
        "UNSETTLED",
        endTime
    );
}
```

---

## Use Case 2: PrintPreSettlementReceiptUseCase

### Responsibilities

1. Validate table has an active session
2. Fetch all order items
3. Calculate preview totals (backend only)
4. Format receipt content
5. Send to printer
6. Return success/failure status

### Inputs

| Parameter | Type | Source | Required |
|-----------|------|--------|----------|
| TableLabel | string | URL path | Yes |

### Outputs

```csharp
record PrintPreSettlementResult(
    bool Success,
    string Message,
    string TableLabel,
    decimal PreviewTotal
);
```

### Side Effects

| Effect | Description |
|--------|-------------|
| None | This is a read-only operation |
| Printer | Receipt sent to printer (external effect) |

### Forbidden Behavior

```csharp
// ❌ FORBIDDEN - Modifying state
session.Status = ...;
bill.Status = ...;

// ❌ FORBIDDEN - Creating records
await _billingRepo.CreateBillAsync(...);

// ❌ FORBIDDEN - Payment info
receipt.PaymentMethod = ...;
receipt.AmountTendered = ...;
```

### Command Definition

```csharp
namespace MagiDesk.Core.Commands;

/// <summary>
/// OPERATIONAL command: Prints an informational pre-settlement receipt.
/// No state changes. For customer review only.
/// </summary>
public record PrintPreSettlementReceiptCommand(string TableLabel);

public record PrintPreSettlementReceiptResult(
    bool Success,
    string Message,
    string TableLabel,
    decimal PreviewTotal
);
```

### Handler Pseudocode

```csharp
public async Task<PrintPreSettlementReceiptResult> HandleAsync(
    PrintPreSettlementReceiptCommand cmd)
{
    // 1. Find active session for table
    var session = await _tableRepo.GetActiveSessionByTableAsync(cmd.TableLabel);
    if (session == null)
        throw new NotFoundException("No active session for this table");
    
    // 2. Fetch items
    var items = await _orderService.GetOrderItemsForSessionAsync(session.SessionId);
    
    // 3. Calculate preview (same as EndSession calculation)
    var now = DateTime.UtcNow;
    var (timeCost, itemCost, total) = _billingService.CalculateTotal(
        session.StartTime, now, items);
    
    // 4. Format receipt
    var receiptContent = FormatPreSettlementReceipt(
        cmd.TableLabel, session, items, timeCost, itemCost, total);
    
    // 5. Send to printer
    try
    {
        await _printerService.PrintAsync(receiptContent);
        return new PrintPreSettlementReceiptResult(
            true, "Receipt sent to printer", cmd.TableLabel, total);
    }
    catch (Exception ex)
    {
        return new PrintPreSettlementReceiptResult(
            false, $"Printer error: {ex.Message}", cmd.TableLabel, total);
    }
}
```

---

## Summary

| Use Case | Type | State Changes | Purpose |
|----------|------|---------------|---------|
| EndSessionUseCase | OPERATIONAL | Yes - creates UNSETTLED bill | Freeze timer, prepare for payment |
| PrintPreSettlementReceiptUseCase | INFORMATIONAL | No | Customer review before payment |

Both use cases:
- ✅ Calculate totals in backend
- ✅ Use existing BillingService
- ❌ Never touch payment fields
- ❌ Never mark as SETTLED
