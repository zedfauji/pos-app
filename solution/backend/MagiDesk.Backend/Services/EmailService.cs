using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MagiDesk.Backend.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string html, CancellationToken ct = default);
}

public class EmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly ILogger<EmailService> _logger;

    public EmailService(HttpClient http, ILogger<EmailService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string html, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("RESEND_API_KEY not configured; skipping email send.");
            return false;
        }
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var payload = new
        {
            from = "onboarding@resend.dev",
            to = new[] { to },
            subject,
            html
        };
        var json = JsonSerializer.Serialize(payload);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Resend failed ({Status}): {Body}", res.StatusCode, body);
            return false;
        }
        return true;
    }
}
