namespace MagiDesk.Backend.Models;

public class Item
{
    public string Id { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
