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
            var list = await _repo.GetActiveSessionsAsync();
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
        }
        catch { }
    }
}
