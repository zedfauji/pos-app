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
                    if (string.Equals(sel, "Active", StringComparison.OrdinalIgnoreCase))
                        list = list.Where(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase)).ToList();
                    else if (string.Equals(sel, "Closed", StringComparison.OrdinalIgnoreCase))
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
        }
        catch { }
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
}
