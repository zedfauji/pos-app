# Fix Execution Order & Detailed Plans

**Strategy:** Bottom-Up Recovery.
**Priority:** Data Integrity > Financial Safety > API Contract > Client UX.

---

## 📅 Execution Schedule

| Order | Gap ID | Component | Focus | Rationale |
|-------|--------|-----------|-------|-----------|
| **1** | **GAP-03** | Database Schema | Data Integrity | Cannot save shifts without tables. Foundation for GAP-01. |
| **2** | **GAP-01** | Shift Controller | Domain Logic | Establishes the "Shift" concept in the backend. |
| **3** | **GAP-02** | Financial Gating | Financial Safety | Enforces "No Shift = No Operations". Critical security. |
| **4** | **GAP-07** | End Session Endpoint | API Completeness | Required for non-financial session closure. |
| **5** | **GAP-06** | Client API (ITableApi) | Type Safety | Aligns client with backend reality. Prerequisite for VM. |
| **6** | **GAP-05** | Payment ViewModel | Business Logic | Handles double/decimal precision logic. |
| **7** | **GAP-04** | Payment Page | UI / UX | Visual layer. Depends on VM and API. |

---

## 🛠️ Detailed Fix Plans

### 1. GAP-03: Shift Database Schema
- **Files**: `solution/backend/shift-migration.sql`, `TablesApi/Services/DatabaseInitializer.cs`
- **Change**: Execute the recovered migration script and ensure `DatabaseInitializer` validates its presence.
- **Clean Arch**: DB infrastructure concern only. No domain logic leakage.
- **Preconditions**: Postgres database accessible. `ord` schema exists.
- **Postconditions**: `public.shifts`, `bills`, `payments` tables exist. `shift_id` columns added to existing tables.
- **Rollback**: Delete created tables/columns (Manual SQL).

### 2. GAP-01: Shift Controller Service & API
- **Files**: `ShiftsController.cs`, `ShiftService.cs`, `IShiftRepository.cs`, `ShiftRepository.cs`, `Program.cs`
- **Change**: Scaffold and implement the Shift backend vertical (Controller -> Service -> Repo).
- **Clean Arch**: Strict separation. Controller (API) -> Service (Application) -> Repository (Infrastructure).
- **Preconditions**: GAP-03 complete (DB tables exist).
- **Postconditions**: Endpoints `GET /shifts/current`, `POST /shifts/open`, `POST /shifts/close` are available.
- **Rollback**: Delete new files. Remove DI registration.

### 3. GAP-02: Backend Financial Gating
- **Files**: `RequireOpenShiftAttribute.cs`, `SessionsController.cs`
- **Change**: Implement `RequireOpenShiftAttribute` filter and apply to `StartSession`, `PostOrder`, `StopSession`.
- **Clean Arch**: AOP (Aspect Oriented Programming) via ActionFilters. Does not clutter business logic.
- **Preconditions**: GAP-01 complete (Service needed to check shift status).
- **Postconditions**: Requests to gated endpoints return `423 Locked` if no shift is open.
- **Rollback**: Remove attribute from Controller methods.

### 4. GAP-07: Operational "End Session" Endpoint
- **Files**: `SessionsController.cs`, `ITableRepository.cs`, `TableRepository.cs`
- **Change**: Add `POST /tables/{id}/end` to close sessions without payment (create "unsettled" bill).
- **Clean Arch**: Pure operational logic. Distinct from `StopSession` (financial).
- **Preconditions**: None (uses existing tables).
- **Postconditions**: Can close a session and see it appear as "Unsettled" in DB.
- **Rollback**: Remove method.

### 5. GAP-06: Strict Typed Client API (ITableApi)
- **Files**: `ITableApi.cs`
- **Change**: Refactor interface to use `List<SessionOverview>` and correct routes.
- **Clean Arch**: Interface Definition Language (IDL) for client. Encapsulates HTTP implementation.
- **Preconditions**: Backend endpoints exist (GAP-07 for completeness, though checks can be mocked).
- **Postconditions**: Compile errors in Client due to signature changes (fixing these is part of this step).
- **Rollback**: Revert file.

### 6. GAP-05: Payment Workspace ViewModel
- **Files**: `PaymentWorkspaceViewModel.cs`, `PaymentService.cs` (if needed)
- **Change**: Implement ViewModel with `double` properties for UI binding and `decimal` casting for DTOs.
- **Clean Arch**: MVVM. View logic isolated from API calls.
- **Preconditions**: GAP-06 complete (API contract defined).
- **Postconditions**: ViewModel can be instantiated and calculates totals correctly.
- **Rollback**: Delete file.

### 7. GAP-04: Payment Workspace Page
- **Files**: `PaymentWorkspacePage.xaml`, `PaymentWorkspacePage.xaml.cs`, `App.xaml` (Converters)
- **Change**: Create XAML layout for Split Payments and bind to ViewModel.
- **Clean Arch**: View layer only. No business logic in code-behind.
- **Preconditions**: GAP-05 complete.
- **Postconditions**: App navigates to Payment Workspace instead of legacy dialog.
- **Rollback**: Delete file. Revert navigation changes.
