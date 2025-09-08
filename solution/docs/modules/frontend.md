# Frontend Overview (MagiDesk.Frontend)

- Project: `solution/frontend/MagiDesk.Frontend.csproj`
- Framework: .NET 8, WinUI 3 (Windows App SDK), MVVM-style organization
- Key areas:
  - `Views/` Pages such as `DashboardPage`, `OrdersPage`, `OrderBuilderPage`, `BillingPage`, `CashFlowPage`, `InventoryPage`, `ItemsPage`, `VendorsPage`, `UsersPage`, `SettingsPage`, `TablesPage`, `LoginPage`, `MainPage`.
  - `Dialogs/` e.g., `OrderBuilderDialog`.
  - `Services/` likely includes API client(s) configured via `frontend/appsettings.json`.
  - `Converters/`, `Assets/` for UI helpers and resources.

## Configuration

`frontend/appsettings.json`:
```json
{
  "Backend": { "BaseUrl": "https://localhost:5001" }
}
```
Replace with deployment URL after backend is deployed.

## Notable UX Flows

- Order lifecycle: `OrderBuilderPage`/`OrderBuilderDialog` to compose items, save draft, and finalize via backend `/api/orders`.
- Billing and Cash Flow pages interact with `/api/cashflow` endpoints.
- Inventory and Items pages integrate with `/api/inventory`, `/api/items`, and vendor-nested item routes.
- Vendors page perform vendor CRUD and item management.
- Users and Settings pages map to `/api/users`, `/api/auth/login`, and `/api/settings/*`.

## WinUI Notes

- Avoid unsupported WinUI 3 XAML attributes like `SelectedDateFormat` on `DatePicker`.
- Ensure `DatePicker.DateChanged` handlers match `EventHandler<DatePickerValueChangedEventArgs>`.

## Build & Run

Use Visual Studio 2022 (Windows App SDK installed). Or CLI build:

```powershell
# from solution/
dotnet build frontend/MagiDesk.Frontend.csproj -c Debug
```
