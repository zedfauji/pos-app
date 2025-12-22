# Audit Phase 5: Confidence & Risk Scoring

## 1. Overall Migration Confidence Score: 85% (If Strategy A is chosen)

If Strategy A (Full Rewrite) is selected, confidence in success is **High**.
If Strategy B (Refactor) is selected, confidence drops to **30%**.

## 2. Risk Metrics

| Risk Category | Score (1-10) | Description |
|---------------|--------------|-------------|
| **Technical Debt Risk** | **9/10** | The existing codebase is a minefield of hidden state and direct DB calls. Touching one piece breaks "Offline Mode". |
| **Regression Risk** | **10/10** | High probability of breaking the Billing/Payment flows during refactoring due to lack of automated tests for the UI logic. |
| **Team Skill Dependency** | 7/10 | Requires team members who understand *why* we are removing the DB calls. Junior devs might try to "fix" it by adding them back. |
| **Timeline Risk** | 5/10 | Modest. A rewrite is predictable. A refactor is an open-ended "rabbit hole". |
| **Maintainability (Current)** | 3/10 | "Spaghetti coupled with Lasagna". Layers exist but leak into each other. |
| **Maintainability (Target)** | 9/10 | Pure Client = Zero Business Logic to debug on user machines. |

## 3. The "Offline Mode" Trap

**Critical Risk**: The current architecture is designed around being "Offline First" (or at least Offline Capable with local DB).
**Migration Risk**: Moving to "UI communicates ONLY via HTTP APIs" means **KILLING Offline Mode** effectively, unless a sophisticated sync engine (like `sqlite-sync`) is built.
**Decision Point**: Does the business *require* offline capability?
- **IF YES**: The "Holy Grail" Clean Architecture (pure API client) is invalid. You need a "Sync Client" architecture.
- **IF NO**: Proceed with migration.

*Assumption for this audit: The User requirement was "UI communicates ONLY via HTTP APIs", implying Offline Mode is NOT a requirement or will be handled by the Backend/Gateway, not the Client logic.*
