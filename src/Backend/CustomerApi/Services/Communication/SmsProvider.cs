using System.Text;
using System.Text.Json;
using CustomerApi.Models;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Services.Communication;

public class SmsProvider : ICommunicationProvider
{
    private readonly ILogger<SmsProvider> _logger;
    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private string? _accountSid;
    private string? _fromNumber;
    private string? _apiUrl;

    public SmsProvider(ILogger<SmsProvider> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public CommunicationProvider ProviderType => CommunicationProvider.SMS;
    public string ProviderName => "Twilio SMS Provider";
    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_accountSid) && !string.IsNullOrEmpty(_fromNumber);
    public bool SupportsDeliveryTracking => true;
    public bool SupportsReadReceipts => false;

    public async Task<bool> SendMessageAsync(string recipient, string subject, string messageBody, Dictionary<string, object>? metadata = null)
    {
        if (!IsConfigured)
        {
            _logger.LogError("SMS provider is not configured");
            return false;
        }

        try
        {
            // Clean phone number (remove non-digits except +)
            var cleanPhone = CleanPhoneNumber(recipient);
            
            var payload = new
            {
                To = cleanPhone,
                From = _fromNumber,
                Body = messageBody
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authentication header
            var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_apiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

            var response = await _httpClient.PostAsync($"{_apiUrl}/2010-04-01/Accounts/{_accountSid}/Messages.json", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                
                _logger.LogInformation("SMS sent successfully to {Recipient}, SID: {MessageSid}", 
                    recipient, result?.GetValueOrDefault("sid"));
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send SMS to {Recipient}: {Error}", recipient, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", recipient);
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
            var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_apiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

            var response = await _httpClient.GetAsync($"{_apiUrl}/2010-04-01/Accounts/{_accountSid}/Messages/{externalMessageId}.json");

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                
                var status = result?.GetValueOrDefault("status")?.ToString()?.ToLower();
                var messageStatus = status switch
                {
                    "queued" or "accepted" => MessageStatus.Pending,
                    "sending" => MessageStatus.Sent,
                    "sent" => MessageStatus.Sent,
                    "delivered" => MessageStatus.Delivered,
                    "failed" or "undelivered" => MessageStatus.Failed,
                    _ => MessageStatus.Pending
                };

                return new MessageDeliveryResult
                {
                    ExternalMessageId = externalMessageId,
                    Status = messageStatus,
                    DeliveredAt = messageStatus == MessageStatus.Delivered ? DateTime.UtcNow : null,
                    FailureReason = messageStatus == MessageStatus.Failed ? result?.GetValueOrDefault("error_message")?.ToString() : null
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
            _logger.LogError(ex, "Failed to get delivery status for message {MessageId}", externalMessageId);
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
            var apiKey = configuration.GetValueOrDefault("api_key")?.ToString();
            var accountSid = configuration.GetValueOrDefault("account_sid")?.ToString();
            var fromNumber = configuration.GetValueOrDefault("from_number")?.ToString();
            var apiUrl = configuration.GetValueOrDefault("api_url", "https://api.twilio.com")?.ToString();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(fromNumber))
                return false;

            // Test API connection by fetching account info
            var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{apiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

            var response = await _httpClient.GetAsync($"{apiUrl}/2010-04-01/Accounts/{accountSid}.json");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMS configuration validation failed");
            return false;
        }
    }

    public void Configure(Dictionary<string, object> configuration)
    {
        _apiKey = configuration.GetValueOrDefault("api_key")?.ToString();
        _accountSid = configuration.GetValueOrDefault("account_sid")?.ToString();
        _fromNumber = configuration.GetValueOrDefault("from_number")?.ToString();
        _apiUrl = configuration.GetValueOrDefault("api_url", "https://api.twilio.com")?.ToString();

        _logger.LogInformation("SMS provider configured successfully for account {AccountSid}", _accountSid);
    }

    private static string CleanPhoneNumber(string phoneNumber)
    {
        // Remove all non-digits except + at the beginning
        var cleaned = new StringBuilder();
        var isFirst = true;
        
        foreach (var c in phoneNumber)
        {
            if (char.IsDigit(c))
            {
                cleaned.Append(c);
                isFirst = false;
            }
            else if (c == '+' && isFirst)
            {
                cleaned.Append(c);
                isFirst = false;
            }
        }

        var result = cleaned.ToString();
        
        // Add + if not present and number looks international
        if (!result.StartsWith("+") && result.Length > 10)
        {
            result = "+" + result;
        }
        
        return result;
    }
}
