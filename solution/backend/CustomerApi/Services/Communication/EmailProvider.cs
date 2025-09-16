using System.Net;
using System.Net.Mail;
using System.Text.Json;
using CustomerApi.Models;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Services.Communication;

public class EmailProvider : ICommunicationProvider
{
    private readonly ILogger<EmailProvider> _logger;
    private SmtpClient? _smtpClient;
    private string? _senderEmail;
    private string? _senderName;

    public EmailProvider(ILogger<EmailProvider> logger)
    {
        _logger = logger;
    }

    public CommunicationProvider ProviderType => CommunicationProvider.Email;
    public string ProviderName => "SMTP Email Provider";
    public bool IsConfigured => _smtpClient != null;
    public bool SupportsDeliveryTracking => false;
    public bool SupportsReadReceipts => true;

    public async Task<bool> SendMessageAsync(string recipient, string subject, string messageBody, Dictionary<string, object>? metadata = null)
    {
        if (!IsConfigured)
        {
            _logger.LogError("Email provider is not configured");
            return false;
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_senderEmail!, _senderName);
            message.To.Add(recipient);
            message.Subject = subject;
            message.Body = messageBody;
            message.IsBodyHtml = IsHtmlContent(messageBody);

            // Add tracking pixels for read receipts if supported
            if (metadata?.ContainsKey("tracking_id") == true)
            {
                var trackingId = metadata["tracking_id"].ToString();
                if (message.IsBodyHtml)
                {
                    message.Body += $"<img src=\"https://tracking.example.com/pixel/{trackingId}\" width=\"1\" height=\"1\" />";
                }
            }

            await _smtpClient!.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return false;
        }
    }

    public async Task<MessageDeliveryResult> GetDeliveryStatusAsync(string externalMessageId)
    {
        // SMTP doesn't provide delivery status by default
        // This would typically integrate with email service providers like SendGrid, Mailgun, etc.
        await Task.CompletedTask;
        
        return new MessageDeliveryResult
        {
            ExternalMessageId = externalMessageId,
            Status = MessageStatus.Sent
        };
    }

    public async Task<bool> ValidateConfigurationAsync(Dictionary<string, object> configuration)
    {
        try
        {
            var host = configuration.GetValueOrDefault("host")?.ToString();
            var port = Convert.ToInt32(configuration.GetValueOrDefault("port", 587));
            var username = configuration.GetValueOrDefault("username")?.ToString();
            var password = configuration.GetValueOrDefault("password")?.ToString();
            var enableSsl = Convert.ToBoolean(configuration.GetValueOrDefault("enable_ssl", true));

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Test connection
            using var testClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            // Try to connect (this will throw if credentials are invalid)
            await Task.Run(() => testClient.Send(new MailMessage(username, username, "Test", "Test")));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email configuration validation failed");
            return false;
        }
    }

    public void Configure(Dictionary<string, object> configuration)
    {
        try
        {
            var host = configuration.GetValueOrDefault("host")?.ToString();
            var port = Convert.ToInt32(configuration.GetValueOrDefault("port", 587));
            var username = configuration.GetValueOrDefault("username")?.ToString();
            var password = configuration.GetValueOrDefault("password")?.ToString();
            var enableSsl = Convert.ToBoolean(configuration.GetValueOrDefault("enable_ssl", true));

            _senderEmail = username;
            _senderName = configuration.GetValueOrDefault("sender_name", "MagiDesk POS")?.ToString();

            _smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                Timeout = 30000
            };

            _logger.LogInformation("Email provider configured successfully with host {Host}", host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure email provider");
            _smtpClient = null;
        }
    }

    private static bool IsHtmlContent(string content)
    {
        return content.Contains("<html>", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("<body>", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("<div>", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("<p>", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}
