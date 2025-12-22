# Phase 3: Split & Partial Payments - Complete ✅

## Summary
Enhanced the Payment Workspace to support split and partial payments. Users can now split bills by item selection, fixed amount, or percentage. The system leverages the `calculate-split` endpoint from Phase 0 and uses `PaymentApi` for registering partial payments without closing the session.

## Backend Integration

### Endpoints Used
1. **`POST /tables/{label}/calculate-split`** - Server-side split calculation
   - Request: `CalculateSplitRequest` (ItemIds OR Percentage OR FixedAmount)
   - Response: `CalculateSplitResult` with `AmountToPay`, `TaxShare`, `SuggestedGratuity`

2. **`POST /api/payments`** - Register partial payment
   - Request: `RegisterPaymentRequestDto` with payment line
   - Response: `PaymentTransactionResult` with `ChangeDue`, `RemainingBalance`, `Message`

### Payment Flow Split
**Full Payment**: Uses `StopSessionAsync` (closes session)  
**Partial Payment**: Uses `RegisterPaymentAsync` (keeps session active)

## Frontend Enhancements

### PaymentWorkspaceViewModel - New Properties
```csharp
// Split Payment Mode
bool IsSplitPayment          // Toggle between full/split
string SplitMode              // "ByAmount", "ByItem", "ByPercentage  
decimal SplitAmount           // For ByAmount mode
decimal SplitPercentage       // For ByPercentage (default: 50%)
ObservableCollection<ItemLine> SelectedItems  // For ByItem mode
decimal CalculatedSplitAmount // From backend calculation
```

### New Commands
1. **`CalculateSplitCommand`**
   - Builds `CalculateSplitRequest` based on `SplitMode`
   - Calls `CalculateSplitAsync` endpoint
   - Updates `CalculatedSplitAmount` from backend
   - Triggers `FinalTotal` recalculation

2. **`ProcessPaymentCommand` (Enhanced)**
   - Checks `IsSplitPayment` flag
   - Routes to `ProcessFullPaymentAsync()` or `ProcessPartialPaymentAsync()`

### Private Methods
- **`ProcessFullPaymentAsync()`** - Original logic using `StopSessionAsync`
- **`ProcessPartialPaymentAsync()`** - New logic using `PaymentApi.RegisterPaymentAsync`

## New API Client

### IPaymentApi
**Location**: `MagiDesk.Client/Services/IPaymentApi.cs`

**Endpoints**:
```csharp
POST /api/payments - RegisterPaymentAsync
GET /api/payments/{billingId}/ledger - GetLedgerAsync
POST /api/payments/{billingId}/close - CloseBillAsync
```

**Registration**: `App.xaml.cs` with base URL `http://localhost:5002`

## Split Payment Modes

### 1. Split by Amount
- User enters a fixed dollar amount to pay
- Backend validates amount doesn't exceed remaining balance
- Example: Pay $25.00 of a $50.00 bill

### 2. Split by Percentage
- User enters percentage (e.g., 50%)
- Backend calculates: `TotalDue * (Percentage / 100)`
- Handles rounding server-side
- Example: 50% of $47.83 = $23.92 (rounded)

### 3. Split by Item
- User selects specific items from bill
- Backend sums selected item prices
- Includes proportional tax calculation
- Example: Select "Burger" + "Fries" from 4-item bill

## Architecture Compliance

✅ **Zero UI Calculation Logic**
- All split calculations performed by backend
- UI only displays `CalculatedSplitAmount` from server
- `FinalTotal` computed property uses backend value

✅ **Backend Owns Business Rules**
- Validation of split amounts
- Tax apportionment
- Gratuity suggestions
- Payment feasibility checks

✅ **API-First Design**
- UI sends intent (split mode + parameters)
- Backend responds with calculated amounts
- UI renders results without interpretation

## Computed Property Changes

