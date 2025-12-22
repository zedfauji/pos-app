# Gap Triage Report

**Context:** Analysis of missing features vs. Canonical Specification.
**Mode:** STRICT TRIAGE (No fixes proposed).

---

## GAP-01: Shift Controller Service & API Missing
- **Description**: The `ShiftsController`, `ShiftService`, and `IShiftRepository` components are completely absent from the codebase.
- **Origin**: 
  - **Canonical Spec**: "Shift Controller Backend Implementation" / "Micro-Monolith Alignment"
  - **Recovered Doc**: `shift-controller/02-backend-enforcement.md`
- **Affected Layer**: Core & API
- **Risk Level**: **CRITICAL** (Financial control system is non-existent)
- **Dependency**: **NO** (Requires GAP-03 Database Schema first, but conceptually distinct)

## GAP-02: Backend Financial Gating Missing
- **Description**: The `[RequireOpenShift]` attribute and its application to `SessionsController` endpoints (`StartSession`, `PostOrder`, `StopSession`) are missing.
- **Origin**: 
  - **Canonical Spec**: "No Shift = No Operations" / "Enforced at API level"
  - **Recovered Doc**: `shift-controller/02-backend-enforcement.md`
- **Affected Layer**: API
- **Risk Level**: **CRITICAL** (Operations allowed without financial tracking)
- **Dependency**: **NO** (Depends on GAP-01 Implementation)

## GAP-03: Shift Database Schema Missing
- **Description**: The `public.shifts` table and `shift_id` foreign keys on `table_sessions`, `orders`, and `bills` do not exist.
- **Origin**: 
  - **Canonical Spec**: "Database Structure: public.shifts"
  - **Recovered Doc**: `shift-controller/migration_script.sql`
- **Affected Layer**: Database
- **Risk Level**: **CRITICAL** (Data integrity impossible without schema)
- **Dependency**: **YES** (Can be executed independently)

## GAP-04: Payment Workspace Page Missing
- **Description**: `PaymentWorkspacePage.xaml` is missing. The system uses the legacy `PaymentPage.xaml` which lacks split payment and bill preview features.
- **Origin**: 
  - **Canonical Spec**: "Page-Based Payment Architecture"
  - **Recovered Doc**: `payments-hub/03-payment-workspace-refinement.md`
- **Affected Layer**: Client
- **Risk Level**: **HIGH** (Workflow mismatch for users)
- **Dependency**: **NO** (Depends on GAP-05 ViewModel)

## GAP-05: Payment Workspace ViewModel Missing
- **Description**: `PaymentWorkspaceViewModel.cs` is missing. This component requires `double` for WinUI binding compatibility and strict `decimal` casting for backend transport.
- **Origin**: 
  - **Canonical Spec**: "WinUI Type Compatibility (double vs decimal)"
  - **Recovered Doc**: `payments-hub/03-payment-workspace-refinement.md`
- **Affected Layer**: Client
- **Risk Level**: **HIGH** (Runtime crashes potential with legacy decimal binding)
- **Dependency**: **YES** (Can be implemented independently of View)

## GAP-06: Strict Typed Client API (ITableApi)
- **Description**: `ITableApi` definition likely uses `object` or outdated types. Spec requires strong typing (`List<SessionOverview>`) and corrected routes (`/tables/active`).
- **Origin**: 
  - **Canonical Spec**: "Route Standardization" / "Client ITableApi updated"
  - **Recovered Doc**: `backend-operational/01-api-and-domain-design.md`
- **Affected Layer**: Client
- **Risk Level**: **MEDIUM** (Maintenance/Stability issue)
- **Dependency**: **YES** (Can be refactored independently)

## GAP-07: Operational "End Session" Endpoint Missing
- **Description**: The `EndSession` endpoint (stop timer w/o payment) is missing. Only `StopSession` (Financial) exists.
- **Origin**: 
  - **Recovered Doc**: `backend-operational/01-api-and-domain-design.md` ("Endpoint 1: End Session")
- **Affected Layer**: API
- **Risk Level**: **MEDIUM** (Operational workflow gap)
- **Dependency**: **YES** (Backend logic update)
