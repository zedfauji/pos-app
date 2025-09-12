namespace MagiDesk.Shared.DTOs;

public class RestockRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Ordered, Received, Cancelled
    public int Priority { get; set; } = 1; // 1=Low, 2=Medium, 3=High, 4=Urgent
    public string? Notes { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? VendorId { get; set; }
    public decimal? UnitCost { get; set; }

    // Computed properties for UI
    public string StatusColor => Status switch
    {
        "Pending" => "Orange",
        "Approved" => "Blue", 
        "Ordered" => "Purple",
        "Received" => "Green",
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

    public string PriorityText => Priority switch
    {
        1 => "Low",
        2 => "Medium",
        3 => "High", 
        4 => "Urgent",
        _ => "Unknown"
    };
}



