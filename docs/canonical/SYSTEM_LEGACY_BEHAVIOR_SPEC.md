# System Legacy Behavior Specification

This document defines the expected behaviors of the legacy system to ensure absolute parity in the new implementation. It is derived from a deep analysis of the `Do-Not-Edit-legacy-frontend` codebase.

## 1. Authentication & Session

### 1.1 Login Flow (`LoginPage.xaml.cs`)
- **Inputs**: Username (Text), Password (PasswordBox).
- **Validation**:
  - Both fields must be non-empty.
  - **Critical**: Password must consist of **digits only**.
- **Process**:
  - Calls `UsersApi.LoginAsync` with username and password.
  - On 200 OK: Returns `UserDto` (Id, Username, Role, LastLogin).
  - On Failure: generic "Invalid credentials" or exception message.
- **Session**:
  - Stores session in `SessionService.Current`.
  - Persists `SessionDto` locally.
  - Navigation: automatic redirect to `MainPage` upon successful login or if valid session exists on load.

## 2. Table Management (`TablesPage.xaml.cs`)

### 2.1 Table Types & Visualization
- **Types**: `Billiard` and `Bar`.
- **State**:
  - `Occupied` boolean.
  - `StartTime`: `DateTimeOffset`.
  - `OrderId`: `long?`.
  - `Server`: Name of the server.
- **Polling**:
  - Polls `TableRepository.GetAllAsync` every 5 seconds (toggleable).
  - Reconciles API state with local state (timers).

### 2.2 Timers & Thresholds
- **Billiard Logic**:
  - Tracks duration since `StartTime`.
  - **Thresholds**:
    - Users can set a duration threshold (e.g., 60 mins) per table.
    - **Alerts**: Visual indicators (color changes? - *Implied by logic, need to confirm visual implementation if rigorous UI parity is needed, but functional parity is priority*).
  - **Persistence**: Thresholds and Timers cached in `MagiDesk/thresholds.json` and `timers.json` for crash recovery.

### 2.3 Operations
- **Move Table**:
  - Allows moving a session from one table to another *available* table.
  - API: `MoveSessionAsync(sourceLabel, targetLabel)`.
- **Diagnostics**:
  - Extensive client-side diagnostics (`DiagnoseTableIssuesAsync`) check:
    - Local vs API state consistency.
    - Active Session existence.
    - Open Orders check.
    - **Auto-Recovery**: Offers to "Repair" session state (e.g., recreate missing session for occupied table).

## 3. Order Entry (`MenuSelectionPage.xaml.cs`)

### 3.1 Setup
- **Preconditions**: Must select a **Server** from a hardcoded/loaded list before adding items.
- **Context**: Binds to a `TableLabel` and `SessionId`.

### 3.2 Item Selection
- **Catalog**: Loads via `MenuService.ListItemsAsync`.
- **Modifiers**:
  - **Structure**: Nested in `MenuItemViewModel`.
  - **Constraints**:
    - `IsRequired`: Must select at least one option.
    - `AllowMultiple`: Single vs Multi-select checkboxes.
    - `MaxSelections`: Limits number of options.
  - **Validation**: Prevents adding item if required modifiers are missing.
- **Quantity**: +/- buttons per item.

### 3.3 Cart & Submission
- **Cart**: List of `SelectedItemViewModel`.
- **Submission**:
  - **New Order**: If `OrderId` is null, calls `CreateOrderAsync` with `SessionId`, `TableLabel`, `Server`, `Items`.
  - **Existing Order**: If `OrderId` exists, calls `AddItemsAsync` to append.
- **Post-Action**: Navigates back to `TablesPage`.

## 4. Payments (`EphemeralPaymentPage.xaml.cs`)

### 4.1 Bill Context
- Loads Bill via `NavigationParameter`.
- **Fallback Loading**:
  - If Bill object has no items, attempts to fetch items via `BillingService.GetOrderItemsByBillingIdAsync`.
  - If that fails, creates a single "Placeholder" item with the total amount.

### 4.2 Split Logic (Complex)
- **Modes**:
  1.  **Amount**: Manual entry for Cash/Card/Digital.
  2.  **Percentage**: Sliders/Input for % split across methods.
  3.  **People**: Divides total by N people. Round-robins remainder to Cash.
  4.  **Items**: Assigns specific order items to specific payment methods (Cash/Card/Digital).
- **Auto-Fill**: Button to assign remaining balance to a specific method.
- **Balance Indicator**: Visual feedback (Green/Yellow/Red) for Over/Short/Balanced states.

### 4.3 Tips & Discounts
- **Tips**: Fixed amounts or percentages.
- **Discounts**: Amount + textual Reason.

### 4.4 Execution
- Submits `PaymentRequestDto` to `PaymentApiService`.
- **Receipts**: Triggers `ReceiptService` logic (via `MainPage` initialization).

## 5. Cash Flow / Shift (`CashFlowPage.xaml.cs`)

### 5.1 Functionality
- **Nature**: A simple log, not a strict "Shift Control" with gating.
- **Entries**:
  - Employee Name (defaults to current user).
  - Date (must be current week).
  - Amount (Cash).
  - Notes.
- **Metrics**: Displays Weekly Total, Today's Entries count, Daily Average.
- **Export**: CSV export of local history.

## 6. Parity Gaps Identified (Preliminary)
- **Strict Shift Gating**: The new system's `ShiftController` implies strict open/close shifts. The legacy system is loose (simple log). *Risk*: New system might block operations if shift is "closed", which legacy users don't expect.
- **Item Assignment Split**: The new system must support "Split by Items" which is a complex UI/Logic feature in legacy.
- **Diagnostics**: The legacy system has a heavy "Self-Repair" logic for table sessions. The new system needs to match this resilience.
