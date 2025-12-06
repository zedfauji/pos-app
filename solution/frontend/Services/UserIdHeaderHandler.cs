using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// HTTP handler that automatically adds X-User-Id header to all API requests
/// </summary>
public class UserIdHeaderHandler : DelegatingHandler
{
    public UserIdHeaderHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get current user ID from session
        var session = SessionService.Current;
        if (session != null && !string.IsNullOrWhiteSpace(session.UserId))
        {
            // Only add header if not already present
            if (!request.Headers.Contains("X-User-Id"))
            {
                request.Headers.Add("X-User-Id", session.UserId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

