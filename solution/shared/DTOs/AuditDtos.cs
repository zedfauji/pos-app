namespace MagiDesk.Shared.DTOs;

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
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}



