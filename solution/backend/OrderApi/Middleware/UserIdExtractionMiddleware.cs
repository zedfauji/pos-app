namespace OrderApi.Middleware;

/// <summary>
/// Middleware to extract user ID from request headers and store in HttpContext.Items
/// This allows authorization handlers to access the user ID without authentication infrastructure
/// </summary>
public class UserIdExtractionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserIdExtractionMiddleware> _logger;

    public UserIdExtractionMiddleware(RequestDelegate next, ILogger<UserIdExtractionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract user ID from various sources (in order of preference)
        string? userId = null;

        // Option 1: From X-User-Id header (primary method for v2 APIs)
        if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
        {
            userId = userIdHeader.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                context.Items["UserId"] = userId;
                _logger.LogDebug("Extracted UserId from X-User-Id header: {UserId}", userId);
            }
        }

        // Option 2: From Authorization header (if JWT is implemented in future)
        // This would parse JWT and extract user ID from claims
        // For now, skipped

        // Option 3: From query string (temporary, for testing - remove in production)
        if (string.IsNullOrWhiteSpace(userId) && context.Request.Query.TryGetValue("userId", out var userIdQuery))
        {
            userId = userIdQuery.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                context.Items["UserId"] = userId;
                _logger.LogDebug("Extracted UserId from query string: {UserId}", userId);
            }
        }

        await _next(context);
    }
}

