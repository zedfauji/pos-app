using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Views;

public sealed partial class SessionsPage : Page
{
    public ObservableCollection<SessionOverview> Sessions { get; } = new();
    private readonly TableRepository _repo = new TableRepository();
    private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };

    public SessionsPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        Loaded += SessionsPage_Loaded;
        Unloaded += SessionsPage_Unloaded;
    }

    private async void SessionsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _timer.Tick += Timer_Tick;
        _timer.Start();
        await RefreshAsync();
    }

    private void SessionsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
    }

    private async void Timer_Tick(object sender, object e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            DateTimeOffset? from = null, to = null;
            try
            {
                if (FromDatePicker != null)
                {
                    var d = FromDatePicker.Date;
                    if (d.Year > 1900) from = d;
                }
            }
            catch { }
            try
            {
                if (ToDatePicker != null)
                {
                    var d = ToDatePicker.Date;
                    if (d.Year > 1900) to = d;
                }
            }
            catch { }
            var table = TableFilterText?.Text;
            var server = ServerFilterText?.Text;
            var list = await _repo.GetSessionsAsync(100, from, to, table, server);
            
            // Apply filters
            try
            {
                // Toggle filter
                if (ActiveOnlyToggle != null && ActiveOnlyToggle.IsOn)
                {
                    list = list.Where(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                // Combo filter
                if (StatusFilterCombo != null && StatusFilterCombo.SelectedItem is ComboBoxItem cbi && cbi.Content is string sel)
                {
                    if (string.Equals(sel, "Active Only", StringComparison.OrdinalIgnoreCase))
                        list = list.Where(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase)).ToList();
                    else if (string.Equals(sel, "Closed Only", StringComparison.OrdinalIgnoreCase))
                        list = list.Where(s => string.Equals(s.Status, "closed", StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            catch { }
            
            // simple reconcile
            var byId = Sessions.ToDictionary(s => s.SessionId);
            foreach (var s in list)
            {
                if (byId.TryGetValue(s.SessionId, out var existing))
                {
                    existing.BillingId = s.BillingId;
                    existing.TableId = s.TableId;
                    existing.ServerName = s.ServerName;
                    existing.StartTime = s.StartTime;
                    existing.Status = s.Status;
                    existing.ItemsCount = s.ItemsCount;
                    existing.Total = s.Total;
                }
                else
                {
                    Sessions.Add(s);
                }
            }
            // remove stale
            for (int i = Sessions.Count - 1; i >= 0; i--)
            {
                if (!list.Any(s => s.SessionId == Sessions[i].SessionId))
                {
                    Sessions.RemoveAt(i);
                }
            }
            // Optional: sort so active sessions appear first
            var ordered = Sessions.OrderByDescending(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase))
                                  .ThenByDescending(s => s.StartTime)
                                  .ToList();
            if (ordered.Count != Sessions.Count || !ordered.SequenceEqual(Sessions))
            {
                Sessions.Clear();
                foreach (var s in ordered) Sessions.Add(s);
            }

            // Update metrics
            UpdateMetrics();
        }
        catch { }
    }

    private void UpdateMetrics()
    {
        try
        {
            var today = DateTime.Today;
            var activeSessions = Sessions.Count(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase));
            var totalSessionsToday = Sessions.Count(s => s.StartTime.Date == today);
            
            // Calculate average session time
            var activeSessionTimes = Sessions
                .Where(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase))
                .Select(s => DateTime.Now - s.StartTime)
                .ToList();
            var averageSessionTime = activeSessionTimes.Any() 
                ? TimeSpan.FromMinutes(activeSessionTimes.Average(t => t.TotalMinutes))
                : TimeSpan.Zero;

            // Calculate total revenue
            var totalRevenue = Sessions.Sum(s => s.Total);

            // Update UI
            ActiveSessionsText.Text = activeSessions.ToString();
            TotalSessionsText.Text = totalSessionsToday.ToString();
            AverageSessionTimeText.Text = $"{averageSessionTime.TotalMinutes:F0}m";
            TotalRevenueText.Text = totalRevenue.ToString("C");
            SessionCountText.Text = $"({Sessions.Count} sessions)";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating metrics: {ex.Message}");
        }
    }

    private async void ActiveOnlyToggle_Toggled(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void FromDatePicker_DateChanged(object sender, Microsoft.UI.Xaml.Controls.DatePickerValueChangedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void ToDatePicker_DateChanged(object sender, Microsoft.UI.Xaml.Controls.DatePickerValueChangedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void TextFilter_Changed(object sender, TextChangedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void RefreshNowButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void ViewSessionDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SessionOverview session)
        {
            // For now, just show a message - in a real implementation, you'd open a details dialog
            var dialog = new ContentDialog()
            {
                Title = "Session Details",
                Content = $"Session ID: {session.SessionId}\nTable: {session.TableId}\nServer: {session.ServerName}\nStatus: {session.Status}\nItems: {session.ItemsCount}\nTotal: {session.Total:C}",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }

    private async void CloseSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SessionOverview session)
        {
            var dialog = new ContentDialog()
            {
                Title = "Close Session",
                Content = $"Are you sure you want to close the session for Table {session.TableId}?",
                PrimaryButtonText = "Close Session",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // In a real implementation, you would call a service to close the session
                    // await _sessionService.CloseSessionAsync(session.SessionId);
                    
                    // For now, just update the status locally
                    session.Status = "closed";
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog()
                    {
                        Title = "Error",
                        Content = $"Failed to close session: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = errorDialog.ShowAsync();
                }
            }
        }
    }
}
