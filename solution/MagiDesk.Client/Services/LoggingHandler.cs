using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace MagiDesk.Client.Services;

public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var requestUri = request.RequestUri?.ToString() ?? "null";
        var method = request.Method;

        Log.Information("HTTP REQ [{CorrelationId}] {Method} {Url}", correlationId, method, requestUri);

        // Optional: Log request body (skip for sensitive endpoints like login)
        if (request.Content != null && !requestUri.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                var logBody = requestBody.Length > 1000 ? requestBody[..1000] + "... [TRUNCATED]" : requestBody;
                Log.Information("HTTP REQ BODY [{CorrelationId}] {Body}", correlationId, logBody);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "HTTP REQ BODY [{CorrelationId}] Failed to read", correlationId);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        Log.Information("HTTP RES [{CorrelationId}] {StatusCode}", correlationId, response.StatusCode);

        if (response.Content != null)
        {
            try
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                // Truncate if too long (e.g. 4000 chars) to prevent log bloat but keep enough context
                var logBody = responseBody.Length > 4000 ? responseBody[..4000] + "... [TRUNCATED]" : responseBody;
                
                // Do not log large binary responses if any (though usually APIs return JSON)
                if ((response.Content.Headers.ContentType?.MediaType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase) ||
                    (response.Content.Headers.ContentType?.MediaType ?? "").Contains("text", StringComparison.OrdinalIgnoreCase))
                {
                     Log.Information("HTTP RES BODY [{CorrelationId}] {Body}", correlationId, logBody);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HTTP RES BODY [{CorrelationId}] Failed to read", correlationId);
            }
        }

        return response;
    }
}
