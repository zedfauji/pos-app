# Library-First Guardrails

## 1. The "Buy-Before-Build" Manifesto

**Rule**: No code shall be written if a Nuget package already exists to solve the problem.
**Exceptions**:
1.  The library is abandoned (Last commit > 2 years ago).
2.  The library introduces unacceptable bloat (e.g., pulling in 50MB of dependencies).
3.  The library violates Clean Architecture (e.g., forces DB logic into UI).

## 2. Default Library Stack (Mandatory)

| Capability | Mandatory Library | Forbidden Alternative |
|------------|-------------------|-----------------------|
| **Logging** | `Serilog` | `System.Console`, `SafeAppendToLog` |
| **HTTP** | `Refit` | Manual `HttpClient` |
| **Resilience** | `Polly` | Custom `try/catch` retry loops |
| **Validation** | `FluentValidation` | Custom `if` statements in Setters |
| **DI** | `Microsoft.Extensions.DI` | `Autofac`, `Ninject` (Keep it simple) |
| **MVVM** | `CommunityToolkit.Mvvm` | `Prism`, Rolling your own `INotifyPropertyChanged` |

## 3. Enforcement Mechanisms

### A. The "Vibe Check"
- **Trigger**: Every Pull Request.
- **Check**: Does this PR add a new class that ends in `Service` or `Helper`?
- **Action**: If yes, ask: "Is there a library for this?"
- **Example**: `CsvHelper.cs` -> **REJECT**. Use `CsvHelper` Nuget.

### B. Wrapper Pattern
**Rule**: External libraries must be wrapped behind Interfaces.
- **Bad**: ViewModel calls `Log.Information("...")`.
- **Good**: ViewModel calls `ILogger.Log(...)`.
- **Reason**: We might swap Serilog for NLog later. The Application Logic shouldn't know.

## 4. Documentation Requirements
If you MUST write custom code for a solved problem:
1.  Create an issue/ADR.
2.  Title: `Custom Implementation Justification: [Capability]`.
3.  Content: "Checked libraries A, B, C. Rejected because X. Estimate to build: 3 days. Estimate to maintain: Forever."
