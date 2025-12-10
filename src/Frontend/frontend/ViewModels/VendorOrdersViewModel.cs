using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using System.ComponentModel;
using System.Linq;

namespace MagiDesk.Frontend.ViewModels;

public class VendorOrdersViewModel : INotifyPropertyChanged
{
    private readonly IVendorOrderService _vendorOrderService;
    private readonly IInventoryService _inventoryService;

    private ObservableCollection<VendorOrderDto> _orders = new();
    private ObservableCollection<VendorOrderDto> _filteredOrders = new();
    private string _searchText = string.Empty;
    private string _statusFilter = string.Empty;

    // Statistics properties
    public int TotalOrders => _orders.Count;
    public int PendingOrders => _orders.Count(o => o.Status is "Draft" or "Sent" or "Confirmed");
    public decimal TotalOrderValue => _orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalValue);
    public int OverdueOrders => _orders.Count(o => o.IsOverdue);
    public int AverageDeliveryDays => (int)_orders.Where(o => o.DeliveryDays > 0).DefaultIfEmpty().Average(o => o?.DeliveryDays ?? 0);

    public ObservableCollection<VendorOrderDto> FilteredOrders => _filteredOrders;

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

    public VendorOrdersViewModel(IVendorOrderService vendorOrderService, IInventoryService inventoryService)
    {
        _vendorOrderService = vendorOrderService;
        _inventoryService = inventoryService;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            // TODO: Replace with actual API call
            // For now, create sample data
            _orders.Clear();
            var sampleOrders = new[]
            {
                new VendorOrderDto
                {
                    OrderId = "PO-2024-001",
                    VendorId = "VENDOR-001",
                    VendorName = "Coffee Supply Co.",
                    OrderDate = DateTime.Now.AddDays(-5),
                    ExpectedDeliveryDate = DateTime.Now.AddDays(2),
                    Status = "Sent",
                    Priority = "High",
                    TotalValue = 1250.00m,
                    ItemCount = 3,
                    Notes = "Urgent coffee bean order",
                    CreatedBy = "Manager",
                    SentBy = "Manager",
                    SentDate = DateTime.Now.AddDays(-4),
                    DeliveryDays = 7
                },
                new VendorOrderDto
                {
                    OrderId = "PO-2024-002",
                    VendorId = "VENDOR-002",
                    VendorName = "Meat Suppliers Inc.",
                    OrderDate = DateTime.Now.AddDays(-3),
                    ExpectedDeliveryDate = DateTime.Now.AddDays(1),
                    Status = "Confirmed",
                    Priority = "Medium",
                    TotalValue = 850.50m,
                    ItemCount = 2,
                    Notes = "Monthly meat order",
                    CreatedBy = "Chef",
                    SentBy = "Chef",
                    SentDate = DateTime.Now.AddDays(-2),
                    ConfirmedBy = "Vendor",
                    ConfirmedDate = DateTime.Now.AddDays(-1),
                    DeliveryDays = 4
                },
                new VendorOrderDto
                {
                    OrderId = "PO-2024-003",
                    VendorId = "VENDOR-003",
                    VendorName = "Supply Solutions",
                    OrderDate = DateTime.Now.AddDays(-7),
                    ExpectedDeliveryDate = DateTime.Now.AddDays(-1),
                    ActualDeliveryDate = DateTime.Now.AddDays(-1),
                    Status = "Delivered",
                    Priority = "Low",
                    TotalValue = 450.75m,
                    ItemCount = 5,
                    Notes = "Regular supply order",
                    CreatedBy = "Staff",
                    SentBy = "Staff",
                    SentDate = DateTime.Now.AddDays(-6),
                    ConfirmedBy = "Vendor",
                    ConfirmedDate = DateTime.Now.AddDays(-5),
                    DeliveryDays = 6
                },
                new VendorOrderDto
                {
                    OrderId = "PO-2024-004",
                    VendorId = "VENDOR-001",
                    VendorName = "Coffee Supply Co.",
                    OrderDate = DateTime.Now.AddDays(-2),
                    ExpectedDeliveryDate = DateTime.Now.AddDays(3),
                    Status = "Draft",
                    Priority = "Medium",
                    TotalValue = 675.25m,
                    ItemCount = 2,
                    Notes = "Additional coffee supplies",
                    CreatedBy = "Manager",
                    DeliveryDays = 0
                },
                new VendorOrderDto
                {
                    OrderId = "PO-2024-005",
                    VendorId = "VENDOR-004",
                    VendorName = "Fresh Produce Ltd.",
                    OrderDate = DateTime.Now.AddDays(-10),
                    ExpectedDeliveryDate = DateTime.Now.AddDays(-2),
                    Status = "Shipped",
                    Priority = "Urgent",
                    TotalValue = 320.00m,
                    ItemCount = 4,
                    Notes = "Fresh vegetables order",
                    CreatedBy = "Chef",
                    SentBy = "Chef",
                    SentDate = DateTime.Now.AddDays(-9),
                    ConfirmedBy = "Vendor",
                    ConfirmedDate = DateTime.Now.AddDays(-8),
                    TrackingNumber = "TRK123456789",
                    IsOverdue = true,
                    DeliveryDays = 0
                }
            };

            foreach (var order in sampleOrders)
            {
                _orders.Add(order);
            }

            ApplyFilters();
            OnPropertyChanged(nameof(TotalOrders));
            OnPropertyChanged(nameof(PendingOrders));
            OnPropertyChanged(nameof(TotalOrderValue));
            OnPropertyChanged(nameof(OverdueOrders));
            OnPropertyChanged(nameof(AverageDeliveryDays));
        }
        catch (Exception ex)
        {
        }
    }

    public void ApplyFilters()
    {
        var filtered = _orders.AsEnumerable();

        if (!string.IsNullOrEmpty(_searchText))
        {
            filtered = filtered.Where(o => 
                o.OrderId.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                o.VendorName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                o.Notes.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(_statusFilter))
        {
            filtered = filtered.Where(o => o.Status == _statusFilter);
        }

        _filteredOrders.Clear();
        foreach (var order in filtered.OrderByDescending(o => o.OrderDate))
        {
            _filteredOrders.Add(order);
        }

        OnPropertyChanged(nameof(FilteredOrders));
    }

    public async Task CreateOrderAsync(VendorOrderDto order)
    {
        try
        {
            order.OrderId = $"PO-{DateTime.Now:yyyy}-{_orders.Count + 1:D3}";
            order.OrderDate = DateTime.Now;
            order.Status = "Draft";
            order.CreatedBy = "Current User";
            
            _orders.Add(order);
            ApplyFilters();
            OnPropertyChanged(nameof(TotalOrders));
            OnPropertyChanged(nameof(PendingOrders));
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<VendorOrderDto?> GetOrderAsync(string orderId)
    {
        try
        {
            return _orders.FirstOrDefault(o => o.OrderId == orderId);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task UpdateOrderAsync(string orderId, VendorOrderDto order)
    {
        try
        {
            var existingOrder = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (existingOrder != null)
            {
                var index = _orders.IndexOf(existingOrder);
                _orders[index] = order;
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task SendOrderAsync(string orderId)
    {
        try
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order != null)
            {
                order.Status = "Sent";
                order.SentBy = "Current User";
                order.SentDate = DateTime.Now;
                ApplyFilters();
                OnPropertyChanged(nameof(PendingOrders));
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task ReceiveOrderAsync(string orderId, Dictionary<string, int> receivedItems)
    {
        try
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order != null)
            {
                order.Status = "Delivered";
                order.ActualDeliveryDate = DateTime.Now;
                order.DeliveryDays = (int)(order.ActualDeliveryDate.Value - order.OrderDate).TotalDays;
                
                ApplyFilters();
                OnPropertyChanged(nameof(PendingOrders));
                OnPropertyChanged(nameof(AverageDeliveryDays));
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task CancelOrderAsync(string orderId)
    {
        try
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order != null)
            {
                order.Status = "Cancelled";
                ApplyFilters();
                OnPropertyChanged(nameof(PendingOrders));
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task ImportOrdersAsync()
    {
        try
        {
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task ExportOrdersAsync()
    {
        try
        {
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}