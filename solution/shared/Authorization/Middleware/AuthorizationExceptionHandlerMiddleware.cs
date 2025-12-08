using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MagiDesk.Shared.Authorization.Middleware;

/// <summary>
/// Middleware to handle authorization exceptions and return proper HTTP status codes
/// This middleware runs after authorization and catches any exceptions, converting them to proper HTTP status codes
/// </summary>
public class AuthorizationExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationExceptionHandlerMiddleware> _logger;

    public AuthorizationExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<AuthorizationExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            
            // If response is 401 or authorization failed, convert to 403
            if (context.Response.StatusCode == 401 && !context.Response.HasStarted)
            {
                _logger.LogWarning("Unauthorized access attempt to {Path}", context.Request.Path);
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new 
                { 
                    error = "Forbidden", 
                    message = "Insufficient permissions",
                    path = context.Request.Path.Value
                });
                await context.Response.WriteAsync(json);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            if (!context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to {Path}", context.Request.Path);
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new 
                { 
                    error = "Forbidden", 
                    message = "Insufficient permissions",
                    path = context.Request.Path.Value
                });
                await context.Response.WriteAsync(json);
            }
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled exception in authorization pipeline for {Path}: {Message}", 
                    context.Request.Path, ex.Message);
                
                // Check if this is an authorization-related error
                if (ex.Message.Contains("authorization") || ex.Message.Contains("permission") || 
                    ex.Message.Contains("Forbidden") || ex.Message.Contains("Unauthorized"))
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    var json = JsonSerializer.Serialize(new 
                    { 
                        error = "Forbidden", 
                        message = "Insufficient permissions",
                        path = context.Request.Path.Value
                    });
                    await context.Response.WriteAsync(json);
                }
                else
                {
                    // Don't expose internal errors - return generic error
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    var json = JsonSerializer.Serialize(new { error = "Internal server error" });
                    await context.Response.WriteAsync(json);
                }
            }
        }
    }
}

