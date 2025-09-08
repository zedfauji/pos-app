namespace MagiDesk.Shared.DTOs;

using System.Text.Json.Serialization;

public class VendorDto
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public string Status { get; set; } = "active"; // active|inactive
    [JsonPropertyName("budget")] public decimal Budget { get; set; }
    [JsonPropertyName("reminder")] public string? Reminder { get; set; } // Weekday name like "Monday"
    [JsonPropertyName("reminderEnabled")] public bool ReminderEnabled { get; set; }
}
