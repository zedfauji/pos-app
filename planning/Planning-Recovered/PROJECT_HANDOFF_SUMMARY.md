# Project Handoff Summary

## 1. Project Goal

**Problem**: The legacy Order Tracking application suffered from high coupling, mixed concerns (logic in UI), and lack of testability, making it rigid and difficult to maintain. Direct database dependencies in the client prevented scalability and web/mobile expansion.

**Why Rewrite**: A clean break was necessary to enforce a strict **Client-Server Architecture** (WinUI 3 Client + ASP.NET Core API). Refactoring the existing entangled codebase would have cost more than rebuilding the client on a modern "Iron Foundation."

**Success Definition**: A compilable, testable WinUI 3 client that contains **Zero Business Logic** and **Zero Database References**, relying entirely on robust APIs (`Refit`) and Shared DTOs.

## 2. Current State Snapshot

- **Current Branch**: `rewrite/ui-thin-client`
- **Planning Status**: **Complete**
- **Execution Started**: **YES** (Client Foundation, Auth, Table Map, Ordering, Printing implemented).
- **Forbidden**: Direct usage of `Npgsql` or `DbContext` in the Client project. Logic in Code-Behind (`.xaml.cs`).

## 3. Architectural Laws (The Iron Foundation)

- **Layered Independence**: UI -> ViewModel -> IApi -> DTO. No layer skipping.
- **API-First**: All backend interaction MUST go through defined `Refit` interfaces.
- **MVVM Strictness**: All state and logic reside in ViewModels (`CommunityToolkit.Mvvm`). Views are dumb bindings.
- **Library-First**: Do not reinvent wheels. Use `Serilog` (Logging), `Polly` (Resilience), `Refit` (HTTP).
- **Zero DB in Client**: The Client project must NOT reference data access libraries.

## 4. Planning Artifacts Generated

### Planning & WBS
*   `01-master-wbs.md`: High-level breakdown of the rewrite.
*   `02-wbs-refinement-iteration-*.md`: Evolution of the task breakdowns.
*   `09-wbs-library-refined.md`: Final actionable WBS with library selections.

### Guardrails & Audit
*   `03-architecture-guardrails.md`: The rulebook for dependencies and patterns.
*   `06-self-audit-checklist.md`: Steps to verify compliance before commiting.
*   `10-library-first-guardrails.md`: Rules preventing "Not Invented Here" syndrome.

### Product & Design
*   `04-product-and-ux-flows.md`: User journeys (Orders, Tables, Billing).

### Execution Strategy
*   `05-execution-phases.md`: Ordered implementation steps (Foundation -> Interactive -> Polish).
*   `07-library-first-review.md`: Justification for chosen NuGet packages.
*   `08-capability-library-mapping.md`: Mapping features to specific libraries.

## 5. Execution Strategy Chosen

- **Strategy**: **Vertical Slice Rewrite (Front-to-Back)**.
- **Why**: Allows building a fully functional, modern UX (WinUI 3) immediately by defining API contracts, allowing the Backend to catch up later. Mitigates risk by validating the UX early.
- **Rejected**:
    - *In-Place Refactor*: Too much "Spaghetti Code" friction; risk of regression was too high.
    - *Big Bang*: Waiting for perfect backend would delay feedback.

## 6. Library-First Decisions

- **Connectivity**: `Refit` (Type-safe REST), `Polly` (Retries/Circuit Breaking).
- **Observability**: `Serilog` (Structured Logging to File/Console).
- **UI Architecture**: `CommunityToolkit.Mvvm` (Observables, RelayCommands).
- **Hardware**: `ESCPOS.NET` (Thermal Printing Abstraction).
- **Custom Code**: Restricted to ViewModels (glue logic) and Views (presentation).

## 7. Risks & Mitigations

1.  **Hardware Integration**: Physical printer commands may vary.
    *   *Mitigation*: Implemented `IPrinterService` abstraction with a "Virtual Mode" (Logging) for dev testing.
2.  **API Drfit**: Backend and Frontend might disagree on contracts.
    *   *Mitigation*: Usage of `MagiDesk.Shared` project for all DTOs guarantees compile-time contract alignment.
3.  **WinUI 3 Maturity**: Occasional XAML/Binding quirks (e.g., StringFormat).
    *   *Mitigation*: Fallback to standard converters or `<Run>` elements; kept UI simple.
4.  **Backend Readiness**: API endpoints are mocks or TBD.
    *   *Mitigation*: Client is built against interfaces (`ITableApi`); implementation can be mocked/stubbed easily.
5.  **Agent Drift**: Future AI agents might introduce "hacky" fixes.
    *   *Mitigation*: `MagiDesk.Client.ArchTests` runs on build to block architecture violations.

## 8. Guardrails Against Agent Drift

- **Architecture Tests**: `MagiDesk.Client.ArchTests` enforces that `ViewModels` cannot depend on `Views`, and `Client` cannot depend on `Data`.
- **Pre-Coding Check**: Must update `task.md` and read `03-architecture-guardrails.md` before starting new features.
- **Violation Consequence**: CI/Build failure (Architecture Tests).

## 9. Next Approved Step

- **Backend Integration**: Implement the server-side logic for the defined `ITableApi` and `IMenuApi` endpoints.
- **Hardware Lab**: Verify `ESCPOS.NET` implementation with a physical EPSON printer.
- **Approval Required**: Before adding any *new* libraries or changing the Layered Architecture structure.

## 10. How to Continue This Project Safely

1.  **Read First**: `03-architecture-guardrails.md` and `task.md`.
2.  **Verify**: Run `dotnet test solution\MagiDesk.Client.ArchTests -r win-arm64` (or your arch) to confirm the baseline is green.
3.  **Respect**: The `MagiDesk.Shared` project is the source of truth for data structures. Do not duplicate DTOs in the client locally if possible.
