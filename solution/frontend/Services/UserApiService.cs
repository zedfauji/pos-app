using System.Net.Http.Json;
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
            using var req = new HttpRequestMessage(HttpMethod.Get, "api/users");
            var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
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
            var res = await _http.GetAsync("api/users", ct);
            if (!res.IsSuccessStatusCode) return new();
            return await res.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GetUsers failed", ex);
            return new();
        }
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/users", req, ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
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