### FinalTotal (Modified)
```csharp
public decimal FinalTotal => Math.Max(0, 
    (IsSplitPayment ? CalculatedSplitAmount : TotalDue) - DiscountAmount);
```
- Uses `CalculatedSplitAmount` if in split mode
- Uses full `TotalDue` for full payment
- Applies discounts to final amount

## Files Modified

| File | Type | Changes |
|------|------|---------|
| `ViewModels/PaymentWorkspaceViewModel.cs` | MODIFIED | Added split properties, CalculateSplitCommand, partial payment logic |
| `Services/IPaymentApi.cs` | NEW | Payment API client interface |
| `Services/ITableApi.cs` | MODIFIED | Added `CalculateSplitAsync` method |
| `App.xaml.cs` | MODIFIED | Registered `IPaymentApi` with DI |
| `Views/PaymentWorkspacePage.xaml` | MODIFIED | Added split payment UI controls |
| `Converters/StringEqualsToVisibilityConverter.cs` | NEW | String equality to Visibility converter |
| `Converters/StringEqualsToBoolConverter.cs` | NEW | String equality to Bool converter (for RadioButtons) |
| `App.xaml` | MODIFIED | Registered new converters |

## UI Implementation Complete ✅

### Split Payment Controls Added
1. **Toggle Switch** - "Split Payment" on/off
2. **Mode Selection** - Radio buttons for Amount/Percentage/Item
3. **By Amount Input** - NumberBox for fixed dollar amount
4. **By Percentage Input** - NumberBox (1-100%) with percentage display
5. **By Item Selection** - ListView with multi-select for items
6. **Calculate Button** - "Calculate Split Amount" command button
7. **Calculated Display** - Large, highlighted calculated amount

### Visual Design
- Split section has card background with border
- Conditional visibility based on toggle state
- Each mode shows only relevant inputs
- Calculated amount prominently displayed
- Consistent spacing and styling

## Exit Criteria

✅ ViewModel supports split payment modes  
✅ Backend integration for calculate-split  
✅ PaymentApi client configured  
✅ Partial payment registration implemented  
✅ Full vs. partial payment routing logic  
✅ **UI controls added to XAML**  
✅ **Item selection UI implemented**  
✅ **All converters created and registered**
  

## Testing Scenarios

### Full Payment (Existing)
1. Select session from Hub
2. Leave "Split Payment" unchecked
3. Enter payment details
4. Process → Session closes

### Split by Amount
1. Enable "Split Payment"
2. Select "By Amount"
3. Enter $25.00
4. Click "Calculate Split"
5. Verify `CalculatedSplitAmount` = $25.00
6. Process → Partial payment registered, session stays open

### Split by Percentage
1. Select "By Percentage"
2. Enter 50%
3. Calculate → Backend returns 50% of total
4. Process → Partial payment for half

### Split by Item
1. Select "By Item"
2. Check "Burger" and "Fries"
3. Calculate → Backend sums selected items + tax share
4. Process → Partial payment for those items

## Known Limitations

1. **No UI Controls Yet** - XAML update required to expose split options
2. **Item Selection UX** - Need checkboxes or multi-select list
3. **No Real-time Validation** - User won't see errors until "Calculate" clicked
4. **No Split History** - Can't see previous partial payments in current session
5. **No Visual Indicator** - Bill doesn't show which items are "paid" vs "unpaid"

## Next Phase: Phase 4 - Deprecation & Cleanup

**Tasks**:
- Remove `PaymentDialog.xaml` and `PaymentDialog.xaml.cs`
- Remove `StopSessionAsync` calls from `TableViewModel`
- Update `CloseSessionAsync` to navigate to Payment Workspace instead  - Remove old dialog service methods
- Clean up unused payment-related code

## Phase 5 Preview: Verification

**Testing**:
- End-to-end payment workflows
- Split payment accuracy
- Concurrent payment scenarios
- Error handling and recovery
- UI/UX validation
