using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.ViewModels;

public class ModernDashboardViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    private readonly TableRepository _tableRepository;
    private readonly NotificationService _notificationService;
    private readonly DispatcherQueue _dispatcher;
    private DispatcherTimer? _refreshTimer;

    // Revenue Metrics
    private decimal _todayRevenue;
    private decimal _settledRevenue;
    private decimal _pendingRevenue;
    private decimal _averageBillAmount;
    private int _totalBillsToday;
    private int _settledBillsToday;

    // Table Metrics
    private int _totalTables;
    private int _occupiedTables;
    private int _availableTables;
    private double _occupancyPercentage;
    private ObservableCollection<TableStatusDto> _tables = new();

    // Order Metrics
    private int _totalOrdersToday;
    private int _openOrders;
    private int _pendingDelivery;
    private int _deliveredOrders;
    private decimal _totalOrderValue;

    // Session Metrics
    private int _activeSessions;
    private int _sessionsToday;
    private double _averageSessionMinutes;
    private ObservableCollection<SessionOverview> _activeSessionsList = new();

    // Server Performance
    private ObservableCollection<ServerPerformance> _serverPerformance = new();

    // Notifications and Reminders
    private ObservableCollection<NotificationItem> _notifications = new();
    private ObservableCollection<ReminderItem> _reminders = new();
    private int _unreadNotifications;

    // Chart Data
    private ObservableCollection<RevenueDataPoint> _revenueChartData = new();
    private ObservableCollection<OccupancyDataPoint> _occupancyChartData = new();

    // Loading States
    private bool _isLoading;
    private bool _isConnected;
    private string _lastUpdated = "Never";

    public ModernDashboardViewModel(ApiService api, TableRepository tableRepository, NotificationService notificationService)
    {
        _api = api;
        _tableRepository = tableRepository;
        _notificationService = notificationService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        
        // Initialize collections with default values for XAML binding
        InitializeDefaultData();
        
        InitializeTimer();
        LoadAsync();
    }

    #region Properties

    // Revenue Properties
    public decimal TodayRevenue
    {
        get => _todayRevenue;
        set => SetProperty(ref _todayRevenue, value);
    }

    public decimal SettledRevenue
    {
        get => _settledRevenue;
        set => SetProperty(ref _settledRevenue, value);
    }

    public decimal PendingRevenue
    {
        get => _pendingRevenue;
        set => SetProperty(ref _pendingRevenue, value);
    }

    public decimal AverageBillAmount
    {
        get => _averageBillAmount;
        set => SetProperty(ref _averageBillAmount, value);
    }

    public int TotalBillsToday
    {
        get => _totalBillsToday;
        set => SetProperty(ref _totalBillsToday, value);
    }

    public int SettledBillsToday
    {
        get => _settledBillsToday;
        set => SetProperty(ref _settledBillsToday, value);
    }

    // Table Properties
    public int TotalTables
    {
        get => _totalTables;
        set => SetProperty(ref _totalTables, value);
    }

    public int OccupiedTables
    {
        get => _occupiedTables;
        set => SetProperty(ref _occupiedTables, value);
    }

    public int AvailableTables
    {
        get => _availableTables;
        set => SetProperty(ref _availableTables, value);
    }

    public double OccupancyPercentage
    {
        get => _occupancyPercentage;
        set => SetProperty(ref _occupancyPercentage, value);
    }

    public ObservableCollection<TableStatusDto> Tables
    {
        get => _tables;
        set => SetProperty(ref _tables, value);
    }

    // Order Properties
    public int TotalOrdersToday
    {
        get => _totalOrdersToday;
        set => SetProperty(ref _totalOrdersToday, value);
    }

    public int OpenOrders
    {
        get => _openOrders;
        set => SetProperty(ref _openOrders, value);
    }

    public int PendingDelivery
    {
        get => _pendingDelivery;
        set => SetProperty(ref _pendingDelivery, value);
    }

    public int DeliveredOrders
    {
        get => _deliveredOrders;
        set => SetProperty(ref _deliveredOrders, value);
    }

    public decimal TotalOrderValue
    {
        get => _totalOrderValue;
        set => SetProperty(ref _totalOrderValue, value);
    }

    // Session Properties
    public int ActiveSessions
    {
        get => _activeSessions;
        set => SetProperty(ref _activeSessions, value);
    }

    public int SessionsToday
    {
        get => _sessionsToday;
        set => SetProperty(ref _sessionsToday, value);
    }

    public double AverageSessionMinutes
    {
        get => _averageSessionMinutes;
        set => SetProperty(ref _averageSessionMinutes, value);
    }

    public ObservableCollection<SessionOverview> ActiveSessionsList
    {
        get => _activeSessionsList;
        set => SetProperty(ref _activeSessionsList, value);
    }

    // Server Performance
    public ObservableCollection<ServerPerformance> ServerPerformance
    {
        get => _serverPerformance;
        set => SetProperty(ref _serverPerformance, value);
    }

    // Notifications and Reminders
    public ObservableCollection<NotificationItem> Notifications
    {
        get => _notifications;
        set => SetProperty(ref _notifications, value);
    }

    public ObservableCollection<ReminderItem> Reminders
    {
        get => _reminders;
        set => SetProperty(ref _reminders, value);
    }

    public int UnreadNotifications
    {
        get => _unreadNotifications;
        set => SetProperty(ref _unreadNotifications, value);
    }

    // Chart Data
    public ObservableCollection<RevenueDataPoint> RevenueChartData
    {
        get => _revenueChartData;
        set => SetProperty(ref _revenueChartData, value);
    }

    public ObservableCollection<OccupancyDataPoint> OccupancyChartData
    {
        get => _occupancyChartData;
        set => SetProperty(ref _occupancyChartData, value);
    }

    // Loading States
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value);
    }

    #endregion

    #region Commands

    public async Task RefreshDataAsync()
    {
        IsLoading = true;
        try
        {
            await LoadRevenueMetricsAsync();
            await LoadTableMetricsAsync();
            await LoadOrderMetricsAsync();
            await LoadSessionMetricsAsync();
            await LoadServerPerformanceAsync();
            await LoadNotificationsAsync();
            await LoadRemindersAsync();
            await LoadChartDataAsync();
            
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            IsConnected = true;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            AddNotification("Error", $"Failed to load dashboard data: {ex.Message}", NotificationType.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Methods

    private void InitializeDefaultData()
    {
        // Initialize RevenueChartData with 7 default items (for 7 days)
        var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var defaultRevenues = new[] { 0m, 0m, 0m, 0m, 0m, 0m, 0m };
        
        for (int i = 0; i < days.Length; i++)
        {
            RevenueChartData.Add(new RevenueDataPoint
            {
                Day = days[i],
                Revenue = defaultRevenues[i]
            });
        }

        // Initialize OccupancyChartData with 7 default items
        var defaultOccupancy = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
        
        for (int i = 0; i < days.Length; i++)
        {
            OccupancyChartData.Add(new OccupancyDataPoint
            {
                Day = days[i],
                OccupancyPercentage = defaultOccupancy[i]
            });
        }

        // Initialize Tables with 10 default items
        var tableLabels = new[] { "B1", "B2", "B3", "B4", "B5", "Bar1", "Bar2", "Bar3", "Bar4", "Bar5" };
        for (int i = 0; i < 10; i++)
        {
            Tables.Add(new TableStatusDto
            {
                Label = tableLabels[i],
                Type = i < 5 ? "billiard" : "bar",
                Occupied = false,
                OrderId = null,
                StartTime = null,
                Server = null
            });
        }
    }

    private void InitializeTimer()
    {
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshDataAsync();
        _refreshTimer.Start();
    }

    private async Task LoadAsync()
    {
        await RefreshDataAsync();
    }

    private async Task LoadRevenueMetricsAsync()
    {
        try
        {
            // This would typically call a dedicated dashboard API endpoint
            // For now, we'll simulate the data based on our database analysis
            TodayRevenue = 190.76m;
            SettledRevenue = 33.76m;
            PendingRevenue = 157.00m;
            AverageBillAmount = 47.69m;
            TotalBillsToday = 4;
            SettledBillsToday = 1;
        }
        catch (Exception ex)
        {
            AddNotification("Revenue Error", $"Failed to load revenue metrics: {ex.Message}", NotificationType.Warning);
        }
    }

    private async Task LoadTableMetricsAsync()
    {
        try
        {
            var tables = await _tableRepository.GetAllAsync();
            if (tables != null)
            {
                // Update existing table items with real data
                for (int i = 0; i < tables.Count && i < Tables.Count; i++)
                {
                    Tables[i] = tables[i];
                }

                // Update remaining items with default data if we have fewer real tables
                var tableLabels = new[] { "B1", "B2", "B3", "B4", "B5", "Bar1", "Bar2", "Bar3", "Bar4", "Bar5" };
                for (int i = tables.Count; i < Tables.Count; i++)
                {
                    Tables[i] = new TableStatusDto
                    {
                        Label = tableLabels[i],
                        Type = i < 5 ? "billiard" : "bar",
                        Occupied = false,
                        OrderId = null,
                        StartTime = null,
                        Server = null
                    };
                }

                TotalTables = tables.Count;
                OccupiedTables = tables.Count(t => t.Occupied);
                AvailableTables = tables.Count(t => !t.Occupied);
                OccupancyPercentage = TotalTables > 0 ? (OccupiedTables * 100.0 / TotalTables) : 0;
            }
        }
        catch (Exception ex)
        {
            AddNotification("Table Error", $"Failed to load table metrics: {ex.Message}", NotificationType.Warning);
        }
    }

    private async Task LoadOrderMetricsAsync()
    {
        try
        {
            // Simulate order data - would come from OrderApi
            TotalOrdersToday = 14;
            OpenOrders = 0;
            PendingDelivery = 12;
            DeliveredOrders = 0;
            TotalOrderValue = 10869.94m;
        }
        catch (Exception ex)
        {
            AddNotification("Order Error", $"Failed to load order metrics: {ex.Message}", NotificationType.Warning);
        }
    }

    private async Task LoadSessionMetricsAsync()
    {
        try
        {
            var activeSessions = await _tableRepository.GetActiveSessionsAsync();
            ActiveSessionsList.Clear();
            foreach (var session in activeSessions)
            {
                ActiveSessionsList.Add(session);
            }

            ActiveSessions = activeSessions.Count;
            SessionsToday = 17; // From our database analysis
            AverageSessionMinutes = 12.1;
        }
        catch (Exception ex)
        {
            AddNotification("Session Error", $"Failed to load session metrics: {ex.Message}", NotificationType.Warning);
        }
    }

    private async Task LoadServerPerformanceAsync()
    {
        try
        {
            ServerPerformance.Clear();
            ServerPerformance.Add(new ServerPerformance
            {
                ServerName = "PREETI",
                SessionsToday = 4,
                RevenueGenerated = 190.76m,
                AverageBillAmount = 47.69m,
                AverageSessionMinutes = 12.1,
                PerformancePercentage = 100.0
            });
        }
        catch (Exception ex)
        {
            AddNotification("Server Error", $"Failed to load server performance: {ex.Message}", NotificationType.Warning);
        }
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            // Simulate notifications
            Notifications.Clear();
            Notifications.Add(new NotificationItem
            {
                Id = Guid.NewGuid(),
                Title = "System Update",
                Message = "Dashboard data refreshed successfully",
                Type = NotificationType.Info,
                Timestamp = DateTime.Now.AddMinutes(-5),
                IsRead = false
            });

            UnreadNotifications = Notifications.Count(n => !n.IsRead);
        }
        catch (Exception ex)
        {
            AddNotification("Notification Error", $"Failed to load notifications: {ex.Message}", NotificationType.Error);
        }
    }

    private async Task LoadRemindersAsync()
    {
        try
        {
            // Simulate reminders
            Reminders.Clear();
            Reminders.Add(new ReminderItem
            {
                Id = Guid.NewGuid(),
                Title = "End of Day Report",
                Description = "Generate daily sales report",
                DueTime = DateTime.Today.AddHours(23),
                Priority = ReminderPriority.Medium,
                IsCompleted = false
            });
        }
        catch (Exception ex)
        {
            AddNotification("Reminder Error", $"Failed to load reminders: {ex.Message}", NotificationType.Error);
        }
    }

    private async Task LoadChartDataAsync()
    {
        try
        {
            // Update existing chart data for the last 7 days
            var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var revenues = new[] { 150.50m, 180.25m, 220.75m, 190.76m, 165.30m, 0m, 0m };
            
            for (int i = 0; i < days.Length && i < RevenueChartData.Count; i++)
            {
                RevenueChartData[i] = new RevenueDataPoint
                {
                    Day = days[i],
                    Revenue = revenues[i]
                };
            }

            // Update occupancy chart data
            var occupancyData = new[] { 60.0, 70.0, 80.0, 0.0, 45.0, 0.0, 0.0 };
            
            for (int i = 0; i < days.Length && i < OccupancyChartData.Count; i++)
            {
                OccupancyChartData[i] = new OccupancyDataPoint
                {
                    Day = days[i],
                    OccupancyPercentage = occupancyData[i]
                };
            }
        }
        catch (Exception ex)
        {
            AddNotification("Chart Error", $"Failed to load chart data: {ex.Message}", NotificationType.Warning);
        }
    }

    private void AddNotification(string title, string message, NotificationType type)
    {
        _dispatcher.TryEnqueue(() =>
        {
            Notifications.Insert(0, new NotificationItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                IsRead = false
            });
            UnreadNotifications = Notifications.Count(n => !n.IsRead);
        });
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    public void Dispose()
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }
}

#region Data Models

public class ServerPerformance
{
    public string ServerName { get; set; } = string.Empty;
    public int SessionsToday { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal AverageBillAmount { get; set; }
    public double AverageSessionMinutes { get; set; }
    public double PerformancePercentage { get; set; }
}

public class NotificationItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}

public class ReminderItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueTime { get; set; }
    public ReminderPriority Priority { get; set; }
    public bool IsCompleted { get; set; }
}

public class RevenueDataPoint
{
    public string Day { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class OccupancyDataPoint
{
    public string Day { get; set; } = string.Empty;
    public double OccupancyPercentage { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public enum ReminderPriority
{
    Low,
    Medium,
    High,
    Critical
}

#endregion
