# 03 - Payment Workspace Refinement

## Purpose

Define the detailed UX for payment processing, including split payments, change calculation, tip/discount placement, and confirmation flow.

---

## Page Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  💳 Payment Workspace - Bar 3                    [ ← Back ]     │
├───────────────────────────────────┬─────────────────────────────┤
│                                   │                             │
│  📋 BILL DETAILS                  │   💵 PAYMENT                │
│  (Read-Only)                      │   (Interactive)             │
│                                   │                             │
│  ┌─────────────────────────────┐  │   ┌───────────────────────┐ │
│  │ Server: John D.             │  │   │ Amount Due            │ │
│  │ Started: 7:30 PM            │  │   │ $38.50                │ │
│  │ Ended: 8:15 PM              │  │   │                       │ │
│  │ Duration: 45 min            │  │   │ Split: [Full] [Split] │ │
│  └─────────────────────────────┘  │   └───────────────────────┘ │
│                                   │                             │
│  ┌─────────────────────────────┐  │   ┌───────────────────────┐ │
│  │ ITEMS                       │  │   │ Payment Method        │ │
│  │ 2x Beer           $12.00    │  │   │ [ 💵 Cash ] [ 💳 Card ]│ │
│  │ 1x Nachos          $8.00    │  │   └───────────────────────┘ │
│  │ Pool Time (30m)   $15.00    │  │                             │
│  │ ─────────────────────────── │  │   ┌───────────────────────┐ │
│  │ Subtotal          $35.00    │  │   │ Amount Tendered       │ │
│  │ Tax                $3.50    │  │   │ [          $50.00   ] │ │
│  │ ─────────────────────────── │  │   │                       │ │
│  │ TOTAL             $38.50    │  │   │ Change Due: $11.50    │ │
│  └─────────────────────────────┘  │   └───────────────────────┘ │
│                                   │                             │
│                                   │   ┌───────────────────────┐ │
│                                   │   │ Adjustments           │ │
│                                   │   │ Tip: [      $5.00   ] │ │
│                                   │   │ Discount: [   $0.00 ] │ │
│                                   │   └───────────────────────┘ │
│                                   │                             │
│                                   │   ─────────────────────────  │
│                                   │   FINAL: $43.50             │
│                                   │   [ Cancel ] [ ✅ Confirm ]  │
└───────────────────────────────────┴─────────────────────────────┘
```

---

## Split Payment Options

### Split Type Selector

```
┌─────────────────────────────────────┐
│ Split Payment                        │
│                                     │
│ [ By Amount ] [ By % ] [ By Items ] │
│                                     │
│ ────────────────────────────────    │
│                                     │
│ Current split: $19.25 of $38.50     │
│ Remaining: $19.25                   │
│                                     │
│ Amount for this payment:            │
│ [              $19.25             ] │
│                                     │
└─────────────────────────────────────┘
```

### Split by Amount
- User enters exact dollar amount for this payment
- Backend validates: amount ≤ remaining balance
- System shows remaining after this payment

### Split by Percentage
- User enters percentage (e.g., 50%)
- System calculates: $38.50 × 50% = $19.25
- Useful for "half each" scenarios

### Split by Items
- User selects specific items for this payment
- System sums selected items
- Remaining items tracked separately

---

## Change Due Display

### Cash Payment Flow

```
┌─────────────────────────────────────┐
│ 💵 Cash Payment                     │
│                                     │
│ Amount Due: $38.50                  │
│                                     │
│ Amount Tendered:                    │
│ ┌─────────────────────────────────┐ │
│ │              $50.00             │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Quick amounts: [$40] [$50] [$60]    │
│                                     │
│ ─────────────────────────────────── │
│                                     │
│ CHANGE DUE: $11.50                  │  ← Large, green
│                                     │
└─────────────────────────────────────┘
```

### Validation Rules

| Scenario | Behavior |
|----------|----------|
| Tendered < Due | Show error, disable Confirm |
| Tendered = Due | Show "Exact change" |
| Tendered > Due | Calculate and display change |
| Tendered blank | Default to exact amount |

---

## Tip & Discount Placement

### Adjustments Section

```
┌─────────────────────────────────────┐
│ Adjustments                         │
│ ────────────────────────────────    │
│                                     │
│ Tip:                                │
│ [ $0.00 ] [15%] [18%] [20%] [Custom]│
│                                     │
│ Discount:                           │
│ [ $0.00 ] [10%] [Manager Override]  │
│                                     │
│ ────────────────────────────────    │
│ Subtotal:    $38.50                 │
│ + Tip:        $5.00                 │
│ - Discount:   $0.00                 │
│ ────────────────────────────────    │
│ FINAL TOTAL: $43.50                 │
└─────────────────────────────────────┘
```

### Tip Quick Buttons

| Button | Calculation |
|--------|-------------|
| 15% | Subtotal × 0.15 |
| 18% | Subtotal × 0.18 |
| 20% | Subtotal × 0.20 |
| Custom | Open input field |

### Discount Application

| Type | Behavior |
|------|----------|
| % Discount | Subtotal × discount% |
| $ Discount | Flat amount off |
| Manager Override | Requires manager PIN |

---

## Confirmation UX

### Pre-Confirmation Summary

Before clicking Confirm, show clear summary:

```
┌─────────────────────────────────────┐
│ Payment Summary                     │
│ ────────────────────────────────    │
│                                     │
│ Bill Total:     $38.50              │
│ Tip:            + $5.00             │
│ Discount:       - $0.00             │
│ ────────────────────────────────    │
│ Amount to Pay:  $43.50              │
│                                     │
│ Method:         💵 Cash             │
│ Tendered:       $50.00              │
│ Change:         $6.50               │
│                                     │
│ [ Cancel ]      [ ✅ Confirm $43.50 ]│
└─────────────────────────────────────┘
```

### Confirmation Dialog

```
┌─────────────────────────────────────┐
│ ⚠️ Confirm Settlement               │
│                                     │
│ You are about to process:           │
│                                     │
│   Table: Bar 3                      │
│   Amount: $43.50                    │
│   Method: Cash                      │
│                                     │
│ ⚠️ This action cannot be undone.    │
│                                     │
│ [ Cancel ]      [ ✅ Process ]       │
└─────────────────────────────────────┘
```

### Post-Settlement

```
┌─────────────────────────────────────┐
│ ✅ Payment Complete                  │
│                                     │
│ Bar 3 - SETTLED                     │
│                                     │
│ Amount: $43.50                      │
│ Change: $6.50                       │
│                                     │
│ Receipt: [Print] [Email]            │
│                                     │
│ [ Return to Payment Hub ]           │
└─────────────────────────────────────┘
```

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Escape | Cancel / Back |
| Enter | Confirm (if valid) |
| Tab | Navigate fields |
| 1 | 15% tip |
| 2 | 18% tip |
| 3 | 20% tip |
| C | Focus cash input |
