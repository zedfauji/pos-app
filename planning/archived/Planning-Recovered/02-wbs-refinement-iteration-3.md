# WBS Refinement - Iteration 3 (Final Polish & Tech Stack)

## 1. Final Tech Stack Decisions
- **Framework**: WinUI 3 (Windows App SDK 1.5+).
- **MVVM Library**: `CommunityToolkit.Mvvm` (Source Generators).
- **DI Container**: `Microsoft.Extensions.DependencyInjection`.
- **API Client**: `Refit` + `Polly`.
- **Validation**: `CommunityToolkit.Mvvm.ComponentModel.ObservableValidator` (Client-side pre-check only).

## 2. Final WBS Adjustments

### 2.2. Navigation (Final)
- **Implementation**: Use a `ShellViewModel` that holds the `CurrentViewModel`.
- **View Binding**: Use `DataTemplate` selectors in `MainWindow` to swap Views based on VM type.
- **Benefit**: Zero "Frame" logic in VMs.

### 4.0. Transactional Safety (Refined)
- **Constraint**: All "Write" operations (Place Order, Pay) must have **Idempotency Keys**.
- **Task 4.0.1**: Implement `IdempotencyService` that generates `Guid` (Request-Id) for every non-safe HTTP request.

## 3. The "Definition of Done" (Global)
For any task to be marked Complete:
1.  **Code**: Compiles and runs.
2.  **Architecture**: Passes `ArchTests`.
3.  **Tests**: At least 1 Happy Path + 1 Sad Path Unit Test.
4.  **UI**: Visual Verified (Screenshot).
5.  **Logic**: Verified to receive data from Mock/Real API.

## 4. Rollback Plan
- If `Refit` proves too rigid for the messy API -> Fallback to `RestSharp` or `HttpClient` wrapper, but maintain Interface abstraction.
