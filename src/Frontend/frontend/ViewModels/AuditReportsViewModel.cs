using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using System.ComponentModel;
using System.Linq;

namespace MagiDesk.Frontend.ViewModels;

public class AuditReportsViewModel : INotifyPropertyChanged
{
    private readonly IAuditService _auditService;
    private readonly IInventoryService _inventoryService;

    private ObservableCollection<AuditLogDto> _transactionLogs = new();
    private ObservableCollection<AuditLogDto> _systemEvents = new();
    private ObservableCollection<InventoryReportDto> _inventoryReports = new();
    private ObservableCollection<TopSellingItemDto> _topSellingItems = new();
    private string _searchText = string.Empty;
    private string _reportTypeFilter = string.Empty;

    // Statistics properties
    public int TotalTransactions => _transactionLogs.Count;
    public int TodaysActivity => _transactionLogs.Count(t => t.Timestamp.Date == DateTime.Today);
    public decimal TotalValue => _transactionLogs
        .Where(t => t.Action == "Sale" || t.Action == "Purchase")
        .Sum(t => ExtractValueFromDetails(t.NewValues));
    public int ErrorCount => _systemEvents.Count(e => e.Action == "Error");
    public int TopItemsCount => _topSellingItems.Count;

    public ObservableCollection<AuditLogDto> TransactionLogs => _transactionLogs;
    public ObservableCollection<AuditLogDto> SystemEvents => _systemEvents;
    public ObservableCollection<InventoryReportDto> InventoryReports => _inventoryReports;
    public ObservableCollection<TopSellingItemDto> TopSellingItems => _topSellingItems;

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
        }
    }

    public string ReportTypeFilter
    {
        get => _reportTypeFilter;
        set
        {
            _reportTypeFilter = value;
            OnPropertyChanged(nameof(ReportTypeFilter));
        }
    }

    public AuditReportsViewModel(IAuditService auditService, IInventoryService inventoryService)
    {
        _auditService = auditService;
        _inventoryService = inventoryService;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            // Load transaction logs
            var logs = await _auditService.GetAuditLogsAsync();
            _transactionLogs.Clear();
            foreach (var log in logs)
            {
                _transactionLogs.Add(log);
            }

            // Load system events (using same method with different parameters)
            var events = await _auditService.GetAuditLogsAsync(entityType: "System");
            _systemEvents.Clear();
            foreach (var evt in events)
            {
                _systemEvents.Add(evt);
            }

            // Load inventory reports
            var report = await _auditService.GenerateInventoryReportAsync();
            _inventoryReports.Clear();
            _inventoryReports.Add(report);

            // Load top selling items (using inventory transactions)
            var topItems = await _auditService.GetInventoryTransactionsAsync();
            var topSellingItems = topItems
                .GroupBy(t => t.ItemName)
                .Select(g => new TopSellingItemDto
                {
                    ItemName = g.Key,
                    QuantitySold = g.Sum(t => Math.Abs(t.Quantity)),
                    Revenue = g.Sum(t => Math.Abs((t.UnitCost ?? 0) * t.Quantity))
                })
                .OrderByDescending(t => t.QuantitySold)
                .Take(10);

            _topSellingItems.Clear();
            foreach (var item in topSellingItems)
            {
                _topSellingItems.Add(item);
            }

            OnPropertyChanged(nameof(TotalTransactions));
            OnPropertyChanged(nameof(TodaysActivity));
            OnPropertyChanged(nameof(TotalValue));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(TopItemsCount));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading audit data: {ex.Message}");
        }
    }

    public void ApplyFilters()
    {
        // Apply search filter
        var filteredLogs = _transactionLogs.AsEnumerable();
        var filteredEvents = _systemEvents.AsEnumerable();

        if (!string.IsNullOrEmpty(_searchText))
        {
            filteredLogs = filteredLogs.Where(l => 
                l.EntityId.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                l.Action.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                l.UserName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            filteredEvents = filteredEvents.Where(e => 
                e.Action.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                e.Details.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                e.UserName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(_reportTypeFilter))
        {
            filteredLogs = filteredLogs.Where(l => l.EntityType.Contains(_reportTypeFilter, StringComparison.OrdinalIgnoreCase));
            filteredEvents = filteredEvents.Where(e => e.EntityType.Contains(_reportTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Update collections
        _transactionLogs.Clear();
        foreach (var log in filteredLogs.OrderByDescending(l => l.Timestamp))
        {
            _transactionLogs.Add(log);
        }

        _systemEvents.Clear();
        foreach (var evt in filteredEvents.OrderByDescending(e => e.Timestamp))
        {
            _systemEvents.Add(evt);
        }

        OnPropertyChanged(nameof(TransactionLogs));
        OnPropertyChanged(nameof(SystemEvents));
    }

    public async Task GenerateCustomReportAsync(Dictionary<string, object> parameters)
    {
        try
        {
            // Extract parameters
            var fromDate = parameters.ContainsKey("fromDate") ? (DateTime)parameters["fromDate"] : DateTime.Now.AddDays(-30);
            var toDate = parameters.ContainsKey("toDate") ? (DateTime)parameters["toDate"] : DateTime.Now;
            var entityType = parameters.ContainsKey("entityType") ? (string)parameters["entityType"] : null;
            var action = parameters.ContainsKey("action") ? (string)parameters["action"] : null;
            var limit = parameters.ContainsKey("limit") ? (int)parameters["limit"] : 1000;

            // Generate custom audit logs
            var customLogs = await _auditService.GetAuditLogsAsync(fromDate, toDate, entityType, action, limit);
            
            // Generate custom inventory transactions
            var customTransactions = await _auditService.GetInventoryTransactionsAsync(fromDate, toDate);
            
            // Generate custom inventory report
            var customReport = await _auditService.GenerateInventoryReportAsync(fromDate, toDate);

            // Update collections with custom data
            _transactionLogs.Clear();
            foreach (var log in customLogs.OrderByDescending(l => l.Timestamp))
            {
                _transactionLogs.Add(log);
            }

            _inventoryReports.Clear();
            _inventoryReports.Add(customReport);

            // Update top selling items from custom transactions
            var customTopItems = customTransactions
                .GroupBy(t => t.ItemName)
                .Select(g => new TopSellingItemDto
                {
                    ItemName = g.Key,
                    QuantitySold = g.Sum(t => Math.Abs(t.Quantity)),
                    Revenue = g.Sum(t => Math.Abs((t.UnitCost ?? 0) * t.Quantity))
                })
                .OrderByDescending(t => t.QuantitySold)
                .Take(10);

            _topSellingItems.Clear();
            foreach (var item in customTopItems)
            {
                _topSellingItems.Add(item);
            }

            // Notify property changes
            OnPropertyChanged(nameof(TransactionLogs));
            OnPropertyChanged(nameof(InventoryReports));
            OnPropertyChanged(nameof(TopSellingItems));
            OnPropertyChanged(nameof(TotalTransactions));
            OnPropertyChanged(nameof(TotalValue));
            OnPropertyChanged(nameof(TopItemsCount));

            System.Diagnostics.Debug.WriteLine($"Generated custom report with {customLogs.Count()} logs and {customTransactions.Count()} transactions");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating custom report: {ex.Message}");
            throw;
        }
    }

    public async Task GenerateInventoryReportAsync(int days)
    {
        try
        {
            var fromDate = DateTime.Now.AddDays(-days);
            var report = await _auditService.GenerateInventoryReportAsync(fromDate);
            _inventoryReports.Clear();
            _inventoryReports.Add(report);
            OnPropertyChanged(nameof(InventoryReports));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating inventory report: {ex.Message}");
            throw;
        }
    }

    public async Task ExportAllDataAsync()
    {
        try
        {
            // Export audit logs
            var auditLogsData = await _auditService.ExportAuditLogsAsync();
            
            // Export inventory report
            var inventoryReportData = await _auditService.ExportInventoryReportAsync();
            
            // Save files using WinUI 3 file picker
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV files", new List<string>() { ".csv" });
            savePicker.FileTypeChoices.Add("Excel files", new List<string>() { ".xlsx" });
            savePicker.SuggestedFileName = $"audit_export_{DateTime.Now:yyyyMMdd_HHmmss}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Save audit logs
                var auditLogsFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync($"audit_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                await Windows.Storage.FileIO.WriteBytesAsync(auditLogsFile, auditLogsData);
                
                // Save inventory report
                var inventoryReportFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync($"inventory_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                await Windows.Storage.FileIO.WriteBytesAsync(inventoryReportFile, inventoryReportData);
                
                System.Diagnostics.Debug.WriteLine($"Exported audit logs to: {auditLogsFile.Path}");
                System.Diagnostics.Debug.WriteLine($"Exported inventory report to: {inventoryReportFile.Path}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting data: {ex.Message}");
            throw;
        }
    }

    public async Task ExportInventoryReportAsync()
    {
        try
        {
            // Export current inventory report
            var inventoryReportData = await _auditService.ExportInventoryReportAsync();
            
            // Save file using WinUI 3 file picker
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Excel files", new List<string>() { ".xlsx" });
            savePicker.SuggestedFileName = $"inventory_report_{DateTime.Now:yyyyMMdd_HHmmss}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteBytesAsync(file, inventoryReportData);
                System.Diagnostics.Debug.WriteLine($"Exported inventory report to: {file.Path}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting inventory report: {ex.Message}");
            throw;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private decimal ExtractValueFromDetails(string? jsonDetails)
    {
        if (string.IsNullOrEmpty(jsonDetails))
            return 0;

        try
        {
            // Try to parse JSON and extract value-related fields
            var json = System.Text.Json.JsonDocument.Parse(jsonDetails);
            var root = json.RootElement;

            // Look for common value fields
            if (root.TryGetProperty("Total", out var totalElement) && totalElement.TryGetDecimal(out var total))
                return total;
            if (root.TryGetProperty("Amount", out var amountElement) && amountElement.TryGetDecimal(out var amount))
                return amount;
            if (root.TryGetProperty("Price", out var priceElement) && priceElement.TryGetDecimal(out var price))
                return price;
            if (root.TryGetProperty("Value", out var valueElement) && valueElement.TryGetDecimal(out var value))
                return value;
            if (root.TryGetProperty("Cost", out var costElement) && costElement.TryGetDecimal(out var cost))
                return cost;

            // If no specific value field found, try to extract from quantity * unit price
            if (root.TryGetProperty("Quantity", out var qtyElement) && qtyElement.TryGetDecimal(out var quantity) &&
                root.TryGetProperty("UnitPrice", out var unitPriceElement) && unitPriceElement.TryGetDecimal(out var unitPrice))
                return quantity * unitPrice;

            return 0;
        }
        catch
        {
            // If JSON parsing fails, try to extract numeric value from string
            var match = System.Text.RegularExpressions.Regex.Match(jsonDetails, @"[\d,]+\.?\d*");
            if (match.Success && decimal.TryParse(match.Value.Replace(",", ""), out var parsedValue))
                return parsedValue;

            return 0;
        }
    }
}
