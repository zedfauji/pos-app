# 02 - Information Layout

## Purpose

Define what the cashier sees, what is read-only, what is actionable, and visual risk indicators for effective payment processing.

---

## Payment Hub - Information Hierarchy

### Level 1: Page Header

```
┌─────────────────────────────────────────────────────────┐
│  💳 Payment Hub                    [ Refresh ] [ ? ]    │
│  ──────────────────────────────────────────────────────  │
│  📊 Summary: 5 unsettled ($423.50) | 2 active ($78.00)  │
└─────────────────────────────────────────────────────────┘
```

| Element | Type | Purpose |
|---------|------|---------|
| Title | Read-only | Page identification |
| Refresh | Action | Reload data |
| Summary | Read-only | Quick overview of pending work |

### Level 2: Tabs

```
[ ALL ] [ UNSETTLED (5) ⚠️ ] [ PARTIAL (1) 💰 ] [ ACTIVE (2) 🟢 ]
```

| Tab | Type | Purpose |
|-----|------|---------|
| ALL | Filter | Show everything |
| UNSETTLED | Filter | **Primary workflow** - ended sessions needing payment |
| PARTIAL | Filter | Split payments in progress |
| ACTIVE | Filter | Optional - running sessions |

### Level 3: Session Cards

```
┌──────────────────────────────────┐
│ 🎱 Bar 3              ⚠️ UNSETTLED│  ← Status badge (color-coded)
│ ──────────────────────────────── │
│ Server: John D.                  │  ← Read-only context
│ Ended: 25 min ago                │  ← Time indicator (risk)
│ Items: 5                         │  ← Quick count
│ ──────────────────────────────── │
│ TOTAL: $45.00                    │  ← Large, prominent
│                                  │
│ [ 💳 Pay Now ]                   │  ← Primary action
└──────────────────────────────────┘
```

---

## Card Elements: Read-Only vs Actionable

### Read-Only Information (Display Only)

| Element | Purpose |
|---------|---------|
| Table Label | Identify which table |
| Table Type Icon | Visual distinction (🎱 billiard, 🍺 bar) |
| Server Name | Staff attribution |
| Time Ended | Risk indicator (older = more urgent) |
| Item Count | Quick reference |
| Total Amount | Backend-calculated (authoritative) |
| Status Badge | Current state |

### Actionable Elements

| Element | Action |
|---------|--------|
| Card Click | Open Payment Workspace |
| "Pay Now" Button | Open Payment Workspace |

**No in-line editing on cards** - all payment actions happen in Payment Workspace.

---

## Visual Risk Indicators

### Time-Based Risk

| Time Since Ended | Visual |
|------------------|--------|
| 0-15 min | Normal (default) |
| 15-30 min | Yellow border |
| 30+ min | Red border + ⚠️ icon |
| 1+ hour | Red background + 🚨 icon |

### Amount-Based Risk

| Amount | Visual |
|--------|--------|
| $0-50 | Normal text |
| $50-100 | Bold text |
| $100+ | Bold + accent color |
| $200+ | Large bold + red |

### Status Indicators

| Status | Badge |
|--------|-------|
| UNSETTLED | ⚠️ Yellow pill |
| PARTIAL | 💰 Blue pill + progress bar |
| ACTIVE | 🟢 Green dot |
| SETTLED | ✅ Green checkmark (history view) |

---

## Payment Workspace - Information Layout

### Left Panel: Bill Summary (Read-Only)

```
┌────────────────────────────────┐
│  Bar 3 - Bill Summary          │
│  ────────────────────────────  │
│  Server: John D.               │
│  Started: 7:30 PM              │
│  Ended: 8:15 PM                │
│  Duration: 45 min              │
│                                │
│  ┌──────────────────────────┐  │
│  │ Items                     │  │
│  │ 2x Beer         $12.00   │  │
│  │ 1x Nachos        $8.00   │  │
│  │ Pool Time (30m) $15.00   │  │
│  │ ────────────────────────  │
│  │ Subtotal       $35.00    │  │
│  │ Tax             $3.50    │  │
│  │ ────────────────────────  │
│  │ TOTAL          $38.50    │  │
│  └──────────────────────────┘  │
└────────────────────────────────┘
```

**Everything in this panel is READ-ONLY** - data from backend.

### Right Panel: Payment Actions

```
┌────────────────────────────────┐
│  Process Payment               │
│  ────────────────────────────  │
│                                │
│  Payment Amount: $38.50        │  ← Default: full amount
│  [ Full Amount ] [ Custom ]    │
│                                │
│  Payment Method:               │
│  [ 💵 Cash ]  [ 💳 Card ]       │  ← Select one
│                                │
│  ────────────────────────────  │
│  Amount Tendered: [    $50.00] │  ← Input (Cash only)
│  Change Due:       $11.50      │  ← Auto-calculated
│  ────────────────────────────  │
│                                │
│  Tip: [     $0.00]             │  ← Optional input
│  Discount: [  $0.00]           │  ← Optional input
│                                │
│  ────────────────────────────  │
│  FINAL TOTAL: $38.50           │  ← Updated live
│                                │
│  [ Cancel ]  [ ✅ Confirm $38.50 ] │
└────────────────────────────────┘
```

### Actionable Elements in Payment Workspace

| Element | Type | Behavior |
|---------|------|----------|
| Payment Amount | Display | Shows amount to collect |
| Full/Custom Toggle | Action | Switch between full and split |
| Payment Method | Toggle | Cash or Card (mutually exclusive) |
| Amount Tendered | Input | For Cash - calculate change |
| Tip | Input | Optional - added to total |
| Discount | Input | Optional - subtracted from total |
| Cancel | Action | Return to Hub (no changes) |
| Confirm | Action | Process payment (requires confirmation) |

---

## Confirmation Dialog

```
┌──────────────────────────────────────┐
│  ⚠️ Confirm Payment                  │
│  ────────────────────────────────    │
│                                      │
│  You are about to settle:            │
│                                      │
│  Table: Bar 3                        │
│  Amount: $38.50                      │
│  Method: Cash                        │
│  Change: $11.50                      │
│                                      │
│  This action cannot be undone.       │
│                                      │
│  [ Cancel ]      [ ✅ Confirm ]      │
└──────────────────────────────────────┘
```

---

## Error States Display

| Error Type | Display Location | Visual |
|------------|------------------|--------|
| Network error | Top banner | Red background, retry button |
| Validation error | Near input field | Red text below field |
| Server error | Dialog | Modal with error message |
| Already settled | Full page | "Bill was already paid" |

---

## Accessibility

| Feature | Implementation |
|---------|----------------|
| High contrast totals | Large font, bold, accent color |
| Tab navigation | All actions reachable via Tab |
| Screen reader | ARIA labels on all interactive elements |
| Keyboard shortcuts | Enter = Confirm, Escape = Cancel |
