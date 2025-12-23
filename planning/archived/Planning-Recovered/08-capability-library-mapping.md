# Capability -> Library Mapping

## 1. Core Infrastructure

| Capability | Planned Approach | Library Option | Decision | Reason |
|------------|------------------|----------------|----------|--------|
| **DI Container** | `Microsoft.Extensions.DependencyInjection` | `Microsoft.Extensions.DependencyInjection` | **USE** | Standard, supported by WinUI 3 templates. |
| **Logging** | Custom `SafeAppendToLog` | `Serilog` + `Serilog.Sinks.File` | **USE** | Handles rotation, async writing, and structured data out of the box. |
| **Resilience** | `Polly` | `Microsoft.Extensions.Http.Polly` | **USE** | Native integration with `HttpClientFactory`. |
| **Configuration** | `Microsoft.Extensions.Configuration` | `Microsoft.Extensions.Configuration.Json` | **USE** | Supports `appsettings.json` hot-reload. |

## 2. API & Data

| Capability | Planned Approach | Library Option | Decision | Reason |
|------------|------------------|----------------|----------|--------|
| **HTTP Client** | `Refit` | `Refit` | **USE** | Typesafe, compiles to optimized code, removes `HttpClient` boilerplate. |
| **Auth Token Storage** | Custom File I/O | `Microsoft.Toolkit.Uwp.Connectivity` (or newer WinUI equivalent) | **CUSTOM (Wrapped)** | WinUI 3 `PasswordVault` is tricky in Unpackaged apps. We will use a `CredentialLocker` wrapper around Windows APIs. |
| **Connectivity Check** | `NetworkHelper` | `CommunityToolkit.WinUI.Connectivity` | **USE** | Don't reinvent "Am I online?" logic. |

## 3. UI & Logic

| Capability | Planned Approach | Library Option | Decision | Reason |
|------------|------------------|----------------|----------|--------|
| **MVVM** | `CommunityToolkit.Mvvm` | `CommunityToolkit.Mvvm` | **USE** | Source Generators reduce boilerplate by 60%. |
| **Validation** | `ObservableValidator` | `FluentValidation` | **USE** | Keeps validation rules *outside* the ViewModel, closer to Domain logic. |
| **Navigation** | Custom `ShellViewModel` | `CommunityToolkit.WinUI.UI.Controls` (Frame) | **CUSTOM** | WinUI 3 frameworks (Prism/TemplateStudio) are heavy. A simple "VM-First" navigator is safer than adopting a dead framework. |

## 4. Hardware

| Capability | Planned Approach | Library Option | Decision | Reason |
|------------|------------------|----------------|----------|--------|
| **Receipt Printing** | Custom Raw Bytes | `ESCPOS.NET` | **USE** | Handles network/serial plumbing and ESC/POS command generation. |

## 5. Summary of Changes
- **Add**: `Serilog`, `FluentValidation`, `ESCPOS.NET`.
- **Remove**: Custom Logging, Custom Validation logic in VMs.
- **Keep Custom**: Navigation (too critical/fragile to trust to abandonment-prone libraries).
