using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Tables;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MagiDesk.Client.ViewModels;

/// <summary>
/// ViewModel for Payment Hub - Dashboard of all active sessions requiring payment.
/// Supports navigation to Payment Workspace for individual session payment processing.
/// </summary>
public partial class PaymentHubViewModel : ObservableObject
{
    private readonly ITableApi _tableApi;
    private readonly ShellViewModel _shellViewModel;

    [ObservableProperty]
    private ObservableCollection<SessionCardViewModel> _activeSessions = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public PaymentHubViewModel(ITableApi tableApi, ShellViewModel shellViewModel)
    {
        _tableApi = tableApi;
        _shellViewModel = shellViewModel;
    }

    [RelayCommand]
    public async Task LoadSessionsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            // Call GET /sessions/active endpoint
            var sessions = await _tableApi.GetActiveSessionsAsync();
            
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            app.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                ActiveSessions.Clear();
                foreach (var session in sessions)
                {
                    ActiveSessions.Add(new SessionCardViewModel(session));
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load sessions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void NavigateToPayment(SessionCardViewModel session)
    {
        if (session == null) return;
        
        // Navigate to PaymentWorkspacePage with session parameters
        var navParams = new Views.PaymentWorkspaceNavParams(
            session.SessionId,
            session.BillingId,
            session.TableLabel
        );
        
        _shellViewModel.NavigateToPaymentWorkspace(navParams);
    }
}

/// <summary>
/// Card representation of an active session for the Payment Hub grid.
/// </summary>
public partial class SessionCardViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _sessionId;

    [ObservableProperty]
    private Guid _billingId;

    [ObservableProperty]
    private string _tableLabel = string.Empty;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _startTime;

    [ObservableProperty]
    private decimal _currentTotal;

    [ObservableProperty]
    private string _status = string.Empty;

    public SessionCardViewModel(SessionOverview session)
    {
        // Map from API response (SessionOverview uses PascalCase properties)
        SessionId = session.SessionId;
        BillingId = session.BillingId ?? Guid.Empty;
        TableLabel = session.TableId ?? "Unknown";  // Note: TableId in SessionOverview
        ServerName = session.ServerName ?? "N/A";
        StartTime = session.StartTime;
        CurrentTotal = session.Total;
        Status = session.Status ?? "Active";
    }

    public string DisplayTime => $"{(DateTimeOffset.Now - StartTime).TotalMinutes:F0} min";
}
