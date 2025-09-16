using System.Text;
using System.Text.Json;
using CustomerApi.Models;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Services.Communication;

public class WhatsAppProvider : ICommunicationProvider
{
    private readonly ILogger<WhatsAppProvider> _logger;
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private string? _phoneNumberId;
    private string? _apiUrl;

    public WhatsAppProvider(ILogger<WhatsAppProvider> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public CommunicationProvider ProviderType => CommunicationProvider.WhatsApp;
    public string ProviderName => "WhatsApp Business API";
    public bool IsConfigured => !string.IsNullOrEmpty(_accessToken) && !string.IsNullOrEmpty(_phoneNumberId);
    public bool SupportsDeliveryTracking => true;
    public bool SupportsReadReceipts => true;

    public async Task<bool> SendMessageAsync(string recipient, string subject, string messageBody, Dictionary<string, object>? metadata = null)
    {
        if (!IsConfigured)
        {
            _logger.LogError("WhatsApp provider is not configured");
            return false;
        }

        try
        {
            var cleanPhone = CleanPhoneNumber(recipient);
            
            var payload = new
            {
                messaging_product = "whatsapp",
                to = cleanPhone,
                type = "text",
                text = new { body = messageBody }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.PostAsync($"{_apiUrl}/{_phoneNumberId}/messages", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                
                _logger.LogInformation("WhatsApp message sent successfully to {Recipient}", recipient);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send WhatsApp message to {Recipient}: {Error}", recipient, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {Recipient}", recipient);
            return false;
        }
    }

    public async Task<MessageDeliveryResult> GetDeliveryStatusAsync(string externalMessageId)
    {
        if (!IsConfigured)
        {
            return new MessageDeliveryResult
            {
                ExternalMessageId = externalMessageId,
                Status = MessageStatus.Failed,
                FailureReason = "Provider not configured"
            };
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.GetAsync($"{_apiUrl}/{externalMessageId}");

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                
                // WhatsApp Business API status mapping
                var status = result?.GetValueOrDefault("status")?.ToString()?.ToLower();
                var messageStatus = status switch
                {
                    "accepted" => MessageStatus.Pending,
                    "sent" => MessageStatus.Sent,
                    "delivered" => MessageStatus.Delivered,
                    "read" => MessageStatus.Opened,
                    "failed" => MessageStatus.Failed,
                    _ => MessageStatus.Pending
                };

                return new MessageDeliveryResult
                {
                    ExternalMessageId = externalMessageId,
                    Status = messageStatus,
                    DeliveredAt = messageStatus == MessageStatus.Delivered ? DateTime.UtcNow : null,
                    OpenedAt = messageStatus == MessageStatus.Opened ? DateTime.UtcNow : null
                };
            }
            else
            {
                return new MessageDeliveryResult
                {
                    ExternalMessageId = externalMessageId,
                    Status = MessageStatus.Failed,
                    FailureReason = "Failed to check delivery status"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get delivery status for WhatsApp message {MessageId}", externalMessageId);
            return new MessageDeliveryResult
            {
                ExternalMessageId = externalMessageId,
                Status = MessageStatus.Failed,
                FailureReason = ex.Message
            };
        }
    }

    public async Task<bool> ValidateConfigurationAsync(Dictionary<string, object> configuration)
    {
        try
        {
            var accessToken = configuration.GetValueOrDefault("access_token")?.ToString();
            var phoneNumberId = configuration.GetValueOrDefault("phone_number_id")?.ToString();
            var apiUrl = configuration.GetValueOrDefault("api_url", "https://graph.facebook.com/v18.0")?.ToString();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(phoneNumberId))
                return false;

            // Test API connection by fetching phone number info
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{apiUrl}/{phoneNumberId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp configuration validation failed");
            return false;
        }
    }

    public void Configure(Dictionary<string, object> configuration)
    {
        _accessToken = configuration.GetValueOrDefault("access_token")?.ToString();
        _phoneNumberId = configuration.GetValueOrDefault("phone_number_id")?.ToString();
        _apiUrl = configuration.GetValueOrDefault("api_url", "https://graph.facebook.com/v18.0")?.ToString();

        _logger.LogInformation("WhatsApp provider configured successfully for phone number {PhoneNumberId}", _phoneNumberId);
    }

    private static string CleanPhoneNumber(string phoneNumber)
    {
        // WhatsApp requires phone numbers in international format without + or 00
        var cleaned = new StringBuilder();
        
        foreach (var c in phoneNumber)
        {
            if (char.IsDigit(c))
            {
                cleaned.Append(c);
            }
        }

        return cleaned.ToString();
    }
}
