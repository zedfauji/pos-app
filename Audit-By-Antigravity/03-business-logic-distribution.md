# Phase 3: Business Logic Distribution Matrix

## 1. Logic Dispersion Analysis

"Logic" is defined as: Rules, Calculations, Validations, Data Persistence, and Workflow Orchestration.

| Layer | % of Total Logic | Examples of Logic Found |
|-------|------------------|-------------------------|
| **App Entry (App.xaml)** | **15%** | Session Recovery Workflow, Error Handling, Logging, Window Management, Service Lifecycle. |
| **UI Classes (Views)** | **10%** | Navigation logic, some formatting. (Surprisingly decent). |
| **ViewModel** | **35%** | **Heavy Usage**. Payment calculation, Receipt generation orchestration, State management, Button command logic. |
| **Frontend Services** | **40%** | **The Core Problem**. `BillingService` holds the "How to Bill" SQL rules. `TableRepository` holds "How to store tables" rules. |
| **Backend API** | **Unknown** | Assumed to have some logic, but the Frontend ignores it in favor of direct DB access for critical paths. |

## 2. Key Metrics

- **Logic Dispersion Score**: **High (Bad)**.
  Logic is smeared across 4 layers. To answer "How is a Bill created?", you must look at `BillingService.cs` (SQL) AND `PaymentViewModel.cs` (Calc) AND `App.xaml.cs` (Session Context).

- **Source-of-Truth Clarity**: **0/10**.
  Is the Database the truth? Or the API? Or the local `OrderContext` static class?
  *Correction: The Database is the truth, but the Frontend acts as the Master.*

- **Business Rule Volatility Index**: **High**.
  Billing and Tax rules change often. They are currently hardcoded in the Desktop App's SQL strings.

## 3. The "Code-Behind" Illusion
While the `.xaml.cs` files are relatively thin, the "Code-Behind" has effectively moved to the **ViewModel** and **Service** classes in the form of procedural glue code, rather than strictly separated responsibilities.
