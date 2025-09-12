namespace MagiDesk.Frontend.Services;

public class ExtendedVendorDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string PaymentTerms { get; set; } = "Net 30";
    public decimal CreditLimit { get; set; } = 0;
    public int OrderCount { get; set; } = 0;
    public string StatusColor { get; set; } = "Green";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
