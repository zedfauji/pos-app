using System.ComponentModel.DataAnnotations;

namespace MagiDesk.Shared.DTOs;

public class CashFlow
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Employee name is required")]
    public string EmployeeName { get; set; } = string.Empty;
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    [Required(ErrorMessage = "Cash amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Cash amount must be greater than zero")]
    public decimal CashAmount { get; set; }
    
    public string? Notes { get; set; }
}
