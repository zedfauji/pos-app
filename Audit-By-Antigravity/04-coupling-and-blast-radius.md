# Phase 4: Coupling & Change Blast Radius

## 1. Coupling Taxonomy

### A. Temporal Coupling (The "Wait for It" Problem)
**Severity: High.**
The application relies on a global `InitializeApiAsync` method in `App.xaml.cs`.
- **Symptom**: ViewModels check `if (App.Api == null)` or rely on `App.IsInitialized`.
- **Consequence**: You cannot instantiate a ViewModel in isolation for testing. It will crash because `App.Api` is null. The entire app lifecycle is coupled to the startup sequence.

### B. Data & Schema Coupling
**Severity: Critical.**
The Frontend is **Schema-Bound**.
- **Vector**: `BillingService.cs` contains raw SQL INSERT statements (`INSERT INTO public.billings...`).
- **Blast Radius**: If a backend dev renames the `billings` table to `orders`, the **Desktop App crashes on 100% of client machines**.
- **Change Amplification Factor**: **1:All**. A change in DB schema = Recompile Backend + Recompile Desktop App + Redeploy Installers.

### C. Container Coupling
**Severity: High.**
ViewModels are coupled to the DI Container (or lack thereof).
- **Vector**: `new TableRepository()` usage inside ViewModels.
- **Consequence**: ViewModels effectively "own" the lifecycle of their dependencies.

## 2. "If I change X, what breaks?"

| Change Event | What Breaks? | Blast Radius Score (1-10) |
|--------------|--------------|---------------------------|
| **Rename DB Column** | `BillingService`, `TableRepository` | **10 (Catastrophic)** |
| **Change API URL** | `App.xaml.cs` (Config logic) | **3 (Manageable)** |
| **Add Argument to Service Ctor** | Every ViewModel using that Service, `App.xaml.cs` initialization | **8 (High)** |
| **Move Logic from VM to Backend** | Nothing breaks, but requires deleting code. | **1 (Ideal)** |

## 3. Average Files Touched Per Feature
Based on the topology:
- **New Feature (e.g., Discounts)**: Requires touching:
    1.  `App.xaml.cs` (Register new Service)
    2.  `SomeViewModel` (Logic)
    3.  `SomeService` (SQL/API)
    4.  `View.xaml`
    5.  `View.xaml.cs`
**Estimated Files Per Feature**: 5. This is relatively low, but the *risk* per file is high.
