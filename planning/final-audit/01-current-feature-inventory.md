# 01 - Current Feature Inventory (New System)

## 1. Tables & Sessions
**Description**: Real-time view of restaurant tables, occupancy status, and session management.
- **Status**: ✅ **Complete**
- **Backend Ownership**: YES (`TablesApi` manages state).
- **UI Logic**: Minimal (View binding only).
- **Components**: `TableViewModel`, `TableMapPage`, `TablesApi`.

## 2. Orders
**Description**: Adding items to a table session.
- **Status**: ✅ **Complete**
- **Backend Ownership**: YES (`OrderApi`, `CreateOrderCommand`).
- **UI Logic**: None (Client sends commands).
- **Components**: `OrderViewModel`, `OrderApi`, `InventoryApi` (for menu).

## 3. Billing & Payments
**Description**: Closing sessions, calculating totals, splitting bills (if implemented), and recording payments.
- **Status**: 🟡 **Partial**
- **Backend Ownership**: YES (`PaymentApi`, `StopSessionAsync` returns final bill).
- **UI Logic**: Some "Pre-calculation" for UI preview exists in `TableViewModel.CloseSessionAsync` (`items.Sum(...)`), likely strictly for display before confirming.
- **Components**: `PaymentApi`, `BillingViewModel` (implied), `TableViewModel`.
- **Note**: "Split Bill" logic functionality not fully verified in code inspection but API endpoints exist.

## 4. Inventory
**Description**: Management of menu items, categories, and prices.
- **Status**: ✅ **Complete**
- **Backend Ownership**: YES (`InventoryApi`, `MenuApi`).
- **UI Logic**: None.
- **Components**: `InventoryApi`, `MenuApi`.

## 5. Reporting & Analytics
**Description**: End-of-day reports, sales summaries, cashier reconciliation.
- **Status**: ❌ **Stub / Missing**
- **Backend Ownership**: NO (No `ReportingApi` found).
- **UI Logic**: N/A.
- **CRITICAL**: No dedicated reporting UI or backend service found.

## 6. Printing / Hardware
**Description**: Receipt printing via thermal printer.
- **Status**: 🟡 **Partial** (Infrastructure Ready, Operational Verification Needed)
- **Backend Ownership**: NO (Client-side driver `ESCPOS.NET`).
- **UI Logic**: Yes (Print formatting logic in `PrinterService` or Client).
- **Note**: Printing is a client-side concern in this architecture, which is acceptable, but formatting logic must be robust.

## 7. Authentication / Authorization
**Description**: User login (PIN/Password) and role enforcement.
- **Status**: ✅ **Complete**
- **Backend Ownership**: YES (`UsersApi`, `AuthService`).
- **UI Logic**: None.
- **Components**: `AuthService`, `UsersApi`.

## 8. System / Infrastructure
**Description**: Settings, API configuration, Resilience.
- **Status**: ✅ **Complete**
- **Backend Ownership**: YES (`SettingsApi`).
- **Client**: `Polly` policies, `Serilog` logging.
