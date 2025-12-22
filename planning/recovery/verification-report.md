# Verification Report: Codebase vs. Recovered Intent

**Date:** 2025-12-22
**Status:** 🔴 CRITICAL GAPS DETECTED
**Context:** Repository restored from checkpoint. Strict read-only verification.

## 1. Executive Summary

The current codebase **DOES NOT MATCH** the Canonical System Specification or the Recovered Planning Documents. While the operational foundation (startup scripts, repository structure) is intact, the major feature increments described in recent planning—specifically the **Shift Controller** and the **Payment Workspace**—are **MISSING** from the codebase.

It appears the repository state corresponds to a point **before** the implementation of the Shift Controller and the backend-driven Payment Workspace, despite the Canonical Spec treating them as "Decided" or "Expected".

## 2. Task A: Specification Verification (Canonical Spec)

### 2.1 Backend Operational & Architecture
| Component | Expectation | Finding | Status |
|-----------|-------------|---------|--------|
| **Startup Script** | Serial build, kill old procs, fail-fast | `start-backend.ps1` implements this logic perfectly. | ✅ MATCH |
| **Persistence** | No `DROP SCHEMA` on startup | `DatabaseInitializer.cs` has `// REMOVED DESTRUCTIVE DROP`. | ✅ MATCH |
| **Shift Controller** | `ShiftsController`, `IShiftService` | Files **NOT FOUND**. `Program.cs` has no registration. | 🔴 MISSING |
| **Route Std.** | Client uses `/tables/active` | `SessionsController` has `[HttpGet("active")]`. | ✅ MATCH |

### 2.2 Payment Workspace (Frontend)
| Component | Expectation | Finding | Status |
|-----------|-------------|---------|--------|
| **Page Architecture** | `PaymentWorkspacePage.xaml` | File **NOT FOUND**. Only legacy `PaymentPage.xaml` exists. | 🔴 MISSING |
| **ViewModel** | `PaymentWorkspaceViewModel` (double types) | File **NOT FOUND**. | 🔴 MISSING |
| **API Contract** | `ITableApi.cs` with strong types | File **NOT FOUND**. | 🔴 MISSING |

### 2.3 Shift Enforcement (Backend)
| Component | Expectation | Finding | Status |
|-----------|-------------|---------|--------|
| **Gating** | `[RequireOpenShift]` attribute | Attribute **NOT FOUND**. Not used on `SessionsController`. | 🔴 MISSING |
| **Database** | `public.shifts` table, `shift_id` FKs | `DatabaseInitializer.cs` creates `ord` schema but **NO** shift tables. | 🔴 MISSING |
| **Endpoints** | 423 Locked for no shift | Control logic missing. Returns standard 200/404. | 🔴 MISSING |

## 3. Task B: Recovered Planning Cross-Check

### 3.1 Backend Operational (`backend-operational/`)
- **Plan**: Define `EndSession` (Operational) vs `StopSession` (Financial).
- **Code**: `SessionsController` has `StopSession` but **NO** `EndSession` endpoint.
- **Verdict**: Implementation incomplete or lost.

### 3.2 Payment Hub (`payments-hub/`)
- **Plan**: "Payment flow moved entirely to PaymentWorkspacePage".
- **Code**: Codebase still uses `PaymentPage.xaml`.
- **Verdict**: The plan was likely created but the code was never committed or was lost in the restore.

### 3.3 Shift Controller (`shift-controller/`)
- **Plan**: "Micro-Monolith Alignment", `ShiftService`, `ShiftGuardMiddleware`.
- **Code**: Zero traces of these components in `TablesApi`.
- **Verdict**: Feature completely missing.

## 4. Anomalies & Invariants

- **Invariant Violation**: The spec says "Shift (Caja) is a hard gate". The current code allows starting/stopping sessions without any shift check (as the check doesn't exist).
- **Invariant Violation**: The spec says "Backend-Driven Session Totals". While `GetBillPreview` exists in `TablesController`, the full payment flow described is not present.
- **Code State**: The codebase seems to be in a stable "Pre-Shift, Pre-Payment-Refactor" state. The `start-backend.ps1` script suggests some operational work was applied, but the feature work is absent.

## 5. Conclusion

The repository is running a **stable but outdated** version of the application compared to the Canonical Specification. The recovery process likely restored a commit predating the implementation of the Shift Controller and Payment Workspace Refactoring.

**Immediate Action Required:**
The missing features (Shifts, Payment Workspace) must be re-implemented following the Recovered Planning Documents, as they are effectively "todo" items now rather than "verified" items.

**UNCERTAIN – REQUIRES HUMAN REVIEW** regarding the timeline of the restore point vs expected progress.
