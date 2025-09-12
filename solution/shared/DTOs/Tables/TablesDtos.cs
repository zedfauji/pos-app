namespace MagiDesk.Shared.DTOs.Tables;

public record StartSessionRequest(string ServerId, string ServerName);

public class TableStatusDto
{
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "billiard"; // billiard | bar
    public bool Occupied { get; set; }
    public string? OrderId { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public string? Server { get; set; }
}

public class BillResult
{
    public Guid BillId { get; set; }
    public Guid? BillingId { get; set; }
    public Guid? SessionId { get; set; }
    public string TableLabel { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalTimeMinutes { get; set; }
    public List<ItemLine> Items { get; set; } = new();
    public decimal TimeCost { get; set; }
    public decimal ItemsCost { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ItemLine
{
    public string itemId { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public int quantity { get; set; }
    public decimal price { get; set; }
}
