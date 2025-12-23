# WBS Refinement - Iteration 1 (Boundary Analysis)

## 1. Critique of v1.0
- **Gap Identified**: "API Client Layer" (1.3) assumes Swagger exists but doesn't handle the *auth header injection* mechanism.
- **Gap Identified**: "Navigation Shell" (2.2) is too vague. It needs to handle *ViewModel-based navigation*, not just Page navigation.
- **Gap Identified**: "Table Map" (3.1) mentions "Real-time" but doesn't specify *Polling* vs *SignalR*. Given the "Thin Client" constraint and existing Backend, we must assume Polling for now unless SignalR is confirmed.
- **Hidden Coupling**: `AuthService` (2.1) needs to persist the token. If we use `LocalSettings`, we couple to UWP/WinUI storage.

## 2. Refined WBS Tasks (Delta)

### 1.3. API Client Layer (Refined)
- **1.3.1. Refit Configuration**
    - **Action**: Install `Refit.HttpClientFactory`.
    - **Constraint**: Register `AuthHeaderHandler` that reads from `AuthService`.
- **1.3.2. Interface Definition**
    - **Action**: Extract `DTOs` from `MagiDesk.Shared`.
    - **Constraint**: Interfaces must return `Task<IApiResponse<T>>` to handle non-200 safely.

### 2.2. Navigation Infrastructure (Refined)
- **2.2.1. View-ViewModel Registration**
    - **Action**: Create a convention-based mapper (e.g., `NavService.NavigateTo<OrderViewModel>()`).
    - **Constraint**: No `Frame.Navigate(typeof(Page))` in ViewModels. ViewModels must be agnostic of Views.

### 4.2. Billing Operations (Refined)
- **4.2.1. Printing Abstraction**
    - **Risk**: "Print Receipt" in v1.0 implies the server does it.
    - **Correction**: If the printer is attached to the *Client*, the Client must receive *RAW ESC/POS DATA* from the backend, not generate PDF.
    - **New Task**: Implement `RawPrinterService` that accepts Bytes from API and sends to USB/Network Printer.

## 3. Architecture Drift Risk
- **Risk**: Developers might try to put "Printing Logic" (formatting the receipt) in the Client because "it's easier".
- **Mitigation**: Add Guardrail: `ReceiptBuilder` class is BANNED. Client only blindly pipes bytes.
