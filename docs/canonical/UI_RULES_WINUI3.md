# WinUI 3 UI Rules

**Status**: ACTIVE  
**Owner**: Senior Windows/Desktop Engineer  
**Last Reviewed**: 2025-12-22  

---

## Technology Stack

| Component | Technology |
|:----------|:-----------|
| Framework | WinUI 3 (Windows App SDK 1.4+) |
| Pattern | MVVM with CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| HTTP | Refit + System.Text.Json |
| Target | Windows 10 19041+ |

---

## MVVM Rules

### ViewModels

✅ **DO**:
- Inherit from `ObservableObject`
- Use `[ObservableProperty]` for bindable properties
- Use `[RelayCommand]` for commands
- Inject services via constructor

❌ **DON'T**:
- Reference `Microsoft.UI.Xaml` types directly
- Access `XamlRoot` or `Window` from ViewModel
- Perform navigation logic in ViewModel (use NavigationService)

### Views (XAML)

✅ **DO**:
- Use `x:Bind` with `Mode=OneWay` or `Mode=TwoWay`
- Bind to ViewModel properties only
- Keep code-behind minimal (navigation, dialog hosting)

❌ **DON'T**:
- Calculate values in converters (display formatting only)
- Use `x:Bind` with complex expressions
- Store state in code-behind

---

## Type Compatibility

### NumberBox Binding

WinUI 3 `NumberBox` requires `double`, not `decimal`:

```csharp
// ✅ CORRECT - ViewModel property
[ObservableProperty]
private double _amountTendered;

// When sending to API, cast to decimal:
var request = new PaymentRequest 
{ 
    Amount = (decimal)AmountTendered 
};
```

### Decimal Precision

| Layer | Type | Why |
|:------|:-----|:----|
| ViewModel | `double` | WinUI binding compatibility |
| API Request | `decimal` | Financial precision |
| Backend | `decimal` | Authoritative calculation |

> [!CAUTION]
> Backend MUST recalculate totals. Never trust client values.

---

## Navigation

### Page-Based Navigation

```csharp
// ShellViewModel handles navigation
NavigationService.NavigateTo<TableMapViewModel>();
```

### No ContentDialog for Complex Flows

❌ Payment dialogs - Use full page (`PaymentWorkspacePage`)  
✅ Confirmation dialogs - OK for simple yes/no  
✅ Input dialogs - OK for single field input

---

## Resource Management

### App.xaml Resources

- Converters registered in `App.xaml`
- Styles in `DesignSystem.xaml` (if exists)
- Templates in `ShellPage.xaml.Resources`

### ViewModel Template Selector

Register all ViewModels in `ViewModelTemplateSelector.cs`:

```csharp
case PaymentWorkspaceViewModel _ :
    return PaymentWorkspaceTemplate;
```

---

## Async Patterns

### Loading States

```csharp
[ObservableProperty]
private bool _isLoading;

[RelayCommand]
private async Task LoadDataAsync()
{
    IsLoading = true;
    try { /* ... */ }
    finally { IsLoading = false; }
}
```

### Error Handling

```csharp
catch (ApiException ex)
{
    ErrorMessage = ex.Message;
    // Show in UI via binding
}
```

---

## Testing

- Unit test ViewModels with mock services
- Integration test API endpoints
- Manual test UI flows

---

**DO NOT MODIFY** without Senior Engineer approval.
