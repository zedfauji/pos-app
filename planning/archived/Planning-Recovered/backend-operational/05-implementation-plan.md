# 05 - Implementation Plan

## Purpose

Define the granular implementation tasks for the OPERATIONAL endpoints, organized by layer and with clear acceptance criteria.

---

## Phase 1: Shared DTOs

### Task 1.1: Add EndSessionResult DTO

**File**: `shared/DTOs/Tables/TablesDtos.cs`
**Layer**: Shared
**Action**: ADD

```csharp
/// <summary>
/// Result from ending a session (OPERATIONAL - NOT payment).
/// Session becomes UNSETTLED and appears in Payment Hub.
/// </summary>
public record EndSessionResult(
    Guid SessionId,
    Guid BillingId,
    string TableLabel,
    decimal TotalAmount,
    string Status,      // "UNSETTLED"
    DateTime EndedAt
);
```

**Acceptance Criteria**:
- [ ] DTO compiles
- [ ] No payment-related fields present

### Task 1.2: Add PrintPreSettlementResult DTO

**File**: `shared/DTOs/Tables/TablesDtos.cs`
**Layer**: Shared
**Action**: ADD

```csharp
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

**Acceptance Criteria**:
- [ ] DTO compiles
- [ ] No payment-related fields present

---

## Phase 2: Core Commands

### Task 2.1: Create EndSessionCommand

**File**: `MagiDesk.Core/Commands/EndSessionCommand.cs` (NEW)
**Layer**: Core
**Action**: CREATE

```csharp
namespace MagiDesk.Core.Commands;

public record EndSessionCommand(Guid SessionId);
```

**Acceptance Criteria**:
- [ ] No payment parameters
- [ ] Only SessionId input

### Task 2.2: Create EndSessionCommandHandler

**File**: `MagiDesk.Core/Commands/EndSessionCommandHandler.cs` (NEW)
**Layer**: Core
**Action**: CREATE

**Acceptance Criteria**:
- [ ] Validates session exists and is active
- [ ] Returns idempotent result if already ended
- [ ] Throws ConflictException if already settled
- [ ] Calculates totals via BillingService
- [ ] Creates bill with Status = "UNSETTLED"
- [ ] Updates session to "ended"
- [ ] NO payment logic

### Task 2.3: Create PrintPreSettlementReceiptCommand

**File**: `MagiDesk.Core/Commands/PrintPreSettlementReceiptCommand.cs` (NEW)
**Layer**: Core
**Action**: CREATE

```csharp
namespace MagiDesk.Core.Commands;

public record PrintPreSettlementReceiptCommand(string TableLabel);
```

### Task 2.4: Create PrintPreSettlementReceiptCommandHandler

**File**: `MagiDesk.Core/Commands/PrintPreSettlementReceiptCommandHandler.cs` (NEW)
**Layer**: Core
**Action**: CREATE

**Acceptance Criteria**:
- [ ] Fetches active session for table
- [ ] Calculates preview totals
- [ ] Sends to printer
- [ ] NO state changes

---

## Phase 3: Repository Updates

### Task 3.1: Add EndSessionAsync to ITableRepository

**File**: `MagiDesk.Core/Interfaces/ITableRepository.cs`
**Layer**: Core (Interface)
**Action**: MODIFY

```csharp
Task EndSessionAsync(Guid sessionId, DateTime endTime);
Task<SessionRecord?> GetActiveSessionByTableAsync(string tableLabel);
```

### Task 3.2: Implement EndSessionAsync in TableRepository

**File**: `MagiDesk.Infrastructure/Repositories/TableRepository.cs`
**Layer**: Infrastructure
**Action**: MODIFY

**Acceptance Criteria**:
- [ ] Updates session.status = "ended"
- [ ] Updates session.end_time
- [ ] Updates table.occupied = false
- [ ] Uses transaction

---

## Phase 4: API Controllers

### Task 4.1: Add EndSession Endpoint

**File**: `TablesApi/Controllers/SessionsController.cs` (NEW or extend TablesController)
**Layer**: API
**Action**: CREATE/MODIFY

```csharp
[HttpPost("{sessionId:guid}/end")]
public async Task<ActionResult<EndSessionResult>> EndSession(Guid sessionId)
{
    var result = await _endSessionHandler.HandleAsync(new EndSessionCommand(sessionId));
    return Ok(result);
}
```

**Acceptance Criteria**:
- [ ] Returns 200 with EndSessionResult
- [ ] Returns 404 if session not found
- [ ] Returns 409 if session already settled

### Task 4.2: Add PrintPreSettlementReceipt Endpoint

**File**: `TablesApi/Controllers/TablesController.cs`
**Layer**: API
**Action**: MODIFY

```csharp
[HttpPost("{label}/print-presettlement")]
public async Task<ActionResult<PrintPreSettlementResult>> PrintPreSettlementReceipt(string label)
{
    var result = await _printHandler.HandleAsync(new PrintPreSettlementReceiptCommand(label));
    return Ok(result);
}
```

**Acceptance Criteria**:
- [ ] Returns 200 with PrintPreSettlementResult
- [ ] Returns 404 if no active session

---

## Phase 5: Dependency Injection

### Task 5.1: Register New Handlers

**File**: `TablesApi/Program.cs`
**Layer**: API
**Action**: MODIFY

```csharp
builder.Services.AddScoped<ICommandHandler<EndSessionCommand, EndSessionResult>, EndSessionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PrintPreSettlementReceiptCommand, PrintPreSettlementResult>, PrintPreSettlementReceiptCommandHandler>();
```

---

## Phase 6: Testing

### Task 6.1: Unit Tests for EndSessionCommandHandler

**File**: `MagiDesk.Core.Tests/Commands/EndSessionCommandHandlerTests.cs` (NEW)
**Layer**: Tests
**Action**: CREATE

**Test Cases**:
- [ ] Returns result for active session
- [ ] Returns idempotent result for already ended session
- [ ] Throws NotFoundException for missing session
- [ ] Throws ConflictException for settled session
- [ ] Creates bill with Status = UNSETTLED
- [ ] NO payment fields set

### Task 6.2: Unit Tests for PrintPreSettlementReceiptCommandHandler

**File**: `MagiDesk.Core.Tests/Commands/PrintPreSettlementReceiptCommandHandlerTests.cs` (NEW)
**Layer**: Tests
**Action**: CREATE

**Test Cases**:
- [ ] Returns success for active session
- [ ] Returns failure for printer error
- [ ] Throws NotFoundException for no active session
- [ ] NO state modifications

### Task 6.3: Integration Tests

**File**: `TablesApi.Tests/EndSessionIntegrationTests.cs` (NEW)
**Layer**: Integration Tests
**Action**: CREATE

**Test Cases**:
- [ ] POST /tables/{sessionId}/end returns 200
- [ ] Bill appears in unsettled bills list
- [ ] Table is released

---

## Execution Order

1. ✅ Phase 1: Shared DTOs
2. ✅ Phase 2: Core Commands & Handlers
3. ✅ Phase 3: Repository Updates
4. ✅ Phase 4: API Controllers
5. ✅ Phase 5: DI Registration
6. ✅ Phase 6: Testing

Each phase must compile and pass tests before proceeding.
