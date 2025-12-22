# 04 - Business Logic & Workflow Coverage

## 1. Session Lifecycle
- **Logic**: Start / Append / Stop Session.
- **Owner**: **Backend** (`TablesApi`).
- **Client Role**: Command initiator.
- **Coverage**: 100%.

## 2. Billing Calculations
- **Logic**: Summing items, Applying Tax, Final Total.
- **Owner**: **Backend** (`StopSession` returns final bill).
- **Duplication**: **YES**. `TableViewModel.CloseSessionAsync` calculates a local sum `items.Sum(i => i.price * i.quantity)` to show the payment dialog.
    - *Risk*: Discrepancy if backend applies tax/discounts that client doesn't know about.
    - *Remediation*: Client should call `GET /api/billing/preview` or similar before showing dialog.
- **Coverage**: 90% (Backend is truth, Client is approximation).

## 3. Inventory Adjustments
- **Logic**: Reducing stock on order.
- **Owner**: **Backend** (Implied in `OrderApi`).
- **Coverage**: Assumed Complete (Backend Black Box).

## 4. Reporting Aggregation
- **Logic**: Summarizing day's sales.
- **Owner**: **MISSING**.
- **Coverage**: 0%.

## 5. Error Handling & Recovery
- **Logic**: Retry on network failure.
- **Owner**: **Client** (`Polly` policies implied by Handoff).
- **Coverage**: Good.
- **Note**: "Offline Mode" is Read-Only.

## Summary
The logic migration is **Successfull** for transactional flows.
The logic for **Aggregates (Reporting)** is completely absent.
The logic for **Pricing** has a minor duplication risk in the UI.
