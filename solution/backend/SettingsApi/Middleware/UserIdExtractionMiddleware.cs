namespace SettingsApi.Middleware;

/// <summary>
/// Middleware to extract user ID from request headers and store in HttpContext.Items
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
        string? userId = null;

        if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
        {
            userId = userIdHeader.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                context.Items["UserId"] = userId;
                _logger.LogDebug("Extracted UserId from X-User-Id header: {UserId}", userId);
            }
        }

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

