# Shift Controller - Execution Phases

> Backend FIRST, UI second. No frontend until backend is enforcing.

---

## Phase Overview

| Phase | Focus | Duration | Dependencies |
|-------|-------|----------|--------------|
| 1 | Database Schema | 2h | None |
| 2 | Core Service Layer | 4h | Phase 1 |
| 3 | API Controller | 2h | Phase 2 |
| 4 | Shift Gating Middleware | 2h | Phase 3 |
| 5 | UI - Shift Controller Page | 4h | Phase 4 |
| 6 | UI - Gating Integration | 2h | Phase 5 |
| 7 | Testing & Verification | 2h | Phase 6 |

**Total Estimated: 18 hours**

---

## Phase 1: Database Schema

### Deliverables
- [ ] Create `shifts` table
- [ ] Add `shift_id` column to `table_sessions`
- [ ] Add `shift_id` column to `orders`
- [ ] Add `shift_id` column to `bills`
- [ ] Add `shift_id` column to `payments`
- [ ] Create migration script

### SQL
```sql
CREATE TABLE shifts (
    shift_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_number SERIAL NOT NULL,
    opened_by_user_id UUID NOT NULL REFERENCES users(user_id),
    opened_by_name VARCHAR(100),
    opened_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    starting_cash DECIMAL(10,2) NOT NULL,
    closed_by_user_id UUID,
    closed_by_name VARCHAR(100),
    closed_at TIMESTAMPTZ,
    declared_cash DECIMAL(10,2),
    expected_cash DECIMAL(10,2),
    difference DECIMAL(10,2),
    difference_category VARCHAR(50),
    close_reason TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'open', -- open, closed
    idempotency_key UUID,
    UNIQUE(shift_number)
);

CREATE INDEX idx_shifts_status ON shifts(status);
CREATE INDEX idx_shifts_opened_at ON shifts(opened_at);

-- Add FK columns to existing tables
ALTER TABLE table_sessions ADD COLUMN shift_id UUID REFERENCES shifts(shift_id);
ALTER TABLE orders ADD COLUMN shift_id UUID REFERENCES shifts(shift_id);
ALTER TABLE bills ADD COLUMN shift_id UUID REFERENCES shifts(shift_id);
ALTER TABLE payments ADD COLUMN shift_id UUID REFERENCES shifts(shift_id);
```

---

## Phase 2: Core Service Layer

### Deliverables
- [ ] `IShiftRepository` interface
- [ ] `ShiftRepository` implementation
- [ ] `IShiftService` interface
- [ ] `ShiftService` implementation
- [ ] Unit tests for service

### Key Methods
```csharp
// IShiftService
Task<Shift?> GetCurrentOpenShiftAsync();
Task<Shift> OpenShiftAsync(OpenShiftRequest request);
Task<ShiftBlockersDto> GetShiftBlockersAsync(Guid shiftId);
Task<ShiftCloseResult> CloseShiftAsync(CloseShiftRequest request);
Task<ShiftReportDto> GetShiftReportAsync(Guid shiftId);
Task<PagedList<ShiftSummary>> GetShiftHistoryAsync(ShiftHistoryFilter filter);
```

---

## Phase 3: API Controller

### Deliverables
- [ ] `ShiftsController` with all endpoints
- [ ] DTOs for requests/responses
- [ ] Authorization attributes
- [ ] Swagger documentation

### Controller Skeleton
```csharp
[ApiController]
[Route("[controller]")]
[Authorize]
public class ShiftsController : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<CurrentShiftDto>> GetCurrent();
    
    [HttpPost("open")]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<ActionResult<ShiftDto>> Open(OpenShiftRequest request);
    
    [HttpPost("{id}/close")]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<ActionResult<ShiftCloseResultDto>> Close(Guid id, CloseShiftRequest request);
    
    [HttpGet("{id}/report")]
    public async Task<ActionResult<ShiftReportDto>> GetReport(Guid id);
    
    [HttpPost("{id}/z-report/print")]
    public async Task<ActionResult> PrintZReport(Guid id);
    
    [HttpGet("history")]
    public async Task<ActionResult<PagedList<ShiftHistorySummary>>> GetHistory([FromQuery] ShiftHistoryFilter filter);
    
    [HttpGet("validate")]
    public async Task<ActionResult<ShiftValidationDto>> Validate();
}
```

---

## Phase 4: Shift Gating Middleware

### Deliverables
- [ ] `ShiftGuardMiddleware` or filter
- [ ] Apply to Tables, Orders, Payments endpoints
- [ ] Return 423 Locked when no shift
- [ ] Inject shift_id into HttpContext

### Implementation Options
1. **Middleware** - Checks all requests
2. **Action Filter** - Apply with `[RequireOpenShift]` attribute
3. **Service injection** - Check in each controller

**Recommended: Attribute-based filter**
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireOpenShiftAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var shiftService = context.HttpContext.RequestServices.GetRequiredService<IShiftService>();
        var shift = await shiftService.GetCurrentOpenShiftAsync();
        
        if (shift == null)
        {
            context.Result = new ObjectResult(new { error = "NO_SHIFT_OPEN", message = "Open a shift first" })
            {
                StatusCode = 423
            };
            return;
        }
        
        context.HttpContext.Items["CurrentShiftId"] = shift.ShiftId;
        await next();
    }
}
```

---

## Phase 5: UI - Shift Controller Page

### Deliverables
- [ ] `ShiftControllerPage.xaml`
- [ ] `ShiftControllerViewModel.cs`
- [ ] `OpenShiftDialog.xaml`
- [ ] `CloseShiftDialog.xaml`
- [ ] `ShiftHistoryItem` data template
- [ ] Add to navigation

### UI Components
| Component | Description |
|-----------|-------------|
| Empty state | No shift banner with Open button |
| Dashboard | Live stats, KPIs |
| History list | Paginated shift history |
| Open dialog | Starting cash input |
| Close dialog | Declared cash, difference, reason |

---

## Phase 6: UI - Gating Integration

### Deliverables
- [ ] Call `/shifts/validate` on app startup
- [ ] Show "No shift" banner in shell
- [ ] Redirect to ShiftController if no shift
- [ ] Disable Table/Order/Payment buttons when no shift

### Integration Points
```csharp
// ShellViewModel
public async Task InitializeAsync()
{
    var status = await _shiftApi.ValidateAsync();
    IsShiftOpen = status.CanOperate;
    CurrentShiftNumber = status.ShiftNumber;
    
    if (!status.CanOperate)
    {
        ShowNoShiftBanner();
    }
}
```

---

## Phase 7: Testing & Verification

### Test Cases
1. **Open shift** - Success, already open error
2. **Close shift** - Success, blocked by tables, blocked by bills
3. **Gating** - API returns 423 when no shift
4. **Cash variance** - Difference calculated correctly
5. **History** - Pagination works
6. **Z-report** - Prints or shows error

### Verification Checklist
- [ ] Cannot start session without shift
- [ ] Cannot send orders without shift
- [ ] Cannot close shift with active tables
- [ ] Cannot close shift with unsettled bills
- [ ] Difference = declared - expected
- [ ] Closed shifts are read-only
- [ ] UI matches reference design
