using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class InventoryDashboardViewModel
{
    private readonly IInventoryService _inventoryService;
    private readonly IVendorService _vendorService;
    private readonly IRestockService _restockService;
    private readonly IVendorOrderService _vendorOrderService;
    private readonly IAuditService _auditService;

    // KPI Properties
    public string TotalStockValue { get; set; } = "$0.00";
    public string StockValueChange { get; set; } = "+0%";
    public int LowStockCount { get; set; } = 0;
    public int PendingOrdersCount { get; set; } = 0;
    public int FastMoversCount { get; set; } = 0;

    // Collections for data
    public ObservableCollection<ItemDto> LowStockItems { get; } = new();
    public ObservableCollection<VendorDto> ActiveVendors { get; } = new();
    public ObservableCollection<InventoryTransactionDto> RecentTransactions { get; } = new();

    public InventoryDashboardViewModel(IInventoryService inventoryService, IVendorService vendorService, IRestockService restockService, IVendorOrderService vendorOrderService, IAuditService auditService)
    {
        _inventoryService = inventoryService;
        _vendorService = vendorService;
        _restockService = restockService;
        _vendorOrderService = vendorOrderService;
        _auditService = auditService;
    }

    public async Task LoadDashboardDataAsync(CancellationToken ct = default)
    {
        try
        {
            // Load KPI data in parallel
            var tasks = new[]
            {
                LoadStockValueDataAsync(ct),
                LoadLowStockDataAsync(ct),
                LoadPendingOrdersDataAsync(ct),
                LoadFastMoversDataAsync(ct),
                LoadRecentTransactionsAsync(ct)
            };

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            // Set default values on error
            SetDefaultValues();
        }
    }

    private async Task LoadStockValueDataAsync(CancellationToken ct)
    {
        try
        {
            var stockValue = await _inventoryService.GetStockValueAsync(ct);
            TotalStockValue = stockValue.TotalValue.ToString("C");
            StockValueChange = $"+{stockValue.ValueChange:F1}%";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading stock value: {ex.Message}");
            TotalStockValue = "$0.00";
            StockValueChange = "+0%";
        }
    }

    private async Task LoadLowStockDataAsync(CancellationToken ct)
    {
        try
        {
            var lowStockItems = await _inventoryService.GetLowStockItemsAsync(null, ct);
            LowStockCount = lowStockItems.Count();
            
            // Load sample low stock items
            LowStockItems.Clear();
            foreach (var item in lowStockItems.Take(5)) // Show top 5
            {
                LowStockItems.Add(new ItemDto 
                { 
                    Id = item.ItemId, 
                    Sku = item.Sku, 
                    Name = item.Name, 
                    Stock = (int)item.QuantityOnHand 
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading low stock data: {ex.Message}");
            LowStockCount = 0;
        }
    }

    private async Task LoadPendingOrdersDataAsync(CancellationToken ct)
    {
        try
        {
            var pendingOrders = await _vendorOrderService.GetPendingOrdersAsync(ct);
            PendingOrdersCount = pendingOrders.Count();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading pending orders: {ex.Message}");
            PendingOrdersCount = 0;
        }
    }

    private async Task LoadFastMoversDataAsync(CancellationToken ct)
    {
        try
        {
            // TODO: Implement fast movers logic based on transaction data
            // For now, simulate with sample data
            FastMoversCount = 15;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading fast movers: {ex.Message}");
            FastMoversCount = 0;
        }
    }

    private async Task LoadRecentTransactionsAsync(CancellationToken ct)
    {
        try
        {
            var transactions = await _inventoryService.GetRecentTransactionsAsync(10, ct);
            RecentTransactions.Clear();
            foreach (var transaction in transactions)
            {
                RecentTransactions.Add(new InventoryTransactionDto
                {
                    Id = transaction.Id,
                    ItemName = transaction.ItemName,
                    Type = transaction.Type,
                    Quantity = transaction.Quantity,
                    Timestamp = transaction.Timestamp
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading recent transactions: {ex.Message}");
        }
    }

    private void SetDefaultValues()
    {
        TotalStockValue = "$0.00";
        StockValueChange = "+0%";
        LowStockCount = 0;
        PendingOrdersCount = 0;
        FastMoversCount = 0;
    }
}

// Additional DTOs for dashboard
public class InventoryTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Sale, Restock, Adjustment, etc.
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Source { get; set; }
    public string? SourceRef { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? Notes { get; set; }
}
