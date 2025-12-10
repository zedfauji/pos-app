namespace MagiDesk.Shared.DTOs;

public class OrdersJobDto
{
    public string JobId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // draft|submitted
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
