# System Parity Gap Matrix

This document tracks identified behavioral discrepancies between the Legacy System and the New System, along with the plan to resolve them to achieve absolute parity.

| # | Feature Area | Legacy Behavior | New System Behavior | Impact | Resolution Plan |
|---|---|---|---|---|---|
| 1 | **Shift Control** | **Loose / Implicit**. Users log in and immediately take orders. "Cash Flow" is just a log. | **Strict Gating**. `[RequireOpenShift]` blocks `CreateOrder` (423 Locked) if no shift is open. | **Critical Blocker**. Users cannot create orders after login pattern. | **Implement Auto-Shift**. Modify `RequireOpenShiftAttribute` to automatically open a "System Shift" or "User Shift" if none exists, mimicking legacy permissiveness. |
| 2 | **Payment Split** | Supports **"Split by Items"** (assigning specific items to payers). | `PaymentHub` core supports split amounts, but "Item Assignment" UI/Logic needs verification. | **Major Functionality Gap**. Waitstaff cannot split bills by what people ate. | **Port Logic**. Verify `PaymentHub` API supports item-level splits and ensure UI exposes it strictly matching legacy flow. |
| 3 | **Table Resilience** | **Aggressive Self-Repair**. `DiagnoseTableIssuesAsync` checks for desync and offers "Repair" (auto-recreate sessions). | Backend relies on data integrity. Frontend resilience is unknown/lighter. | **Operational Risk**. If network flakes, tables might get stuck "Occupied" without session. | **Port Diagnostics**. Implement the legacy diagnostic/repair logic in the new `TablesPage` (or equivalent). |
| 4 | **Timer Persistence** | Caches billiard timers in `localApplicationData/timers.json` for crash recovery. | Likely relies on Backend `start_time`. | **Minor UX Gap**. If offline/crash, might lose precise local timer display (though backend has truth). | **Adopt Caching**. Ensure `TablesPage` in new system respects/implements local timer caching for redundancy. |
| 5 | **Login Validation** | Password must be **digits only**. | Standard auth (likely permits complex passwords). | **Parity Deviation**. Users accustomed to numeric passcodes might be confused if validation changes. | **Enforce Numeric**. Ensure frontend login validation restricts password to digits if legacy users require it (or support both but warn). *Decision: Support both, but valid legacy passcodes must work.* |
| 6 | **ID Types** | Uses `varchar` (Legacy) for User IDs. | New system focuses on `UUID`. | **Migration Risk**. | **Hybrid Support**. Confirmed in Phase B (ID Policy): `varchar` allowed for Users. |

## detailed Resolution Strategy

### 1. Auto-Shift Implementation
- **Target**: `MagiDesk.Core` & `OrderApi` / `TablesApi`.
- **Logic**:
  - In `RequireOpenShiftAttribute`:
  - If `GetCurrentOpenShiftAsync()` returns null:
  - Call `ShiftService.OpenSystemShiftAsync()` (new method).
  - This method creates a shift for the current user (or system user) with 0 starting cash, logged as "Auto-Open".
  - Proceed with request.

### 2. Payment Split Parity
- **Target**: `PaymentApi` & Frontend Payment Dialog.
- **Verification**: Check `PaymentRequestBody` for `ItemAssignments`.
- **Action**: If missing, implement `PaymentItemAssignment` table/logic.

### 3. Diagnostics Porting
- **Target**: New `TablesPage` (WinUI).
- **Action**: Copy-paste relevant sections of `DiagnoseTableIssuesAsync` into a new `TableDiagnosticsService` and expose via UI context menu.
