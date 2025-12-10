namespace MagiDesk.Shared.DTOs;

public class ExtendedVendorDto : VendorDto
{
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty; // Net 30, COD, etc.
    public decimal CreditLimit { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int AverageDeliveryTimeDays { get; set; }

    // Computed property for UI
    public string StatusColor => Status switch
    {
        "active" => "Green",
        "inactive" => "Red",
        "suspended" => "Orange",
        _ => "Gray"
    };
}



