# 07 - Priority Completion Plan

## "The Last Mile"

| Priority | Description | Reason | Estimated Effort | Dependency |
| :--- | :--- | :--- | :--- | :--- |
| **P0** | **Implement Reporting API** | **Business Block** | 1 Week | `Database Access` |
| **P0** | **Implement Z-Report UI** | **Business Block** | 3 Days | `Reporting API` |
| **P0** | **Verify / Fix Split Bill UI** | Parity | 2 Days | `Billing API` |
| **P1** | **Physical Printer Verification** | Risk Mitigation | 1 Day | Hardware |
| **P1** | **Add Bill Preview Endpoint** | Data Integrity | 1 Day | `Billing API` |
| **P2** | **Staff Training Materials** | Adoption | 2 Days | None |
| **P2** | **Refactor `TableViewModel`** | Tech Debt | 3 Days | None |

## STOP CONDITION
> **At this point, legacy system can be retired.**
> 
> *Criteria:*
> 1. Managers can print a Z-Report at end of day.
> 2. Cash drawers balance against the report.
> 3. Waiters can split bills without error.

Only after **P0 items** are complete can the legacy system be turned off.
