# Split Payments

Complete documentation for the split payment feature in MagiDesk POS.

## Overview

The split payment feature allows customers to pay a single bill using multiple payment methods, with support for equal splits, percentage-based splits, amount-based splits, and item-level splits.

## Current Implementation

### Basic Features ✅

- Manual amount entry for Cash, Card, and Digital/Mobile
- Equal split (divides total by 3)
- Validation that split total equals order total
- Proportional tip and discount distribution
- Basic UI with 3 fixed payment methods

## Recommended Features

### 1. Split Methods

#### A. Equal Split
- **"Split Evenly"** button automatically divides total by number of payment methods
- **"Split by People"** option: Enter number of people, automatically calculates per-person amount
- Visual indicator showing per-person amount

#### B. Percentage-Based Split
- Enter percentages for each payment method (e.g., 50% Cash, 30% Card, 20% Mobile)
- Auto-calculate amounts from percentages
- Toggle between percentage and dollar amount input

#### C. Amount-Based Split (Current - Enhanced)
- Manual entry for each method (current)
- **"Auto-Fill Remaining"** button: Fill remaining amount in selected method
- **"Distribute Remaining"** button: Distribute leftover amount proportionally

#### D. Item-Level Split
- Split by individual menu items
- Each person/card pays for specific items
- Useful for groups where people order separately
- Visual item assignment interface

### 2. Payment Method Management

#### Dynamic Payment Methods
- Add/Remove payment methods dynamically
- Support for:
  - Cash
  - Credit/Debit Card
  - Mobile Payment (Apple Pay, Google Pay, etc.)
  - Gift Card
  - Store Credit
  - Bank Transfer
  - Cryptocurrency (if applicable)
- Custom payment method names

#### Payment Method Presets
- Quick buttons for common splits:
  - **50/50** (two methods)
  - **60/40** (two methods)
  - **Equal 3-way**
  - **Equal 4-way**
  - **Custom preset** (save frequently used splits)

### 3. Visual Feedback & Validation

#### Real-Time Balance Display
- **Remaining Balance** indicator (green when balanced, red when over/under)
- **Progress bar** showing how much of the total is covered
- **Color-coded status**:
  - 🟢 Green: Fully balanced
  - 🟡 Yellow: Close to balanced (±$0.10)
  - 🔴 Red: Not balanced

#### Smart Validation
- **Auto-adjust** option: Automatically adjust last payment method to balance
- **Rounding helper**: Suggest rounding to nearest dollar/cent
- **Warning indicators** for common mistakes:
  - "Split total exceeds order total"
  - "Split total is less than order total"
  - "One or more amounts are zero"

### 4. Tip & Discount Handling

#### Tip Distribution Options
- **Split tip equally** across all payment methods
- **Tip on each method** separately
- **Tip on one method only** (e.g., tip on card only)
- **No tip** option
- **Custom tip distribution** (manual entry per method)

#### Discount Distribution
- **Apply discount proportionally** (current)
- **Apply discount to specific method** (e.g., discount on cash only)
- **Split discount equally**
- **Discount preview** showing savings per method

### 5. Customer Assignment (Optional)

#### Assign Payments to Customers
- Link each split portion to a customer profile
- Track who paid what for loyalty/rewards
- Generate individual receipts per customer
- Useful for corporate accounts or regular customers

### 6. Receipt & Reporting

#### Split Receipt Generation
- **Master receipt** showing total split
- **Individual receipts** per payment method
- **Breakdown receipt** showing:
  - Items paid by each method
  - Tip per method
  - Discount per method
  - Total per method

#### Payment History
- Track split payments in payment history
- Filter by payment method
- Analytics: Most common split patterns

## Implementation Priority

### Phase 1: Essential Features (MVP)
1. ✅ Equal split by number of people
2. ✅ Percentage-based split
3. ✅ Auto-fill remaining amount
4. ✅ Visual balance indicator
5. ✅ Enhanced validation with warnings
6. ✅ Tip distribution options

### Phase 2: Enhanced Features
1. Item-level splitting
2. Dynamic payment methods
3. Split templates
4. Customer assignment
5. Enhanced receipt generation

### Phase 3: Advanced Features
1. Split by table section
2. Partial payment support
3. Payment gateway integration
4. Advanced analytics

## API Integration

### PaymentApi Endpoint

**POST** `/api/payments`
```json
{
  "sessionId": "uuid",
  "billingId": "uuid",
  "totalDue": 100.00,
  "lines": [
    {
      "amountPaid": 60.00,
      "paymentMethod": "card",
      "discountAmount": 0.00,
      "tipAmount": 3.00
    },
    {
      "amountPaid": 40.00,
      "paymentMethod": "cash",
      "discountAmount": 0.00,
      "tipAmount": 2.00
    }
  ],
  "serverId": "user-id"
}
```

## UI/UX Recommendations

### Layout
```
┌─────────────────────────────────────────┐
│  Split Payment                          │
├─────────────────────────────────────────┤
│  Split Method:                          │
│  ○ Equal  ○ Percentage  ○ Amount       │
│  ○ By People: [2] people               │
│                                         │
│  Payment Methods:                       │
│  ┌─────────┬─────────┬─────────┐      │
│  │ Cash    │ Card    │ Mobile  │      │
│  │ $50.00  │ $30.00  │ $20.00  │      │
│  │ [50%]   │ [30%]   │ [20%]   │      │
│  └─────────┴─────────┴─────────┘      │
│                                         │
│  Balance: $100.00 / $100.00 ✅         │
│  [Auto-Fill Remaining]                 │
│                                         │
│  Tip Distribution:                      │
│  ○ Split equally  ○ On card only       │
│                                         │
│  [Process Split Payment]                │
└─────────────────────────────────────────┘
```

## Technical Considerations

1. **Rounding**: Handle penny differences gracefully
2. **Validation**: Real-time validation with helpful error messages
3. **State Management**: Save split configuration if user navigates away
4. **Performance**: Fast calculations for large splits
5. **Accessibility**: Screen reader support, keyboard navigation

## Related Documentation

- [Payment Processing](./payments.md)
- [PaymentApi Reference](../backend/payment-api.md)
