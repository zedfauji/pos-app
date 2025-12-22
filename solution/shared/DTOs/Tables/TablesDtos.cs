namespace MagiDesk.Shared.DTOs.Tables;

public record StartSessionRequest(string ServerId, string ServerName);

public sealed record ReopenSessionResult(
    Guid SessionId,
    Guid BillingId,
    string TableLabel,
    string Message
);

public class MoveSessionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public bool ConfirmationNeeded { get; set; }
    public decimal CostFinalized { get; set; }
    public string ActionTaken { get; set; } = string.Empty; // TimerStopped, TimerStarted, Moved
}

public class TableStatusDto
{
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "billiard"; // Legacy string for compatibility
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public TableConfigDto Config { get; set; } = new();
    
    public bool Occupied { get; set; }
    public string? OrderId { get; set; }
    public Guid? CurrentSessionId { get; set; } // Explicit Session ID
    public DateTimeOffset? StartTime { get; set; }
    public string? Server { get; set; }
}

public class TableConfigDto 
{
    public bool HasTimer { get; set; }
    public decimal HourlyRate { get; set; }
    public bool AllowOrders { get; set; }
    public bool RequiresServer { get; set; }
}

public class TableTypeDto : TableConfigDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BillResult
{
    public Guid BillId { get; set; }
    public Guid? BillingId { get; set; }
    public Guid? SessionId { get; set; }
    public string TableLabel { get; set; } = string.Empty;
    public string? OriginalTableLabel { get; set; }
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalTimeMinutes { get; set; }
    public List<ItemLine> Items { get; set; } = new();
    public decimal TimeCost { get; set; }
    public decimal ItemsCost { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Payment Details
    public MagiDesk.Shared.Enums.PaymentMethod PaymentMethod { get; set; }
    public decimal AmountTendered { get; set; }
    public decimal ChangeDue { get; set; }
    public decimal TipAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CustomerEmail { get; set; }
}

public class ItemLine
{
    public string itemId { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public int quantity { get; set; }
    public decimal price { get; set; }
}
