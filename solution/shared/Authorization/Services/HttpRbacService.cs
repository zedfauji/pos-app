using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MagiDesk.Shared.DTOs.Users;

namespace MagiDesk.Shared.Authorization.Services;

/// <summary>
/// HTTP-based implementation of IRbacService that calls UsersApi for permission checks
/// This allows other APIs to check permissions without directly referencing UsersApi
/// </summary>
public class HttpRbacService : IRbacService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpRbacService> _logger;
    private readonly string _usersApiBaseUrl;

    public HttpRbacService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HttpRbacService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Get UsersApi base URL from configuration
        _usersApiBaseUrl = configuration["UsersApi:BaseUrl"] 
            ?? configuration["Api:UsersApi:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("USERSAPI_BASEURL")
            ?? throw new InvalidOperationException("UsersApi:BaseUrl not configured");
        
        _httpClient.BaseAddress = new Uri(_usersApiBaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(3); // Reduced timeout for faster failure
    }

    #region Permission Management (Core - Required by authorization handlers)

    public async Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetUserPermissionsAsync called with null or empty userId");
            return Array.Empty<string>();
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2)); // Max 2 seconds for permission check

            var response = await _httpClient.GetAsync($"api/v2/rbac/users/{Uri.EscapeDataString(userId)}/permissions", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var permissions = await response.Content.ReadFromJsonAsync<string[]>(cts.Token);
                return permissions ?? Array.Empty<string>();
            }
            
            _logger.LogWarning("Failed to get permissions for user {UserId}: {StatusCode}", userId, response.StatusCode);
            return Array.Empty<string>();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout getting permissions for user {UserId}", userId);
            return Array.Empty<string>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error getting permissions for user {UserId}: {Message}", userId, ex.Message);
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting permissions for user {UserId}: {Message}", userId, ex.Message);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("HasPermissionAsync called with null or empty userId or permission");
            return false;
        }

        try
        {
            // First, try to get all user permissions and check locally
            var permissions = await GetUserPermissionsAsync(userId, ct);
            
            if (permissions == null || permissions.Length == 0)
            {
                _logger.LogDebug("User {UserId} has no permissions", userId);
                return false;
            }

            var hasPermission = permissions.Contains(permission);
            
            if (!hasPermission)
            {
                _logger.LogDebug(
                    "User {UserId} does not have permission {Permission}. User has {Count} permissions.",
                    userId,
                    permission,
                    permissions.Length);
            }
            
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}: {Error}", permission, userId, ex.Message);
            // Fail closed - deny access if we can't verify permission
            return false;
        }
    }

    public async Task<bool> HasAnyPermissionAsync(string userId, string[] permissions, CancellationToken ct = default)
    {
        try
        {
            var request = new { Permissions = permissions };
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v2/rbac/users/{userId}/permissions/check-any",
                request,
                ct);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                if (result.TryGetProperty("hasPermission", out var hasPermission))
                {
                    return hasPermission.GetBoolean();
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasAllPermissionsAsync(string userId, string[] permissions, CancellationToken ct = default)
    {
        try
        {
            var request = new { Permissions = permissions };
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v2/rbac/users/{userId}/permissions/check-all",
                request,
                ct);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                if (result.TryGetProperty("hasPermission", out var hasPermission))
                {
                    return hasPermission.GetBoolean();
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permissions for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region Role Management (Not typically needed by authorization handlers)

    public Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<RoleDto?> GetRoleByIdAsync(string roleId, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<RoleDto?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<PagedResult<RoleDto>> GetRolesAsync(RoleSearchRequest request, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<RoleStatsDto> GetRoleStatsAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<string[]> GetRolePermissionsAsync(string roleId, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<string[]> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<bool> CanInheritFromRoleAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<bool> ValidateRoleCompositionAsync(CreateRoleRequest request, CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task InitializeSystemRolesAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<RoleDto[]> GetSystemRolesAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    public Task<RoleDto[]> GetCustomRolesAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Role management not supported via HTTP. Use UsersApi directly.");

    #endregion
}

