using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class VendorsManagementViewModel : INotifyPropertyChanged
{
    private readonly IVendorService _vendorService;
    private readonly IInventoryService _inventoryService;
    private readonly IVendorOrderService? _vendorOrderService;

    public IVendorOrderService? GetVendorOrderService() => _vendorOrderService;
    public IInventoryService GetInventoryService() => _inventoryService;
    private bool _isLoading;
    private string _statusMessage = "Ready";
    private string _searchText = string.Empty;
    private string _statusFilter = "all";
    private VendorDisplay? _selectedVendor;
    private string _sortBy = "Name";
    private string _sortDirection = "Asc";
    private int _currentPage = 1;
    private int _pageSize = 25;

    public VendorsManagementViewModel(IVendorService vendorService, IInventoryService inventoryService, IVendorOrderService? vendorOrderService = null)
    {
        _vendorService = vendorService ?? throw new ArgumentNullException(nameof(vendorService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _vendorOrderService = vendorOrderService;
        Vendors = new ObservableCollection<VendorDisplay>();
        FilteredVendors = new ObservableCollection<VendorDisplay>();
    }

    public ObservableCollection<VendorDisplay> Vendors { get; }
    public ObservableCollection<VendorDisplay> FilteredVendors { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
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
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EmptyStateMessage));
                ApplyFilters();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (_statusFilter != value)
            {
                _statusFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EmptyStateMessage));
                ApplyFilters();
            }
        }
    }

    public VendorDisplay? SelectedVendor
    {
        get => _selectedVendor;
        set
        {
            if (_selectedVendor != value)
            {
                _selectedVendor = value;
                OnPropertyChanged();
            }
        }
    }

    public int TotalVendors => Vendors.Count;
    public int ActiveVendors => Vendors.Count(v => v.Status == "active");
    public int InactiveVendors => Vendors.Count(v => v.Status == "inactive");
    public decimal TotalBudget => Vendors.Sum(v => v.Budget);

    public string SortBy
    {
        get => _sortBy;
        set
        {
            if (_sortBy != value)
            {
                _sortBy = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
    }

    public string SortDirection
    {
        get => _sortDirection;
        set
        {
            if (_sortDirection != value)
            {
                _sortDirection = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value && value >= 1)
            {
                _currentPage = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize != value && value > 0)
            {
                _pageSize = value;
                _currentPage = 1; // Reset to first page
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPage));
                ApplyFilters();
            }
        }
    }

    public int TotalPages => (int)Math.Ceiling((double)GetFilteredCount() / _pageSize);
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
    public int DisplayedCount => FilteredVendors.Count;
    public int TotalCount => Vendors.Count;
    public bool HasVendors => !IsLoading && FilteredVendors.Count > 0;
    public bool ShowEmptyState => !IsLoading && FilteredVendors.Count == 0;
    public bool ShowSkeleton => IsLoading && Vendors.Count == 0;
    public string EmptyStateMessage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SearchText) || StatusFilter != "all")
            {
                return "No vendors match your current filters. Try adjusting your search or filter criteria.";
            }
            return "Get started by adding your first vendor. Vendors help you manage suppliers and track inventory sources.";
        }
    }

    public async Task LoadVendorsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading vendors...";

            var vendors = await _vendorService.GetVendorsAsync();
            
            Vendors.Clear();
            foreach (var vendor in vendors)
            {
                var vendorDisplay = new VendorDisplay(vendor);
                Vendors.Add(vendorDisplay);
                
                // Load order summaries asynchronously
                if (_vendorOrderService != null && !string.IsNullOrWhiteSpace(vendor.Id))
                {
                    _ = LoadVendorOrderSummaryAsync(vendorDisplay, vendor.Id);
                }
            }

            ApplyFilters();
            StatusMessage = $"Loaded {Vendors.Count} vendors";
            
            OnPropertyChanged(nameof(HasVendors));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowSkeleton));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading vendors: {ex.Message}";
            throw;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasVendors));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowSkeleton));
        }
    }

    public async Task<VendorDto> CreateVendorAsync(VendorDto vendor)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating vendor...";

            var created = await _vendorService.CreateVendorAsync(vendor);
            
            Vendors.Add(new VendorDisplay(created));
            ApplyFilters();
            
            StatusMessage = "Vendor created successfully";
            return created;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating vendor: {ex.Message}";
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> UpdateVendorAsync(string id, VendorDto vendor)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Updating vendor...";

            var success = await _vendorService.UpdateVendorAsync(id, vendor);
            
            if (success)
            {
                var existing = Vendors.FirstOrDefault(v => v.Id == id);
                if (existing != null)
                {
                    existing.UpdateFrom(vendor);
                }
                ApplyFilters();
                StatusMessage = "Vendor updated successfully";
            }
            else
            {
                StatusMessage = "Vendor not found";
            }

            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating vendor: {ex.Message}";
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> DeleteVendorAsync(string id)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Deleting vendor...";

            var success = await _vendorService.DeleteVendorAsync(id);
            
            if (success)
            {
                var vendor = Vendors.FirstOrDefault(v => v.Id == id);
                if (vendor != null)
                {
                    Vendors.Remove(vendor);
                }
                ApplyFilters();
                StatusMessage = "Vendor deleted successfully";
            }
            else
            {
                StatusMessage = "Vendor not found";
            }

            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting vendor: {ex.Message}";
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<IEnumerable<ItemDto>> GetVendorItemsAsync(string vendorId)
    {
        try
        {
            return await _vendorService.GetVendorItemsAsync(vendorId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading vendor items: {ex.Message}";
            throw;
        }
    }

    private int GetFilteredCount()
    {
        var filtered = Vendors.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(v => 
                v.Name.ToLowerInvariant().Contains(searchLower) ||
                (v.ContactInfo?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (v.Notes?.ToLowerInvariant().Contains(searchLower) ?? false));
        }

        // Apply status filter
        if (StatusFilter != "all")
        {
            filtered = filtered.Where(v => v.Status == StatusFilter);
        }

        return filtered.Count();
    }

    private void ApplyFilters()
    {
        FilteredVendors.Clear();

        var filtered = Vendors.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(v => 
                v.Name.ToLowerInvariant().Contains(searchLower) ||
                (v.ContactInfo?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (v.Notes?.ToLowerInvariant().Contains(searchLower) ?? false));
        }

        // Apply status filter
        if (StatusFilter != "all")
        {
            filtered = filtered.Where(v => v.Status == StatusFilter);
        }

        // Apply sorting
        filtered = SortDirection == "Asc" 
            ? GetSortedQuery(filtered, true)
            : GetSortedQuery(filtered, false);

        // Apply pagination
        var totalCount = filtered.Count();
        var skip = (CurrentPage - 1) * PageSize;
        var paged = filtered.Skip(skip).Take(PageSize);

        foreach (var vendor in paged)
        {
            FilteredVendors.Add(vendor);
        }

        OnPropertyChanged(nameof(TotalVendors));
        OnPropertyChanged(nameof(ActiveVendors));
        OnPropertyChanged(nameof(InactiveVendors));
        OnPropertyChanged(nameof(TotalBudget));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(DisplayedCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasVendors));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowSkeleton));
        OnPropertyChanged(nameof(EmptyStateMessage));
    }

    private IEnumerable<VendorDisplay> GetSortedQuery(IEnumerable<VendorDisplay> query, bool ascending)
    {
        return SortBy switch
        {
            "Name" => ascending ? query.OrderBy(v => v.Name) : query.OrderByDescending(v => v.Name),
            "Budget" => ascending ? query.OrderBy(v => v.Budget) : query.OrderByDescending(v => v.Budget),
            "Status" => ascending ? query.OrderBy(v => v.Status) : query.OrderByDescending(v => v.Status),
            "CreatedAt" => ascending ? query.OrderBy(v => v.CreatedAt ?? DateTime.MinValue) : query.OrderByDescending(v => v.CreatedAt ?? DateTime.MinValue),
            _ => query.OrderBy(v => v.Name)
        };
    }

    public void NextPage()
    {
        if (HasNextPage)
        {
            CurrentPage++;
        }
    }

    public void PreviousPage()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
        }
    }

    public void GoToPage(int page)
    {
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
        }
    }

    public void ToggleSort(string sortBy)
    {
        if (SortBy == sortBy)
        {
            SortDirection = SortDirection == "Asc" ? "Desc" : "Asc";
        }
        else
        {
            SortBy = sortBy;
            SortDirection = "Asc";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async Task LoadVendorOrderSummaryAsync(VendorDisplay vendorDisplay, string vendorId)
    {
        try
        {
            if (_vendorOrderService == null) return;

            var orders = await _vendorOrderService.GetVendorOrdersByVendorIdAsync(vendorId);
            var ordersList = orders.ToList();

            vendorDisplay.TotalOrders = ordersList.Count;
            vendorDisplay.PendingOrders = ordersList.Count(o => o.Status == "Draft" || o.Status == "Sent" || o.Status == "Confirmed" || o.Status == "Shipped");
            vendorDisplay.TotalOrderValue = ordersList.Sum(o => o.TotalValue);
            vendorDisplay.LastOrderDate = ordersList.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;

            // Calculate performance metrics
            if (ordersList.Any())
            {
                var deliveredOrders = ordersList.Where(o => o.Status == "Delivered" && o.ActualDeliveryDate.HasValue && o.ExpectedDeliveryDate.HasValue).ToList();
                
                if (deliveredOrders.Any())
                {
                    // On-time delivery rate
                    var onTimeDeliveries = deliveredOrders.Count(o => o.ActualDeliveryDate.Value <= o.ExpectedDeliveryDate.Value);
                    vendorDisplay.OnTimeDeliveryRate = (double)onTimeDeliveries / deliveredOrders.Count * 100;

                    // Average delivery time (in days)
                    var deliveryTimes = deliveredOrders
                        .Where(o => o.SentDate.HasValue && o.ActualDeliveryDate.HasValue)
                        .Select(o => (o.ActualDeliveryDate.Value - o.SentDate.Value).TotalDays)
                        .ToList();
                    if (deliveryTimes.Any())
                    {
                        vendorDisplay.AverageDeliveryDays = deliveryTimes.Average();
                    }

                    // Average order value
                    vendorDisplay.AverageOrderValue = deliveredOrders.Average(o => o.TotalValue);
                }

                // Monthly spend (last 30 days)
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var recentOrders = ordersList.Where(o => o.OrderDate >= thirtyDaysAgo).ToList();
                vendorDisplay.MonthlySpend = recentOrders.Sum(o => o.TotalValue);

                // Orders this month
                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                vendorDisplay.OrdersThisMonth = ordersList.Count(o => o.OrderDate >= startOfMonth);
            }
        }
        catch
        {
            // Silently fail - order summary is not critical
        }
    }
}

public class VendorDisplay : INotifyPropertyChanged
{
    private string _id;
    private string _name;
    private string? _contactInfo;
    private string _status;
    private decimal _budget;
    private string? _reminder;
    private bool _reminderEnabled;
    private string? _notes;
    private DateTime? _createdAt;
    private DateTime? _updatedAt;
    private int _totalOrders;
    private int _pendingOrders;
    private decimal _totalOrderValue;
    private DateTime? _lastOrderDate;
    private double _onTimeDeliveryRate;
    private double _averageDeliveryDays;
    private decimal _averageOrderValue;
    private decimal _monthlySpend;
    private int _ordersThisMonth;

    public VendorDisplay(VendorDto dto)
    {
        _id = dto.Id ?? string.Empty;
        _name = dto.Name;
        _contactInfo = dto.ContactInfo;
        _status = dto.Status;
        _budget = dto.Budget;
        _reminder = dto.Reminder;
        _reminderEnabled = dto.ReminderEnabled;
        _notes = dto.Notes;
        _createdAt = dto.CreatedAt;
        _updatedAt = dto.UpdatedAt;
    }

    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ContactInfo
    {
        get => _contactInfo;
        set
        {
            if (_contactInfo != value)
            {
                _contactInfo = value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public decimal Budget
    {
        get => _budget;
        set
        {
            if (_budget != value)
            {
                _budget = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BudgetDisplay));
            }
        }
    }

    public string? Reminder
    {
        get => _reminder;
        set
        {
            if (_reminder != value)
            {
                _reminder = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ReminderEnabled
    {
        get => _reminderEnabled;
        set
        {
            if (_reminderEnabled != value)
            {
                _reminderEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReminderVisibility));
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            if (_notes != value)
            {
                _notes = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? CreatedAt
    {
        get => _createdAt;
        set
        {
            if (_createdAt != value)
            {
                _createdAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CreatedAtDisplay));
            }
        }
    }

    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set
        {
            if (_updatedAt != value)
            {
                _updatedAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UpdatedAtDisplay));
            }
        }
    }

    public string StatusDisplay => Status == "active" ? "Active" : "Inactive";
    public bool IsActive => Status == "active";
    public string BudgetDisplay => Budget.ToString("C");
    public Windows.UI.Color StatusColor => Status == "active" ? Windows.UI.Color.FromArgb(255, 0, 128, 0) : Windows.UI.Color.FromArgb(255, 128, 128, 128);
    public Microsoft.UI.Xaml.Visibility ReminderVisibility => ReminderEnabled ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public string CreatedAtDisplay => CreatedAt?.ToString("MM/dd/yyyy") ?? "N/A";
    public string UpdatedAtDisplay => UpdatedAt?.ToString("MM/dd/yyyy") ?? "N/A";
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    public int TotalOrders
    {
        get => _totalOrders;
        set
        {
            if (_totalOrders != value)
            {
                _totalOrders = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasOrders));
            }
        }
    }

    public int PendingOrders
    {
        get => _pendingOrders;
        set
        {
            if (_pendingOrders != value)
            {
                _pendingOrders = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal TotalOrderValue
    {
        get => _totalOrderValue;
        set
        {
            if (_totalOrderValue != value)
            {
                _totalOrderValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalOrderValueDisplay));
            }
        }
    }

    public DateTime? LastOrderDate
    {
        get => _lastOrderDate;
        set
        {
            if (_lastOrderDate != value)
            {
                _lastOrderDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastOrderDateDisplay));
            }
        }
    }

    public string TotalOrderValueDisplay => TotalOrderValue.ToString("C");
    public string LastOrderDateDisplay => LastOrderDate?.ToString("MM/dd/yyyy") ?? "Never";
    public bool HasOrders => TotalOrders > 0;

    public double OnTimeDeliveryRate
    {
        get => _onTimeDeliveryRate;
        set
        {
            if (_onTimeDeliveryRate != value)
            {
                _onTimeDeliveryRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OnTimeDeliveryRateDisplay));
            }
        }
    }

    public double AverageDeliveryDays
    {
        get => _averageDeliveryDays;
        set
        {
            if (_averageDeliveryDays != value)
            {
                _averageDeliveryDays = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AverageDeliveryDaysDisplay));
            }
        }
    }

    public decimal AverageOrderValue
    {
        get => _averageOrderValue;
        set
        {
            if (_averageOrderValue != value)
            {
                _averageOrderValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AverageOrderValueDisplay));
            }
        }
    }

    public decimal MonthlySpend
    {
        get => _monthlySpend;
        set
        {
            if (_monthlySpend != value)
            {
                _monthlySpend = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MonthlySpendDisplay));
            }
        }
    }

    public int OrdersThisMonth
    {
        get => _ordersThisMonth;
        set
        {
            if (_ordersThisMonth != value)
            {
                _ordersThisMonth = value;
                OnPropertyChanged();
            }
        }
    }

    public string OnTimeDeliveryRateDisplay => OnTimeDeliveryRate > 0 ? $"{OnTimeDeliveryRate:F1}%" : "N/A";
    public string AverageDeliveryDaysDisplay => AverageDeliveryDays > 0 ? $"{AverageDeliveryDays:F1} days" : "N/A";
    public string AverageOrderValueDisplay => AverageOrderValue > 0 ? AverageOrderValue.ToString("C") : "N/A";
    public string MonthlySpendDisplay => MonthlySpend.ToString("C");

    public void UpdateFrom(VendorDto dto)
    {
        Name = dto.Name;
        ContactInfo = dto.ContactInfo;
        Status = dto.Status;
        Budget = dto.Budget;
        Reminder = dto.Reminder;
        ReminderEnabled = dto.ReminderEnabled;
        Notes = dto.Notes;
        CreatedAt = dto.CreatedAt;
        UpdatedAt = dto.UpdatedAt;
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(ReminderVisibility));
        OnPropertyChanged(nameof(HasNotes));
    }

    public VendorDto ToDto()
    {
        return new VendorDto
        {
            Id = Id,
            Name = Name,
            ContactInfo = ContactInfo,
            Status = Status,
            Budget = Budget,
            Reminder = Reminder,
            ReminderEnabled = ReminderEnabled,
            Notes = Notes,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

