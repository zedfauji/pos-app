using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using System.ComponentModel;
using System.Linq;

namespace MagiDesk.Frontend.ViewModels;

public class RestockViewModel : INotifyPropertyChanged
{
    private readonly IRestockService _restockService;
    private readonly IInventoryService _inventoryService;

    private ObservableCollection<RestockRequestDto> _requests = new();
    private ObservableCollection<RestockRequestDto> _filteredRequests = new();
    private string _searchText = string.Empty;
    private string _statusFilter = string.Empty;

    // Statistics properties
    public int TotalRequests => _requests.Count;
    public int PendingRequests => _requests.Count(r => r.Status == "Pending");
    public int LowStockItems => _requests.Count(r => r.CurrentStock <= 10);
    public decimal TotalRestockValue => _requests.Where(r => r.Status != "Cancelled").Sum(r => r.TotalCost);
    public int UrgentItems => _requests.Count(r => r.IsUrgent || r.Priority >= 4);

    public ObservableCollection<RestockRequestDto> FilteredRequests => _filteredRequests;

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            _statusFilter = value;
            OnPropertyChanged(nameof(StatusFilter));
        }
    }

    public RestockViewModel(IRestockService restockService, IInventoryService inventoryService)
    {
        _restockService = restockService;
        _inventoryService = inventoryService;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            // TODO: Replace with actual API call
            // For now, create sample data
            _requests.Clear();
            var sampleRequests = new[]
            {
                new RestockRequestDto
                {
                    RequestId = "REQ-001",
                    ItemName = "Coffee Beans",
                    CurrentStock = 5,
                    RequestedQuantity = 50,
                    VendorName = "Coffee Supply Co.",
                    UnitCost = 15.99m,
                    TotalCost = 799.50m,
                    Status = "Pending",
                    CreatedDate = DateTime.Now.AddDays(-2),
                    IsUrgent = true,
                    Priority = 4
                },
                new RestockRequestDto
                {
                    RequestId = "REQ-002", 
                    ItemName = "Burger Patties",
                    CurrentStock = 8,
                    RequestedQuantity = 25,
                    VendorName = "Meat Suppliers Inc.",
                    UnitCost = 2.50m,
                    TotalCost = 62.50m,
                    Status = "Approved",
                    CreatedDate = DateTime.Now.AddDays(-1),
                    ApprovedDate = DateTime.Now.AddHours(-2),
                    Priority = 3
                },
                new RestockRequestDto
                {
                    RequestId = "REQ-003",
                    ItemName = "Cooking Oil", 
                    CurrentStock = 3,
                    RequestedQuantity = 10,
                    VendorName = "Supply Solutions",
                    UnitCost = 8.99m,
                    TotalCost = 89.90m,
                    Status = "Ordered",
                    CreatedDate = DateTime.Now.AddDays(-3),
                    ApprovedDate = DateTime.Now.AddDays(-2),
                    OrderedDate = DateTime.Now.AddDays(-1),
                    Priority = 2
                },
                new RestockRequestDto
                {
                    RequestId = "REQ-004",
                    ItemName = "Paper Cups",
                    CurrentStock = 45,
                    RequestedQuantity = 200,
                    VendorName = "Supply Solutions", 
                    UnitCost = 0.25m,
                    TotalCost = 50.00m,
                    Status = "Received",
                    CreatedDate = DateTime.Now.AddDays(-5),
                    ApprovedDate = DateTime.Now.AddDays(-4),
                    OrderedDate = DateTime.Now.AddDays(-3),
                    ReceivedDate = DateTime.Now.AddDays(-1),
                    Priority = 1
                },
                new RestockRequestDto
                {
                    RequestId = "REQ-005",
                    ItemName = "Salt",
                    CurrentStock = 12,
                    RequestedQuantity = 20,
                    VendorName = "Fresh Produce Ltd.",
                    UnitCost = 1.50m,
                    TotalCost = 30.00m,
                    Status = "Cancelled",
                    CreatedDate = DateTime.Now.AddDays(-4),
                    Priority = 1
                }
            };

            foreach (var request in sampleRequests)
            {
                _requests.Add(request);
            }

            ApplyFilters();
            OnPropertyChanged(nameof(TotalRequests));
            OnPropertyChanged(nameof(PendingRequests));
            OnPropertyChanged(nameof(LowStockItems));
            OnPropertyChanged(nameof(TotalRestockValue));
            OnPropertyChanged(nameof(UrgentItems));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading restock data: {ex.Message}");
        }
    }

    public void ApplyFilters()
    {
        var filtered = _requests.AsEnumerable();

        if (!string.IsNullOrEmpty(_searchText))
        {
            filtered = filtered.Where(r => 
                r.ItemName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                r.VendorName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                r.RequestId.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(_statusFilter))
        {
            filtered = filtered.Where(r => r.Status == _statusFilter);
        }

        _filteredRequests.Clear();
        foreach (var request in filtered.OrderByDescending(r => r.CreatedDate))
        {
            _filteredRequests.Add(request);
        }

        OnPropertyChanged(nameof(FilteredRequests));
    }

    public async Task AddRestockRequestAsync(RestockRequestDto request)
    {
        try
        {
            // TODO: Call actual API
            _requests.Add(request);
            ApplyFilters();
            OnPropertyChanged(nameof(TotalRequests));
            OnPropertyChanged(nameof(PendingRequests));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding restock request: {ex.Message}");
            throw;
        }
    }

    public async Task ApproveRequestAsync(string requestId)
    {
        try
        {
            var request = _requests.FirstOrDefault(r => r.RequestId == requestId);
            if (request != null)
            {
                request.Status = "Approved";
                request.ApprovedDate = DateTime.Now;
                request.ApprovedBy = "Current User"; // TODO: Get actual user
                ApplyFilters();
                OnPropertyChanged(nameof(PendingRequests));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error approving request: {ex.Message}");
            throw;
        }
    }

    public async Task MarkAsOrderedAsync(string requestId)
    {
        try
        {
            var request = _requests.FirstOrDefault(r => r.RequestId == requestId);
            if (request != null)
            {
                request.Status = "Ordered";
                request.OrderedDate = DateTime.Now;
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking as ordered: {ex.Message}");
            throw;
        }
    }

    public async Task MarkAsReceivedAsync(string requestId)
    {
        try
        {
            var request = _requests.FirstOrDefault(r => r.RequestId == requestId);
            if (request != null)
            {
                request.Status = "Received";
                request.ReceivedDate = DateTime.Now;
                
                // TODO: Update inventory stock
                // await _inventoryService.UpdateStockAsync(request.ItemId, request.RequestedQuantity);
                
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking as received: {ex.Message}");
            throw;
        }
    }

    public async Task CancelRequestAsync(string requestId)
    {
        try
        {
            var request = _requests.FirstOrDefault(r => r.RequestId == requestId);
            if (request != null)
            {
                request.Status = "Cancelled";
                ApplyFilters();
                OnPropertyChanged(nameof(PendingRequests));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cancelling request: {ex.Message}");
            throw;
        }
    }

    public async Task AutoRestockLowStockAsync()
    {
        try
        {
            // TODO: Get low stock items from inventory service
            // For now, create sample auto-restock requests
            var lowStockItems = new[]
            {
                new RestockRequestDto
                {
                    RequestId = $"AUTO-{Guid.NewGuid().ToString()[..8]}",
                    ItemName = "Sugar",
                    CurrentStock = 2,
                    RequestedQuantity = 30,
                    VendorName = "Fresh Produce Ltd.",
                    UnitCost = 2.00m,
                    TotalCost = 60.00m,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,
                    IsUrgent = true,
                    Priority = 4,
                    Notes = "Auto-generated restock request"
                },
                new RestockRequestDto
                {
                    RequestId = $"AUTO-{Guid.NewGuid().ToString()[..8]}",
                    ItemName = "Flour",
                    CurrentStock = 1,
                    RequestedQuantity = 20,
                    VendorName = "Supply Solutions",
                    UnitCost = 3.50m,
                    TotalCost = 70.00m,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,
                    IsUrgent = true,
                    Priority = 4,
                    Notes = "Auto-generated restock request"
                }
            };

            foreach (var request in lowStockItems)
            {
                _requests.Add(request);
            }

            ApplyFilters();
            OnPropertyChanged(nameof(TotalRequests));
            OnPropertyChanged(nameof(PendingRequests));
            OnPropertyChanged(nameof(UrgentItems));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in auto-restock: {ex.Message}");
            throw;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}



