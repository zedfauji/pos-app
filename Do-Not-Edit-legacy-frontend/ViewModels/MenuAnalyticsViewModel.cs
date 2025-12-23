using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class MenuAnalyticsViewModel : INotifyPropertyChanged
{
    private readonly MenuAnalyticsService? _analyticsService;
    private bool _isLoading;
    private string _statusMessage = "";
    private DateTime _fromDate = DateTime.Today.AddDays(-30);
    private DateTime _toDate = DateTime.Today;
    private string? _selectedCategory;
    private string? _selectedSortBy;
    private string? _selectedSortDirection;

    public ObservableCollection<MenuAnalyticsItemViewModel> AnalyticsItems { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<MenuPerformanceItemViewModel> PerformanceData { get; } = new();
    public ObservableCollection<MenuTrendItemViewModel> Trends { get; } = new();
    public ObservableCollection<MenuInsightItemViewModel> Insights { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate != value)
            {
                _fromDate = value;
                OnPropertyChanged();
                _ = LoadAnalyticsAsync();
            }
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate != value)
            {
                _toDate = value;
                OnPropertyChanged();
                _ = LoadAnalyticsAsync();
            }
        }
    }

    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                OnPropertyChanged();
                _ = LoadAnalyticsAsync();
            }
        }
    }

    public string? SelectedSortBy
    {
        get => _selectedSortBy;
        set
        {
            if (_selectedSortBy != value)
            {
                _selectedSortBy = value;
                OnPropertyChanged();
                _ = LoadAnalyticsAsync();
            }
        }
    }

    public string? SelectedSortDirection
    {
        get => _selectedSortDirection;
        set
        {
            if (_selectedSortDirection != value)
            {
                _selectedSortDirection = value;
                OnPropertyChanged();
                _ = LoadAnalyticsAsync();
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand LoadDashboardCommand { get; }
    public ICommand LoadPerformanceCommand { get; }
    public ICommand LoadTrendsCommand { get; }
    public ICommand LoadInsightsCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MenuAnalyticsViewModel()
    {
        _analyticsService = App.Services?.GetService(typeof(MenuAnalyticsService)) as MenuAnalyticsService;
        
        RefreshCommand = new RelayCommand(async () => await LoadAnalyticsAsync());
        LoadDashboardCommand = new RelayCommand(async () => await LoadDashboardAsync());
        LoadPerformanceCommand = new RelayCommand(async () => await LoadPerformanceAsync());
        LoadTrendsCommand = new RelayCommand(async () => await LoadTrendsAsync());
        LoadInsightsCommand = new RelayCommand(async () => await LoadInsightsAsync());
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await LoadAnalyticsAsync();
            await LoadDashboardAsync();
            await LoadPerformanceAsync();
            await LoadTrendsAsync();
            await LoadInsightsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initializing analytics: {ex.Message}";
        }
    }

    private async Task LoadAnalyticsAsync()
    {
        if (_analyticsService == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Loading analytics...";

            var query = new MenuAnalyticsService.MenuAnalyticsQueryDto(
                FromDate: FromDate,
                ToDate: ToDate,
                Category: SelectedCategory,
                SortBy: SelectedSortBy,
                SortDirection: SelectedSortDirection
            );

            var analytics = await _analyticsService.GetMenuAnalyticsAsync(query);
            
            AnalyticsItems.Clear();
            foreach (var item in analytics)
            {
                AnalyticsItems.Add(new MenuAnalyticsItemViewModel(item));
            }

            StatusMessage = $"Loaded {analytics.Count} analytics items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading analytics: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDashboardAsync()
    {
        if (_analyticsService == null) return;

        try
        {
            var dashboard = await _analyticsService.GetDashboardDataAsync(FromDate, ToDate);
            // Process dashboard data and update UI
            StatusMessage = "Dashboard data loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading dashboard: {ex.Message}";
        }
    }

    private async Task LoadPerformanceAsync()
    {
        if (_analyticsService == null) return;

        try
        {
            var query = new MenuAnalyticsService.MenuPerformanceQueryDto(
                FromDate: FromDate,
                ToDate: ToDate,
                Category: SelectedCategory,
                GroupBy: "Day"
            );

            var performance = await _analyticsService.GetMenuPerformanceAsync(query);
            
            PerformanceData.Clear();
            foreach (var item in performance)
            {
                PerformanceData.Add(new MenuPerformanceItemViewModel(item));
            }

            StatusMessage = $"Loaded {performance.Count} performance data points";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading performance: {ex.Message}";
        }
    }

    private async Task LoadTrendsAsync()
    {
        if (_analyticsService == null) return;

        try
        {
            var query = new MenuAnalyticsService.MenuTrendQueryDto(
                FromDate: FromDate,
                ToDate: ToDate,
                Category: SelectedCategory,
                IncludeRecommendations: true
            );

            var trends = await _analyticsService.GetMenuTrendsAsync(query);
            
            Trends.Clear();
            foreach (var trend in trends)
            {
                Trends.Add(new MenuTrendItemViewModel(trend));
            }

            StatusMessage = $"Loaded {trends.Count} trends";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading trends: {ex.Message}";
        }
    }

    private async Task LoadInsightsAsync()
    {
        if (_analyticsService == null) return;

        try
        {
            var query = new MenuAnalyticsService.MenuInsightQueryDto(
                FromDate: FromDate,
                ToDate: ToDate
            );

            var insights = await _analyticsService.GetMenuInsightsAsync(query);
            
            Insights.Clear();
            foreach (var insight in insights)
            {
                Insights.Add(new MenuInsightItemViewModel(insight));
            }

            StatusMessage = $"Loaded {insights.Count} insights";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading insights: {ex.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class MenuAnalyticsItemViewModel
{
    public long MenuItemId { get; }
    public string Name { get; }
    public string Category { get; }
    public decimal TotalRevenue { get; }
    public int TotalOrders { get; }
    public int TotalQuantitySold { get; }
    public decimal AverageOrderValue { get; }
    public decimal ProfitMargin { get; }
    public decimal CostOfGoodsSold { get; }
    public DateTime LastSold { get; }
    public DateTime FirstSold { get; }
    public bool IsTopSeller { get; }
    public int Ranking { get; }
    public decimal ConversionRate { get; }
    public TimeSpan AveragePreparationTime { get; }
    public int CustomerRating { get; }
    public int ReviewCount { get; }

    public string ProfitMarginText => $"{ProfitMargin:P1}";
    public string ConversionRateText => $"{ConversionRate:P1}";
    public string AveragePreparationTimeText => $"{AveragePreparationTime.TotalMinutes:F0} min";
    public string CustomerRatingText => $"{CustomerRating}/5 ({ReviewCount} reviews)";
    public string LastSoldText => LastSold.ToString("MMM dd, yyyy");
    public string FirstSoldText => FirstSold.ToString("MMM dd, yyyy");

    public MenuAnalyticsItemViewModel(MenuAnalyticsService.MenuAnalyticsDto dto)
    {
        MenuItemId = dto.MenuItemId;
        Name = dto.Name;
        Category = dto.Category;
        TotalRevenue = dto.TotalRevenue;
        TotalOrders = dto.TotalOrders;
        TotalQuantitySold = dto.TotalQuantitySold;
        AverageOrderValue = dto.AverageOrderValue;
        ProfitMargin = dto.ProfitMargin;
        CostOfGoodsSold = dto.CostOfGoodsSold;
        LastSold = dto.LastSold;
        FirstSold = dto.FirstSold;
        IsTopSeller = dto.IsTopSeller;
        Ranking = dto.Ranking;
        ConversionRate = dto.ConversionRate;
        AveragePreparationTime = dto.AveragePreparationTime;
        CustomerRating = dto.CustomerRating;
        ReviewCount = dto.ReviewCount;
    }
}

public sealed class MenuPerformanceItemViewModel
{
    public DateTime Date { get; }
    public string Category { get; }
    public decimal Revenue { get; }
    public int Orders { get; }
    public int QuantitySold { get; }
    public decimal AverageOrderValue { get; }
    public decimal ProfitMargin { get; }
    public int UniqueCustomers { get; }
    public decimal CustomerSatisfactionScore { get; }

    public string DateText => Date.ToString("MMM dd");
    public string RevenueText => Revenue.ToString("C");
    public string AverageOrderValueText => AverageOrderValue.ToString("C");
    public string ProfitMarginText => $"{ProfitMargin:P1}";
    public string CustomerSatisfactionText => $"{CustomerSatisfactionScore:F1}/5";

    public MenuPerformanceItemViewModel(MenuAnalyticsService.MenuPerformanceDto dto)
    {
        Date = dto.Date;
        Category = dto.Category;
        Revenue = dto.Revenue;
        Orders = dto.Orders;
        QuantitySold = dto.QuantitySold;
        AverageOrderValue = dto.AverageOrderValue;
        ProfitMargin = dto.ProfitMargin;
        UniqueCustomers = dto.UniqueCustomers;
        CustomerSatisfactionScore = dto.CustomerSatisfactionScore;
    }
}

public sealed class MenuTrendItemViewModel
{
    public string Category { get; }
    public string Trend { get; }
    public decimal ChangePercentage { get; }
    public int PeriodDays { get; }
    public decimal RevenueChange { get; }
    public int OrderChange { get; }
    public string Recommendation { get; }

    public string ChangePercentageText => $"{ChangePercentage:P1}";
    public string RevenueChangeText => RevenueChange.ToString("C");
    public string OrderChangeText => OrderChange > 0 ? $"+{OrderChange}" : OrderChange.ToString();
    public string TrendIcon => Trend switch
    {
        "Rising" => "📈",
        "Declining" => "📉",
        _ => "➡️"
    };
    public string TrendColor => Trend switch
    {
        "Rising" => "Green",
        "Declining" => "Red",
        _ => "Gray"
    };

    public MenuTrendItemViewModel(MenuAnalyticsService.MenuTrendDto dto)
    {
        Category = dto.Category;
        Trend = dto.Trend;
        ChangePercentage = dto.ChangePercentage;
        PeriodDays = dto.PeriodDays;
        RevenueChange = dto.RevenueChange;
        OrderChange = dto.OrderChange;
        Recommendation = dto.Recommendation;
    }
}

public sealed class MenuInsightItemViewModel
{
    public string Type { get; }
    public string Title { get; }
    public string Description { get; }
    public string Priority { get; }
    public string ActionRequired { get; }
    public DateTime GeneratedAt { get; }
    public Dictionary<string, object> Data { get; }

    public string PriorityIcon => Priority switch
    {
        "High" => "🔴",
        "Medium" => "🟡",
        "Low" => "🟢",
        _ => "⚪"
    };
    public string TypeIcon => Type switch
    {
        "Performance" => "📊",
        "Trend" => "📈",
        "Recommendation" => "💡",
        "Alert" => "⚠️",
        _ => "ℹ️"
    };
    public string GeneratedAtText => GeneratedAt.ToString("MMM dd, yyyy HH:mm");

    public MenuInsightItemViewModel(MenuAnalyticsService.MenuInsightDto dto)
    {
        Type = dto.Type;
        Title = dto.Title;
        Description = dto.Description;
        Priority = dto.Priority;
        ActionRequired = dto.ActionRequired;
        GeneratedAt = dto.GeneratedAt;
        Data = dto.Data;
    }
}
