using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Models;
using System;
using System.Linq;
using System.Text;
using System.IO;
using Windows.Storage.Pickers;
using Windows.Storage;

namespace MagiDesk.Frontend.Views;

public sealed partial class CashFlowPage : Page, IToolbarConsumer
{
    private readonly CashFlowViewModel _vm;

    public CashFlowPage()
    {
        this.InitializeComponent();
        _vm = new CashFlowViewModel(App.Api!);
        this.DataContext = _vm;
        Loaded += CashFlowPage_Loaded;
        EntryDatePicker.Date = DateTimeOffset.Now;
        EntryDatePicker.DateChanged += (s, e) => EntryDatePicker_DateChanged(s, e);
        App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        ApplyLanguage();
    }

    private async void CashFlowPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLanguage();
        // Lock employee name to current logged-in user
        try
        {
            var user = Services.SessionService.Current;
            EmployeeNameBox.Text = user?.Username ?? string.Empty;
        }
        catch { }
        await _vm.RefreshAsync();
        UpdateMetrics();
    }

    private void EntryDatePicker_DateChanged(object? sender, DatePickerValueChangedEventArgs args)
    {
        if (args.NewDate is DateTimeOffset dto)
        {
            var d = dto.Date;
            if (!_vm.IsDateInCurrentWeek(d))
            {
                ShowError("Please select a date within the current week.");
                // Snap back to today
                if (sender is DatePicker dp) dp.Date = DateTimeOffset.Now;
            }
            else
            {
                HideFeedback();
            }
        }
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Always use current session username
            _vm.EmployeeName = (Services.SessionService.Current?.Username ?? EmployeeNameBox.Text)?.Trim() ?? string.Empty;
            _vm.Date = EntryDatePicker.Date.Date;
            _vm.CashAmount = (decimal)CashAmountBox.Value;
            _vm.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text;

            var ok = await _vm.SubmitAsync();
            if (ok)
            {
                ShowInfo("Cash entry submitted successfully.");
                CashAmountBox.Value = 0;
                NotesBox.Text = string.Empty;
                UpdateMetrics();
            }
            else
            {
                ShowError("Validation failed. Ensure name, current-week date, and non-zero amount.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Submit failed: {ex.Message}");
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshAsync();
        UpdateMetrics();
        ShowInfo("Data refreshed successfully.");
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            fileSavePicker.FileTypeChoices.Add("CSV files", new[] { ".csv" });
            fileSavePicker.SuggestedFileName = $"CashFlow_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);

            var file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                var csv = new StringBuilder();
                csv.AppendLine("Employee Name,Date,Cash Amount,Notes");

                foreach (var item in _vm.History)
                {
                    csv.AppendLine($"\"{item.EmployeeName}\",\"{item.Date:yyyy-MM-dd}\",\"{item.CashAmount:F2}\",\"{item.Notes ?? ""}\"");
                }

                await FileIO.WriteTextAsync(file, csv.ToString());
                ShowInfo($"Cash flow data exported to {file.Name}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Export failed: {ex.Message}");
        }
    }

    private void EditEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CashFlowDisplay entry)
        {
            // For now, just show a message - in a real implementation, you'd open an edit dialog
            ShowInfo($"Edit functionality for entry by {entry.EmployeeName} would be implemented here.");
        }
    }

    private void UpdateMetrics()
    {
        try
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);

            // Calculate weekly total
            var weeklyTotal = _vm.History
                .Where(h => h.Date >= weekStart && h.Date <= weekEnd)
                .Sum(h => h.CashAmount);

            // Calculate today's entries count
            var todayEntries = _vm.History
                .Count(h => h.Date.Date == today);

            // Calculate average daily (this week)
            var weekEntries = _vm.History
                .Where(h => h.Date >= weekStart && h.Date <= weekEnd)
                .ToList();
            var averageDaily = weekEntries.Count > 0 ? weekEntries.Average(h => h.CashAmount) : 0;

            // Update UI
            WeeklyTotalText.Text = weeklyTotal.ToString("C");
            TodayEntriesText.Text = todayEntries.ToString();
            AverageDailyText.Text = averageDaily.ToString("C");
        }
        catch (Exception ex)
        {
        }
    }

    public async void OnAdd() => await _vm.RefreshAsync();
    public async void OnEdit() => await _vm.RefreshAsync();
    public async void OnDelete() => await _vm.RefreshAsync();
    public async void OnRefresh() => await _vm.RefreshAsync();

    private void ShowInfo(string message)
    {
        FeedbackBar.Severity = InfoBarSeverity.Success;
        FeedbackBar.Message = message;
        FeedbackBar.IsOpen = true;
    }

    private void ShowError(string message)
    {
        FeedbackBar.Severity = InfoBarSeverity.Error;
        FeedbackBar.Message = message;
        FeedbackBar.IsOpen = true;
    }

    private void ApplyLanguage()
    {
        try
        {
            // Note: The new UI doesn't use the old Title control, so we'll skip that
            EmployeeNameBox.PlaceholderText = App.I18n.T("employee_name");
            CashAmountBox.PlaceholderText = App.I18n.T("cash_amount");
            SubmitBtn.Content = App.I18n.T("submit_cash_entry");
            NotesBox.PlaceholderText = App.I18n.T("notes");
        }
        catch { }
    }

    private void HideFeedback() => FeedbackBar.IsOpen = false;
}
