namespace MagiDesk.Shared.DTOs;

public class OrderNotificationDto
{
    public string EventType { get; set; } = string.Empty; // order-finalized | email-sent | email-failed
    public string VendorName { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string StatusMessage { get; set; } = string.Empty;
}
