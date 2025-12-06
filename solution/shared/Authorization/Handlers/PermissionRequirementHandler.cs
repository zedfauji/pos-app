using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MagiDesk.Shared.Authorization.Requirements;
using MagiDesk.Shared.Authorization.Services;

namespace MagiDesk.Shared.Authorization.Handlers;

/// <summary>
/// Authorization handler that checks if user has the required permission
/// Uses IRbacService from the shared library to check permissions
/// </summary>
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRbacService _rbacService;
    private readonly ILogger<PermissionRequirementHandler> _logger;

    public PermissionRequirementHandler(
        IRbacService rbacService,
        ILogger<PermissionRequirementHandler> logger)
    {
        _rbacService = rbacService;
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
            _logger.LogWarning("Permission check failed: User ID not found in request for permission {Permission}", requirement.Permission);
            context.Fail(new AuthorizationFailureReason(this, "User ID not found in request"));
            return;
        }

        if (string.IsNullOrWhiteSpace(requirement.Permission))
        {
            _logger.LogError("Permission requirement is null or empty");
            context.Fail(new AuthorizationFailureReason(this, "Invalid permission requirement"));
            return;
        }

        try
        {
            // Use CancellationToken.None if no token is available, or create a timeout token
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
            cts.CancelAfter(TimeSpan.FromSeconds(3)); // Max 3 seconds for permission check

            var hasPermission = await _rbacService.HasPermissionAsync(
                userId,
                requirement.Permission,
                cts.Token);

            if (hasPermission)
            {
                _logger.LogDebug("User {UserId} has permission {Permission}", userId, requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access {Permission} without authorization",
                    userId,
                    requirement.Permission);
                context.Fail(new AuthorizationFailureReason(this, $"User does not have permission: {requirement.Permission}"));
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout checking permission {Permission} for user {UserId}", requirement.Permission, userId);
            context.Fail(new AuthorizationFailureReason(this, "Permission check timed out"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}: {Message}", 
                requirement.Permission, userId, ex.Message);
            context.Fail(new AuthorizationFailureReason(this, $"Error checking permission: {ex.Message}"));
        }
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

