# 06 - Confidence & Risk Scoring

## Quantified Scores (0–100)

| Category | Score | Rationale |
| :--- | :--- | :--- |
| **Feature Completeness** | **75** | Core flows work. Reporting is a 0. |
| **Legacy Coverage** | **65** | Missing the "Manager" persona features entirely. |
| **Business Logic Correctness** | **90** | Backend-driven design prevents client logic bugs. |
| **Operational Readiness** | **40** | **CANNOT GO LIVE** without End-of-Day reports. |
| **Architectural Integrity** | **98** | Strongest part of the project. Clean & Verified. |
| **Migration Closure** | **30** | Legacy system still required for Accounting/Closing. |

## Top Remaining Risks

| Risk | Severity | Likelihood | Mitigation |
| :--- | :--- | :--- | :--- |
| **Missing Financial Reporting** | **Critical** | **100% (Certain)** | Build `ReportingApi` immediately. |
| **Client/Server Math Mismatch** | High | Medium | Add `GetBillPreview` endpoint to align UI dialog. |
| **Printer Hardware Incompatibility** | Medium | Low | `ESCPOS.NET` is standard, but physical test needed. |
| **User Training (Offline Mode)** | Medium | High | Users will panic when WiFi drops. Training required. |
