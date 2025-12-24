# System Financial Parity Checklist

This document outlines the specific financial calculations and rules that must be strictly adhered to in the new system to match legacy behavior.

## 1. Tax Calculation
- **Source**: Legacy reads `Printer:TaxPercent` from `appsettings.json` (Client-Side).
- **Target**: Backend `SettingsApi` (Server-Side).
- **Rule**:
  - `OrderService.CreateOrderAsync` must calculate tax.
  - `TaxAmount` = `Subtotal` * `TaxRate` (e.g., 0.08).
  - Rounding: `Math.Round(amount, 2)`.
  - **Gap**: Current `OrderService` sets Tax = 0. **Must Fix**.

## 2. Totals Aggregation
- **Formula**: `Total` = `Subtotal` + `Tax` - `Discount`.
- **Validation**:
  - Verify `OrderDto.Total` matches this formula.
  - Verify `Bill.TotalAmount` matches sum of Orders + Time Cost.

## 3. Time Cost
- **Rate**: Legacy hardcodes `$70.00 / hour` (in `BillingService.cs` equivalent or logic).
- **Grace Period**: 10 Minutes free? (Found in `BillingService.cs` new code, need to verify if legacy had it).
- **Tax on Time**: Does Time Cost get taxed?
  - *Observation*: Legacy `TablesPage` preview just showed "Total".
  - *Action*: Configure "IsTimeTaxable" setting. Default to `True` to be safe, or check local regulations.

## 4. Discounts
- **Application**: Applied *after* Tax? OR *before* Tax?
  - Standard Logic: Discount on Subtotal -> Tax on (Subtotal - Discount).
  - Legacy Logic: Needs robust testing.
  - **Current New Logic**: `OrderRepository` stores `discount_total`. `CreateOrder` takes `DiscountTotal`.
  - **Action**: Verify `OrderService` formula.

## 5. Rounding
- **Rule**: `MidpointRounding.AwayFromZero` or `ToEven`?
- Standard: `Math.Round(val, 2)` (MidpointRounding.ToEven by default in .NET).
- **Action**: Standardize on `Math.Round(val, 2, MidpointRounding.AwayFromZero)` for currency to avoid "Banker's Rounding" surprises if Legacy used simple rounding.

## 6. Split Payments
- **Constraint**: Sum of splits must equal Total +- 0.01.
- **Handling**: Remainder assigned to "Cash" (Legacy behavior observed in `EphemeralPaymentPage.xaml.cs`).
- **New System**: `PaymentHub` must replicate this auto-assignment.
