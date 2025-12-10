using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class SessionRecoveryDialog : ContentDialog
{
    public ObservableCollection<SessionRecoveryItem> Sessions { get; } = new();
    public SessionRecoveryItem? SelectedSession { get; private set; }

    public SessionRecoveryDialog()
    {
        this.InitializeComponent();
        SessionsListView.SelectionChanged += SessionsListView_SelectionChanged;
    }

    public void SetSessions(IEnumerable<SessionOverview> sessions)
    {
        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(new SessionRecoveryItem(session));
        }
        
        SessionCountText.Text = Sessions.Count == 1 
            ? "1 active session found" 
            : $"{Sessions.Count} active sessions found";
            
        // Set up data binding for the ListView
        SessionsListView.ItemsSource = Sessions;
    }

    private void SessionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedSession = SessionsListView.SelectedItem as SessionRecoveryItem;
        ResumeButton.IsEnabled = SelectedSession != null;
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSession != null)
        {
            this.Hide();
        }
    }

    private void CloseAllButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedSession = null; // Signal to close all
        this.Hide();
    }
}

public class SessionRecoveryItem
{
    public string SessionId { get; }
    public string TableId { get; }
    public string ServerName { get; }
    public DateTime StartTime { get; }
    public int ItemsCount { get; }
    public decimal Total { get; }
    public string StartTimeText { get; }
    public string DurationText { get; }
    public string ItemsCountText { get; }
    public string TotalText { get; }

    public SessionRecoveryItem(SessionOverview session)
    {
        SessionId = session.SessionId.ToString();
        TableId = session.TableId;
        ServerName = session.ServerName ?? "Unknown Server";
        StartTime = session.StartTime;
        ItemsCount = session.ItemsCount;
        Total = session.Total;

        StartTimeText = $"Started: {StartTime:HH:mm}";
        DurationText = $"{(DateTime.UtcNow - StartTime).TotalMinutes:F0} min";
        ItemsCountText = ItemsCount == 1 ? "1 item" : $"{ItemsCount} items";
        TotalText = $"${Total:F2}";
    }
}
