namespace MagiDesk.Frontend.Models;

public class InventoryDisplay
{
    public string? Id { get; set; }
    public string? clave { get; set; }
    public string? productname { get; set; }
    public string entradas { get; set; } = string.Empty;
    public string salidas { get; set; } = string.Empty;
    public string saldo { get; set; } = string.Empty;
    public string migrated_at { get; set; } = string.Empty;
}
