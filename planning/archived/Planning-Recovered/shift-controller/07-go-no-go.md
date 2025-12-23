# Shift Controller - Go / No-Go Checkpoint

> Authorize execution ONLY IF all criteria are met.

---

## Pre-Execution Checklist

### ✅ Domain Design Complete
- [x] Shift states defined (NONE, OPEN, CLOSING, CLOSED)
- [x] Blocking rules documented
- [x] Entity relationships mapped

### ✅ Backend Enforcement Planned
- [x] ShiftGuard middleware designed
- [x] Validation chain documented
- [x] Idempotency handling specified
- [x] Error codes defined

### ✅ API Design Complete
- [x] All 7 endpoints specified
- [x] Request/response DTOs defined
- [x] Authorization requirements clear

### ✅ UI Architecture Defined
- [x] ViewModels specified
- [x] UI states documented
- [x] Matches reference design
- [x] Blocking behavior clear

### ✅ Risk & Fraud Prevention
- [x] Cash mismatch handling
- [x] Forced close prevention
- [x] Power loss recovery
- [x] Audit trail requirements

### ✅ Execution Phases Ordered
- [x] Backend first, UI second
- [x] Dependencies mapped
- [x] Deliverables listed

---

## Success Criteria Verification

| Criterion | Status | Verified By |
|-----------|--------|-------------|
| Nothing can be sold without a shift | 🔲 | Phase 4 test |
| Shifts cannot close with unsettled money | 🔲 | Phase 3 test |
| Owners trust end-of-day numbers | 🔲 | Cash reconciliation |
| UI matches reference design | 🔲 | Visual comparison |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Existing sessions have no shift_id | High | Medium | Migration script + allow null temporarily |
| Breaking existing workflows | Medium | High | Phase carefully, test thoroughly |
| Performance impact of gating | Low | Low | Single DB call, cacheable |

---

## Rollback Plan

If critical issues arise:
1. Remove `[RequireOpenShift]` attributes from controllers
2. Revert UI to non-gated navigation
3. Keep shift table for audit (don't drop)

---

## Authorization Request

### Documents Created

1. [01-shift-invariants.md](01-shift-invariants.md) - Domain rules
2. [02-backend-enforcement.md](02-backend-enforcement.md) - API gating
3. [03-shift-apis.md](03-shift-apis.md) - Endpoint design
4. [04-ui-architecture.md](04-ui-architecture.md) - UI components
5. [05-risk-and-fraud.md](05-risk-and-fraud.md) - Risk mitigation
6. [06-execution-phases.md](06-execution-phases.md) - Implementation order

### Reference Design

![Shift Controller Reference](reference-design.png)

---

## ⏳ AWAITING AUTHORIZATION

**Reply "GO" to begin Phase 1: Database Schema**

Before authorizing, please confirm:
1. Is the domain model correct?
2. Are the blocking rules acceptable?
3. Does the UI design match your vision?
4. Any modifications needed before we proceed?
