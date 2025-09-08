namespace MagiDesk.Shared.DTOs;

public class CartDraftDto
{
    public string CartId { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "draft"; // draft|submitted
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
