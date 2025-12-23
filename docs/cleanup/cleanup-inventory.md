# Cleanup Inventory

**Date**: 2025-12-22  
**Purpose**: Complete classification of all files for controlled cleanup  
**Rule**: NO code changes, NO logic changes, documentation only

---

## Classification Legend

| Category | Meaning |
|:---------|:--------|
| **ACTIVE** | Used in runtime, do not touch |
| **LEGACY** | Old but referenced, read-only |
| **PLANNING** | Future intent, review before acting |
| **HISTORICAL** | Past decisions, archive candidate |
| **ABANDONED** | Never used / dead, archive candidate |
| **UNKNOWN** | Needs human decision |

---

## ROOT LEVEL FILES

| File | Category | Why | Risk if Touched |
|:-----|:---------|:----|:----------------|
| `MagiDesk.sln` | ACTIVE | Main solution file | Build break |
| `start-backend.ps1` | ACTIVE | Backend launcher | Workflow break |
| `run-client.ps1` | ACTIVE | Client launcher | Workflow break |
| `db-interact.ps1` | ACTIVE | DB utility | Operations |
| `query-db.ps1` | ACTIVE | DB utility | Operations |
| `.gitignore` | ACTIVE | Git config | Git issues |
| `LICENSE` | ACTIVE | Legal | Legal issues |
| `README.md` | ACTIVE | Project entry | Documentation |
| `BUSINESS_LOGIC_WORKBOOK.md` | HISTORICAL | Reference doc | None |
| `COMPREHENSIVE_FEATURE_EXTRACTION.md` | HISTORICAL | Legacy analysis | None |
| `MISSING_INCOMPLETE_LOGIC_REPORT.md` | HISTORICAL | Audit report | None |
| `WORKFLOW_DIAGRAMS.md` | HISTORICAL | Reference | None |
| `WPF_TO_WINUI3_MIGRATION_REPORT.md` | HISTORICAL | Migration notes | None |
| `WEB_VERSION_ESTIMATE.md` | PLANNING | Future estimate | None |
| `payment-flow-refactor-plan.md` | PLANNING | Refactor intent | None |
| `infra_build_errors.txt` | ABANDONED | Old errors | None |
| `menu_api_errors.txt` | ABANDONED | Old errors | None |
| `tables_api_errors.txt` | ABANDONED | Old errors | None |
| `Program.cs` | UNKNOWN | Orphan file at root | Needs review |
| `MagiDesk-BusinessLogic-Source.zip` | LEGACY | Archived source | None |
| `postgres-dump-20251209-210039.sql` | HISTORICAL | DB backup | None |
| `receipt_*.pdf` | ABANDONED | Test artifact | None |
| `create-frontend-installer.ps1` | LEGACY | Old installer | None |
| `test-*.ps1` (root level) | ACTIVE | Test utilities | Testing |
| `postgres-mcp-env-template.txt` | ACTIVE | MCP config | Config |

---

## ROOT LEVEL DIRECTORIES

### ACTIVE (Do Not Touch)

| Directory | Files | Purpose |
|:----------|:------|:--------|
| `solution/` | 852 | Main codebase |
| `planning/` | 172 | Planning docs |
| `.git/` | - | Git repository |
| `.github/` | - | GitHub config |
| `.vscode/` | - | VS Code config |
| `postgres-mcp/` | - | MCP server |
| `tests/` | 2 | Test files |

### LEGACY (Read-Only)

| Directory | Files | Purpose |
|:----------|:------|:--------|
| `Do-Not-Edit-legacy-frontend/` | 793 | Legacy reference |
| `legacy-reference/` | - | Legacy docs |
| `MagiDesk-Frontend-Installer/` | 572 | Old installer |

### HISTORICAL / ARCHIVE CANDIDATES

