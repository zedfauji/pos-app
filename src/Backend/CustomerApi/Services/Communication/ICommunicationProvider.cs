using CustomerApi.Models;

namespace CustomerApi.Services.Communication;

public interface ICommunicationProvider
{
    CommunicationProvider ProviderType { get; }
    string ProviderName { get; }
    bool IsConfigured { get; }
    
    Task<bool> SendMessageAsync(string recipient, string subject, string messageBody, Dictionary<string, object>? metadata = null);
    Task<MessageDeliveryResult> GetDeliveryStatusAsync(string externalMessageId);
    Task<bool> ValidateConfigurationAsync(Dictionary<string, object> configuration);
    bool SupportsDeliveryTracking { get; }
    bool SupportsReadReceipts { get; }
}

public class MessageDeliveryResult
{
    public string ExternalMessageId { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public string? FailureReason { get; set; }
}
