using System;

namespace MagiDesk.Frontend.Services;

public class VendorOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string Priority { get; set; } = "Medium";
    public decimal TotalValue { get; set; }
    public int ItemCount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string SentBy { get; set; } = string.Empty;
    public DateTime? SentDate { get; set; }
    public string ConfirmedBy { get; set; } = string.Empty;
    public DateTime? ConfirmedDate { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
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
        "Low" => "Green",
        "Medium" => "Blue",
        "High" => "Orange",
        "Urgent" => "Red",
        _ => "Blue"
    };

    public bool CanEdit => Status is "Draft";
    public bool CanSend => Status is "Draft";
    public bool CanReceive => Status is "Shipped";
    public bool CanCancel => Status is not "Delivered" and not "Cancelled";
}



