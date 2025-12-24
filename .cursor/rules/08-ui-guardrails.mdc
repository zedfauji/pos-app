---
alwaysApply: true
---
### ❌ Rule 2: No Manual INotifyPropertyChanged

**Prohibition**: NEVER write manual `OnPropertyChanged()` calls for `[ObservableProperty]` fields

**Rationale**: 
- `[ObservableProperty]` generates notifications automatically
- Manual calls are boilerplate
- Easy to forget, causes bugs

**Allowed Exception**: Computed properties (use `[NotifyPropertyChangedFor]` instead)

**Enforcement**: 
- Code review check
- Linter rule (if possible)

**Current Status**: ❌ **VIOLATED** - 11+ manual calls found

**Fix**: Use `[NotifyPropertyChangedFor]` attributes

---

### ❌ Rule 3: No Code-Behind Logic (Except UI Events)

**Prohibition**: No business logic in code-behind

**Allowed Exceptions**:
- ✅ Navigation event routing (OnItemInvoked)
- ✅ Dialog hosting
- ✅ UI event handlers that route to ViewModel

**Prohibited**:
- ❌ Data manipulation
- ❌ Business calculations
- ❌ State management
- ❌ API calls

**Enforcement**: 
- Code review check
- Architecture tests

**Current Status**: ⚠️ **PARTIAL** - Some violations found (ListView selection sync)

---

### ❌ Rule 4: No ViewModel → View References

**Prohibition**: ViewModels MUST NOT reference View types

**Prohibited**:
- ❌ `Microsoft.UI.Xaml.Application.Current`
- ❌ `MainWindow`
- ❌ `DispatcherQueue` (direct access)
- ❌ Any `Microsoft.UI.Xaml.*` types

**Allowed**:
- ✅ DTOs and shared types
- ✅ Services (via interfaces)
- ✅ `ObservableObject`, `RelayCommand` (MVVM framework)

**Enforcement**: 
- Code review check
- Architecture tests
- Compile-time: Namespace restrictions

**Current Status**: ❌ **VIOLATED** - 15+ UI type references found

**Fix**: Use `IDispatcherService` abstraction

---

### ❌ Rule 5: No UI Math

**Prohibition**: ViewModels MUST NOT perform business calculations

**Rationale**: 
- Backend is source of truth
- UI calculations are for display only
- Business logic belongs in backend

**Allowed**:
- ✅ Display formatting (currency, dates)
- ✅ UI state calculations (visibility, enabled states)
- ✅ Validation (client-side, but backend validates)

**Prohibited**:
- ❌ Financial calculations (totals, taxes)
- ❌ Business rule enforcement
- ❌ Data transformations (beyond display)

**Enforcement**: 
- Code review check
- Architecture tests

**Current Status**: ⚠️ **PARTIAL** - Some display calculations (acceptable)

---

### ❌ Rule 6: No Silent Binding Failures

**Prohibition**: Binding failures MUST be logged and visible

**Rationale**: 
- Silent failures are hard to debug
- Users see broken UI with no explanation
- Developers can't diagnose issues

**Requirements**:
- ✅ Enable x:Bind diagnostics in Debug builds
- ✅ Log binding failures
- ✅ Show error messages in UI (Debug builds)
- ✅ Never swallow exceptions silently

**Enforcement**: 
- Code review check
- Debug build validation

**Current Status**: ❌ **VIOLATED** - Silent catch blocks, no binding diagnostics

---

## ✅ MANDATORY PATTERNS

### ✅ Rule 7: Always Use [ObservableProperty]

**Requirement**: All bindable properties MUST use `[ObservableProperty]`

**Pattern**:
```csharp
[ObservableProperty]
private string _tableLabel = string.Empty;
```

**Rationale**: 
- Automatic property change notification
- Reduces boilerplate
- Consistent pattern

**Enforcement**: 
- Code review check
- Linter rule (if possible)

**Current Status**: ✅ **FOLLOWED** - Consistent usage

---

### ✅ Rule 8: Always Use [RelayCommand]

**Requirement**: All commands MUST use `[RelayCommand]`

**Pattern**:
```csharp
[RelayCommand]
public async Task LoadDataAsync() { ... }
```

**Rationale**: 
- Automatic command implementation
- CanExecute support
- Consistent pattern

**Enforcement**: 
- Code review check

**Current Status**: ✅ **FOLLOWED** - Consistent usage

---

### ✅ Rule 9: Always Use Dependency Injection

**Requirement**: ViewModels MUST use constructor injection

**Pattern**:
```csharp
public PaymentWorkspaceViewModel(
    ITableApi tableApi,
    IPaymentApi paymentApi,
    INavigationService navigation)
{
    _tableApi = tableApi;
    _paymentApi = paymentApi;
    _navigation = navigation;
}
```

**Rationale**: 
- Testability
- Loose coupling
- Consistent pattern

**Enforcement**: 
- Code review check
- Architecture tests

