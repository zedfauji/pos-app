using System.Text.Json;
using System.Net.Http.Json;
using MagiDesk.Shared.DTOs.Settings;
using Microsoft.Extensions.Logging;
using SharedInventorySettings = MagiDesk.Shared.DTOs.Settings.InventorySettings;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// API service for hierarchical settings with comprehensive error handling and offline support
/// </summary>
public sealed class HierarchicalSettingsApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HierarchicalSettingsApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HierarchicalSettingsApiService()
    {
        var baseUrl = GetSettingsApiBaseUrl();
        
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _logger = NullLoggerFactory.Create<HierarchicalSettingsApiService>();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public HierarchicalSettingsApiService(HttpClient httpClient, ILogger<HierarchicalSettingsApiService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLoggerFactory.Create<HierarchicalSettingsApiService>();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    #region Main Settings Operations

    /// <summary>
    /// Get all hierarchical settings for a host
    /// </summary>
    public async Task<HierarchicalSettings> GetSettingsAsync(string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var url = "api/v2/settings";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"?host={Uri.EscapeDataString(hostKey)}";
            }

            var response = await _httpClient.GetAsync(url, ct);
            
            if (response.IsSuccessStatusCode)
            {
                var settings = await response.Content.ReadFromJsonAsync<HierarchicalSettings>(_jsonOptions, ct);
                return settings ?? GetDefaultSettings();
            }
            else
            {
                _logger.LogWarning("Failed to get settings. Status: {StatusCode}", response.StatusCode);
                return GetDefaultSettings();
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("No connection"))
        {
            _logger.LogInformation("Settings API is offline, using default settings");
            return GetDefaultSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hierarchical settings");
            return GetDefaultSettings();
        }
    }

    /// <summary>
    /// Get settings for a specific category
    /// </summary>
    public async Task<T?> GetSettingsCategoryAsync<T>(string category, string? hostKey = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var url = $"api/v2/settings/{Uri.EscapeDataString(category)}";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"?host={Uri.EscapeDataString(hostKey)}";
            }

            var response = await _httpClient.GetAsync(url, ct);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
            }
            else
            {
                _logger.LogWarning("Failed to get settings category {Category}. Status: {StatusCode}", category, response.StatusCode);
                return GetDefaultCategorySettings<T>(category);
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("No connection"))
        {
            _logger.LogInformation("Settings API is offline, using default settings for category {Category}", category);
            return GetDefaultCategorySettings<T>(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings category {Category}", category);
            return GetDefaultCategorySettings<T>(category);
        }
    }

    /// <summary>
    /// Save all hierarchical settings
    /// </summary>
    public async Task<bool> SaveSettingsAsync(HierarchicalSettings settings, string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var url = "api/v2/settings";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"?host={Uri.EscapeDataString(hostKey)}";
            }

            var response = await _httpClient.PutAsJsonAsync(url, settings, _jsonOptions, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Settings saved successfully");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to save settings. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("No connection"))
        {
            _logger.LogWarning("Settings API is offline, cannot save settings");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving hierarchical settings");
            return false;
        }
    }

    /// <summary>
    /// Save settings for a specific category
    /// </summary>
    public async Task<bool> SaveSettingsCategoryAsync<T>(string category, T settings, string? hostKey = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var url = $"api/v2/settings/{Uri.EscapeDataString(category)}";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"?host={Uri.EscapeDataString(hostKey)}";
            }

            var response = await _httpClient.PutAsJsonAsync(url, settings, _jsonOptions, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Settings category {Category} saved successfully", category);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to save settings category {Category}. Status: {StatusCode}", category, response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("No connection"))
        {
            _logger.LogWarning("Settings API is offline, cannot save category {Category}", category);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings category {Category}", category);
            return false;
        }
    }

    #endregion

    #region Utility Operations

    /// <summary>
    /// Get settings metadata for UI generation
    /// </summary>
    public async Task<List<SettingMetadata>> GetSettingsMetadataAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v2/settings/metadata", ct);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<SettingMetadata>>(_jsonOptions, ct) ?? new List<SettingMetadata>();
            }
            else
            {
                _logger.LogWarning("Failed to get settings metadata. Status: {StatusCode}", response.StatusCode);
                return GetDefaultMetadata();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings metadata");
            return GetDefaultMetadata();
        }
    }

    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    public async Task<bool> ResetToDefaultsAsync(string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var url = "api/v2/settings/reset";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"?host={Uri.EscapeDataString(hostKey)}";
            }

            var response = await _httpClient.PostAsync(url, null, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Settings reset to defaults successfully");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to reset settings. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings to defaults");
            return false;
        }
    }

    /// <summary>
    /// Get settings audit log
    /// </summary>
    public async Task<List<SettingsAuditEntry>> GetAuditLogAsync(string? hostKey = null, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/v2/settings/audit?limit={limit}";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"&host={Uri.EscapeDataString(hostKey)}";
            }

            var response = await _httpClient.GetAsync(url, ct);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<SettingsAuditEntry>>(_jsonOptions, ct) ?? new List<SettingsAuditEntry>();
            }
            else
            {
                _logger.LogWarning("Failed to get audit log. Status: {StatusCode}", response.StatusCode);
                return new List<SettingsAuditEntry>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings audit log");
            return new List<SettingsAuditEntry>();
        }
    }

    /// <summary>
    /// Test connections for various settings
    /// </summary>
    public async Task<Dictionary<string, bool>> TestConnectionsAsync(HierarchicalSettings settings, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v2/settings/test-connections", settings, _jsonOptions, ct);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(_jsonOptions, ct) ?? new Dictionary<string, bool>();
            }
            else
            {
                _logger.LogWarning("Failed to test connections. Status: {StatusCode}", response.StatusCode);
                return new Dictionary<string, bool>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connections");
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Validate settings without saving
    /// </summary>
    public async Task<bool> ValidateSettingsAsync(HierarchicalSettings settings, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v2/settings/validate", settings, _jsonOptions, ct);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ValidationResult>(_jsonOptions, ct);
                return result?.IsValid ?? false;
            }
            else
            {
                _logger.LogWarning("Failed to validate settings. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating settings");
            return false;
        }
    }

    /// <summary>
    /// Get available printers
    /// </summary>
    public async Task<List<PrinterDevice>> GetAvailablePrintersAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v2/settings/printers/available", ct);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PrinterDevice>>(_jsonOptions, ct) ?? new List<PrinterDevice>();
            }
            else
            {
                _logger.LogWarning("Failed to get available printers. Status: {StatusCode}", response.StatusCode);
                return GetLocalPrinters();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available printers");
            return GetLocalPrinters();
        }
    }

    /// <summary>
    /// Test print functionality
    /// </summary>
    public async Task<bool> TestPrintAsync(string printerName, string paperSize = "80mm", bool includeLogo = true, string testMessage = "Test Print - MagiDesk POS", CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                PrinterName = printerName,
                PaperSize = paperSize,
                IncludeLogo = includeLogo,
                TestMessage = testMessage
            };

            var response = await _httpClient.PostAsJsonAsync("api/v2/settings/printers/test-print", request, _jsonOptions, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Test print completed successfully for printer {PrinterName}", printerName);
                return true;
            }
            else
            {
                _logger.LogWarning("Test print failed for printer {PrinterName}. Status: {StatusCode}", printerName, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing test print for printer {PrinterName}", printerName);
            return false;
        }
    }

    /// <summary>
    /// Export settings to JSON string
    /// </summary>
    public async Task<string> ExportSettingsToJsonAsync(string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var url = "api/v2/settings/export";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"?host={Uri.EscapeDataString(hostKey)}";
            }

            var response = await _httpClient.GetAsync(url, ct);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("Settings exported successfully");
                return json;
            }
            else
            {
                _logger.LogWarning("Failed to export settings. Status: {StatusCode}", response.StatusCode);
                // Fallback to local export
                var settings = await GetSettingsAsync(hostKey, ct);
                return JsonSerializer.Serialize(settings, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting settings to JSON");
            // Fallback to local export
            var settings = await GetSettingsAsync(hostKey, ct);
            return JsonSerializer.Serialize(settings, _jsonOptions);
        }
    }

    /// <summary>
    /// Import settings from file
    /// </summary>
    public async Task<bool> ImportSettingsFromFileAsync(string filePath, string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Settings file not found: {FilePath}", filePath);
                return false;
            }

            var jsonSettings = await File.ReadAllTextAsync(filePath, ct);
            return await ImportSettingsFromJsonAsync(jsonSettings, hostKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing settings from file {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Import settings from JSON string
    /// </summary>
    public async Task<bool> ImportSettingsFromJsonAsync(string jsonSettings, string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var url = "api/v2/settings/import";
            if (!string.IsNullOrEmpty(hostKey))
            {
                url += $"?host={Uri.EscapeDataString(hostKey)}";
            }

            var content = new StringContent(jsonSettings, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Settings imported successfully");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to import settings. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing settings from JSON");
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private static string GetSettingsApiBaseUrl()
    {
        try
        {
            // Try to get from configuration
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
                
                if (config?.TryGetValue("SettingsApi", out var settingsApiObj) == true &&
                    settingsApiObj is JsonElement settingsApiElement &&
                    settingsApiElement.TryGetProperty("BaseUrl", out var baseUrlElement))
                {
                    return baseUrlElement.GetString() ?? GetDefaultSettingsApiUrl();
                }
            }
        }
        catch (Exception ex)
        {
        }

        return GetDefaultSettingsApiUrl();
    }

    private static string GetDefaultSettingsApiUrl()
    {
        return "https://magidesk-settings-904541739138.us-central1.run.app";
    }

    private static HierarchicalSettings GetDefaultSettings()
    {
        return new HierarchicalSettings
        {
            General = new GeneralSettings(),
            Pos = new PosSettings(),
            Inventory = new SharedInventorySettings(),
            Customers = new CustomerSettings(),
            Payments = new PaymentSettings
            {
                EnabledMethods = new List<PaymentMethod>
                {
                    new() { Name = "Cash", Type = "Cash", IsEnabled = true },
                    new() { Name = "Credit Card", Type = "Card", IsEnabled = true },
                    new() { Name = "Debit Card", Type = "Card", IsEnabled = true }
                }
            },
            Printers = new PrinterSettings(),
            Notifications = new NotificationSettings(),
            Security = new SecuritySettings(),
            Integrations = new IntegrationSettings
            {
                Api = new ApiSettings
                {
                    Endpoints = new List<ApiEndpoint>
                    {
                        new() { Name = "MenuApi", BaseUrl = "https://magidesk-menu-904541739138.northamerica-south1.run.app" },
                        new() { Name = "OrderApi", BaseUrl = "https://magidesk-order-904541739138.northamerica-south1.run.app" },
                        new() { Name = "PaymentApi", BaseUrl = "https://magidesk-payment-904541739138.northamerica-south1.run.app" },
                        new() { Name = "InventoryApi", BaseUrl = "https://magidesk-inventory-904541739138.northamerica-south1.run.app" },
                        new() { Name = "TablesApi", BaseUrl = "https://magidesk-tables-904541739138.northamerica-south1.run.app" },
                        new() { Name = "UsersApi", BaseUrl = "https://magidesk-users-23sbzjsxaq-pv.a.run.app" },
                        new() { Name = "SettingsApi", BaseUrl = "https://magidesk-settings-904541739138.us-central1.run.app" },
                        new() { Name = "VendorOrdersApi", BaseUrl = "https://magidesk-vendororders-904541739138.northamerica-south1.run.app" }
                    }
                }
            },
            System = new SystemSettings()
        };
    }

    private static T? GetDefaultCategorySettings<T>(string category) where T : class
    {
        var defaults = GetDefaultSettings();
        
        return category.ToLowerInvariant() switch
        {
            "general" => defaults.General as T,
            "pos" => defaults.Pos as T,
            "inventory" => defaults.Inventory as T,
            "customers" => defaults.Customers as T,
            "payments" => defaults.Payments as T,
            "printers" => defaults.Printers as T,
            "notifications" => defaults.Notifications as T,
            "security" => defaults.Security as T,
            "integrations" => defaults.Integrations as T,
            "system" => defaults.System as T,
            _ => null
        };
    }

    private static List<SettingMetadata> GetDefaultMetadata()
    {
        return new List<SettingMetadata>
        {
            new() { Key = "General.BusinessName", DisplayName = "Business Name", Category = "General", Type = SettingType.Text, IsRequired = true },
            new() { Key = "General.Theme", DisplayName = "Theme", Category = "General", Type = SettingType.Dropdown, Options = new List<string> { "System", "Light", "Dark" } },
            new() { Key = "Printers.Receipt.DefaultPrinter", DisplayName = "Default Printer", Category = "Printers", Type = SettingType.Text, IsRequired = true },
            new() { Key = "Printers.Receipt.PaperSize", DisplayName = "Paper Size", Category = "Printers", Type = SettingType.Dropdown, Options = new List<string> { "58mm", "80mm", "A4" } }
        };
    }

    private static List<PrinterDevice> GetLocalPrinters()
    {
        try
        {
            var printers = new List<PrinterDevice>();
            
            // Get system printers
            foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                printers.Add(new PrinterDevice
                {
                    Name = printerName,
                    Type = "System",
                    ConnectionString = printerName,
                    IsOnline = true
                });
            }

            return printers;
        }
        catch (Exception ex)
        {
            return new List<PrinterDevice>();
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #endregion
}

// Supporting classes
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
}

public class SettingsAuditEntry
{
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ChangedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Changes { get; set; }
}

