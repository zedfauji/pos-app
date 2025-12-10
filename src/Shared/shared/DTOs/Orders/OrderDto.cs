namespace MagiDesk.Shared.DTOs;

public class OrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "submitted"; // draft|submitted
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
