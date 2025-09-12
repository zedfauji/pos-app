namespace MagiDesk.Shared.DTOs;

public class VendorOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sent, Confirmed, Shipped, Delivered, Cancelled
    public int Priority { get; set; } = 1; // 1=Low, 2=Medium, 3=High, 4=Urgent
    public decimal TotalValue { get; set; }
    public int ItemCount { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? SentBy { get; set; }
    public DateTime? SentDate { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public string? TrackingNumber { get; set; }
    public int DeliveryDays { get; set; }

    // Computed properties for UI
    public string StatusColor => Status switch
    {
        "Draft" => "Gray",
        "Sent" => "Blue",
        "Confirmed" => "Purple", 
        "Shipped" => "Orange",
        "Delivered" => "Green",
        "Cancelled" => "Red",
        _ => "Gray"
    };

    public string PriorityColor => Priority switch
    {
        1 => "Green",    // Low
        2 => "Blue",     // Medium
        3 => "Orange",   // High
        4 => "Red",      // Urgent
        _ => "Gray"
    };

    public bool IsOverdue => ExpectedDeliveryDate.HasValue && 
                           ExpectedDeliveryDate.Value < DateTime.Now && 
                           Status != "Delivered" && 
                           Status != "Cancelled";
}



