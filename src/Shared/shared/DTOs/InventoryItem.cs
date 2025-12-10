namespace MagiDesk.Shared.DTOs;

public class InventoryItem
{
    public string? Id { get; set; }
    public string? clave { get; set; }
    public string? productname { get; set; }
    public double entradas { get; set; }
    public double salidas { get; set; }
    public double saldo { get; set; }
    public DateTime? migrated_at { get; set; }
}
