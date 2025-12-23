# Implementation Phases

## Phase 0: Backend Prerequisities
*   **Goal**: Enable logic-free UI by exposing calculation endpoints.
*   **Tasks**:
    1.  Implement `POST /api/payments/calculate` in `PaymentApi`.
    2.  Update `RegisterPayment` response to include `ChangeDue` context.
    3.  Verify `TablesApi` and `PaymentApi` integration for Item retrieval.
*   **Validation**: `test-payment-flow.ps1` verifies new endpoints.

## Phase 1: Payment Hub (Read-Only)
*   **Goal**: Visual dashboard of sessions.
*   **Tasks**:
    1.  Create `PaymentHubPage.xaml` and `PaymentHubViewModel.cs`.
    2.  Implement `LoadSessionsCommand` fetching from `TablesApi`.
    3.  Add Navigation entry in `ShellPage` (or Main Menu).
*   **Exit Criteria**: User can see all active tables and their totals.

## Phase 2: Payment Workspace (Full Payment)
*   **Goal**: Replace "Stop Session" dialog with a Page for full payments.
*   **Tasks**:
    1.  Create `PaymentWorkspacePage.xaml` and `PaymentWorkspaceViewModel`.
    2.  Implement "Bill View" (Right Column) fetching `BillLedger`.
    3.  Implement "Full Payment" Action (Left Column).
    4.  Wire `ProcessPaymentCommand` to `PaymentApi`.
    5.  Implement `Close` button for 0-balance sessions.
*   **Exit Criteria**: User can pay a full bill and close the table via the Page.

## Phase 3: Split & Partial Payments
*   **Goal**: Enable granular payment control.
*   **Tasks**:
    1.  Implement `SplitPaymentViewModel`.
    2.  Wire `Split by Amount` (Manual entry).
    3.  Wire `Split by Item` (Select -> API Calculate -> Pay).
    4.  Wire `Split by %` (Input -> API Calculate -> Pay).
*   **Exit Criteria**: User can pay $10 of a $50 bill, then pay the rest.

## Phase 4: Deprecation & Clean Up
*   **Goal**: Remove legacy artifacts.
*   **Tasks**:
    1.  Locate all `PaymentDialog` usages.
    2.  Replace with Navigation to `PaymentHubPage` or `PaymentWorkspacePage`.
    3.  Delete `PaymentDialog.xaml` and `PaymentDialog.xaml.cs`.
    4.  Verify `SessionsController.StopSession` is no longer called by UI.
*   **Exit Criteria**: No modal dialogs for payments. Codebase clean.

## Phase 5: Verification
*   **Tasks**:
    1.  Run `05-failure-and-edge-cases.md` scenarios.
    2.  Simulate network failure.
    3.  Verify Change Due display.
