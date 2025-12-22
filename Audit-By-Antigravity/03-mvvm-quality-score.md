# Audit Phase 3: MVVM & UI Architecture Quality Score

## 1. Compliance Score: 45/100

| Category | Score (0-10) | Notes |
|----------|--------------|-------|
| **Separation of Concerns** | 6/10 | Views and ViewModels are physically separated, but VMs know too much about API/Printing implementation details. |
| **Data Binding** | 8/10 | Correct usage of `INotifyPropertyChanged` and `ObservableCollection`. Binding paths seem standard. |
| **Dependency Injection** | **2/10** | **Critical Failure**. ViewModels directly instantiate Services (e.g., `new TableRepository()`) or access global statics (`App.OrdersApi`). Unit testing is nearly impossible without refactoring. |
| **Command Pattern** | 7/10 | Good usage of `RelayCommand` for button actions. Async commands are handled reasonably well. |
| **Reactive Readiness** | 3/10 | No use of Reactive Extensions (Rx.NET). State updates are manual and imperative. |
| **Testability** | 2/10 | Due to tight coupling with `new` and `App` statics, and direct `Npgsql` dependency in services, writing isolated unit tests for ViewModels is extremely difficult. |
| **Error Handling** | 4/10 | `Debug.WriteLine` is used extensively instead of a proper logging abstraction or user error propagation. |

## 2. Deep Dive Findings

### A. The "New" Operator Anti-Pattern
In `OrdersManagementViewModel.cs`, we see:
```csharp
_tables = new TableRepository(); // HARD COUPLING
```
This defeats the purpose of Dependency Injection. The ViewModel is permanently glued to the concrete implementation of `TableRepository` (which we know is checking for local DBs). You cannot swap this for a mock in tests.

### B. Global State Access
Many ViewModels access `App.OrdersApi` or `Services.SessionService.Current`. This Hidden State makes the system fragile; dependencies are not explicit in the constructor.

### C. Logic Leaks in ViewModels
`PaymentViewModel` contains:
- Receipt generation logic (PDF creation coordination)
- Calculation logic (Splits, Tips, Totals)
- Validation logic
Ideally, `PaymentViewModel` should just be a dumb bag of state that sends a `SubmitPaymentCommand` to a strictly defined Application Service.

## 3. Recommendations

### Salvage or Scrap?
**Recommendation: SALVAGE VIEWS, SCRAP VIEWMODELS.**

- **Views (XAML)**: The UI layout itself (`Grid`, `StackPanel`, `DataTemplate`) is likely reusable.
- **ViewModels**: They are too coupled to the "Fat Client" architecture. It would be faster to rewrite them as thin API-client adapters than to refactor the existing ones.
- **Services**: Scrap entirely. Replace with Refit/HttpClient factory generated clients.

## 4. Reactive Strategy
Migration should introduce:
- **System.Reactive (Rx.NET)**: For event streams (e.g., Table updates pushed from SignalR).
- **DynamicData**: For managing collections (`ObservableCollection` is poor for high-frequency updates).
