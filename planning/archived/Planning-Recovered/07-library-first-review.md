# Library-First Planning Review

## 1. Executive Summary
The current plan (v1.0 - v3.0) is "Architecture-Aware" but not fully "Library-First".
- **Good**: It correctly chooses `Refit` and `NetArchTest` over custom solutions.
- **Risk**: It assumes custom implementations for `AuthService`, `Navigation`, and `Printing`.
- **Verdict**: We are at risk of "Not Invented Here" (NIH) syndrome in the `Printing` and `Validation` domains.

## 2. Capability Gap Analysis

| Capability | Current Plan | Library-First Verdict | Risk Level |
|------------|--------------|-----------------------|------------|
| **API Client** | `Refit` | ✅ **Approved**. Standard industry practice. | Low |
| **Resilience** | `Polly` | ✅ **Approved**. | Low |
| **MVVM** | `CommunityToolkit.Mvvm` | ✅ **Approved**. Lighter than Prism/ReactiveUI for this scale. | Low |
| **Printing** | `RawPrinterService` (Custom) | ❌ **Reject**. Reinventing ESC/POS protocols is error-prone. Should use `ESCPOS.NET` or similar. | **High** |
| **Logging** | "SafeAppendToLog" (Custom) | ❌ **Reject**. This is dangerous. Must use `Serilog` + `Microsoft.Extensions.Logging`. | **Critical** |
| **Validation** | `ObservableValidator` | ⚠️ **Caution**. Good for simple UI, but mixing rules in ViewModels violates "Backend as Truth". Consider `FluentValidation` mapping DTOs. | Medium |
| **Navigation** | Custom `ShellViewModel` | ⚠️ **Caution**. Custom navigation stacks often have edge-case bugs (back button, modal stacking). | Medium |

## 3. Areas of "Wheel Reinvention"
1.  **Crash Logging**: `App.xaml.cs` currently has a custom `SafeAppendToLog` method. This is technical debt from Day 1. It must be replaced by a structured logger (Serilog) writing to a Rolling File Sink.
2.  **Printer Communication**: Writing raw bytes to a USB stream is complex (finding the device, claiming interface, error handling). A library like `ESCPOS.NET` handles the hardware abstraction.

## 4. Recommendation
The WBS must be updated to replace "Implement X" with "Integrate Library Y".
