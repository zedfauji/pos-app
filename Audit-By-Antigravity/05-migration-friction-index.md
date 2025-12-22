# Phase 5: Migration Friction Index (MFI)

## 1. The MFI Formula

The Migration Friction Index (MFI) quantifies how hard the codebase "fights back" against refactoring.

| Factor | Weight | Score (0=Easy, 10=Hard) | Weighted Score |
|--------|--------|-------------------------|----------------|
| **Legacy Framework Limit** | 1.0 | 4 (WinUI 3 is modern, but heavy) | 4.0 |
| **Hidden State (Statics)** | 2.0 | **9 (Global App.Api)** | 18.0 |
| **Side Effects (DB Writes)** | 2.0 | **10 (Direct SQL)** | 20.0 |
| **Lack of Tests** | 1.5 | **10 (Zero Tests)** | 15.0 |
| **Tooling Maturity** | 1.0 | 5 (Standard VS) | 5.0 |
| **Total MFI** | | | **62 / 100** |

## 2. Interpretation

**Score: 62 (High Friction)**.
*Range: 0-30 (Easy), 31-60 (Hard), 61-100 (Rewrite Territory).*

The score is driven predominantly by **Hidden State** and **Side Effects**.
- **Hidden State**: You cannot move a class without dragging the `App` object with it.
- **Side Effects**: You cannot run the code without a live Database.

## 3. The "Gravity" of the Codebase
The code has high "Gravity". It is heavy.
- **Light Code**: Pure functions, Interfaces, DTOs.
- **Heavy Code**: `new SqlConnection(...)`, `App.Current.Properties[...]`.
This codebase is 80% Heavy Code. Moving it requires immense energy.

## 4. Conclusion
The MFI suggests that **Refactoring is more expensive than Rewriting**. Trying to lower the friction of the existing code (e.g., injecting interfaces) will take months before you see value.
