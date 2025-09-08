namespace MagiDesk.Shared.DTOs;

public class OrderItemDto
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal => Price * Quantity;
}
