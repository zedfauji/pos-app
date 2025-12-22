# Audit Phase 4: Migration Strategies

## 1. Strategy A: "The Clean Break" (Full Rewrite of UI Layer)

**Concept**: Treat the existing `frontend` folder as a legacy reference. Create a brand new WinUI 3 project `MagiDesk.Client` that strictly adheres to the new architecture from Day 1.

- **Description**: Build a pure Thin Client that talks *only* to the Backend APIs. No `Npgsql` reference allowed.
- **Execution Steps**:
    1.  Create usage-driven API Clients (using `Refit`) for all Backend services.
    2.  Implement Authentication flow first.
    3.  Re-implement Screens one by one, copying XAML from old app but writing fresh Clean ViewModels.
- **Complexity**: **High** (initial setup) -> **Low** (development velocity).
- **Risk Profile**: **Medium**. Risk of missing obscure features, but zero risk of carrying over technical debt.
- **Time Estimate**: 3-4 months.
- **Completion Probability**: **90%**.
- **Recommended?**: **YES**. This guarantees the "Clean Architecture" goal.

## 2. Strategy B: "The Strangler Fig" (Incremental)

**Concept**: Keeping the running application alive while replacing its organs one by one.

- **Description**: Introduce a "V2" folder in the existing project. Route specific features (e.g., Settings) to V2 ViewModels while keeping the "Fat" Billing system running.
- **Execution Steps**:
    1.  Introduce IOC Container (Dependency Injection) alongside existing spaghetti.
    2.  Pick a low-risk module (e.g., Inventory).
    3.  Refactor it to use API-only.
    4.  Repeat until only Billing/Tables remains, then tackle that.
- **Complexity**: **EXTREME**. You will be fighting the existing specific tightly-coupled code (`TableRepository` logic) constantly.
- **Risk Profile**: **High**. High chance of regression in the "Offline Mode" logic that currently holds the app together.
- **Time Estimate**: 6-8 months (due to regression testing overhead).
- **Completion Probability**: **40%**. Teams often get tired halfway and leave a "Frankenstein" app.
- **Recommended?**: **NO**. The codebase is not modular enough to support this easily.

## 3. Strategy C: "Hybrid Shell Replacement" (In-Place Refactor)

**Concept**: Keep the Views (XAML files), but gut the ViewModels and Services.

- **Description**: Delete `BillingService.cs`. Replace it with an Interface `IBillingService`. Implement two versions: `ApiBillingService` (new) and `LegacyBillingService` (old). Switch the app to use `ApiBillingService`.
- **Execution Steps**:
    1.  Interface extraction for all "Fat Services".
    2.  Rewrite `PaymentViewModel` to depend on `IPaymentService` instead of concrete classes.
    3.  Fix the "New Operator" Direct Instantiation issues.
- **Complexity**: **High**. You have to untangle the XAML bindings that might rely on specific property behaviors of the old "Fat" ViewModels.
- **Risk Profile**: **Medium-High**.
- **Time Estimate**: 4-5 months.
- **Completion Probability**: **60%**.
- **Recommended?**: **MAYBE**. Only if preserving exact pixel-perfect layout is critical and XAML is complex.

## Summary Table

| Metric | Strategy A (Rewrite) | Strategy B (Strangler) | Strategy C (Hybrid) |
|--------|----------------------|------------------------|---------------------|
| **Clean Architecture Guarantee** | 100% | 50% | 80% |
| **Regression Risk** | Low (Feature Missing) | High (Bugs) | Medium |
| **Dev Satisfaction** | High | Low | Medium |
| **Cost** | $$$ | $$$$ | $$$ |
