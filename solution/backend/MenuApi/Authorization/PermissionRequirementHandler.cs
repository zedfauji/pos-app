using Microsoft.AspNetCore.Authorization;

namespace MenuApi.Authorization;

/// <summary>
/// Authorization handler that checks if user has the required permission
/// 
/// TODO: This handler needs IRbacService to check permissions.
/// Options:
/// 1. Reference UsersApi project and use UsersApi.Services.IRbacService
/// 2. Create HTTP client to call UsersApi /api/v2/rbac/users/{userId}/permissions/check
/// 3. Create shared authorization service library
/// 
/// For now, this is a placeholder that will fail all permission checks until configured.
/// </summary>
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionRequirementHandler> _logger;

    public PermissionRequirementHandler(ILogger<PermissionRequirementHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Extract user ID from claims or request headers
        var userId = ExtractUserId(context);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Permission check failed: User ID not found in request");
            context.Fail();
            return;
        }

        // TODO: Implement permission check via IRbacService or HTTP call to UsersApi
        // For now, fail all checks until properly configured
        _logger.LogWarning(
            "Permission check not implemented: User {UserId} attempted to access {Permission}. " +
            "Please configure IRbacService in Program.cs",
            userId,
            requirement.Permission);
        context.Fail();
    }

    private string? ExtractUserId(AuthorizationHandlerContext context)
    {
        // Option 1: From claims (if using JWT/authentication)
        var userIdClaim = context.User?.FindFirst("UserId")?.Value
                       ?? context.User?.FindFirst("sub")?.Value
                       ?? context.User?.FindFirst("nameid")?.Value;

        if (!string.IsNullOrWhiteSpace(userIdClaim))
        {
            return userIdClaim;
        }

        // Option 2: From HttpContext items (set by middleware)
        if (context.Resource is HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is string userId)
            {
                return userId;
            }

            // Option 3: From request headers (for now, until proper authentication is implemented)
            if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            {
                return userIdHeader.ToString();
            }

            // Option 4: From query string (temporary, for testing)
            if (httpContext.Request.Query.TryGetValue("userId", out var userIdQuery))
            {
                return userIdQuery.ToString();
            }
        }

        return null;
    }
}

