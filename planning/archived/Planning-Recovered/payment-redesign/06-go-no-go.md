# Execution Authorization: Go / No-Go

## Status: ✅ CONDITIONAL GO (Phase 0 Complete)

## Criteria Review

| Criterion | Status | Notes |
|-----------|--------|-------|
| **Backend owns all totals** | ✅ PASS | `POST /tables/{label}/calculate-split` implemented. Handles item/percentage/amount splits. |
| **UI only sends intent** | ✅ PASS | Calculate endpoint available. UI can request calculations before payment. |
| **Dialog deprecated** | ⏳ PENDING | Part of execution (Phases 1-4). |
| **No Payment Logic in UI** | ✅ READY | Backend now supports all required calculations. `PaymentTransactionResult` returns `ChangeDue`. |

## Authorization Decision
Execution of UI Scaffolding (Phases 1-4) is **AUTHORIZED** pending successful backend verification.

## Phase 0 Deliverables ✅
1.  **`POST /tables/{label}/calculate-split`**: 
    - Accepts `CalculateSplitRequest` (ItemIds, Percentage, or FixedAmount)
    - Returns `CalculateSplitResult` with `AmountToPay`, `SuggestedGratuity`
2.  **`PaymentTransactionResult`**:
    - Wraps `BillLedgerDto`
    - Includes `ChangeDue` (calculated from `AmountTendered`)
    - Includes `RemainingBalance` and status `Message`
3.  **`RegisterPaymentRequestDto` Enhancement**:
    - Added optional `AmountTendered` parameter for cash transactions

## Next Steps
1.  Verify backend compiles and runs
2.  Test calculate-split endpoint with sample data
3.  Proceed to Phase 1 (Payment Hub Page) on success
