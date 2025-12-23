# 01 - Execution Phases (Final Mile)

## Strategy
**"Risk First, Complexity Second"**
We tackle **Reporting** (Phases 1-2) first because it is the primary blocker for legacy retirement and carries the highest data integrity risk. 
We tackle **Split Bill** (Phase 3) later as it is an operational refinement. 
Each phase is designed to be **atomic**: failure in one does not compromise the others.

---

## Phase 1: The "Truth" Layer (Reporting Backend)
**Objective**: Establish a trusted source of truth for financial data.
- **Included Items**: 
    - [P0] Implement `ReportingApi` (Backend).
    - [P0] Implement `Z-Report` Aggregation Logic (Core).
- **Entry Criteria**: Authorization received.
- **Exit Criteria**: `GET /api/reporting/z-report` returns correct JSON for a simulated day.
- **Rollback**: None (New code only).
- **Validation**:
    - Integration Test: Generate 5 orders -> Call Report -> Verify Sum.

## Phase 2: The "Manager's View" (Reporting UI & Print)
**Objective**: Deliver the Z-Report to the physical world.
- **Included Items**: 
    - [P0] Implement `Z-Report` UI (Button/Dialog).
    - [P1] Wire up `Z-Report` Printing.
- **Entry Criteria**: Phase 1 Complete & Verified.
- **Exit Criteria**: Clicking "Day Close" prints a correct slip.
- **Rollback**: Revert Client changes (git revert).
- **Validation**:
    - Manual: Print to "Virtual Printer" (Log) and verify formatting.

## Phase 3: The "Split" Refinement (Bill Stability)
**Objective**: Ensure complex payment flows do not crash or corrupt data.
- **Included Items**: 
    - [P0] Verify/Fix `Split Bill` UI.
    - [P1] Add `GetBillPreview` endpoint (Safe Pre-calc).
- **Entry Criteria**: Phase 2 Complete (Reporting acts as safety net to detect errors).
- **Exit Criteria**: Split Bill operation results in 2 valid orders that sum to original total.
- **Rollback**: Revert to current "Partial" state.
- **Validation**:
    - Manual: Split a $50 order into $25/$25. Pay both. Verify Z-Report shows $50.

## Phase 4: Final Verification & Cutover
**Objective**: Prove readiness to stakeholders.
- **Included Items**: 
    - [P1] Physical Printer Verification.
    - [P2] Staff Training Material.
    - [P0] Legacy Retirement Check.
- **Entry Criteria**: Phases 1-3 Stable.
- **Exit Criteria**: **GO/NO-GO Decision** for Production.
