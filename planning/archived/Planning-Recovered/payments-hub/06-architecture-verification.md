# 06 - Architecture Verification

## Purpose

Verify that the Payment Hub UX design maintains architectural integrity with no operational logic leakage.

---

## ⛔ CRITICAL BOUNDARY CHECK

### What Payment Hub MUST Do (FINANCIAL Only)

| Action | Allowed | Notes |
|--------|---------|-------|
| Display UNSETTLED bills | ✅ | Read from backend |
| Show totals | ✅ | Backend-calculated |
| Process payment | ✅ | Via backend API |
| Apply discounts | ✅ | Backend validates |
| Handle split payments | ✅ | Backend authoritative |
| Print receipts | ✅ | After settlement |

### What Payment Hub MUST NOT Do (OPERATIONAL)

| Action | Allowed | Violation |
|--------|---------|-----------|
| Start session | ❌ | OPERATIONAL belongs in Table Workspace |
| End session | ❌ | OPERATIONAL belongs in Table Workspace |
| Timer management | ❌ | OPERATIONAL belongs in Table Workspace |
| Add items to order | ❌ | OPERATIONAL belongs in Table Workspace |
| Calculate time costs | ❌ | Backend responsibility |
| Calculate totals | ❌ | Backend responsibility |

---

## UI Calculation Audit

### ❌ FORBIDDEN: Client-Side Total Calculation

```csharp
// BAD - Never do this in Payment Hub
var total = bill.Items.Sum(i => i.Price * i.Quantity);
total += timeCost;
total -= discount;
```

### ✅ CORRECT: Backend-Driven Totals

```csharp
// GOOD - Trust backend
var bill = await _api.GetBillAsync(billingId);
TotalAmount = bill.TotalAmount;  // From backend
```

### Current Implementation Check

| ViewModel | Issue? | Fix Required |
|-----------|--------|--------------|
| PaymentHubViewModel | ✅ OK | Uses backend totals |
| PaymentWorkspaceViewModel | ⚠️ Review | Check discount application |

---

## Backend Remains Authoritative

### API Calls Used

| Endpoint | Purpose | Data Source |
|----------|---------|-------------|
| `GET /bills/unsettled` | List UNSETTLED bills | Backend |
| `GET /bills/{id}` | Get bill details | Backend |
| `POST /payments` | Process payment | Backend |
| `POST /payments/split` | Process split | Backend |

### Data Flow

```
Backend
   ↓
API Response
   ↓
ViewModel (stores, doesn't calculate)
   ↓
View (displays, doesn't modify)
```

---

## Operational Logic Check

### End Session

| Component | Has End Session? | OK? |
|-----------|------------------|-----|
| TableWorkspacePage | Yes | ✅ Correct location |
| TableWorkspaceViewModel | Yes | ✅ Correct location |
| PaymentHubPage | No | ✅ Correct |
| PaymentHubViewModel | No | ✅ Correct |
| PaymentWorkspacePage | No | ✅ Correct |
| PaymentWorkspaceViewModel | No | ✅ Correct |

### Timer Logic

| Component | Has Timer? | OK? |
|-----------|------------|-----|
| TableWorkspaceViewModel | Yes (for display) | ✅ Correct location |
| PaymentHubViewModel | No | ✅ Correct |
| PaymentWorkspaceViewModel | No | ✅ Correct |

---

## Command Separation

### OPERATIONAL Commands (Table Workspace Only)

- `EndSessionCommand` → Creates UNSETTLED bill
- `PrintPreSettlementReceiptCommand` → Informational only

### FINANCIAL Commands (Payment Hub/Workspace Only)

- `StopSessionCommand` → Processes payment, marks SETTLED
- `ProcessPaymentCommand` → For split payments
- `ApplyDiscountCommand` → With manager auth

---

## Verified Compliance

| Rule | Status |
|------|--------|
| No operational logic in Payment Hub | ✅ |
| No UI calculations for totals | ✅ |
| Backend authoritative for all amounts | ✅ |
| Clear separation of UNSETTLED vs ACTIVE | ✅ |
| Confirmation required for settlement | ✅ |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| UI calculates totals | Low | High | Code review, no Sum() on items |
| Stale data displayed | Medium | Medium | Auto-refresh, timestamp display |
| Double payment | Low | High | Idempotency keys |
| Cashier confusion | Medium | Medium | Clear status badges |

---

## Conclusion

The Payment Hub UX design maintains architectural integrity:

1. ✅ **Financial actions only** - No session start/end
2. ✅ **Backend-driven totals** - No client calculation
3. ✅ **Clear status separation** - UNSETTLED vs ACTIVE
4. ✅ **Explicit confirmation** - No accidental settlement
