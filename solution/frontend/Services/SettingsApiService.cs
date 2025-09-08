using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public class SettingsApiService
{
    private readonly HttpClient _http;

    public SettingsApiService(string baseUrl)
    {
        var inner = new HttpClientHandler();
        inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var logging = new HttpLoggingHandler(inner);
        _http = new HttpClient(logging) { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(20) };
    }

    public class FrontendSettings
    {
        public string? ApiBaseUrl { get; set; }
        public string? Theme { get; set; }
        public decimal? RatePerMinute { get; set; }
    }

    public class AppSettings
    {
        public string? Locale { get; set; }
        public bool? EnableNotifications { get; set; }
        public Dictionary<string, object?>? Extras { get; set; }
    }

    public class BackendSettings
    {
        // Service URLs used by the frontend (persisted server-side to keep clients in sync)
        public string? SettingsApiUrl { get; set; }
        public string? TablesApiUrl { get; set; }
        public string? BackendApiUrl { get; set; }
    }

    public async Task<FrontendSettings?> GetFrontendAsync(string? host = null, CancellationToken ct = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(host) ? "api/settings/frontend" : $"api/settings/frontend?host={Uri.EscapeDataString(host)}";
            return await _http.GetFromJsonAsync<FrontendSettings>(uri, ct);
        }
        catch (Exception ex)
        {
            Log.Error("GET settings/frontend failed", ex);
            return null;
        }
    }

    public async Task<bool> SaveFrontendAsync(FrontendSettings settings, string? host = null, CancellationToken ct = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(host) ? "api/settings/frontend" : $"api/settings/frontend?host={Uri.EscapeDataString(host)}";
            var res = await _http.PutAsJsonAsync(uri, settings, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"PUT settings/frontend HTTP {(int)res.StatusCode}: {msg}");
            }
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("PUT settings/frontend failed", ex);
            return false;
        }
    }

    public async Task<AppSettings?> GetAppAsync(string? host = null, CancellationToken ct = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(host) ? "api/settings/app" : $"api/settings/app?host={Uri.EscapeDataString(host)}";
            var res = await _http.GetAsync(uri, ct);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<AppSettings>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("GET settings/app failed", ex);
            return null;
        }
    }

    public async Task<bool> SaveAppAsync(AppSettings settings, string? host = null, CancellationToken ct = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(host) ? "api/settings/app" : $"api/settings/app?host={Uri.EscapeDataString(host)}";
            var res = await _http.PutAsJsonAsync(uri, settings, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"PUT settings/app HTTP {(int)res.StatusCode}: {msg}");
            }
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("PUT settings/app failed", ex);
            return false;
        }
    }

    public async Task<BackendSettings?> GetBackendAsync(string? host = null, CancellationToken ct = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(host) ? "api/settings/backend" : $"api/settings/backend?host={Uri.EscapeDataString(host)}";
            var res = await _http.GetAsync(uri, ct);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<BackendSettings>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("GET settings/backend failed", ex);
            return null;
        }
    }

    public async Task<bool> SaveBackendAsync(BackendSettings settings, string? host = null, CancellationToken ct = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(host) ? "api/settings/backend" : $"api/settings/backend?host={Uri.EscapeDataString(host)}";
            var res = await _http.PutAsJsonAsync(uri, settings, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"PUT settings/backend HTTP {(int)res.StatusCode}: {msg}");
            }
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("PUT settings/backend failed", ex);
            return false;
        }
    }

    // Defaults accessors
    public async Task<FrontendSettings?> GetFrontendDefaultsAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FrontendSettings>("api/settings/frontend/defaults", ct); }
        catch (Exception ex) { Log.Error("GET settings/frontend/defaults failed", ex); return null; }
    }

    public async Task<BackendSettings?> GetBackendDefaultsAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<BackendSettings>("api/settings/backend/defaults", ct); }
        catch (Exception ex) { Log.Error("GET settings/backend/defaults failed", ex); return null; }
    }

    public async Task<AppSettings?> GetAppDefaultsAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<AppSettings>("api/settings/app/defaults", ct); }
        catch (Exception ex) { Log.Error("GET settings/app/defaults failed", ex); return null; }
    }
}