**Current Status**: ✅ **FOLLOWED** - Consistent usage

---

### ✅ Rule 10: Always Use x:Bind (When Possible)

**Requirement**: Prefer `x:Bind` over `{Binding}` for performance

**Pattern**:
```xaml
<!-- Prefer x:Bind -->
<TextBlock Text="{x:Bind ViewModel.TableLabel, Mode=OneWay}"/>

<!-- Use {Binding} only when necessary -->
<PasswordBox Password="{Binding ViewModel.Password, Mode=TwoWay}"/>
```

**Rationale**: 
- x:Bind is compile-time (faster)
- x:Bind is type-safe
- Better performance

**Enforcement**: 
- Code review check
- Linter rule (if possible)

**Current Status**: ⚠️ **PARTIAL** - Mixed usage (~50/50)

---

## ⚠️ RECOMMENDED PATTERNS

### ⚠️ Rule 11: Use [NotifyPropertyChangedFor] for Computed Properties

**Recommendation**: Use attributes instead of manual `OnPropertyChanged()`

**Pattern**:
```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FinalTotal))]
[NotifyPropertyChangedFor(nameof(ChangeDue))]
private double _amountTendered;

public double FinalTotal => Math.Max(0, TotalDue - DiscountAmount);
```

**Rationale**: 
- Eliminates manual calls
- Compile-time validation
- Less boilerplate

**Enforcement**: 
- Code review recommendation

**Current Status**: ❌ **NOT USED** - Manual calls instead

---

### ⚠️ Rule 12: Use Behaviors Instead of Code-Behind

**Recommendation**: Use CommunityToolkit.WinUI behaviors for UI interactions

**Pattern**:
```xaml
<Button>
    <Interactivity:Interaction.Behaviors>
        <Core:EventTriggerBehavior EventName="Click">
            <Core:InvokeCommandAction Command="{x:Bind ViewModel.NavigateCommand}"/>
        </Core:EventTriggerBehavior>
    </Interactivity:Interaction.Behaviors>
</Button>
```

**Rationale**: 
- Declarative UI logic
- Less code-behind
- Better MVVM separation

**Enforcement**: 
- Code review recommendation

**Current Status**: ❌ **NOT USED** - Code-behind event handlers

---

## Enforcement Strategy

### Code Review Checklist

**Every PR Must Check**:
- [ ] ViewModel inherits from ObservableObject
- [ ] No manual OnPropertyChanged() calls (except computed with attributes)
- [ ] No UI type references in ViewModel
- [ ] No business logic in code-behind
- [ ] All properties use [ObservableProperty]
- [ ] All commands use [RelayCommand]
- [ ] x:Bind used where possible
- [ ] No silent catch blocks
- [ ] Binding errors logged (Debug builds)

---

### Architecture Tests

**Create Tests For**:
- ViewModels don't reference UI types
- ViewModels inherit from ObservableObject
- Code-behind has minimal logic
- Services are injected (not static)

**Tools**: 
- Architecture tests (ArchUnitNET or similar)
- Static analysis

---

### Linter Rules (Future)

**Consider Adding**:
- Rule: No `Microsoft.UI.Xaml.*` in ViewModels
- Rule: No manual `OnPropertyChanged()` for `[ObservableProperty]`
- Rule: Prefer x:Bind over {Binding}

**Tools**: 
- Analyzers
- Custom rules

---

## Guardrail Summary

| Rule | Status | Priority |
|------|--------|----------|
| ObservableObject Required | ✅ Followed | CRITICAL |
| No Manual INotifyPropertyChanged | ❌ Violated | CRITICAL |
| No Code-Behind Logic | ⚠️ Partial | HIGH |
| No ViewModel → View References | ❌ Violated | CRITICAL |
| No UI Math | ⚠️ Partial | MEDIUM |
| No Silent Failures | ❌ Violated | HIGH |
| Use [ObservableProperty] | ✅ Followed | CRITICAL |
| Use [RelayCommand] | ✅ Followed | CRITICAL |
| Use Dependency Injection | ✅ Followed | CRITICAL |
| Use x:Bind | ⚠️ Partial | MEDIUM |
| Use [NotifyPropertyChangedFor] | ❌ Not Used | HIGH |
| Use Behaviors | ❌ Not Used | MEDIUM |

**Compliance Score**: **58%** (7/12 rules fully followed)

---

## Next Steps

### Immediate (Fix Violations)

1. **Eliminate UI Type References** (CRITICAL)
2. **Fix Manual OnPropertyChanged** (CRITICAL)
3. **Fix Silent Failures** (HIGH)
4. **Add [NotifyPropertyChangedFor]** (HIGH)

### Short-Term (Improve Patterns)

5. **Standardize x:Bind Usage** (MEDIUM)
6. **Add Behaviors** (MEDIUM)
7. **Create Architecture Tests** (MEDIUM)

---

**END OF UI GUARDRAILS**

