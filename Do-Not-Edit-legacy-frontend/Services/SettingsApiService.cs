using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Service for interacting with the Settings API
/// </summary>
public sealed class SettingsApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SettingsApiService> _logger;

    public SettingsApiService(HttpClient httpClient, ILogger<SettingsApiService>? logger)
    {
        _httpClient = httpClient;
        _logger = logger ?? new NullLogger<SettingsApiService>();
    }

    // Legacy constructor for backward compatibility
    public SettingsApiService(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _logger = new NullLogger<SettingsApiService>();
    }

    /// <summary>
    /// Get application settings including receipt settings
    /// </summary>
    public async Task<AppSettings?> GetAppSettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/settings");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var settings = JsonSerializer.Deserialize<AppSettings>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return settings;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get app settings");
            return null;
        }
    }

    /// <summary>
    /// Save application settings
    /// </summary>
    public async Task<bool> SaveAppSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/settings", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save app settings");
            return false;
        }
    }

    // Legacy methods for backward compatibility with existing SettingsPage
    public async Task<AppSettings?> GetAppAsync(string? host = null)
    {
        return await GetAppSettingsAsync();
    }

    public async Task<bool> SaveAppAsync(AppSettings settings, string? host = null)
    {
        return await SaveAppSettingsAsync(settings);
    }

    public async Task<Dictionary<string, object>?> GetFrontendAsync(string? host = null)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/settings/frontend");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get frontend settings");
            return null;
        }
    }

    public async Task<bool> SaveFrontendAsync(Dictionary<string, object> settings, string? host = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/settings/frontend", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save frontend settings");
            return false;
        }
    }

    public async Task<Dictionary<string, object>?> GetBackendAsync(string? host = null)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/settings/backend");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backend settings");
            return null;
        }
    }

    public async Task<bool> SaveBackendAsync(Dictionary<string, object> settings, string? host = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/settings/backend", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save backend settings");
            return false;
        }
    }

    public async Task<List<Dictionary<string, object>>?> GetAuditAsync(string host, int limit = 50)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/settings/audit?host={host}&limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log");
            return null;
        }
    }

    public async Task<Dictionary<string, object>?> GetFrontendDefaultsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/settings/frontend/defaults");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get frontend defaults");
            return null;
        }
    }

    public async Task<Dictionary<string, object>?> GetBackendDefaultsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/settings/backend/defaults");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backend defaults");
            return null;
        }
    }

    public async Task<AppSettings?> GetAppDefaultsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/settings/defaults");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AppSettings>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get app defaults");
            return null;
        }
    }

    public class AppSettings
    {
        public string? Locale { get; set; }
        public bool? EnableNotifications { get; set; }
        public ReceiptSettings? ReceiptSettings { get; set; }
        public Dictionary<string, object?>? Extras { get; set; }
    }

    public class ReceiptSettings
    {
        public string? DefaultPrinter { get; set; }
        public string? ReceiptSize { get; set; } = "80mm";
        public bool? AutoPrintOnPayment { get; set; } = true;
        public bool? PreviewBeforePrint { get; set; } = true;
        public bool? PrintProForma { get; set; } = true;
        public bool? PrintFinalReceipt { get; set; } = true;
        public int? CopiesForFinalReceipt { get; set; } = 2;
        public string? BusinessName { get; set; } = "MagiDesk Billiard Club";
        public string? BusinessAddress { get; set; } = "123 Main Street, City, State 12345";
        public string? BusinessPhone { get; set; } = "(555) 123-4567";
    }

    // Legacy type aliases for backward compatibility
    public class FrontendSettings : Dictionary<string, object> 
    {
        public string? ApiBaseUrl 
        { 
            get => TryGetValue("ApiBaseUrl", out var value) ? value?.ToString() : null;
            set => this["ApiBaseUrl"] = value ?? "";
        }
        
        public string? Theme 
        { 
            get => TryGetValue("Theme", out var value) ? value?.ToString() : null;
            set => this["Theme"] = value ?? "";
        }
        
        public decimal? RatePerMinute 
        { 
            get => TryGetValue("RatePerMinute", out var value) && decimal.TryParse(value?.ToString(), out var rate) ? rate : null;
            set => this["RatePerMinute"] = value ?? 0m;
        }
    }
    
    public class BackendSettings : Dictionary<string, object> 
    {
        public string? BackendApiUrl 
        { 
            get => TryGetValue("BackendApiUrl", out var value) ? value?.ToString() : null;
            set => this["BackendApiUrl"] = value ?? "";
        }
        
        public string? SettingsApiUrl 
        { 
            get => TryGetValue("SettingsApiUrl", out var value) ? value?.ToString() : null;
            set => this["SettingsApiUrl"] = value ?? "";
        }
        
        public string? TablesApiUrl 
        { 
            get => TryGetValue("TablesApiUrl", out var value) ? value?.ToString() : null;
            set => this["TablesApiUrl"] = value ?? "";
        }
        
        public string? InventoryApiUrl 
        { 
            get => TryGetValue("InventoryApiUrl", out var value) ? value?.ToString() : null;
            set => this["InventoryApiUrl"] = value ?? "";
        }
    }
}