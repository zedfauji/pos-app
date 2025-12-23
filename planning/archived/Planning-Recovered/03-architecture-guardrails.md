# Architectural Guardrails (Enforcement System)

## 1. The "Thin Client" Purity Law
**Rule**: The Project `MagiDesk.Client` must **NEVER** fail an architecture test.
**Enforcement**: A dedicated XUnit project `MagiDesk.Client.ArchTests` will run on every build.

## 2. Forbidden Dependencies (The Banned List)

| Package / Namespace | Reason | Enforcement |
|---------------------|--------|-------------|
| `Npgsql` | **Direct DB Access**. Zero tolerance. | `Directory.Build.props` `<Target Name="BanNpgsql" ...>` |
| `System.Data.SqlClient` | Direct DB Access. | ArchTest: `ShouldNotHaveDependencyOn("System.Data")` |
| `MagiDesk.Frontend.Services.BillingService` (Legacy) | Hardcoded SQL logic. | Naming Convention Ban. |
| `Newtonsoft.Json` | Legacy overhead. Use `System.Text.Json`. | Nuget Ban. |

## 3. Forbidden Patterns

### A. The "Service Locator" Anti-Pattern
**Forbidden**:
```csharp
var api = App.Services.GetService<IApiService>(); // BANNED
```
**Required**:
```csharp
public OrderViewModel(IApiService api) { ... } // REQUIRED
```
**Enforcement**: Code Review + ArchTest `Classes().That().AreClasses().Should().HaveDependencyOn("System.IServiceProvider") == False`.

### B. "Code-Behind" Logic
**Forbidden**: Writing logic in `Page.xaml.cs` (except pure UI composition).
**Required**: `Command="{Binding MyCommand}"`.
**Enforcement**: Manual Review (Hard to automate fully, but can scan for distinct keywords).

### C. "Static Global State"
**Forbidden**: `public static User CurrentUser { get; set; }` in `App.xaml.cs`.
**Required**: `IUserSession` injected service.

## 4. Required Patterns

### A. API Interaction
**Rule**: UI never calls `HttpClient` directly.
**Pattern**: UI calls `IOrderApi` (Refit Interface).
**Violation Handling**: Revert PR immediately.

### B. Navigation
**Rule**: Navigation is a Side Effect.
**Pattern**: `Messenger.Send(new NavigateToMessage(typeof(OrderViewModel)))`.

## 5. "Break Glass" Procedure
If a requirement seems to necessitate breaking a guardrail (e.g., "We need 5ms latency, API is too slow"):
1.  **Stop**.
2.  **Write an Architecture Decision Record (ADR)**.
3.  **Get Approval** from Principal Architect.
4.  **Implement** as an isolated "Anti-Corruption Layer" with a huge `// WARNING` banner.
