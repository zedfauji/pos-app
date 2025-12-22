# Audit Phase 2: Business Logic Leakage

## 1. Executive Summary

A "Leakage" occurs when the UI layer (ViewModel/View/Frontend Service) takes responsibility for logic that belongs in the Domain or Backend.
**Severity: CRITICAL**. The Frontend contains a nearly complete copy of the Backend's persistence logic for core features (Tables, Billing, Payment).

## 2. Findings & Classification

| File Path | Component | Leaked Logic Category | Details | Migration Difficulty |
|-----------|-----------|------------------------|---------|----------------------|
| `Services/BillingService.cs` | Service | **Data Persistence & Transaction Management** | Directly opens `NpgsqlConnection`, uses `BeginTransactionAsync`, and executes raw SQL for specific tables: `billings`, `table_sessions`, `billing_sessions`. | **HIGH** |
| `Services/TableRepository.cs` | Service | **Data Persistence** | Implements a dual-mode repository (API or DB). executing SQL updates and inserts for table status. | **MEDIUM** |
| `Services/BillingService.cs` | Service | **State Transitions** | `MoveSessionAsync` logic handles complex state transition (Active -> Moved -> New Session) locally in SQL transaction. | **HIGH** |
| `ViewModels/PaymentViewModel.cs` | ViewModel | **Calculation** | Method `UpdateComputed()` calculates Totals, Tips, Splits, and Change locally. While some display calc is fine, this drives the "Total Due" state. | **MEDIUM** |
| `ViewModels/PaymentViewModel.cs` | ViewModel | **Validation** | Method `ConfirmAsync()` contains payment validation logic (Amount > 0, Discount logic) mixed with UI state management. | **LOW** |
| `Services/ReceiptBuilder.cs` | Service | **Output Logic** | Contains PDF generation logic including layouting. While not strictly "business logic", it couples the client to a specific output format engine (`PdfSharpCore`) rather than receiving a receipt URL/blob from backend. | **LOW** |

## 3. Deep Dive Analysis

### A. The `BillingService` Anomaly
This file is the single biggest blocker to a Clean Architecture migration.
- **Why it is there**: To support "Offline Mode". The app falls back to direct DB access when the API is unreachable (or configured to do so).
- **The Violation**: The UI client is now a Database Client. It knows the schema (`public.billings`, `public.table_sessions`).
- **Migration Strategy**: This entire service needs to be replaced with a dumb API client. Offline support — if required — must be handled via a local queue/sync mechanism (e.g., SQLite that syncs to API later), NOT by talking to the Postgres Backend DB directly.

### B. `PaymentViewModel` Responsibilities
The ViewModel is doing too much.
- It manages the Printing flow (`InitializePrinting`).
- It manages the receipt preview generation.
- It calculates splits.
- In a Clean Architecture, the ViewModel should send a "Pay" command to the backend, and the backend should return the result (Success/Failure + New Balance). The split calculation for UI purposes is acceptable, but the *final* split logic relies on the backend.

## 4. Migration Impact Assessment

- **High Effort**: Removing `BillingService` SQL logic. This logic must be ensured to exist 100% in the Backend APIs (`OrderApi`, `TablesApi`) before the frontend code is deleted.
- **Medium Effort**: Refactoring ViewModels to stop calculating totals and instead ask the backend "What is the total for this session?" (Single Source of Truth).
