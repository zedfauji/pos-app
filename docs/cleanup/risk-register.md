# Risk Register

**Status**: ACTIVE  
**Date**: 2025-12-22  
**Purpose**: Flag suspicious patterns for human review

---

## Zombie Features (Backend with No Frontend)

| Backend Component | Files | Frontend UI | Risk |
|:------------------|:------|:------------|:-----|
| `CustomerApi/` | 43 | ❌ None | MEDIUM - CRM unusable |
| `DiscountApi/` | 11 | ❌ None | HIGH - Mock data in use |
| `ReportingApi/` | 5 | ⚠️ Stub page | HIGH - No reports |

---

## Orphan UIs (Frontend with Limited Backend)

| Frontend Page | Has Backend | Issue |
|:--------------|:------------|:------|
| `ReportsPage` | ⚠️ Stub | No data |
| `InventoryPage` | ⚠️ View-only | No CRUD |
| `TableManagementPage` | ⚠️ Binding errors | Broken |

---

## Dual Implementations

| Feature | Location 1 | Location 2 | Risk |
|:--------|:-----------|:-----------|:-----|
| Item storage | `table_sessions.items` (JSONB) | `ord.order_items` (normalized) | HIGH - Merge bugs |
| Planning docs | `planning/` | `planning/Planning-Recovered/` | LOW - Confusion |

---

## Type Mismatches

| Field | Table 1 | Table 2 | Risk |
|:------|:--------|:--------|:-----|
| `billing_id` | UUID (`table_sessions`) | TEXT (`bills`, `payments`) | HIGH - Cast errors |

---

## Hardcoded Values

| Value | Location | Risk |
|:------|:---------|:-----|
| 30% profit margin | `OrderService.cs:163` | HIGH - Wrong analytics |
| $10.00 / $90.00 discount | `DiscountService.cs:137` | CRITICAL - Wrong charges |
| 2-year loyalty expiry | `LoyaltyService.cs:67` | LOW - Not configurable |
| 5-min stale timeout | `TablesApi/Program.cs:1390` | LOW - Not configurable |

---

## Stale Test Projects (Confirmed)

| Path | In Solution? | Status | Action |
|:-----|:-------------|:-------|:-------|
| `MagiDesk.Tests/` | ✅ Yes | Stale | Remove from solution |
| `MagiDesk.Client.ArchTests/` | Unknown | Stale | Archive |
| `MagiDesk.Core.Tests/` | Unknown | Stale | Archive |
| `SettingsApi.Tests/` | Unknown | Stale | Archive |
| `TestWinUI3/` | Has own .sln | Standalone, stale | Archive |

---

## Orphan Files (Confirmed NOT Referenced)

| File | Location | Purpose | Action |
|:-----|:---------|:--------|:-------|
| `Program.cs` | Root | Old JSON test script | ✅ Safe to archive |
| `cloudbuild-users.yaml` | solution | Old Cloud Build | ✅ Safe to archive |
| `Dockerfile.UsersApi` | solution | Old Docker config | ✅ Safe to archive |
| `testEnvironments.json` | solution | Old test config | ✅ Safe to archive |

---

## Duplicate Content (User Confirmed: Archive)

| Original | Duplicate | User Decision |
|:---------|:----------|:--------------|
| `planning/*.md` | `planning/Planning-Recovered/*.md` | **Archive** - backup from restore |
| `planning/*/` | `planning/Planning-Recovered/*/` | **Archive** - backup from restore |

---

## Missing Critical Features

| Feature | Impact | Owner |
|:--------|:-------|:------|
| End-of-Day Report | Cannot reconcile cash | CRITICAL |
| Shift Reports | Cannot track shift revenue | CRITICAL |
| Void/Refund | Cannot correct mistakes | HIGH |

---

**This register is informational. Review before acting.**
