# Phase 2: Architectural Quality Attributes

## 1. ISO-Style Quality Scoring (0-5)

| Attribute | Score | Justification |
|-----------|-------|---------------|
| **Maintainability** | **1/5** | A change in `BillingService` constructor requires editing `App.xaml.cs`. Direct SQL strings in C# code mean schema changes break the build. |
| **Testability** | **0/5** | **Critical Failure**. Use of Global Statics (`App.Api`) and direct `new NpgsqlConnection()` makes Unit Testing mathematically impossible without mocking the Universe. |
| **Evolvability** | **2/5** | Can add new distinct pages easily (WinUI 3 is good at this), but changing *core behavior* (like "how tax is calculated") is dangerous. |
| **Replaceability** | **1/5** | You cannot replace the "Database Layer" because there isn't one. There are just scattered SQL statements. |
| **Deployability** | **2/5** | Fat Client requires installer updates for every logic change. |
| **Observability** | **3/5** | `App.xaml.cs` has a decent crash logger (`SafeAppendToLog`). However, it writes to a local file, not a central telemetry server. |
| **Security Posture** | **1/5** | **Critical Risk**. Database connection strings are read from `appsettings.json` locally. If the client connects directly to DB, it likely holds credentials with read/write access. |
| **Consistency** | **2/5** | Some services use HTTP, some use SQL, some use local files. |

## 2. Migration Impact Analysis

- **Testability** is the blocker. You cannot safely refactor what you cannot test.
- **Security Posture** is the driver. The current "Direct DB Access" model is acceptable for a prototype but unacceptable for a Production Enterprise App.
- **Maintainability** is the cost. Every week spent on this Codebase is high-interest debt accumulation.
