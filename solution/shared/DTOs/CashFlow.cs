namespace MagiDesk.Shared.DTOs;

public class CashFlow
{
    public string? Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal CashAmount { get; set; }
    public string? Notes { get; set; }
}