| Directory | Files | Purpose |
|:----------|:------|:--------|
| `Audit-By-Antigravity/` | 16 | Prior audit |
| `.cursor/` | - | Editor config |
| `.windsurf/` | - | Editor config |
| `.vs/` | - | VS config |

---

## SOLUTION DIRECTORY CLASSIFICATION

### ACTIVE (Runtime Code)

| Path | Files | Purpose | Risk |
|:-----|:------|:--------|:-----|
| `solution/MagiDesk.Client/` | 97 | WinUI 3 client | HIGH |
| `solution/backend/TablesApi/` | 29 | Tables API | HIGH |
| `solution/backend/OrderApi/` | 37 | Orders API | HIGH |
| `solution/backend/PaymentApi/` | 13 | Payments API | HIGH |
| `solution/backend/MenuApi/` | 30 | Menu API | HIGH |
| `solution/backend/InventoryApi/` | 39 | Inventory API | HIGH |
| `solution/backend/SettingsApi/` | 10 | Settings API | HIGH |
| `solution/backend/UsersApi/` | 24 | Users API | HIGH |
| `solution/backend/CustomerApi/` | 43 | Customer API | MEDIUM |
| `solution/backend/DiscountApi/` | 11 | Discount API | MEDIUM |
| `solution/backend/ShiftsApi/` | ? | Shifts API | MEDIUM |
| `solution/backend/ReportingApi/` | 5 | Reporting API | MEDIUM |
| `solution/backend/MagiDesk.Core/` | 16 | Core domain | HIGH |
| `solution/backend/MagiDesk.Infrastructure/` | 9 | Infrastructure | HIGH |
| `solution/shared/` | 40 | Shared DTOs | HIGH |

### PARTIALLY ACTIVE (May Contain Dead Code)

| Path | Files | Purpose | Risk |
|:-----|:------|:--------|:-----|
| `solution/MagiDesk.Package/` | 12 | Packaging | LOW |
| `solution/sync/` | 8 | Sync utilities | UNKNOWN |
| `solution/backend/InventoryProxy/` | 2 | Proxy service | UNKNOWN |

### TESTS (Review Coverage)

| Path | Files | Status |
|:-----|:------|:-------|
| `solution/MagiDesk.Tests/` | 46 | UNKNOWN coverage |
| `solution/MagiDesk.Client.ArchTests/` | 3 | UNKNOWN |
| `solution/backend/MagiDesk.Core.Tests/` | 3 | UNKNOWN |
| `solution/backend/SettingsApi.Tests/` | 5 | UNKNOWN |
| `solution/TestWinUI3/` | 12 | UNKNOWN purpose |
| `solution/TestResults/` | 7 | Test output |

### ARCHIVED (Already Marked)

| Path | Files | Purpose |
|:-----|:------|:--------|
| `solution/archive/` | 29 | Old frontend |
| `solution/archive/frontend_old/` | 29 | Old frontend |

### DOCUMENTATION

| Path | Files | Status |
|:-----|:------|:-------|
| `solution/docs/` | 45 | Mixed active/historical |

### BUILD ARTIFACTS (Exclude from Cleanup)

| Path | Files | Note |
|:-----|:------|:-----|
| `solution/MagiDesk-Frontend-Portable/` | 242 | Build output |
| `solution/MagiDesk-Frontend-Installer.zip` | 1 | Build output |
| `solution/build.log` | 1 | Build log |

---

## PLANNING DIRECTORY CLASSIFICATION

### ACTIVE PLANNING (Current Work)

| Path | Files | Purpose |
|:-----|:------|:--------|
| `planning/canonical-system-spec.md` | 1 | **Canonical truth** |
| `planning/shift-controller/` | 9 | Shift feature |
| `planning/payments-hub/` | 8 | Payment hub |
| `planning/table-workspace/` | 7 | Table workspace |
| `planning/tables-v2/` | 7 | Admin tables |
| `planning/admin-tables/` | 6 | Admin tables |
| `planning/task.md` | 1 | Current task |

