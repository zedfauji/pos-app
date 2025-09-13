using System.Net.Http.Json;
using System.Text.Json;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.DTOs.Auth;

namespace MagiDesk.Frontend.Services;

public class UserApiService
{
    private readonly HttpClient _http;
    public UserApiService(HttpClient http) { _http = http; }

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
                return new PagedResult<UserDto> { Items = Array.Empty<UserDto>() };
                
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return await res.Content.ReadFromJsonAsync<PagedResult<UserDto>>(options, cancellationToken: ct) 
                   ?? new PagedResult<UserDto> { Items = Array.Empty<UserDto>() };
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
}
