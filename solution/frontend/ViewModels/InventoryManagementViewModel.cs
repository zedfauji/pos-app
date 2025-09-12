using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class InventoryDashboardViewModel
{
    private readonly ApiService _api;

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

    public InventoryDashboardViewModel(ApiService api)
    {
        _api = api;
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
            // TODO: Replace with actual API call to get stock value
            // For now, simulate with sample data
            var totalValue = 125000.50m;
            var change = 5.2m;
            
            TotalStockValue = totalValue.ToString("C");
            StockValueChange = $"+{change:F1}%";
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
            // TODO: Call inventoryApi reports/low-stock endpoint
            // For now, simulate with sample data
            LowStockCount = 12;
            
            // Load sample low stock items
            LowStockItems.Clear();
            var sampleItems = new[]
            {
                new ItemDto { Id = "1", Sku = "COF-BEAN-001", Name = "Coffee Beans", Stock = 5 },
                new ItemDto { Id = "2", Sku = "BUR-PAT-001", Name = "Burger Patties", Stock = 8 },
                new ItemDto { Id = "3", Sku = "FRI-OIL-001", Name = "Cooking Oil", Stock = 3 }
            };

            foreach (var item in sampleItems)
            {
                LowStockItems.Add(item);
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
            // TODO: Call inventoryApi to get pending vendor orders
            // For now, simulate with sample data
            PendingOrdersCount = 7;
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
            // TODO: Call inventoryApi to get fast moving items
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
            // TODO: Call inventoryApi to get recent transactions
            // For now, simulate with sample data
            RecentTransactions.Clear();
            var sampleTransactions = new[]
            {
                new InventoryTransactionDto { Id = "1", ItemName = "Coffee Beans", Type = "Sale", Quantity = -2, Timestamp = DateTime.Now.AddHours(-1) },
                new InventoryTransactionDto { Id = "2", ItemName = "Burger Patties", Type = "Restock", Quantity = 50, Timestamp = DateTime.Now.AddHours(-2) },
                new InventoryTransactionDto { Id = "3", ItemName = "Cooking Oil", Type = "Sale", Quantity = -1, Timestamp = DateTime.Now.AddHours(-3) }
            };

            foreach (var transaction in sampleTransactions)
            {
                RecentTransactions.Add(transaction);
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
    public string ItemName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Sale, Restock, Adjustment, etc.
    public decimal Quantity { get; set; }
    public DateTime Timestamp { get; set; }
}