### HISTORICAL (Keep for Reference)

| Path | Files | Purpose |
|:-----|:------|:--------|
| `planning/final-audit/` | 8 | Audit results |
| `planning/final-execution/` | 9 | Execution log |
| `planning/backend-operational/` | 8 | Backend docs |
| `planning/payment-redesign/` | 13 | Payment history |
| `planning/recovery/` | 3 | Recovery docs |

### SUPERSEDED / ARCHIVE CANDIDATES

| Path | Files | Why |
|:-----|:------|:----|
| `planning/01-master-wbs.md` | 1 | Superseded by iterations |
| `planning/02-wbs-refinement-iteration-*.md` | 3 | Old iterations |
| `planning/03-architecture-guardrails.md` | 1 | Superseded |
| `planning/04-product-and-ux-flows.md` | 1 | Superseded |
| `planning/05-execution-phases.md` | 1 | Superseded |
| `planning/06-self-audit-checklist.md` | 1 | Completed |
| `planning/07-library-first-review.md` | 1 | Completed |
| `planning/08-capability-library-mapping.md` | 1 | Completed |
| `planning/09-wbs-library-refined.md` | 1 | Completed |
| `planning/10-library-first-guardrails.md` | 1 | Completed |
| `planning/EXECUTION_GO_NO_GO_CHECKPOINT.md` | 1 | Completed |
| `planning/PROJECT_HANDOFF_SUMMARY.md` | 1 | Historical |

### DUPLICATE (Needs Decision)

| Path | Files | Issue |
|:-----|:------|:------|
| `planning/Planning-Recovered/` | 78 | **DUPLICATE of planning/** |

> [!IMPORTANT]
> `Planning-Recovered/` contains exact duplicates of most planning files plus additional subdirectories. This should be archived or removed to reduce confusion.

---

## LEGACY BOUNDARY

### `Do-Not-Edit-legacy-frontend/` (793 files)

| Status | **READ-ONLY** |
|:-------|:--------------|
| Purpose | Business logic reference |
| Contains | ViewModels, Services, Views, Dialogs |
| Action | **DO NOT MODIFY. DO NOT MOVE FILES OUT.** |
| Risk | Loss of operational truth reference |

---

## ZOMBIE FEATURES (Backend Exists, No Frontend)

| Backend | Files | Frontend UI |
|:--------|:------|:------------|
| `CustomerApi/` | 43 | ❌ None |
| `DiscountApi/` | 11 | ❌ None (mock data) |
| `ReportingApi/` | 5 | ⚠️ Stub page |

---

## ORPHAN FILES (Purpose Unclear)

| File | Location | Issue |
|:-----|:---------|:------|
| `Program.cs` | Root | What project? |
| `Order-Tracking-By-GPT.code-workspace` | Root | Outdated? |
| `solution/testEnvironments.json` | solution | Active? |
| `solution/cloudbuild-users.yaml` | solution | Active? |
| `solution/Dockerfile.UsersApi` | solution | Active? |

---

## RECOMMENDED ACTIONS

### Immediate (Safe)

1. ✅ Create `/docs/canonical/` with guardrail documents
2. ✅ Create `/docs/cleanup/` with this inventory
3. ✅ Add README.md to `Do-Not-Edit-legacy-frontend/`
4. ✅ Add status headers to planning docs

### Needs Decision

1. ❓ Archive `planning/Planning-Recovered/` (duplicate)
2. ❓ Archive `*_errors.txt` files (old logs)
3. ❓ Move orphan `Program.cs` or delete
4. ❓ Clarify test project status

### DO NOT TOUCH

1. ❌ `solution/MagiDesk.Client/` - runtime code
2. ❌ `solution/backend/` - runtime code
3. ❌ `solution/shared/` - runtime code
4. ❌ `Do-Not-Edit-legacy-frontend/` - legacy reference

---

**Next Step**: Create canonical guardrail documents
