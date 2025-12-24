# Task 3.1: Standardize x:Bind Usage - IN PROGRESS ⏳

**Date**: 2025-12-23  
**Status**: ⏳ **IN PROGRESS**

---

## Summary

Converting `{Binding}` to `x:Bind` where appropriate, prioritizing high-traffic pages (PaymentWorkspacePage, TableWorkspacePage).

---

## Strategy

### Conversion Rules

✅ **Convert to x:Bind**:
- One-way bindings: `Text`, `Visibility`, `ItemsSource`, `Command` (one-way), `IsEnabled`
- Read-only properties

❌ **Keep as {Binding}**:
- Two-way bindings: `NumberBox.Value`, `TextBox.Text`, `ToggleSwitch.IsOn`, `RadioButton.IsChecked`
- ElementName bindings (to be addressed in Task 3.3)
- Complex binding expressions

---

## Progress

### PaymentWorkspacePage.xaml

**Converted to x:Bind** (one-way bindings):
- ✅ `TableLabel` (2 occurrences)
- ✅ `CancelCommand`
- ✅ `IsSplitPayment` (Visibility)
- ✅ `CalculateSplitCommand`
- ✅ `CalculatedSplitAmount` (Text and Visibility)
- ✅ `BillItems` (ItemsSource - 2 occurrences)
- ✅ `Subtotal`, `Tax`, `DiscountAmount` (Text)
- ✅ `FinalTotal` (Text)
- ✅ `PaymentHistory` (ItemsSource)
- ✅ `PaymentHistory.Count` (Visibility)
- ✅ `ProcessPaymentCommand`
- ✅ `IsProcessing` (IsEnabled, IsActive, Visibility)
- ✅ `StatusMessage` (Text and Visibility)
- ✅ `ErrorMessage` (Text and Visibility)
- ✅ `ChangeDue` (Text)

**Remaining {Binding}** (intentionally kept):
- ⚠️ Two-way bindings: `IsSplitPayment` (ToggleSwitch.IsOn), `SplitMode`, `SplitAmount`, `SplitPercentage`, `SelectedPaymentMethod`, `AmountTendered`, `TipAmount`, `DiscountAmount` (NumberBox.Value), `CustomerEmail` (TextBox.Text)
- ⚠️ ElementName bindings: `Command="{Binding DataContext.VoidPaymentCommand, ElementName=RootGrid}"`

---

### TableWorkspacePage.xaml

**Status**: 🔴 Not started

**Target bindings to convert**:
- `TableLabel` (2 occurrences)
- `GoBackCommand` (2 occurrences)
- `ServerName`, `SessionStartTime`, `Duration`, `CurrentTime`
- `MenuItems` (ItemsSource)
- `TicketItems` (ItemsSource)
- `SubmitOrderCommand`
- `OrderedItems` (ItemsSource)
- `StatusMessage` (2 occurrences)
- `PrintBillCommand`, `MoveTableCommand`, `GoToPaymentsCommand`, `EndSessionCommand`
- `TableConfig.HourlyRate`, `TableConfig.AllowOrders`
- `ServerName` (TextBox.Text - two-way, keep as {Binding})
- `StartSessionCommand`

**Keep as {Binding}**:
- ElementName bindings: `Command="{Binding ElementName=RootGrid, Path=DataContext.AddToTicketCommand}"` etc.

---

## Next Steps

1. ✅ Complete PaymentWorkspacePage conversions
2. ⏳ Convert TableWorkspacePage one-way bindings
3. ⏳ Audit other high-traffic pages if time permits
4. ⏳ Document conversion patterns

---

## Notes

- x:Bind requires explicit `Mode=OneWay` for one-way bindings (default is OneTime)
- x:Bind requires explicit `Mode=TwoWay` for two-way bindings
- Converters work with both {Binding} and x:Bind
- ElementName bindings are more complex with x:Bind (Task 3.3)

---

**END OF PROGRESS UPDATE**

