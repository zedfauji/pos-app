# Phase 1: Codebase Topology & Shape Analysis

## 1. Structural Depth & Coupling Profile

The application exhibits a **"Inverted Pyramid"** topology.
- **Top Level (App.xaml.cs)**: Heaviest class. Acts as a Service Locator, Global State Container, and Error Handler.
- **Middle Layer (ViewModels)**: Shallow. They do not own their dependencies; they reach up to `App.StaticProperties`.
- **Bottom Layer (Services)**: Mixed. Some are thin API wrappers, others are thick Database Clients.

### Dependency Graph (Conceptual)

```mermaid
graph TD
    App[App.xaml.cs (God Object)] -->|Static Holds| ApiService
    App -->|Static Holds| BillingService
    App -->|Static Holds| TableRepository
    App -->|Static Holds| Config[IConfiguration]
    
    ViewModel[AnyViewModel] -->|Accesses| App
    
    BillingService -->|Direct Dep| Npgsql[PostgreSQL Driver]
    TableRepository -->|Direct Dep| Npgsql
    
    LegacyView[Views] -->|Direct Logic| CodeBehind
```

## 2. Metrics & Scores

| Metric | Score | Analysis |
|--------|-------|----------|
| **Architectural Entropy** | **8.5/10** | High disorder. New features are added by "bolting on" another static property to `App.xaml.cs`. |
| **Modularity Maturity** | **Ad-Hoc** | Boundaries exist only as folders, not as code constraints. `internal` vs `public` usage is inconsistent. |
| **Fan-Out (App.xaml.cs)** | **Extremely High** | The entry point knows about *every* single service type. |
| **Cyclic Dependencies** | **Likely** | Services likely reference DTOs which are referenced by ViewModels which reference App which references Services. |

## 3. The "Static Glue" Anti-Pattern
The reliance on `App.Api`, `App.Menu`, etc. means that:
1.  **Dependency Inversion** is non-existent.
2.  **Module Granularity** is effectively "The Entire Application". You cannot extract the "Menu Module" because it expects `App.Menu` to exist globally.

## 4. Boundary Clarity
**Score: Very Low.**
There is no "Domain" boundary. Database logic sits next to HTTP Logic. UI logic sits next to Window Management logic.
