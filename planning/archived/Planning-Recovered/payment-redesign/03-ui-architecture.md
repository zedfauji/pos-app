# UI Architecture

## Overview
The architecture follows M-V-VM (Model-View-ViewModel). 
**Strict Rule**: ViewModels must NOT contain business logic (calculating taxes, totals, or change). They acts as a bridge between the View and the Backend API.

## 1. ViewModels

### A. PaymentHubViewModel
*   **Scope**: The dashboard of all active sessions.
*   **Properties**:
    *   `ObservableCollection<SessionCardViewModel> ActiveSessions`
    *   `bool IsLoading`
    *   `string ErrorMessage`
*   **Commands**:
    *   `LoadSessionsCommand`: Fetches from `TablesApi`.
    *   `NavigateToPaymentCommand`: Takes `SessionId`, navigates to `PaymentWorkspacePage`.

### B. PaymentWorkspaceViewModel
*   **Scope**: The main payment interaction screen.
*   **Properties**:
    *   `BillLedgerDto Ledger` (Source of Truth for Right Column).
    *   `ObservableCollection<ItemLine> Items` (For display/selection).
    *   `SplitPaymentViewModel SplitVm` (Sub-VM for split logic).
    *   `PaymentMethod SelectedMethod`
    *   `decimal AmountTendered`
    *   `string PaymentStatusMessage`
    *   `bool IsProcessing`
*   **Commands**:
    *   `RefreshLedgerCommand`: Calls `GET /ledger`.
    *   `ProcessPaymentCommand`:
        *   Gathers data (Method, Amount, Split Details).
        *   Calls `PaymentApi.RegisterPayment`.
        *   Updates `Ledger` from response.
        *   Sets `PaymentStatusMessage` (e.g., "Change Due: $5.00").
    *   `CloseSessionCommand`: Available when `Ledger.Remaining == 0`. Calls `POST /close`.

### C. SplitPaymentViewModel
*   **Scope**: Handles the "Split / Partial" tab inputs.
*   **Properties**:
    *   `SplitMode Mode` (Amount, Item, Percent).
    *   `decimal SplitAmountInput`
    *   `decimal SplitPercentInput`
    *   `ObservableCollection<SelectableItemViewModel> SelectableItems`
    *   `decimal CalculatedTotal` (Result from Backend).
*   **Commands**:
    *   `CalculateSplitCommand`:
        *   Triggers when selection changes or input loses focus.
        *   Payload: `[ItemIds]` or `Percentage`.
        *   Calls `POST /calculate`.
        *   Updates `CalculatedTotal` with backend result.
*   **Events**:
    *   `SplitDefined`: Notifies parent `PaymentWorkspaceViewModel` that a split amount is ready to be paid.

## 2. Event/Message Flow

1.  **User Selects Items** in `SplitPaymentViewModel`.
2.  `SplitPaymentViewModel` fires `CalculateSplitCommand`.
3.  **Backend** returns `$15.50`.
4.  `SplitPaymentViewModel` updates `CalculatedTotal`.
5.  **User Clicks Pay** in Parent VM.
6.  Parent VM sends `$15.50` (and `items` meta if supported) to `RegisterPayment`.
7.  **Backend** processes, returns new Ledger.
8.  Parent VM updates UI.

## 3. Technology Stack
*   **Framework**: WinUI 3 / WPF (implied by current codebase).
*   **DI**: Microsoft.Extensions.DependencyInjection.
*   **Messaging**: `IMesseger` or weak events for navigation.

## 4. Forbidden Patterns (Audit)
*   `var total = items.Sum(x => x.Price)` -> **BANNED**. Use `Calculate` API.
*   `var change = tendered - due` -> **BANNED**. Use Backend response.
*   `ContentDialog` for Payment -> **BANNED**. Use Page navigation.
