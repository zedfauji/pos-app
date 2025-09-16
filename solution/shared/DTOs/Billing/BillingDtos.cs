using System.ComponentModel.DataAnnotations;

namespace MagiDesk.Shared.DTOs.Billing;

/// <summary>
/// Core billing entity that aggregates multiple sessions and orders
/// </summary>
public class BillingDto
{
    public Guid BillingId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    
    [Required]
    public string Status { get; set; } = "open"; // open, closed, paid, cancelled
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    
    public List<BillingSessionDto> Sessions { get; set; } = new();
    public List<BillingOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// Session information within a billing context
/// </summary>
public class BillingSessionDto
{
    public Guid SessionId { get; set; }
    public Guid BillingId { get; set; }
    public string TableLabel { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    
    [Required]
    public string Status { get; set; } = "active"; // active, closed, moved, transferred
    
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    // Session movement tracking
    public string? OriginalTableId { get; set; }
    public string? DestinationTableId { get; set; }
    public DateTime? MovedAt { get; set; }
    
    // Session totals
    public decimal SessionTotal { get; set; }
    public int OrdersCount { get; set; }
    
    public List<BillingOrderDto> Orders { get; set; } = new();
}

/// <summary>
/// Order information within a billing context
/// </summary>
public class BillingOrderDto
{
    public long OrderId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? BillingId { get; set; }
    public string TableId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    
    [Required]
    public string Status { get; set; } = "open";
    
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal ProfitTotal { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    public string DeliveryStatus { get; set; } = "pending";
    
    public List<BillingOrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// Order item information within a billing context
/// </summary>
public class BillingOrderItemDto
{
    public long OrderItemId { get; set; }
    public long OrderId { get; set; }
    public long? MenuItemId { get; set; }
    public long? ComboId { get; set; }
    
    public int Quantity { get; set; } = 1;
    public int DeliveredQuantity { get; set; } = 0;
    
    public decimal BasePrice { get; set; }
    public decimal VendorPrice { get; set; }
    public decimal PriceDelta { get; set; }
    public decimal LineDiscount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal Profit { get; set; }
    
    public bool IsDeleted { get; set; }
    public string? Notes { get; set; }
    
    // Snapshot data for historical accuracy
    public string? SnapshotName { get; set; }
    public string? SnapshotSku { get; set; }
    public string? SnapshotCategory { get; set; }
    public string? SnapshotGroup { get; set; }
    public int? SnapshotVersion { get; set; }
    public string? SnapshotPictureUrl { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request for moving a session between tables
/// </summary>
public class SessionMoveRequest
{
    [Required]
    public Guid SessionId { get; set; }
    
    [Required]
    public string FromTableId { get; set; } = string.Empty;
    
    [Required]
    public string ToTableId { get; set; } = string.Empty;
    
    [Required]
    public string ServerId { get; set; } = string.Empty;
    
    [Required]
    public string ServerName { get; set; } = string.Empty;
    
    public string? Reason { get; set; }
}

/// <summary>
/// Response for session move operation
/// </summary>
public class SessionMoveResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? NewSessionId { get; set; }
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Consolidated billing summary for UI display
/// </summary>
public class BillingSummaryDto
{
    public Guid BillingId { get; set; }
    public string DisplayId { get; set; } = string.Empty; // Short ID for UI
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    
    // Aggregated data across all sessions
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int MovedSessions { get; set; }
    public int TotalOrders { get; set; }
    public int TotalItems { get; set; }
    
    // Financial summary
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Time tracking
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivity { get; set; }
    public TimeSpan? TotalDuration { get; set; }
    
    // Current tables (for active sessions)
    public List<string> CurrentTables { get; set; } = new();
    public List<string> MovementHistory { get; set; } = new(); // "Table A → Table B"
    
    // Primary server (most recent or most active)
    public string PrimaryServer { get; set; } = string.Empty;
}

/// <summary>
/// Request for creating a new billing entity
/// </summary>
public class CreateBillingRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    
    [Required]
    public string ServerId { get; set; } = string.Empty;
    
    [Required]
    public string ServerName { get; set; } = string.Empty;
    
    [Required]
    public string TableId { get; set; } = string.Empty;
}

/// <summary>
/// Response for creating a new billing entity
/// </summary>
public class CreateBillingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? BillingId { get; set; }
    public Guid? SessionId { get; set; }
    public string? ErrorCode { get; set; }
}
