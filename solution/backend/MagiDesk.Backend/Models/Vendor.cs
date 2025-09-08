namespace MagiDesk.Backend.Models;

public class Vendor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public string Status { get; set; } = "active"; // active|inactive
}
