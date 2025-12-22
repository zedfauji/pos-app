using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class DashboardPage : Page
{
    private readonly DashboardViewModel _vm;

    public DashboardPage()
    {
        this.InitializeComponent();
        _vm = new DashboardViewModel(App.Api!);
        Loaded += DashboardPage_Loaded;
    }

    private async void BtnRefreshReminders_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.LoadRemindersAsync();
            if (UpcomingList != null) UpcomingList.ItemsSource = _vm.UpcomingReminders;
            if (TodayList != null) TodayList.ItemsSource = _vm.TodayReminders;
            if (NoRemindersText != null)
                NoRemindersText.Visibility = (_vm.TodayReminders.Count == 0 && _vm.UpcomingReminders.Count == 0) ? Visibility.Visible : Visibility.Collapsed;

            if (_vm.TodayReminders.Count > 0 && FeedbackBar is not null)
            {
                FeedbackBar.Severity = InfoBarSeverity.Informational;
                FeedbackBar.Message = $"Today's Reminder: {string.Join(", ", _vm.TodayReminders.Select(v => v.Name))}";
                FeedbackBar.IsOpen = true;
            }
            else if (_vm.UpcomingReminders.Count > 0 && FeedbackBar is not null)
            {
                FeedbackBar.Severity = InfoBarSeverity.Informational;
                FeedbackBar.Message = $"Upcoming Reminder: {string.Join(", ", _vm.UpcomingReminders.Select(v => v.Name))}";
                FeedbackBar.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Refresh reminders failed", ex);
        }
    }

    private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await CheckConnectivityAsync();
            await _vm.LoadAsync();
            VendorsCountText.Text = _vm.VendorsCount.ToString();
            ItemsCountText.Text = _vm.ItemsCount.ToString();
            // Bind reminders
            if (UpcomingList != null) UpcomingList.ItemsSource = _vm.UpcomingReminders;
            if (TodayList != null) TodayList.ItemsSource = _vm.TodayReminders;
            // Show reminder banners if any
            if (_vm.TodayReminders.Count > 0 && FeedbackBar is not null)
            {
                FeedbackBar.Severity = InfoBarSeverity.Informational;
                FeedbackBar.Message = $"Today's Reminder: {string.Join(", ", _vm.TodayReminders.Select(v => v.Name))}";
                FeedbackBar.IsOpen = true;
            }
            else if (_vm.UpcomingReminders.Count > 0 && FeedbackBar is not null)
            {
                FeedbackBar.Severity = InfoBarSeverity.Informational;
                FeedbackBar.Message = $"Upcoming Reminder: {string.Join(", ", _vm.UpcomingReminders.Select(v => v.Name))}";
                FeedbackBar.IsOpen = true;
            }
            if (FeedbackBar is not null && !FeedbackBar.IsOpen)
            {
                FeedbackBar.Severity = InfoBarSeverity.Success;
                FeedbackBar.Message = "Dashboard counts loaded.";
                FeedbackBar.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Dashboard load failed", ex);
            if (FeedbackBar is not null)
            {
                FeedbackBar.Severity = InfoBarSeverity.Error;
                FeedbackBar.Message = $"Failed to load counts: {ex.Message}";
                FeedbackBar.IsOpen = true;
            }
        }
    }

    private async Task CheckConnectivityAsync()
    {
        try
        {
            var ok = await App.UsersApi!.PingAsync();
            if (!ok && FeedbackBar is not null)
            {
                FeedbackBar.Severity = InfoBarSeverity.Warning;
                FeedbackBar.Message = "Backend is unavailable. Some data may be missing.";
                FeedbackBar.IsOpen = true;
            }
        }
        catch
        {
            if (FeedbackBar is not null)
            {
                FeedbackBar.Severity = InfoBarSeverity.Warning;
                FeedbackBar.Message = "Backend is unavailable. Some data may be missing.";
                FeedbackBar.IsOpen = true;
            }
        }
    }
}
