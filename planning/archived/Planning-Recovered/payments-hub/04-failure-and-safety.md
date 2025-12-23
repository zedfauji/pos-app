# 04 - Failure & Safety UX

## Purpose

Define error handling, edge cases, and safety mechanisms to prevent mis-settlement and ensure audit compliance.

---

## Payment Validation Errors

### Underpayment

**Scenario**: Tendered amount < Due amount

```
┌─────────────────────────────────────┐
│ ⚠️ Insufficient Payment             │
│                                     │
│ Amount Due:     $38.50              │
│ Tendered:       $30.00              │
│ Shortfall:      $8.50               │
│                                     │
│ Options:                            │
│ [ Add More ] [ Split Payment ]       │
│                                     │
└─────────────────────────────────────┘
```

**Behavior**:
- Confirm button disabled
- Red warning shown
- Suggest split payment if intentional

### Overpayment (Change Due)

**Scenario**: Tendered amount > Due amount (expected for cash)

```
┌─────────────────────────────────────┐
│ 💵 Change Required                  │
│                                     │
│ Amount Due:     $38.50              │
│ Tendered:       $50.00              │
│ CHANGE:         $11.50              │  ← Large, green
│                                     │
│ [ ✅ Confirm $38.50 ]                │
│                                     │
└─────────────────────────────────────┘
```

**Behavior**:
- Change amount prominently displayed
- Confirm enabled
- Change shown again in confirmation dialog

---

## Cancel Mid-Payment

### Before Any Input

| User Action | System Response |
|-------------|-----------------|
| Back button | Return to Hub immediately |
| X button | Return to Hub immediately |
| Escape | Return to Hub immediately |

No confirmation needed - nothing to lose.

### After Partial Input (Tip/Discount Entered)

```
┌─────────────────────────────────────┐
│ Discard Changes?                    │
│                                     │
│ You have entered:                   │
│   Tip: $5.00                        │
│                                     │
│ [ Stay ] [ Discard & Leave ]        │
└─────────────────────────────────────┘
```

### During Payment Processing

```
┌─────────────────────────────────────┐
│ ⏳ Processing Payment...            │
│                                     │
│ Please wait. Do not close.          │
│                                     │
│ ████████████░░░░░░░░                │
│                                     │
└─────────────────────────────────────┘
```

- Cancel button hidden during processing
- Screen locked until complete
- Timeout after 30 seconds → error dialog

---

## Resume Later Flow

### Partial Payment Recorded

```
┌─────────────────────────────────────┐
│ Partial Payment Recorded            │
│                                     │
│ Table: Bar 3                        │
│ Paid: $20.00                        │
│ Remaining: $18.50                   │
│                                     │
│ This bill will remain UNSETTLED     │
│ until the balance is paid.          │
│                                     │
│ [ Return to Hub ]                   │
└─────────────────────────────────────┘
```

### Resuming Partial Payment

In Payment Hub, partial bills show:

```
┌──────────────────────────────────┐
│ 🎱 Bar 3              💰 PARTIAL │
│ ──────────────────────────────── │
│ Total: $38.50                    │
│ Paid: $20.00                     │
│ ▓▓▓▓▓▓▓▓░░░░░░ 52%               │
│ REMAINING: $18.50                │
│ [ Pay Balance ]                  │
└──────────────────────────────────┘
```

---

## Retry Safety

### Network Failure During Payment

```
┌─────────────────────────────────────┐
│ ❌ Payment Failed                   │
│                                     │
│ Network error occurred.             │
│ No payment was processed.           │
│                                     │
│ [ Retry ] [ Cancel ]                │
└─────────────────────────────────────┘
```

**Idempotency Guarantee**:
- Each payment attempt has unique request ID
- Backend rejects duplicate requests
- Safe to retry

### Timeout

```
┌─────────────────────────────────────┐
│ ⏱️ Request Timed Out                │
│                                     │
│ The server did not respond.         │
│ The payment may or may not have     │
│ been processed.                     │
│                                     │
│ [ Check Status ] [ Cancel ]         │
│                                     │
└─────────────────────────────────────┘
```

**Check Status** queries backend for payment status.

---

## Edge Case Matrix

| Scenario | Detection | Response |
|----------|-----------|----------|
| Bill already settled | Backend returns 409 | "This bill was already paid" |
| Session re-opened | Backend returns 409 | "Session was re-opened" |
| Amount mismatch | Backend validation | Reload bill, show new amount |
| Stale data | ETag mismatch | Reload bill, warn user |
| Manager discount without auth | Local validation | Prompt for manager PIN |
| Printer failure | Print callback | Show retry, allow skip |

---

## Confirmation Requirements

### Required Confirmations

| Action | Confirmation |
|--------|--------------|
| Full payment | Dialog with amount |
| Partial payment | Dialog with partial amount + remaining |
| Zero payment (e.g., comped) | Manager override PIN |
| Large discount (>20%) | Manager override PIN |
| Void/refund | Not in scope (separate flow) |

### Manager Override

```
┌─────────────────────────────────────┐
│ 🔐 Manager Authorization Required   │
│                                     │
│ This action requires manager        │
│ approval:                           │
│                                     │
│   Discount: $20.00 (52%)            │
│                                     │
│ Manager PIN:                        │
│ [ ● ● ● ● ]                         │
│                                     │
│ [ Cancel ] [ Authorize ]            │
└─────────────────────────────────────┘
```

---

## Audit Trail

Every payment action logs:

| Field | Value |
|-------|-------|
| Timestamp | UTC |
| Session ID | GUID |
| Billing ID | GUID |
| User ID | Cashier |
| Action | PAYMENT_PROCESSED |
| Amount | $X.XX |
| Method | Cash/Card |
| Tip | $X.XX |
| Discount | $X.XX |
| Manager Auth | If applicable |

---

## Success Criteria

1. **No accidental settlement**
   - Confirmation required
   - Amount clearly shown

2. **Safe retries**
   - Idempotent backend
   - Clear status after retry

3. **Partial payments tracked**
   - Visual progress indicator
   - Clear remaining balance

4. **Audit-safe**
   - All actions logged
   - Manager auth for exceptions
