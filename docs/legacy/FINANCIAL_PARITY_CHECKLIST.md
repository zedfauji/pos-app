# Financial Parity Verification Checklist
**Status**: ACTIVE
**Purpose**: Mandatory validation for all financial logic changes.

## 1. Core Calculations
- [ ] **Order Total**
    - [ ] `Sum(Item.Price * Quantity) == Subtotal`
    - [ ] `Subtotal - Discount + Tax == Total`
    - [ ] Verify with 3 decimal places, round to 2 for display? No, **Round at line item** or **Round at total**? *Legacy Rule: Round at Line Item.*
- [ ] **Tax Math**
    - [ ] Apply Tax Rate (e.g. 10%) to `(Subtotal - Discount)`.
    - [ ] Verify rounding behavior (HalfEven).

## 2. Split Payments
- [ ] **Summation**
    - [ ] `Sum(SplitAmount) == TotalDue`.
    - [ ] If `Sum != Total`, is the remainder handled?
- [ ] **Penny Allocation**
    - [ ] Create a bill for `$10.00` split 3 ways. (`3.33`, `3.33`, `3.33` -> Total `9.99`).
    - [ ] Verify system allocates extra penny to one share (`3.34`).
    - [ ] Verify `TotalPaid` equals `$10.00` exactly.

## 3. Discounts & Vouchers
- [ ] **Percentage**
    - [ ] Apply 10% to `$15.55`. Result `$1.555`.
    - [ ] Legacy behavior: Round up or down? *Legacy: Standard Rounding ($1.56)*.
- [ ] **Stacking**
    - [ ] Apply 10% + $5.00 off.
    - [ ] Verify order of operations matches Legacy (Percentage then Fixed? or Fixed then Percentage?). *Legacy Spec: Logic unclear, need to verify.*

## 4. Refunds & Voids
- [ ] **Partial Refund**
    - [ ] Refund $5.00 of a $20.00 payment.
    - [ ] Verify `Ledger` shows `Paid: $20.00`, `Refunded: $5.00`, `Net: $15.00`.
    - [ ] Verify Audit Log contains `PAYMENT_REFUNDED` event.
- [ ] **Full Void**
    - [ ] Void a transition.
    - [ ] Verify `Net == 0`.
    - [ ] Verify Inventory is restored (if applicable).

## 5. End of Day
- [ ] **Reconciliation**
    - [ ] `Sum(Cash Payments) == Cash in Drawer`.
    - [ ] `Sum(Card Payments) == Terminal Batch Total`.
    - [ ] Verify "Shift Crossing" logic (Payment belongs to Shift Time, not Order Time).

---
**Sign-off Criteria**:
All checks must pass explicitly. Discrepancies of $0.01 are **FAILURES**.
