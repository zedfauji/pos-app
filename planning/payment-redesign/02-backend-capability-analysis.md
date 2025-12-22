# Domain & API Gap Analysis

## 1. Existing Capabilities (As-Is)

### TablesApi (`SessionsController`)
*   `POST /tables/{label}/start`: Start Session.
*   `POST /tables/{label}/order`: Add Items.
*   `GET /tables/{label}/items`: Get Items (returns `List<ItemLine>`).
*   `POST /tables/{sessionId}/stop`: **Legacy Monolithic Payment**. Handles Payment + Close in one go.
    *   *Issue*: Does not support partial payments or splits naturally (though it allows "AmountTendered", it expects to close).

### PaymentApi (`PaymentsController`)
*   `POST /api/payments`: Register a payment.
    *   Supports `RegisterPaymentRequestDto` with `List<RegisterPaymentLineDto>`.
    *   Allows partial payments (cumulative).
*   `GET /api/payments/{billingId}/ledger`: Get totals (Due, Paid, Tip, etc.).
*   `POST /api/payments/{billingId}/close`: Close the bill.

## 2. Gap Analysis

### A. Calculation Logic (CRITICAL)
*   **Requirement**: "No calculations in UI".
*   **Gap**: UI presently calculates "Change Due" and "Remaining".
*   **Gap**: No endpoint to calculate "Split by Item" total. If user selects 3 items, UI cannot sum up their prices.
*   **Solution**: Introduce `POST /api/payments/calculate`.
    *   **Input**: `CalculateSplitRequest { List<Guid> ItemIds, decimal? Percentage, decimal? FixedAmount }`
    *   **Output**: `CalculatedSplitResult { decimal TotalToPay, decimal TaxShare, decimal SuggestedGratuity }`

### B. Item-Level Payment Tracking
*   **Requirement**: "Split by Item".
*   **Gap**: `GET /tables/{label}/items` returns items, but `BillLedger` only tracks global `TotalPaid`. We don't know *which* items are paid.
*   **Gap**: If user pays for "Burger", next user shouldn't see "Burger" as payable.
*   **Solution (MVP)**:
    *   *Option 1 (Full)*: Add `PaymentId` to `OrderItems` table or a join table.
    *   *Option 2 (Calculator Only)*: "Split by Item" is just a convenience to sum amounts. It doesn't lock the item.
    *   *Decision*: For "Redesign" prioritizing "Clean Architecture", we will assume **Option 2** for Phase 1 unless backend table changes are authorized. However, providing a `Calculate` endpoint is mandatory.
    *   *Refinement*: The UI will fetch items. To pay specifically for them, we send their IDs to `Calculate`. The backend sums them up.

### C. Change Due
*   **Requirement**: Backend owns totals.
*   **Gap**: `RegisterPayment` returns `BillLedgerDto`. It has `TotalDue` and `TotalPaid`.
*   **Gap**: It does not explicitly return "Change Due" for the *current* transaction context (e.g. if I hand $50 for a $20 split).
*   **Solution**: `RegisterPayment` response should include `TransactionResult { decimal ChangeDue }` or similar, OR UI derives `Change = Tendered - Allocated`.
    *   *Strict interpretation*: If UI derives `Tendered - Allocated`, that is a calculation.
    *   *Recommendation*: Backend should return `ChangeDue` in the response of `RegisterPayment`.

### D. Preview Bill
*   **Requirement**: "Preview bill".
*   **Gap**: `GetReceiptDataAsync` exists (`GET /api/payments/{billingId}/receipt`). Checks out.

## 3. Required New Artifacts (API)

### New Endpoints
1.  **`POST /api/payments/calculate`**
    *   Calculates totals for a proposed split (Items or %).
2.  **`POST /api/payments/preview-change`** (Optional, or part of Calculate)
    *   Calculates change before committing? Or just return it after commit.

### DTO Updates
*   **`RegisterPaymentResultDto`**:
    *   Extends `BillLedgerDto`? Or wraps it.
    *   `decimal ChangeDue`
    *   `string Message` ("Payment Successful", "Partial Payment Accepted").

## 4. Summary of Work
1.  Create `Calculate` endpoint in `PaymentApi`.
2.  Update `RegisterPayment` to return `ChangeDue`.
3.  Deprecate `SessionsController.StopSession` (use `PaymentApi` from UI).
