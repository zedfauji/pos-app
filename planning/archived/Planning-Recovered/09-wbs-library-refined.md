# WBS Refinement (Library-First) - v2.0

*Supersedes `01-master-wbs.md` and `02-wbs-refinement*.md`. Refers to them for context.*

## 1. Project Infrastructure Setup
- **Objective**: Establish the "Library-First Golden Path".

### 1.1. Project Shell & Nuget Inventory
- **Action**: Create `MagiDesk.Client` (WinUI 3).
- **Library Integration**:
    - Install `CommunityToolkit.Mvvm`.
    - Install `Refit`.
    - Install `Serilog` + `Serilog.Sinks.File`.
    - Install `Microsoft.Extensions.Hosting` (for generic host/DI).
    - Install `Microsoft.Extensions.Http.Polly`.
- **Constraint**: `Directory.Build.props` bans `Npgsql` (as per Guardrails).

### 1.2. Architecture Enforcer
- **Library Integration**: Install `NetArchTest.Rules`.
- **Test**: `Frontend_Must_Not_Use_HttpClient_Directly()`.
- **Test**: `Frontend_Must_Use_Refit_Interfaces()`.

## 2. Core Plumbings

### 2.1. Authentication
- **Constraint**: Do NOT write a custom `AuthService` logic for token parsing if possible.
- **Library Integration**: Use `System.IdentityModel.Tokens.Jwt` for token inspection.
- **Storage**: Use `CredentialLocker` wrapper (Custom but unavoidable on Windows).

### 2.3. Logging Infrastructure
- **Action**: Configure `Serilog` in `App.xaml.cs`.
- **Configuration**: Write to `%LOCALAPPDATA%/MagiDesk/logs/log-.txt` with 7-day retention.
- **Benefit**: Replaces custom `SafeAppendToLog`.

## 3. The "Table Map" Feature

### 3.1. Table State (Polly Integration)
- **Library Integration**: Define `Polly` Retry Policy in `Program.cs` for `ITableApi`.
- **Logic**: Zero retry logic in ViewModel.

## 4. The "Order & Billing" Feature

### 4.0. Hardware Integration (Receipts)
- **Library Integration**: Install `ESCPOS.NET`.
- **Action**: Create `IPrinterService` that wraps `ESCPOS.NET`.
    - *Why Wrap?* To allow swapping `ESCPOS.NET` for another lib if hardware changes.
- **Logic**: Backend returns JSON -> Client maps to `EscPos` commands -> Library sends to USB.

### 4.1. Validation Logic
- **Library Integration**: Install `FluentValidation`.
- **Action**: Create `OrderValidator : AbstractValidator<OrderDto>`.
- **Usage**: ViewModel calls `validator.Validate(order)`.

### 4.2. Navigation
- **Implementation**: Custom `ShellViewModel` (Maintained decision from Phase 10).
- **Justification**: No suitable lightweight WinUI 3 navigation library exists that supports our strict "VM-First" requirement without bloat.

## 5. Summary of Diff
- **Added**: Serilog setup, Polly policies, ESCPOS.NET wrapper task, FluentValidation rules.
- **Removed**: "Implement Logging", "Implement Retry Logic", "Implement Raw Printer Service" (Replaced with "Wrap Library").
