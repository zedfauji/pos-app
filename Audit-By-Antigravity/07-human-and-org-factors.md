# Phase 7: Human & Org Dependency Factors

## 1. The "Vibe Coding" Blast Radius
This project exhibits signs of "Vibe Coding" (coding for speed/flow rather than structure).
- **Agent-Friendliness (AI-Readability)**: **Medium**.
  - **Good**: Clear naming (`BillingService`, `PaymentViewModel`).
  - **Bad**: Hidden Side Effects. An AI agent trying to "Add a Payment feature" might write valid C# code that crashes because it didn't know about `App.xaml.cs` initialization secrets.
  - **Risk**: Agents tend to follow existing patterns. If an agent sees `new NpgsqlConnection()` in one service, it will replicate that anti-pattern in 10 other services, accelerating debt.

## 2. Bus Factor & Cognitive Load

| Factor | Score | Implication |
|--------|-------|-------------|
| **Bus Factor** | **1 (Critical)** | The "Secret Knowledge" (e.g., how Offline Mode syncs) likely lives in one person's head. |
| **Cognitive Load** | **High** | A developer needs to be a Full Stack Engineer (DB Admin, API Dev, UI Dev) just to change a button color if that button triggers a billing action. |
| **Documentation Gap** | **High** | No architecture diagrams found. The code *is* the documentation, and it tells a conflicting story. |

## 3. Implicit Knowledge
- **"The Shadow Schema"**: The code assumes a specific DB schema exists. There are no Entity Framework migrations or Fluent configurations to document this. You have to read SQL strings to know the schema.
