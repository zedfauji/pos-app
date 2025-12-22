# Audit Phase 6: Final Architect Recommendation

## 1. Verdict: GO for Strategy A (Full Rewrite)

**Do not attempt to refactor the existing Frontend project.**

The cost of untangling the `Npgsql` dependency, the `BillingService` transaction logic, and the "Fat Client" state management exceeds the cost of copying the XAML Views into a fresh, clean project.

## 2. The Recommendation

### Strategy: "The Phoenix Rewrite"
1.  **Freeze** the existing `frontend` code. No new features.
2.  **Create** `solution/frontend-v2` (Project Name: `MagiDesk.POS.Client`).
3.  **Architecture**:
    -   **Framework**: WinUI 3.
    -   **API Client**: Refit (Auto-generated from Backend Swagger).
    -   **State**: CommunityToolkit.Mvvm.
    -   **Logic**: ZERO. If a button needs to "Split Bill", it calls `POST /api/bills/{id}/split`. It does *not* calculate math locally.
4.  **Copy-Paste**: Copy XAML files from old project. Change `x:Bind` paths to match new ViewModels.

## 3. 30-60-90 Day Execution Plan

### Day 0-30: The "Steel Thread" (Authentication & Menu)
-   Set up `frontend-v2` solution.
-   Implement Identity Service (Login).
-   Implement `MenuViewModel` fetching data from `MenuApi`.
-   **Goal**: App opens, logs in, shows menu items. Zero database connections.

### Day 30-60: The "Billing Core"
-   Implement `TableMapViewModel` (fetching status from `TablesApi`).
-   Implement `OrderViewModel` (Pure command relay).
-   **Goal**: Can open a table, add items (via API), and see the Total update (from API).

### Day 60-90: Payment & Polish
-   Implement `PaymentViewModel` (integrating Stripe/Terminal API).
-   Implement Printing (sending print jobs to sidecar or using dumb logic).
-   **Goal**: Full Loop: Open -> Order -> Pay -> Close.

## 4. What must be DISCARDED
-   **`BillingService.cs`**: Delete it. Burn it.
-   **`TableRepository.cs`**: Delete it.
-   **`Npgsql` Dependency**: Banned.
-   **Offline Logic**: Discard implementation. If offline is needed, use a ready-made sync agent, do not write custom sync logic in UI.

## 5. Explicit Tradeoffs
-   **Pros**: You get a maintainable, light, modern codebase.
-   **Cons**: You lose "Offline Mode" until it is explicitly re-architected as a Sync feature (if needed).
