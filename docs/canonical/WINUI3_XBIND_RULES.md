# WinUI 3 x:Bind Rules — Canonical Binding Law

> **This document defines when `x:Bind` (compiled bindings) is allowed vs forbidden in this WinUI 3 project.**

## Why This Matters

`x:Bind` evaluates at **compile-time**, generating strongly-typed code paths. When the XAML parser encounters an `x:Bind` expression and the target property/method is unavailable, null, or incorrectly typed, **the app crashes natively inside `InitializeComponent()`** — with no managed exception, no stack trace, and no debugging information.

---

## ✅ x:Bind is ALLOWED when ALL of the following are TRUE

| Requirement | Example |
|-------------|---------|
| **Page/UserControl level binding** | Binding is NOT inside a `DataTemplate` |
| **Known ViewModel property** | `{x:Bind ViewModel.PropertyName}` |
| **ViewModel is guaranteed non-null** | Page sets `ViewModel` property before `InitializeComponent()` or in constructor |
| **Simple properties only** | No method calls, no async, no navigation |
| **Mode is explicit** | Always specify `Mode=OneWay` or `Mode=TwoWay` |

**Safe examples:**
```xml
<!-- Page-level bindings to ViewModel -->
<TextBlock Text="{x:Bind ViewModel.TableLabel, Mode=OneWay}"/>
<Button Command="{x:Bind ViewModel.GoBackCommand, Mode=OneWay}"/>
<Grid Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVis}}"/>
```

---

## ❌ x:Bind is FORBIDDEN in these scenarios

### 1️⃣ Inside DataTemplates (ALWAYS FORBIDDEN)

```xml
<!-- ❌ NEVER DO THIS -->
<DataTemplate x:DataType="dto:MyDto">
    <TextBlock Text="{x:Bind Name}"/>  <!-- CRASH RISK -->
</DataTemplate>

<!-- ✅ USE THIS INSTEAD -->
<DataTemplate x:DataType="dto:MyDto">
    <TextBlock Text="{Binding Name}"/>
</DataTemplate>
```

**Reason:** DataTemplates are instantiated dynamically at runtime. The compile-time binding path may not resolve correctly, especially with converters.

### 2️⃣ With Converters inside DataTemplates

```xml
<!-- ❌ NEVER DO THIS -->
<TextBlock Text="{x:Bind Price, Converter={StaticResource CurrencyConverter}}"/>

<!-- ✅ USE THIS INSTEAD -->
<TextBlock Text="{Binding Price, Converter={StaticResource CurrencyConverter}}"/>
```

### 3️⃣ CommandParameter self-reference in DataTemplates

```xml
<!-- ❌ NEVER DO THIS -->
<Button CommandParameter="{x:Bind}"/>

<!-- ✅ USE THIS INSTEAD -->
<Button CommandParameter="{Binding}"/>
```

### 4️⃣ In App.xaml / ResourceDictionary

Never use `x:Bind` in application-level resources — DataContext is not available.

### 5️⃣ In ControlTemplates

Template bindings evaluate in a different context than page bindings.

---

## Replacement Strategy

When replacing dangerous `x:Bind`:

| Before | After |
|--------|-------|
| `{x:Bind PropertyName}` | `{Binding PropertyName}` |
| `{x:Bind Path, Converter=...}` | `{Binding Path, Converter=...}` |
| `CommandParameter="{x:Bind}"` | `CommandParameter="{Binding}"` |

For access to parent page's ViewModel from within a DataTemplate:
```xml
<Button Command="{Binding ElementName=RootGrid, Path=DataContext.SomeCommand}"
        CommandParameter="{Binding}"/>
```

---

## Performance Tradeoff

| Binding Type | Performance | Safety |
|--------------|-------------|--------|
| `x:Bind` (compiled) | ⚡ Faster (no reflection) | ⚠️ Can crash natively |
| `{Binding}` (runtime) | 🐢 Slightly slower | ✅ Safe, never crashes |

**For this project, we prioritize stability over performance.**

---

## Quick Reference

| Location | x:Bind Allowed? |
|----------|-----------------|
| Page-level ViewModel properties | ✅ Yes |
| Page-level Command bindings | ✅ Yes |
| Inside `<DataTemplate>` | ❌ **NO** |
| With converters in DataTemplate | ❌ **NO** |
| `CommandParameter={x:Bind}` in DataTemplate | ❌ **NO** |
| App.xaml / ResourceDictionary | ❌ **NO** |
| ControlTemplate | ❌ **NO** |

---

## Summary

> **Use `x:Bind` only at page/control level. Inside DataTemplates, always use `{Binding}`.**

This is **binding law** for this project.

---

*Last updated: 2025-12-23*
