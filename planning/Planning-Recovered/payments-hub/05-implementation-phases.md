# 05 - Implementation Phases

## Purpose

Define incremental, reversible implementation phases that avoid breaking live flows.

---

## Phase Overview

| Phase | Scope | Risk | Reversible |
|-------|-------|------|------------|
| 1 | Data layer: Fetch UNSETTLED bills | Low | Yes |
| 2 | Payment Hub tabs: Add UNSETTLED filter | Low | Yes |
| 3 | Card visuals: Status badges & risk indicators | Low | Yes |
| 4 | Payment Workspace: UNSETTLED support | Medium | Yes |
| 5 | Confirmation UX: Explicit settlement | Medium | Yes |
| 6 | Split payment refinement | Medium | Yes |
| 7 | Error handling & retry | Low | Yes |

---

## Phase 1: Data Layer (UNSETTLED Bills)

### Objective
Enable Payment Hub to fetch UNSETTLED bills from backend.

### Changes

#### `ITableApi.cs` or `IPaymentApi.cs`
```csharp
// Add endpoint
[Get("/bills/unsettled")]
Task<List<BillResult>> GetUnsettledBillsAsync(CancellationToken ct = default);
```

#### `PaymentHubViewModel.cs`
```csharp
// Add method
public async Task LoadUnsettledBillsAsync() { ... }
```

### Acceptance Criteria
- [ ] API call returns list of UNSETTLED bills
- [ ] No change to existing active sessions flow

### Rollback
Remove the new method. No impact on existing functionality.

---

## Phase 2: Payment Hub Tabs

### Objective
Add tab filtering for UNSETTLED, PARTIAL, and ACTIVE sessions.

### Changes

#### `PaymentHubPage.xaml`
```xml
<!-- Add tab bar -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <RadioButton Content="All" GroupName="StatusFilter" IsChecked="True"/>
    <RadioButton Content="Unsettled (5)" GroupName="StatusFilter"/>
    <RadioButton Content="Partial (1)" GroupName="StatusFilter"/>
    <RadioButton Content="Active (2)" GroupName="StatusFilter"/>
</StackPanel>
```

#### `PaymentHubViewModel.cs`
```csharp
[ObservableProperty]
private string _selectedFilter = "All";

public ObservableCollection<SessionCardViewModel> FilteredSessions => 
    SelectedFilter switch { ... };
```

### Acceptance Criteria
- [ ] Tabs display with counts
- [ ] Filtering works correctly
- [ ] "Unsettled" tab shows only UNSETTLED bills

### Rollback
Remove tabs, revert to single list.

---

## Phase 3: Card Visuals

### Objective
Add status badges and risk indicators to session cards.

### Changes

#### `PaymentHubPage.xaml`
- Add status badge (UNSETTLED/PARTIAL/ACTIVE)
- Add time-based color coding
- Add amount highlighting

### Acceptance Criteria
- [ ] Status badges visible
- [ ] Old bills (>30 min) have red border
- [ ] High amounts are bold

### Rollback
Remove styling. Cards revert to default.

---

## Phase 4: Payment Workspace UNSETTLED Support

### Objective
Ensure Payment Workspace correctly handles UNSETTLED bills (from ended sessions).

### Changes

#### `PaymentWorkspaceViewModel.cs`
```csharp
// Add support for loading from bill (not just active session)
public async Task LoadBillByIdAsync(Guid billingId) { ... }
```

#### `PaymentWorkspacePage.xaml`
- Update layout per design spec
- Two-column layout (Bill | Payment)

### Acceptance Criteria
- [ ] UNSETTLED bills can be opened
- [ ] Bill details display correctly
- [ ] Payment processing works

### Rollback
Revert ViewModel changes.

---

## Phase 5: Confirmation UX

### Objective
Add explicit confirmation before settlement.

### Changes

#### New: `ConfirmSettlementDialog.xaml`
- Shows amount, method, change
- Requires explicit confirm

#### `PaymentWorkspaceViewModel.cs`
```csharp
[RelayCommand]
async Task ShowConfirmationAsync() { ... }
```

### Acceptance Criteria
- [ ] Dialog appears before payment
- [ ] Amount clearly displayed
- [ ] Cancel returns to workspace

### Rollback
Remove dialog, process directly.

---

## Phase 6: Split Payment Refinement

### Objective
Improve split payment UX with amount/percent/items options.

### Changes

#### `PaymentWorkspacePage.xaml`
- Add split type selector
- Add progress indicator for partial payments

#### `PaymentWorkspaceViewModel.cs`
- Add split calculation methods

### Acceptance Criteria
- [ ] Split by amount works
- [ ] Split by percent works
- [ ] Remaining balance tracked

### Rollback
Revert to existing split logic.

---

## Phase 7: Error Handling & Retry

### Objective
Robust error handling with safe retry.

### Changes

#### `PaymentWorkspaceViewModel.cs`
- Add retry logic with exponential backoff
- Add idempotency key generation

### Acceptance Criteria
- [ ] Network errors show retry option
- [ ] Retries are safe (idempotent)
- [ ] Timeout handled gracefully

### Rollback
Revert to existing error handling.

---

## Implementation Order

```
Week 1: Phases 1-3 (Data + UI)
Week 2: Phases 4-5 (Workspace + Confirmation)
Week 3: Phases 6-7 (Split + Error)
```

Each phase can be deployed independently.

---

## Feature Flags

For controlled rollout, consider:

```csharp
if (FeatureFlags.ShowUnsettledTabs)
{
    // Show tabs
}
```

This allows quick disable if issues arise.
