# Phase 6: Technical Debt Taxonomy

## 1. Debt Classification

| Category | High Interest Item (The "Payday Loan") | Interest Rate | Debt Payoff Feasibility |
|----------|----------------------------------------|---------------|-------------------------|
| **Architecture Debt** | **The Fat Client Model**. Using the Desktop App as a Database Client instead of an API Client. | **Compound (Daily)**. Every new feature adds more SQL strings to the client, making the backend less relevant. | **Low (Bankruptcy)**. Requires a paradigm shift, not a patch. |
| **Design Debt** | **God Class (`App.xaml.cs`)**. Putting everything in one bucket. | **Linear**. It gets annoying, but you can find things with Ctrl+F. | **Medium**. Can be extracted into a DI Container. |
| **Logic Debt** | **Duplicated Billing Rules**. logic exists in both SQL (Client) and C# (Backend). | **High**. Divergence risk. One day the Client calculates tax differently than the API. | **Medium**. Delete Client Logic. |
| **Dependency Debt** | **Npgsql in UI**. Hard dependency on a specific DB Driver. | **Low**. Until you want to switch databases or go cloud-native. | **High**. Just delete the package references. |

## 2. The "Interest Rate" Explained
- **Compound Interest**: Architecture Debt. It affects *every* future decision. If you want to add a Mobile App, you can't reuse the Desktop Logic because it talks to SQL directly. You have to rewrite logic.
- **Linear Interest**: Design Debt. Poor variable names or long methods. It slows you down by 5 minutes every day.

## 3. Recommendation
Focus on paying off the **Architecture Debt** first.
- If you fix the "God Class" (Design Debt) but leave the "SQL in UI" (Architecture Debt), you still have a broken distributed system.
- If you fix the "SQL in UI" (Architecture Debt), the "God Class" becomes less harmful (just a messy configuration root).
