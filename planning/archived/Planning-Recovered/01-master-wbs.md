# Master Work Breakdown Structure (WBS) - v1.0

## 1. Project Infrastructure Setup
- **Objective**: Establish the "Golden Path" for the new project.
- **Constraints**: Force strict architecture.

### 1.1. Project Shell Initialization
- **Description**: Create new `MagiDesk.Client` (WinUI 3) project.
- **Inputs**: None.
- **Outputs**: `MagiDesk.Client.csproj` with no `Npgsql` reference.
- **Dependencies**: None.
- **Acceptance Criteria**:
    - Project compiles.
    - `Directory.Build.props` explicitly bans `Npgsql` and `System.Data.SqlClient`.
    - Project references `MagiDesk.Shared`.
    - `App.xaml.cs` is clean (no god-class pattern).

### 1.2. Automated Architecture Enforcer
- **Description**: Add unit test project `MagiDesk.Client.ArchTests` using `NetArchTest.Rules`.
- **Inputs**: Architecture Guardrails Doc.
- **Outputs**: Test suite failing on architecture violations.
- **Dependencies**: 1.1.
- **Acceptance Criteria**:
    - Test: `Frontend_Cannot_Reference_Data_Layer()` passes.
    - Test: `Services_Must_Implement_Interface()` passes.
    - Test: `ViewModels_Must_Not_Depend_On_Concrete_Services()` passes.

### 1.3. API Client Layer Generation
- **Description**: Set up `Refit` to auto-generate clients from Backend Swagger.
- **Inputs**: Backend Swagger JSON (or running local API).
- **Outputs**: `IOrderApi`, `ITableApi`, `IAuthApi` interfaces.
- **Dependencies**: 1.1.
- **Acceptance Criteria**:
    - Clients can invoke a `GetVersion` or `Health` endpoint.
    - No manual `HttpClient` instantiation in consumer code.

## 2. Core Plumbings ("The Steel Thread")
- **Objective**: Authentication and Basic Navigation.

### 2.1. Authentication Service
- **Description**: Implement `AuthService` interacting with Backend Auth.
- **Inputs**: `IAuthApi`.
- **Outputs**: `AuthService`, `UserSession` (in-memory only).
- **Dependencies**: 1.3.
- **Acceptance Criteria**:
    - Login Screen functions.
    - Tokens stored securely (OS storage), NOT in text files.
    - App starts -> Checks Token -> Navigates to Login or Dashboard.

### 2.2. Navigation Shell
- **Description**: Implement `ShellPage` with `NavigationView`.
- **Inputs**: UX Flows.
- **Outputs**: The main frame.
- **Dependencies**: 2.1.
- **Acceptance Criteria**:
    - Navigation is driven by `NavigationService` (EventBus), not Frame manipulation.

## 3. The "Table Map" Feature (Read-Only)
- **Objective**: Display real-time table status.

### 3.1. Table State Management
- **Description**: Implement `TableViewModel` fetching from `ITableApi`.
- **Inputs**: `ITableApi`.
- **Outputs**: `ObservableCollection<TableDto>`.
- **Dependencies**: 1.3.
- **Acceptance Criteria**:
    - UI refreshes from API.
    - No "local table status" cache files.

### 3.2. Table Map UI
- **Description**: Port XAML from legacy `TableMapPage`.
- **Inputs**: Legacy XAML.
- **Outputs**: `TableMapPage.xaml` (Pure View).
- **Dependencies**: 3.1.
- **Acceptance Criteria**:
    - Binds to `TableViewModel`.
    - Clicking a table fires `TableSelected` event.

## 4. The "Order & Billing" Feature (Transactional)
- **Objective**: Replace the `BillingService` SQL logic.

### 4.1. Order Creation Logic
- **Description**: Implement `OrderViewModel` that sends `CreateOrderCommand`.
- **Inputs**: `IOrderApi`.
- **Outputs**: `OrderViewModel`.
- **Dependencies**: 1.3.
- **Acceptance Criteria**:
    - "Add Item" sends API request.
    - UI updates only after API confirms (Simulated Latency check).

### 4.2. Billing Operations
- **Description**: Implement `BillingViewModel` using `IBillingApi`.
- **Inputs**: `IBillingApi`.
- **Outputs**: Billing UI.
- **Dependencies**: 4.1.
- **Acceptance Criteria**:
    - "Split Bill" is a server-side operation.
    - "Print Receipt" requests PDF URL from server (or raw data), does not generate PDF locally.

## 5. Offline Handling (The "No" Feature)
- **Objective**: Define behavior when API is down.
- **Description**: Implement Global Exception Handler for `HttpRequestException`.
- **Acceptance Criteria**:
    - Application shows "Offline / Reconnecting" overlay.
    - Operations are disabled (Read-Only mode).
    - **Explicit Decision**: No local write buffering (per Phase 1 scope).

## 6. Migration & Cutover
- **Objective**: Switch users to new app.

### 6.1. Installer Creation
- **Description**: MSIX package for `MagiDesk.Client`.
- **Inputs**: Project.
- **Outputs**: `.msix`.

### 6.2. User Training
- **Description**: "How to use the new app".
- **Acceptance Criteria**: Users know that "Offline Mode" is now "Read Only".
