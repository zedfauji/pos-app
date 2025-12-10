namespace MagiDesk.Shared.DTOs;

public class ItemDto
{
    public string? Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
