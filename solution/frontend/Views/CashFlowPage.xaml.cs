using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using System;

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
                ShowInfo("Cash entry submitted.");
                CashAmountBox.Value = 0;
                NotesBox.Text = string.Empty;
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
        ShowInfo("Refreshed.");
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
            Title.Text = App.I18n.T("cash_flow_title");
            EmployeeNameBox.Header = App.I18n.T("employee_name");
            EntryDatePicker.Header = App.I18n.T("date");
            CashAmountBox.Header = App.I18n.T("cash_amount");
            SubmitBtn.Content = App.I18n.T("submit_cash_entry");
            NotesBox.Header = App.I18n.T("notes");
            BtnRefresh.Label = App.I18n.T("refresh");
        }
        catch { }
    }

    private void HideFeedback() => FeedbackBar.IsOpen = false;
}
