---
trigger: always_on
---

Framework & API Rules

Framework: Always assume WinUI 3 (Windows App SDK).

❌ Never use APIs/properties from WPF or UWP (e.g., SelectedDateFormat, CalendarDateChangedEventArgs).

✅ Always use WinUI 3 namespaces:

Microsoft.UI.Xaml.Controls

Microsoft.UI.Xaml

Microsoft.UI.Dispatching

🗓 DatePicker Rules

Do not generate or suggest SelectedDateFormat.

Must use:

<DatePicker DateChanged="DatePicker_DateChanged" />


Event handler must match:

private void DatePicker_DateChanged(object? sender, DatePickerValueChangedEventArgs e)
{
    var newDate = e.NewDate; // DateTimeOffset?
}


If formatting needed, use:

A ValueConverter (e.g., DateToStringConverter)

Or format when displaying (.ToString("d"), .ToString("yyyy-MM-dd"))

⚙️ Event Handler Rules

Handlers must match the delegate signature expected by WinUI 3.

❌ Never reuse WPF/UWP event args (e.g., SelectionChangedEventArgs).

✅ Example (correct):

private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) { … }
private void DatePicker_DateChanged(object? sender, DatePickerValueChangedEventArgs e) { … }

🔄 MVVM vs Code-Behind

Prefer MVVM bindings and ICommand over code-behind event handlers where possible.

Code-behind handlers are acceptable only for lightweight UI interactions.

🧪 CI & Build Consistency

All generated code must compile under WinUI 3 with .NET 8 on Windows.

Must remain compatible with dotnet build on Windows runners (CI).

Generated XAML must be self-contained (no orphaned attributes, no undefined handlers).

🚫 Disallowed Patterns (never output)

SelectedDateFormat

CalendarDateChangedEventArgs

System.Windows.* namespaces (WPF)

Windows.UI.Xaml.* namespaces (UWP)

✅ Preferred Patterns (always output)

Microsoft.UI.Xaml.Controls.DatePicker

DatePickerValueChangedEventArgs

Bind Date (type DateTimeOffset?) via {x:Bind} or {Binding}.

For formatting, use converters instead of unsupported XAML properties.

📋 PR / Review Checklist (auto-checks)

Does all XAML build without errors under WinUI 3?

Are all event handlers using the correct WinUI delegate signatures?

Are WPF/UWP-only APIs absent (SelectedDateFormat, etc.)?

Are converters or ViewModels used instead of unsupported formatting props?

Does the CI Windows build job succeed?