# 05 - Anti-Drift Guardrails

## Purpose

This document explicitly forbids patterns that violate Clean Architecture and the Table Workspace's defined scope.

---

## ⛔ CRITICAL ARCHITECTURAL BOUNDARY

> [!CAUTION]
> **The Table Workspace CANNOT SETTLE (process payments).**
> 
> All payment processing and financial settlement MUST occur ONLY in:
> **Payments Hub → Payment Workspace**

### Key Terminology:
- **OPERATIONAL**: Session lifecycle (start, end, timer) - ALLOWED in Table Workspace
- **FINANCIAL**: Payment processing, settlement - NOT allowed in Table Workspace

### What Table Workspace MAY Do (OPERATIONAL):
- ✅ Display table/session status
- ✅ Show ordered items (read-only)
- ✅ Allow adding orders (intent only)
- ✅ **End Session** → stops timer, marks as UNSETTLED (NOT payment)
- ✅ **Print Pre-Settlement Receipt** → informational only
- ✅ Navigate to Payments (navigation only)

### What Table Workspace MUST NOT Do (FINANCIAL):
- ❌ Settle payments
- ❌ Process payment methods (Cash/Card/Split)
- ❌ Calculate totals in UI
- ❌ Call settlement/payment APIs
- ❌ Use payment DTOs

---

## ❌ FORBIDDEN PATTERNS

### 1. Totals Creeping Into This Page

**BANNED Code Examples**:
```csharp
// ❌ FORBIDDEN - calculating in ViewModel
public decimal Subtotal => Items.Sum(x => x.Price * x.Quantity);

// ❌ FORBIDDEN - calculating in XAML converter
public object Convert(object value, ...) 
{
    var items = (IEnumerable<OrderItem>)value;
    return items.Sum(x => x.Price * x.Quantity);
}

// ❌ FORBIDDEN - displaying any total
<TextBlock Text="{Binding Subtotal}" />
<TextBlock Text="Total: $XX.XX" />
```

**WHY**: Totals belong to Payment Workspace. Backend is authoritative.

---

### 2. Payment Shortcuts

**BANNED UI Elements**:
- "Settle & Close Session" button
- "Pay Now" button
- Payment method selector (Cash/Card/Split)
- Tip entry field
- "Close Table" action
- Any button that triggers payment flow directly

**BANNED Code**:
```csharp
// ❌ FORBIDDEN - payment logic in workspace
await _tableApi.StopSessionAsync(sessionId, paymentRequest);

// ❌ FORBIDDEN - payment navigation from workspace commands
_shell.NavigateToPaymentWorkspace();  // Not from workspace commands
```

**WHY**: Payment is a separate workflow accessed via Payment Hub.

---

### 3. Dialog-Based Workflows

**BANNED Patterns**:
```csharp
// ❌ FORBIDDEN - dialog for core actions
await _dialogService.ShowPaymentDialogAsync(total);

// ❌ FORBIDDEN - confirmation dialogs for primary actions
var result = await ContentDialog.ShowAsync(...);
if (result == ContentDialogResult.Primary)
{
    // Do the thing
}
```

**ALLOWED Exceptions**:
- Error message dialogs (`ShowMessageAsync`)
- Destructive action confirmations (e.g., "Clear all draft items?")

**WHY**: Core workflows should be page-based per architectural rules.

---

### 4. Backend Logic Duplication

**BANNED Calculations**:
```csharp
// ❌ FORBIDDEN - tax calculation
var tax = subtotal * 0.08m;

// ❌ FORBIDDEN - discount application  
var discounted = total - (total * discountPercent);

// ❌ FORBIDDEN - time-based pricing
var timeCost = minutes * ratePerMinute;

// ❌ FORBIDDEN - change calculation
var change = tendered - due;
```

**WHY**: All business calculations happen in backend only.

---

### 5. Session State Manipulation

**BANNED Actions**:
```csharp
// ❌ FORBIDDEN - ending session from workspace
await _tableApi.StopSessionAsync(...);

// ❌ FORBIDDEN - modifying session state
table.Status = "CLOSED";

// ❌ FORBIDDEN - local session tracking
_localSessionCache[tableId] = session;
```

**WHY**: Backend is the single source of truth for session state.

---

## ✅ ALLOWED PATTERNS

### Display-Only Timer
```csharp
// ✅ ALLOWED - Display elapsed time from StartTime
public string ElapsedTime 
{
    get 
    {
        if (StartTime == null) return "--:--:--";
        var elapsed = DateTime.Now - StartTime.Value;
        return elapsed.ToString(@"hh\:mm\:ss");
    }
}

// Timer tick just raises PropertyChanged, no calculations
_timer.Tick += (s, e) => OnPropertyChanged(nameof(ElapsedTime));
```

### Simple UI State
```csharp
// ✅ ALLOWED - Simple boolean checks
public bool HasItems => Items.Count > 0;
public bool CanSend => HasItems && !IsLoading;
```

### Navigation Intent
```csharp
// ✅ ALLOWED - Back navigation
_shell.NavigateToTables();

// ✅ ALLOWED - Navigate to view (read-only)
OpenOrderedItemsView();

// ✅ ALLOWED - Navigate to Payments (navigation only, NO logic)
_shell.NavigateToPaymentHub();
```

> [!IMPORTANT]
> The "Go to Payments" action performs **NAVIGATION ONLY**.
> It must NOT:
> - Pass session data
> - Calculate totals
> - Call any APIs
> - Modify session state

---

## Code Review Checklist

Before approving any PR on Table Workspace:

| Check | Pass? |
|-------|-------|
| No `Sum()`, `Average()`, or aggregate methods on prices | ☐ |
| No "Total", "Subtotal", "Tax", "Tip" in XAML | ☐ |
| No payment method selection UI | ☐ |
| No "Close Session" or "Settle" buttons | ☐ |
| No `StopSessionAsync` calls | ☐ |
| No `ContentDialog` for primary workflows | ☐ |
| All data comes from API responses | ☐ |
| Timer is display-only (no time-based calculations) | ☐ |

---

## Escalation Protocol

If during implementation you encounter a requirement that seems to require forbidden patterns:

1. **STOP** implementation
2. **DOCUMENT** the requirement in conflict
3. **REPORT** to architect for design clarification
4. **DO NOT** implement a workaround

---

## Rationale

These guardrails exist because:

1. **Consistency**: Payment flows should work identically regardless of entry point
2. **Accuracy**: Backend calculations are authoritative (tax rules, discounts, etc.)
3. **Maintainability**: Single source of truth reduces bugs
4. **Auditability**: Financial calculations must be traceable to backend
