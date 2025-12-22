using System.Net.Http.Json;
using System.Text.Json;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.DTOs.Auth;

namespace MagiDesk.Frontend.Services;

public class UserApiService
{
    private readonly HttpClient _http;
    public UserApiService(HttpClient http) { 
        _http = http; 
        Log.Info($"UserApiService initialized with base address: {_http.BaseAddress}");
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/users/ping", ct);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("Backend ping failed", ex);
            return false;
        }
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", req, ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("Login request failed", ex);
            return null;
        }
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            var request = new UserSearchRequest { PageSize = 1000 }; // Get all users for backward compatibility
            var result = await GetUsersPagedAsync(request, ct);
            return result.Items.ToList();
        }
        catch (Exception ex)
        {
            Log.Error("GetUsers failed", ex);
            return new();
        }
    }

    public async Task<PagedResult<UserDto>> GetUsersPagedAsync(UserSearchRequest request, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
            if (!string.IsNullOrWhiteSpace(request.Role))
                queryParams.Add($"role={Uri.EscapeDataString(request.Role)}");
            if (request.IsActive.HasValue)
                queryParams.Add($"isActive={request.IsActive.Value}");
            queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
            queryParams.Add($"sortDescending={request.SortDescending}");
            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var res = await _http.GetAsync($"api/users{query}", ct);
            
            if (!res.IsSuccessStatusCode) 
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"GetUsersPagedAsync failed with status {res.StatusCode}: {errorContent}");
                return new PagedResult<UserDto> { Items = Array.Empty<UserDto>() };
            }
                
            var jsonContent = await res.Content.ReadAsStringAsync(ct);
            Log.Info($"Received JSON response: {jsonContent}");
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = await res.Content.ReadFromJsonAsync<PagedResult<UserDto>>(options, cancellationToken: ct);
            return result ?? new PagedResult<UserDto> { Items = Array.Empty<UserDto>() };
        }
        catch (Exception ex)
        {
            Log.Error("GetUsersPagedAsync failed", ex);
            return new PagedResult<UserDto> { Items = Array.Empty<UserDto>() };
        }
    }

    public async Task<UserStatsDto?> GetUserStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/users/stats", ct);
            if (!res.IsSuccessStatusCode) return null;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return await res.Content.ReadFromJsonAsync<UserStatsDto>(options, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("GetUserStats failed", ex);
            return null;
        }
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/users", req, ct);
            if (!res.IsSuccessStatusCode) return null;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return await res.Content.ReadFromJsonAsync<UserDto>(options, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("CreateUser failed", ex);
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUserRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PutAsJsonAsync($"api/users/{userId}", req, ct);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("UpdateUser failed", ex);
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.DeleteAsync($"api/users/{userId}", ct);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("DeleteUser failed", ex);
            return false;
        }
    }

    #region RBAC Methods

    public async Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/rbac/roles", ct);
            if (!res.IsSuccessStatusCode) return new List<RoleDto>();
            var pagedResult = await res.Content.ReadFromJsonAsync<PagedResult<RoleDto>>(cancellationToken: ct);
            return pagedResult?.Items?.ToList() ?? new List<RoleDto>();
        }
        catch (Exception ex)
        {
            Log.Error("GetRoles failed", ex);
            return new List<RoleDto>();
        }
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/rbac/permissions", ct);
            if (!res.IsSuccessStatusCode) return new List<PermissionDto>();
            return await res.Content.ReadFromJsonAsync<List<PermissionDto>>(cancellationToken: ct) ?? new List<PermissionDto>();
        }
        catch (Exception ex)
        {
            Log.Error("GetAllPermissions failed", ex);
            return new List<PermissionDto>();
        }
    }

    public async Task<List<string>> GetRolePermissionsAsync(string roleId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/rbac/roles/{roleId}/permissions", ct);
            if (!res.IsSuccessStatusCode) return new List<string>();
            var permissions = await res.Content.ReadFromJsonAsync<string[]>(cancellationToken: ct);
            return permissions?.ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            Log.Error("GetRolePermissions failed", ex);
            return new List<string>();
        }
    }

    public async Task<bool> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/rbac/roles", request, ct);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("CreateRole failed", ex);
            return false;
        }
    }

    public async Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            // Log the update request for debugging
            System.Diagnostics.Debug.WriteLine($"Updating role {roleId} with request: {System.Text.Json.JsonSerializer.Serialize(request)}");
            var res = await _http.PutAsJsonAsync($"api/rbac/roles/{roleId}", request, ct);
            
            if (!res.IsSuccessStatusCode)
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"UpdateRole failed with status {res.StatusCode}: {errorContent}");
            }
            
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error($"UpdateRole failed for role {roleId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.DeleteAsync($"api/rbac/roles/{roleId}", ct);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("DeleteRole failed", ex);
            return false;
        }
    }

    public async Task<bool> UpdateRolePermissionsAsync(string roleId, List<string> permissions, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PutAsJsonAsync($"api/rbac/roles/{roleId}/permissions", permissions, ct);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("UpdateRolePermissions failed", ex);
            return false;
        }
    }

    #endregion
}
