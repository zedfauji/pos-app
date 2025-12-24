using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Client.Services.Dtos;
using MagiDesk.Shared.DTOs.Tables;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Client.ViewModels;

/// <summary>
/// ViewModel for Payment Hub - Dashboard of all bills requiring payment.
/// Supports tabs for filtering: ALL, UNSETTLED, PARTIAL, ACTIVE.
/// </summary>
public partial class PaymentHubViewModel : BaseViewModel
{
    private readonly ITableApi _tableApi;
    private readonly INavigationService _navigationService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty]
    private ObservableCollection<BillCardViewModel> _allBills = new();

    [ObservableProperty]
    private ObservableCollection<BillCardViewModel> _filteredBills = new();

    [ObservableProperty]
    private string _selectedFilter = "Unsettled";

    // Tab counts
    [ObservableProperty]
    private int _allCount;

    [ObservableProperty]
    private int _unsettledCount;

    [ObservableProperty]
    private int _partialCount;

    [ObservableProperty]
    private int _activeCount;

    // Summary
    [ObservableProperty]
    private decimal _totalUnsettledAmount;

    public PaymentHubViewModel(ITableApi tableApi, INavigationService navigationService, IDispatcherService dispatcherService)
    {
        _tableApi = tableApi;
        _navigationService = navigationService;
        _dispatcherService = dispatcherService;
    }

    [RelayCommand]
    public async Task LoadBillsAsync()
    {
        IsLoading = true;
        ClearError();

        try
        {
            // Load unsettled bills from backend
            var bills = await _tableApi.GetUnsettledBillsAsync();
            
            // DEBUG: Log each bill's table information
            Serilog.Log.Information("PaymentHub: Loaded {Count} bills from API", bills.Count());
            foreach (var bill in bills)
            {
                Serilog.Log.Information("PaymentHub: Bill {BillId} - TableLabel={TableLabel}, ServerName={ServerName}, SessionId={SessionId}, TableId={TableId}", 
                    bill.BillId, 
                    bill.TableLabel ?? "NULL", 
                    bill.ServerName ?? "NULL",
                    bill.SessionId,
                    bill.TableId);
            }
            
            _dispatcherService.InvokeOnUIThread(() =>
            {
                AllBills.Clear();
                foreach (var bill in bills)
                {
                    AllBills.Add(new BillCardViewModel(bill));
                }

                // Update counts
                AllCount = AllBills.Count;
                UnsettledCount = AllBills.Count(b => b.Status == "AwaitingPayment");
                PartialCount = AllBills.Count(b => b.Status == "Partial");
                ActiveCount = 0; // Active sessions handled separately
                
                // Update summary
                TotalUnsettledAmount = AllBills.Where(b => b.Status == "AwaitingPayment").Sum(b => b.TotalAmount);

                // Apply filter
                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            SetError($"Failed to load bills: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredBills.Clear();
        
        var filtered = SelectedFilter switch
        {
            "All" => AllBills,
            "Unsettled" => new ObservableCollection<BillCardViewModel>(AllBills.Where(b => b.Status == "AwaitingPayment")),
            "Partial" => new ObservableCollection<BillCardViewModel>(AllBills.Where(b => b.Status == "Partial")),
            "Active" => new ObservableCollection<BillCardViewModel>(), // Would need separate endpoint
            _ => AllBills
        };

        foreach (var bill in filtered)
        {
            FilteredBills.Add(bill);
        }
    }

    [RelayCommand]
    public void SelectFilter(string filter)
    {
        SelectedFilter = filter;
    }

    [RelayCommand]
    public void NavigateToPayment(object? parameter)
    {
        // Robust parameter handling to avoid InvalidCastException
        if (parameter == null)
        {
            Serilog.Log.Warning("NavigateToPayment: parameter is null");
            return;
        }
        
        if (parameter is not BillCardViewModel bill)
        {
            Serilog.Log.Warning("NavigateToPayment: parameter is {Type}, expected BillCardViewModel", parameter.GetType().Name);
            return;
        }
        
        Serilog.Log.Information("NavigateToPayment: BillId={BillId}, SessionId={SessionId}, TableLabel={TableLabel}", 
            bill.BillId, bill.SessionId, bill.TableLabel);
        
        // Navigate to PaymentWorkspacePage with bill parameters
        var navParams = new Views.PaymentWorkspaceNavParams(
            bill.SessionId,
            bill.BillingId,
            bill.TableLabel ?? "Unknown"
        );
        
        _navigationService.NavigateTo<PaymentWorkspaceViewModel>(navParams);
    }
}

/// <summary>
/// Card representation of a bill for the Payment Hub grid.
/// </summary>
public partial class BillCardViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _billId;

    [ObservableProperty]
    private Guid _billingId;

    [ObservableProperty]
    private Guid _sessionId;

    [ObservableProperty]
    private string? _tableLabel;

    [ObservableProperty]
    private string? _serverName;

    [ObservableProperty]
    private DateTimeOffset? _startTime;

    [ObservableProperty]
    private DateTimeOffset? _endTime;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private string _status = "AwaitingPayment";

    [ObservableProperty]
    private DateTimeOffset _createdAt;

    public BillCardViewModel(BillDto bill)
    {
        BillId = bill.BillId;
        BillingId = bill.BillingId;
        SessionId = bill.SessionId;
        TableLabel = bill.TableLabel ?? "Unknown";
        ServerName = bill.ServerName ?? "N/A";
        StartTime = bill.StartTime;
        EndTime = bill.EndTime;
        TotalAmount = bill.TotalAmount;
        Status = bill.Status;
        CreatedAt = bill.CreatedAt;
    }

    // Display helpers
    public string DisplayTime => EndTime.HasValue 
        ? $"Ended {(DateTimeOffset.Now - EndTime.Value).TotalMinutes:F0} min ago"
        : "Active";

    public string DisplayAmount => $"${TotalAmount:F2}";

    public string StatusBadge => Status switch
    {
        "AwaitingPayment" => "⚠️ UNSETTLED",
        "Partial" => "💰 PARTIAL",
        "Paid" => "✅ PAID",
        _ => Status
    };

    // Risk indicators
    public bool IsUrgent => EndTime.HasValue && (DateTimeOffset.Now - EndTime.Value).TotalMinutes > 30;
    public bool IsHighAmount => TotalAmount >= 100;
}
