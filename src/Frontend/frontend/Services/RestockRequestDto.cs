using System;

namespace MagiDesk.Frontend.Services;

public class RestockRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int RequestedQuantity { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? OrderedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public int Priority { get; set; } // 1=Low, 2=Medium, 3=High, 4=Urgent

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

    public string StockStatusColor => CurrentStock switch
    {
        <= 5 => "Red",
        <= 10 => "Orange",
        _ => "Green"
    };

    public bool CanApprove => Status == "Pending";
    public bool CanOrder => Status == "Approved";
    public bool CanReceive => Status == "Ordered";
    public bool CanCancel => Status is "Pending" or "Approved";
}



