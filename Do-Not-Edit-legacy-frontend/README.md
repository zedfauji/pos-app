# ⚠️ DO NOT EDIT - LEGACY REFERENCE

**Status**: READ-ONLY  
**Purpose**: Business logic reference and parity audit

---

## What This Is

This directory contains the **original MagiDesk WPF/WinUI 3 frontend** that was used in production. It serves as:

1. **Business Logic Reference** - How workflows actually behave
2. **Parity Audit Source** - Compare new implementation against
3. **Edge Case Discovery** - Find "ugly but relied upon" behaviors

---

## Contents

| Folder | Count | Description |
|:-------|:------|:------------|
| `ViewModels/` | 27 | Business logic |
| `Services/` | 51 | API and domain services |
| `Views/` | 140 | XAML pages |
| `Dialogs/` | 24 | Modal workflows |
| `Converters/` | 14 | Data formatting |

---

## Rules

### ❌ DO NOT

- Edit any file
- Move any file out of this folder
- Delete any file
- Use this code in production
- Copy code directly (rewrite instead)

### ✅ DO

- Read to understand workflows
- Compare against new implementation
- Reference for edge cases
- Use for parity audits

---

## Key Files for Reference

| File | Why Important |
|:-----|:--------------|
| `ViewModels/PaymentViewModel.cs` | Payment workflow logic |
| `Views/TablesPage.xaml.cs` | Complete table management (1944 lines) |
| `Services/BillingService.cs` | Billing calculations |
| `Services/TableRepository.cs` | Session management |
| `Services/SplitPaymentCalculator.cs` | Split payment logic |

---

## Migration Status

The new implementation in `solution/MagiDesk.Client/` is a **rewrite**, not a refactor. 

This legacy code remains for reference until the new system reaches full parity.

---

**DO NOT MODIFY THIS DIRECTORY**
