# 01 - Cashier Workflow Design

## Purpose

Define the cashier workflow for handling UNSETTLED sessions in the Payment Hub, making settlement obvious, safe, and fast.

---

## ⛔ CRITICAL BOUNDARY

This page handles **FINANCIAL** actions only:

- ❌ No operational actions (session start/end)
- ❌ No session timers
- ❌ No order entry
- ❌ No logic duplication from Table Workspace

- ✅ Financial actions only (payment, settlement)
- ✅ Clear UNSETTLED vs SETTLED distinction
- ✅ Backend-driven totals
- ✅ Explicit confirmation steps

---

## Entry Points

### 1. Primary Entry: Navigation Menu
- Cashier clicks "Payments" in main navigation
- Opens Payment Hub showing all UNSETTLED bills

### 2. Secondary Entry: Table Workspace "Go to Payments"
- User clicks "Go to Payments" in Table Workspace
- Navigates to Payment Hub (filtered to all, not specific table)

### 3. Direct Entry: Session End Notification
- After clicking "End Session" in Table Workspace
- Dialog shows: "Session ended. Bill is now in Payments for settlement."
- Optional: Navigate directly to Payment Hub

---

## Payment Hub Layout

### State Tabs

```
┌─────────────────────────────────────────────────────────┐
│  Payment Hub                                             │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  [ ALL ] [ UNSETTLED ⚠️ ] [ PARTIAL 💰 ] [ ACTIVE 🟢 ]   │
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ Bar 3        │  │ Pool 1       │  │ Bar 5        │   │
│  │ John D.      │  │ Sarah K.     │  │ Mike R.      │   │
│  │              │  │              │  │              │   │
│  │ ⚠️ UNSETTLED │  │ ⚠️ UNSETTLED │  │ 🟢 ACTIVE    │   │
│  │ $45.00       │  │ $128.50      │  │ $22.00       │   │
│  │ [ Pay Now ]  │  │ [ Pay Now ]  │  │ [ View ]     │   │
│  └──────────────┘  └──────────────┘  └──────────────┘   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Tab Definitions

| Tab | Shows | Purpose |
|-----|-------|---------|
| ALL | All sessions and bills | Overview |
| UNSETTLED ⚠️ | Bills from ended sessions awaiting payment | **PRIMARY WORKFLOW** |
| PARTIAL 💰 | Partially paid bills | Split payment tracking |
| ACTIVE 🟢 | Currently active sessions (timer running) | Legacy/optional |

---

## Unsettled List Behavior

### Data Source
```
GET /bills/unsettled
→ Returns bills with status = "UNSETTLED"
```

### Card Information

| Field | Display |
|-------|---------|
| Table Label | Bold, prominent (e.g., "Bar 3") |
| Server Name | Secondary text |
| Time Since End | "Ended 15 min ago" |
| Total Amount | Large, red if > threshold |
| Status Badge | ⚠️ UNSETTLED (yellow) |
| Item Count | "5 items" |

### Sorting
1. Amount Due (highest first) - default
2. Time since ended (oldest first) - risk indicator
3. Table label (alphabetical)

### Visual Risk Indicators

| Condition | Visual |
|-----------|--------|
| Ended > 30 min ago | Red background |
| Amount > $100 | Large bold text |
| Partial payment exists | 💰 indicator |

---

## Tap Behavior: UNSETTLED Session

### Tap → Payment Workspace

When cashier taps an UNSETTLED card:

1. Navigate to Payment Workspace
2. Load bill details from backend
3. Display itemized receipt
4. Show payment options

**No confirmation needed to view - easy access.**

---

## Resume vs Pay Flow

### UNSETTLED → Pay Now

```
Cashier taps UNSETTLED card
    ↓
Payment Workspace opens
    ↓
Shows: Table, Items, Total
    ↓
Cashier selects payment method
    ↓
Enters amount tendered
    ↓
System calculates change
    ↓
[ Confirm Payment ] button
    ↓
Confirmation dialog: "Settle $45.00 for Bar 3?"
    ↓
Backend processes payment
    ↓
Receipt printed
    ↓
Return to Payment Hub
```

### ACTIVE → Close Table (Legacy Flow)

```
Cashier taps ACTIVE card
    ↓
Payment Workspace opens
    ↓
Warning: "Session still active. Timer running."
    ↓
Option: [ End Session First ] or [ Cancel ]
    ↓
If End Session chosen → Session becomes UNSETTLED
    ↓
Continue with payment flow
```

---

## Cancel / Back Flows

### From Payment Hub

| Action | Behavior |
|--------|----------|
| Back button | Return to previous page |
| Close | Return to main navigation |

### From Payment Workspace

| Action | Behavior |
|--------|----------|
| Back / Cancel | Return to Payment Hub (no changes) |
| X button | Return to Payment Hub (no changes) |
| Escape key | Return to Payment Hub (no changes) |

### Mid-Payment Cancel

| Stage | Behavior |
|-------|----------|
| Before confirmation | Simply return to Payment Hub |
| During processing | Wait for completion or timeout |
| After confirmation | Cannot cancel (payment recorded) |

---

## Error States

| Error | Display | Action |
|-------|---------|--------|
| Bill not found | "This bill no longer exists" | Return to Hub |
| Already settled | "This bill was already paid" | Return to Hub |
| Backend error | "Payment failed. Please try again." | Retry button |
| Network timeout | "Connection lost. Retrying..." | Auto-retry |

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| F5 | Refresh list |
| Enter | Select highlighted card |
| Escape | Cancel / Back |
| Tab | Navigate between cards |

---

## Success Criteria

1. **Cashiers understand what to do instantly**
   - UNSETTLED cards are visually distinct
   - "Pay Now" action is obvious

2. **No accidental settlement**
   - Confirmation required before processing
   - Amount clearly displayed

3. **Partial payments are obvious**
   - 💰 indicator shows partial status
   - Remaining balance prominent

4. **System is audit-safe**
   - All actions logged
   - Backend authoritative for totals
