# 04 - Implementation Phases

## Overview

Implementation is ordered to minimize risk and enable incremental validation.

---

## Phase 1: Table Status Header

**Goal**: Replace prototype header with production-grade status bar.

### Entry Criteria
- [ ] Planning documents approved
- [ ] Backend contracts verified
- [ ] Development environment ready

### Implementation Steps
1. Create `TableStatusViewModel.cs`
2. Create `TableStatusHeader.xaml` UserControl
3. Bind to `TableStatusDto` from API
4. Add live timer display (UI tick only)
5. Style status badge (OPEN/PAUSED/PAYING)
6. Add quick action icons (placeholder commands)

### Exit Criteria
- [ ] Header displays correct table info from backend
- [ ] Timer updates visually
- [ ] All data comes from API, no hardcoding
- [ ] No payment-related elements present

### Validation Checklist
- [ ] Open workspace for occupied table → header shows correct data
- [ ] Timer counts up visually
- [ ] Status badge reflects actual state
- [ ] Code review: no totals/calculations

---

## Phase 2: Ordered Items View

**Goal**: Prominent access to complete order history.

### Entry Criteria
- [ ] Phase 1 complete
- [ ] Table Status Header working

### Implementation Steps
1. Create `OrderedItemsViewModel.cs`
2. Create "View All Ordered Items" primary button
3. Create OrderedItems panel (SplitView pane or separate page)
4. Bind to `GET /tables/{label}/items`
5. Group items (simple grouping, or flat list initially)
6. Display item status

### Exit Criteria
- [ ] "View All Ordered Items" button is visually prominent
- [ ] Clicking opens ordered items display
- [ ] Shows all items for current session
- [ ] Read-only (no edit/payment actions)

### Validation Checklist
- [ ] Submit order → items appear in ordered view
- [ ] Multiple orders → all visible
- [ ] No totals displayed
- [ ] Code review: no calculation logic

---

## Phase 3: Menu Categorization & Layout

**Goal**: Touch-optimized menu browsing with category filtering.

### Entry Criteria
- [ ] Phase 2 complete
- [ ] Ordered Items View working

### Implementation Steps
1. Extract categories from `MenuItemDto` collection
2. Create category tab bar / filter chips
3. Implement category filtering
4. Optimize menu cards for touch (larger tap targets)
5. Add item images (if available)
6. Ensure "Add" sends to draft panel only

### Exit Criteria
- [ ] Categories displayed as tabs/chips
- [ ] Filtering works correctly
- [ ] Menu cards are touch-friendly
- [ ] No totals or pricing summaries

### Validation Checklist
- [ ] Tap category → filtered items shown
- [ ] "All" shows all items
- [ ] Add button adds to draft (no totals)
- [ ] Code review: no business logic

---

## Phase 4: Current Order Panel Refinement

**Goal**: Clean draft order experience without payment artifacts.

### Entry Criteria
- [ ] Phase 3 complete
- [ ] Menu browsing working

### Implementation Steps
1. Refactor `CurrentOrderViewModel` (simplify from OrderViewModel)
2. Clean up Current Order panel UI
3. Implement quantity +/- controls
4. Implement remove item button
5. Implement "Send Order" action
6. Implement "Cancel Draft" action
7. **REMOVE** all payment-related buttons

### Exit Criteria
- [ ] Draft panel shows only current interaction items
- [ ] Quantity controls work
- [ ] Send Order submits to backend
- [ ] No "Settle", "Pay", or "Close Session" buttons

### Validation Checklist
- [ ] Add items → appear in draft
- [ ] Adjust quantities → reflected correctly
- [ ] Send Order → items move to "ordered"
- [ ] Cancel Draft → clears draft
- [ ] NO totals, tips, or payment UI
- [ ] Code review: no payment logic

---

## Phase 5: Prototype Artifact Removal

**Goal**: Clean codebase of prototype remnants.

### Entry Criteria
- [ ] Phases 1-4 complete
- [ ] New workspace fully functional

### Implementation Steps
1. Remove `StopSessionCommand` from new workspace
2. Remove `ViewOrdersCommand` debug button
3. Remove any dialog-based flows
4. Remove inline totals calculations
5. Clean up unused code paths
6. Update navigation from TableMap to new workspace

### Exit Criteria
- [ ] No prototype code remains
- [ ] Clean separation from Payment Workspace
- [ ] All navigation correct

### Validation Checklist
- [ ] Full workflow test: open table → add items → send → view ordered
- [ ] No payment dialogs appear
- [ ] Navigation to Payment Workspace is separate action
- [ ] Code review: no architectural violations

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Scope creep to payment | Explicit guardrails doc (05) |
| Backend API changes | Verified contracts doc (03) |
| Regression in existing flows | Incremental phases, test each |
| Timer accuracy | Pure display, no calculations |

---

## Timeline Estimate

| Phase | Effort | Dependencies |
|-------|--------|--------------|
| Phase 1: Header | 2-3 hours | None |
| Phase 2: Ordered Items | 2-3 hours | Phase 1 |
| Phase 3: Menu Layout | 3-4 hours | Phase 2 |
| Phase 4: Draft Panel | 2-3 hours | Phase 3 |
| Phase 5: Cleanup | 1-2 hours | All |
| **Total** | **10-15 hours** | |
