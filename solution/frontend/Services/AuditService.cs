using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

public interface IAuditService
{
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? entityType = null, string? action = null, int limit = 100, CancellationToken ct = default);
    Task<IEnumerable<InventoryTransactionAuditDto>> GetInventoryTransactionsAsync(DateTime? from = null, DateTime? to = null, string? itemId = null, string? source = null, int limit = 100, CancellationToken ct = default);
    Task<InventoryReportDto> GenerateInventoryReportAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<byte[]> ExportAuditLogsAsync(DateTime? from = null, DateTime? to = null, string format = "csv", CancellationToken ct = default);
    Task<byte[]> ExportInventoryReportAsync(DateTime? from = null, DateTime? to = null, string format = "excel", CancellationToken ct = default);
}

public class AuditService : IAuditService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditService> _logger;

    public AuditService(HttpClient httpClient, ILogger<AuditService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? entityType = null, string? action = null, int limit = 100, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual audit logs endpoint
            // For now, return sample data
            return new List<AuditLogDto>
            {
                new AuditLogDto
                {
                    Id = "1",
                    Timestamp = DateTime.Now.AddHours(-1),
                    EntityType = "Item",
                    EntityId = "COF-BEAN-001",
                    Action = "Update",
                    UserId = "user123",
                    UserName = "Manager",
                    Details = "Updated stock quantity from 50 to 45",
                    OldValues = "{\"Stock\": 50}",
                    NewValues = "{\"Stock\": 45}"
                },
                new AuditLogDto
                {
                    Id = "2",
                    Timestamp = DateTime.Now.AddHours(-2),
                    EntityType = "Vendor",
                    EntityId = "VENDOR-001",
                    Action = "Create",
                    UserId = "user456",
                    UserName = "Admin",
                    Details = "Created new vendor Coffee Supply Co.",
                    OldValues = null,
                    NewValues = "{\"Name\": \"Coffee Supply Co.\", \"Status\": \"Active\"}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            return new List<AuditLogDto>();
        }
    }

    public async Task<IEnumerable<InventoryTransactionAuditDto>> GetInventoryTransactionsAsync(DateTime? from = null, DateTime? to = null, string? itemId = null, string? source = null, int limit = 100, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual transactions endpoint
            // For now, return sample data
            return new List<InventoryTransactionAuditDto>
            {
                new InventoryTransactionAuditDto
                {
                    Id = "1",
                    ItemId = "COF-BEAN-001",
                    ItemName = "Coffee Beans",
                    Type = "Sale",
                    Quantity = -2,
                    UnitCost = 15.99m,
                    Source = "customer_order",
                    SourceRef = "ORDER-123",
                    Timestamp = DateTime.Now.AddHours(-1),
                    UserId = "user123",
                    Notes = "Customer order deduction"
                },
                new InventoryTransactionAuditDto
                {
                    Id = "2",
                    ItemId = "BUR-PAT-001",
                    ItemName = "Burger Patties",
                    Type = "Restock",
                    Quantity = 50,
                    UnitCost = 2.50m,
                    Source = "vendor_order",
                    SourceRef = "VENDOR-ORDER-456",
                    Timestamp = DateTime.Now.AddHours(-2),
                    UserId = "user456",
                    Notes = "Vendor restock"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory transactions");
            return new List<InventoryTransactionAuditDto>();
        }
    }

    public async Task<InventoryReportDto> GenerateInventoryReportAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual report generation
            // For now, return sample data
            return new InventoryReportDto
            {
                ReportDate = DateTime.Now,
                PeriodFrom = from ?? DateTime.Now.AddDays(-30),
                PeriodTo = to ?? DateTime.Now,
                TotalItems = 150,
                TotalValue = 125000.50m,
                LowStockItems = 12,
                FastMovingItems = 15,
                SlowMovingItems = 8,
                TopSellingItems = new List<TopSellingItemDto>
                {
                    new TopSellingItemDto { ItemName = "Coffee Beans", QuantitySold = 150, Revenue = 2398.50m },
                    new TopSellingItemDto { ItemName = "Burger Patties", QuantitySold = 200, Revenue = 500.00m }
                },
                Summary = "Inventory report generated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate inventory report");
            return new InventoryReportDto { Summary = "Failed to generate report" };
        }
    }

    public async Task<byte[]> ExportAuditLogsAsync(DateTime? from = null, DateTime? to = null, string format = "csv", CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual export functionality
            var logs = await GetAuditLogsAsync(from, to, null, null, 1000, ct);
            var csv = GenerateCsvFromAuditLogs(logs);
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export audit logs");
            return Array.Empty<byte>();
        }
    }

    public async Task<byte[]> ExportInventoryReportAsync(DateTime? from = null, DateTime? to = null, string format = "excel", CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual Excel export functionality
            var report = await GenerateInventoryReportAsync(from, to, ct);
            var csv = GenerateCsvFromInventoryReport(report);
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export inventory report");
            return Array.Empty<byte>();
        }
    }

    private string GenerateCsvFromAuditLogs(IEnumerable<AuditLogDto> logs)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Timestamp,EntityType,EntityId,Action,UserId,UserName,Details");
        
        foreach (var log in logs)
        {
            csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.EntityType},{log.EntityId},{log.Action},{log.UserId},{log.UserName},\"{log.Details}\"");
        }
        
        return csv.ToString();
    }

    private string GenerateCsvFromInventoryReport(InventoryReportDto report)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Inventory Report");
        csv.AppendLine($"Report Date: {report.ReportDate:yyyy-MM-dd}");
        csv.AppendLine($"Period: {report.PeriodFrom:yyyy-MM-dd} to {report.PeriodTo:yyyy-MM-dd}");
        csv.AppendLine($"Total Items: {report.TotalItems}");
        csv.AppendLine($"Total Value: {report.TotalValue:C}");
        csv.AppendLine($"Low Stock Items: {report.LowStockItems}");
        csv.AppendLine($"Fast Moving Items: {report.FastMovingItems}");
        csv.AppendLine($"Slow Moving Items: {report.SlowMovingItems}");
        csv.AppendLine();
        csv.AppendLine("Top Selling Items");
        csv.AppendLine("Item Name,Quantity Sold,Revenue");
        
        foreach (var item in report.TopSellingItems)
        {
            csv.AppendLine($"{item.ItemName},{item.QuantitySold},{item.Revenue:C}");
        }
        
        return csv.ToString();
    }
}

// DTOs for audit service
public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string EntityType { get; set; } = string.Empty; // Item, Vendor, Order, etc.
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, etc.
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public class InventoryReportDto
{
    public DateTime ReportDate { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockItems { get; set; }
    public int FastMovingItems { get; set; }
    public int SlowMovingItems { get; set; }
    public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class TopSellingItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class InventoryTransactionAuditDto
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
