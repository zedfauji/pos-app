# 04 - Anti-Drift Guardrails

## 1. Forbidden Patterns (Zero Tolerance)
- **NO SQL in Controllers**: All DB access must be in `Core` / `Infrastructure` repositories.
- **NO Business Logic in ViewModels**: `TableViewModel` must blindly display what API returns.
    - *Violation*: `var total = items.Sum(x => x.price)` is BANNED.
    - *Correction*: `var total = apiResult.TotalAmount`.
- **NO Direct Printing from UI Logic**: UI sends data to `PrinterService`; `PrinterService` formats it. The ViewModel should not know what ESC/POS is.

## 2. API Layer Checks
- **New Controllers**: Must return `ActionResult<Dto>`, not Entity.
- **Error Handling**: Must use `Result` pattern or Global Exception Handler (don't wrap every controller method in try/catch).

## 3. UI Layer Checks
- **WinUI**: No code-behind logic in `Page.xaml.cs` except constructor `InitializeComponent()`.
- **State**: No `static` "Cart" class. All state is in ViewModel or API.

## 4. Enforcement Strategy
- **Pre-Commit**: Run `dotnet test MagiDesk.Client.ArchTests`.
- **Manual Review**: Any PR with `System.Data` or `Microsoft.EntityFrameworkCore` in Client project is rejected.
