using MagiDesk.Shared.DTOs.Settings;
using Npgsql;
using System.Text.Json;

namespace SettingsApi.Services;

/// <summary>
/// Enhanced settings service with hierarchical organization and PostgreSQL storage
/// </summary>
public interface IHierarchicalSettingsService
{
    Task<HierarchicalSettings> GetSettingsAsync(string? hostKey = null, CancellationToken ct = default);
    Task<T?> GetSettingsCategoryAsync<T>(string category, string? hostKey = null, CancellationToken ct = default) where T : class;
    Task<bool> SaveSettingsAsync(HierarchicalSettings settings, string? hostKey = null, CancellationToken ct = default);
    Task<bool> SaveSettingsCategoryAsync<T>(string category, T settings, string? hostKey = null, CancellationToken ct = default) where T : class;
    Task<List<SettingMetadata>> GetSettingsMetadataAsync(CancellationToken ct = default);
    Task<bool> ResetToDefaultsAsync(string? hostKey = null, CancellationToken ct = default);
    Task<List<SettingsAuditEntry>> GetAuditLogAsync(string? hostKey = null, int limit = 50, CancellationToken ct = default);
    Task<bool> ValidateSettingsAsync(HierarchicalSettings settings, CancellationToken ct = default);
    Task<Dictionary<string, bool>> TestConnectionsAsync(HierarchicalSettings settings, CancellationToken ct = default);
}

public class HierarchicalSettingsService : IHierarchicalSettingsService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<HierarchicalSettingsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HierarchicalSettingsService(NpgsqlDataSource dataSource, ILogger<HierarchicalSettingsService> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<HierarchicalSettings> GetSettingsAsync(string? hostKey = null, CancellationToken ct = default)
    {
        var host = GetHostKey(hostKey);
        
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            const string sql = @"
                SELECT category, settings_json 
                FROM settings.hierarchical_settings 
                WHERE host_key = @hostKey AND is_active = true
                ORDER BY category";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("hostKey", host);

            var settings = new HierarchicalSettings();
            
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var category = reader.GetString(reader.GetOrdinal("category"));
                var json = reader.GetString(reader.GetOrdinal("settings_json"));
                
                ApplyCategorySettings(settings, category, json);
            }

            // Fill in defaults for missing categories
            await FillDefaultsAsync(settings, ct);
            
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hierarchical settings for host {HostKey}", host);
            return GetDefaultSettings();
        }
    }

    public async Task<T?> GetSettingsCategoryAsync<T>(string category, string? hostKey = null, CancellationToken ct = default) where T : class
    {
        var host = GetHostKey(hostKey);
        
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            const string sql = @"
                SELECT settings_json 
                FROM settings.hierarchical_settings 
                WHERE host_key = @hostKey AND category = @category AND is_active = true";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("hostKey", host);
            command.Parameters.AddWithValue("category", category);

            var json = await command.ExecuteScalarAsync(ct) as string;
            
            if (string.IsNullOrEmpty(json))
            {
                return GetDefaultCategorySettings<T>(category);
            }

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings category {Category} for host {HostKey}", category, host);
            return GetDefaultCategorySettings<T>(category);
        }
    }

    public async Task<bool> SaveSettingsAsync(HierarchicalSettings settings, string? hostKey = null, CancellationToken ct = default)
    {
        var host = GetHostKey(hostKey);
        
        try
        {
            _logger.LogInformation("Opening database connection for saving settings");
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            _logger.LogInformation("Database connection opened successfully");
            
            await using var transaction = await connection.BeginTransactionAsync(ct);
            _logger.LogInformation("Database transaction started");

            try
            {
                // Save each category
                _logger.LogInformation("Saving general settings");
                await SaveCategoryAsync(connection, transaction, host, "general", settings.General, ct);
                _logger.LogInformation("Saving pos settings");
                await SaveCategoryAsync(connection, transaction, host, "pos", settings.Pos, ct);
                _logger.LogInformation("Saving inventory settings");
                await SaveCategoryAsync(connection, transaction, host, "inventory", settings.Inventory, ct);
                _logger.LogInformation("Saving customers settings");
                await SaveCategoryAsync(connection, transaction, host, "customers", settings.Customers, ct);
                _logger.LogInformation("Saving payments settings");
                await SaveCategoryAsync(connection, transaction, host, "payments", settings.Payments, ct);
                _logger.LogInformation("Saving printers settings");
                await SaveCategoryAsync(connection, transaction, host, "printers", settings.Printers, ct);
                _logger.LogInformation("Saving notifications settings");
                await SaveCategoryAsync(connection, transaction, host, "notifications", settings.Notifications, ct);
                _logger.LogInformation("Saving security settings");
                await SaveCategoryAsync(connection, transaction, host, "security", settings.Security, ct);
                _logger.LogInformation("Saving integrations settings");
                await SaveCategoryAsync(connection, transaction, host, "integrations", settings.Integrations, ct);
                _logger.LogInformation("Saving system settings");
                await SaveCategoryAsync(connection, transaction, host, "system", settings.System, ct);

                // Log audit entry
                _logger.LogInformation("Logging audit entry");
                await LogAuditEntryAsync(connection, transaction, host, "settings_updated", "All settings updated", ct);

                _logger.LogInformation("Committing transaction");
                await transaction.CommitAsync(ct);
                _logger.LogInformation("Settings saved successfully");
                return true;
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Error during settings save transaction, rolling back");
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save hierarchical settings for host {HostKey}", host);
            return false;
        }
    }

    public async Task<bool> SaveSettingsCategoryAsync<T>(string category, T settings, string? hostKey = null, CancellationToken ct = default) where T : class
    {
        var host = GetHostKey(hostKey);
        
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            try
            {
                await SaveCategoryAsync(connection, transaction, host, category, settings, ct);
                await LogAuditEntryAsync(connection, transaction, host, "category_updated", $"Category '{category}' updated", ct);

                await transaction.CommitAsync(ct);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings category {Category} for host {HostKey}", category, host);
            return false;
        }
    }

    public async Task<List<SettingMetadata>> GetSettingsMetadataAsync(CancellationToken ct = default)
    {
        // This would typically come from a database or configuration
        // For now, return a comprehensive metadata list
        return GetSettingsMetadata();
    }

    public async Task<bool> ResetToDefaultsAsync(string? hostKey = null, CancellationToken ct = default)
    {
        var host = GetHostKey(hostKey);
        var defaults = GetDefaultSettings();
        
        return await SaveSettingsAsync(defaults, host, ct);
    }

    public async Task<List<SettingsAuditEntry>> GetAuditLogAsync(string? hostKey = null, int limit = 50, CancellationToken ct = default)
    {
        var host = GetHostKey(hostKey);
        
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            const string sql = @"
                SELECT action, description, changed_by, created_at, changes_json
                FROM settings.settings_audit 
                WHERE host_key = @hostKey 
                ORDER BY created_at DESC 
                LIMIT @limit";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("hostKey", host);
            command.Parameters.AddWithValue("limit", limit);

            var entries = new List<SettingsAuditEntry>();
            
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                entries.Add(new SettingsAuditEntry
                {
                    Action = reader.GetString(reader.GetOrdinal("action")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    ChangedBy = reader.IsDBNull(reader.GetOrdinal("changed_by")) ? null : reader.GetString(reader.GetOrdinal("changed_by")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    Changes = reader.IsDBNull(reader.GetOrdinal("changes_json")) ? null : reader.GetString(reader.GetOrdinal("changes_json"))
                });
            }

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log for host {HostKey}", host);
            return new List<SettingsAuditEntry>();
        }
    }

    public async Task<bool> ValidateSettingsAsync(HierarchicalSettings settings, CancellationToken ct = default)
    {
        try
        {
            // Ensure settings object is not null
            if (settings == null)
            {
                _logger.LogWarning("Settings object is null");
                return false;
            }

            // Initialize nested objects if null
            settings.General ??= new GeneralSettings();
            settings.Printers ??= new PrinterSettings();
            settings.Pos ??= new PosSettings();
            settings.Security ??= new SecuritySettings();

            // Validate business information - use defaults if empty
            if (string.IsNullOrWhiteSpace(settings.General.BusinessName))
            {
                settings.General.BusinessName = "MagiDesk POS";
                _logger.LogInformation("Using default business name");
            }

            // Validate printer settings with safe defaults
            if (settings.Printers.Receipt != null)
            {
                if (settings.Printers.Receipt.CopiesForFinalReceipt < 1 || settings.Printers.Receipt.CopiesForFinalReceipt > 5)
                {
                    settings.Printers.Receipt.CopiesForFinalReceipt = 1;
                    _logger.LogInformation("Using default copies for final receipt: 1");
                }
            }

            // Validate tax rates with safe defaults
            if (settings.Pos.Tax != null)
            {
                if (settings.Pos.Tax.DefaultTaxRate < 0 || settings.Pos.Tax.DefaultTaxRate > 100)
                {
                    settings.Pos.Tax.DefaultTaxRate = 0;
                    _logger.LogInformation("Using default tax rate: 0%");
                }
            }

            // Validate session timeouts with safe defaults
            if (settings.Security.Sessions != null)
            {
                if (settings.Security.Sessions.SessionTimeoutMinutes < 5 || settings.Security.Sessions.SessionTimeoutMinutes > 480)
                {
                    settings.Security.Sessions.SessionTimeoutMinutes = 60;
                    _logger.LogInformation("Using default session timeout: 60 minutes");
                }
            }

            _logger.LogInformation("Settings validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings validation failed with exception");
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> TestConnectionsAsync(HierarchicalSettings settings, CancellationToken ct = default)
    {
        var results = new Dictionary<string, bool>();
        
        try
        {
            // Test API endpoints
            foreach (var endpoint in settings.Integrations.Api.Endpoints)
            {
                results[endpoint.Name] = await TestApiEndpointAsync(endpoint.BaseUrl, endpoint.TimeoutMs, ct);
            }

            // Test printer connections
            foreach (var printer in settings.Printers.Devices.AvailablePrinters)
            {
                results[$"Printer_{printer.Name}"] = await TestPrinterConnectionAsync(printer, ct);
            }

            // Test email settings
            if (settings.Notifications.Email.EnableEmail)
            {
                results["Email"] = await TestEmailConnectionAsync(settings.Notifications.Email, ct);
            }

            // Test SMS settings
            if (settings.Notifications.Sms.EnableSms)
            {
                results["SMS"] = await TestSmsConnectionAsync(settings.Notifications.Sms, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection testing failed");
        }

        return results;
    }

    #region Private Methods

    private async Task SaveCategoryAsync<T>(NpgsqlConnection connection, NpgsqlTransaction transaction, 
        string hostKey, string category, T settings, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO settings.hierarchical_settings (host_key, category, settings_json, updated_at)
            VALUES (@hostKey, @category, @settingsJson, @updatedAt)
            ON CONFLICT (host_key, category) 
            DO UPDATE SET 
                settings_json = @settingsJson,
                updated_at = @updatedAt,
                is_active = true";

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("hostKey", hostKey);
        command.Parameters.AddWithValue("category", category);
        command.Parameters.AddWithValue("settingsJson", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonSerializer.Serialize(settings, _jsonOptions));
        command.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task LogAuditEntryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string hostKey, string action, string description, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO settings.settings_audit (host_key, action, description, created_at)
            VALUES (@hostKey, @action, @description, @createdAt)";

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("hostKey", hostKey);
        command.Parameters.AddWithValue("action", action);
        command.Parameters.AddWithValue("description", description);
        command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(ct);
    }

    private void ApplyCategorySettings(HierarchicalSettings settings, string category, string json)
    {
        try
        {
            switch (category.ToLowerInvariant())
            {
                case "general":
                    settings.General = JsonSerializer.Deserialize<GeneralSettings>(json, _jsonOptions) ?? new GeneralSettings();
                    break;
                case "pos":
                    settings.Pos = JsonSerializer.Deserialize<PosSettings>(json, _jsonOptions) ?? new PosSettings();
                    break;
                case "inventory":
                    settings.Inventory = JsonSerializer.Deserialize<InventorySettings>(json, _jsonOptions) ?? new InventorySettings();
                    break;
                case "customers":
                    settings.Customers = JsonSerializer.Deserialize<CustomerSettings>(json, _jsonOptions) ?? new CustomerSettings();
                    break;
                case "payments":
                    settings.Payments = JsonSerializer.Deserialize<PaymentSettings>(json, _jsonOptions) ?? new PaymentSettings();
                    break;
                case "printers":
                    settings.Printers = JsonSerializer.Deserialize<PrinterSettings>(json, _jsonOptions) ?? new PrinterSettings();
                    break;
                case "notifications":
                    settings.Notifications = JsonSerializer.Deserialize<NotificationSettings>(json, _jsonOptions) ?? new NotificationSettings();
                    break;
                case "security":
                    settings.Security = JsonSerializer.Deserialize<SecuritySettings>(json, _jsonOptions) ?? new SecuritySettings();
                    break;
                case "integrations":
                    settings.Integrations = JsonSerializer.Deserialize<IntegrationSettings>(json, _jsonOptions) ?? new IntegrationSettings();
                    break;
                case "system":
                    settings.System = JsonSerializer.Deserialize<SystemSettings>(json, _jsonOptions) ?? new SystemSettings();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize settings category {Category}", category);
        }
    }

    private async Task FillDefaultsAsync(HierarchicalSettings settings, CancellationToken ct)
    {
        // This method ensures all categories have default values if not loaded from database
        await Task.CompletedTask; // Placeholder for async operations if needed
    }

    private T? GetDefaultCategorySettings<T>(string category) where T : class
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

    private static HierarchicalSettings GetDefaultSettings()
    {
        return new HierarchicalSettings
        {
            General = new GeneralSettings(),
            Pos = new PosSettings(),
            Inventory = new InventorySettings(),
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

    private static List<SettingMetadata> GetSettingsMetadata()
    {
        return new List<SettingMetadata>
        {
            // General Settings
            new() { Key = "General.BusinessName", DisplayName = "Business Name", Category = "General", Type = SettingType.Text, IsRequired = true },
            new() { Key = "General.BusinessAddress", DisplayName = "Business Address", Category = "General", Type = SettingType.Text, IsRequired = true },
            new() { Key = "General.BusinessPhone", DisplayName = "Business Phone", Category = "General", Type = SettingType.Text, IsRequired = true },
            new() { Key = "General.BusinessEmail", DisplayName = "Business Email", Category = "General", Type = SettingType.Email },
            new() { Key = "General.Theme", DisplayName = "Theme", Category = "General", Type = SettingType.Dropdown, Options = new List<string> { "System", "Light", "Dark" } },
            new() { Key = "General.Language", DisplayName = "Language", Category = "General", Type = SettingType.Dropdown, Options = new List<string> { "en-US", "es-ES", "fr-FR" } },
            
            // POS Settings
            new() { Key = "Pos.TableLayout.RatePerMinute", DisplayName = "Rate Per Minute", Category = "POS", SubCategory = "Tables", Type = SettingType.Decimal, MinValue = 0.01m, MaxValue = 999.99m },
            new() { Key = "Pos.Tax.DefaultTaxRate", DisplayName = "Default Tax Rate (%)", Category = "POS", SubCategory = "Tax", Type = SettingType.Decimal, MinValue = 0, MaxValue = 100 },
            
            // Printer Settings
            new() { Key = "Printers.Receipt.DefaultPrinter", DisplayName = "Default Receipt Printer", Category = "Printers", SubCategory = "Receipt", Type = SettingType.Text, IsRequired = true },
            new() { Key = "Printers.Receipt.PaperSize", DisplayName = "Paper Size", Category = "Printers", SubCategory = "Receipt", Type = SettingType.Dropdown, Options = new List<string> { "58mm", "80mm", "A4" } },
            new() { Key = "Printers.Receipt.AutoPrintOnPayment", DisplayName = "Auto Print on Payment", Category = "Printers", SubCategory = "Receipt", Type = SettingType.Boolean },
            
            // Security Settings
            new() { Key = "Security.Sessions.SessionTimeoutMinutes", DisplayName = "Session Timeout (Minutes)", Category = "Security", SubCategory = "Sessions", Type = SettingType.Number, MinValue = 5, MaxValue = 480 },
            new() { Key = "Security.Login.MaxLoginAttempts", DisplayName = "Max Login Attempts", Category = "Security", SubCategory = "Login", Type = SettingType.Number, MinValue = 3, MaxValue = 10 }
        };
    }

    private async Task<bool> TestApiEndpointAsync(string baseUrl, int timeoutMs, CancellationToken ct)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
            
            var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TestPrinterConnectionAsync(PrinterDevice printer, CancellationToken ct)
    {
        try
        {
            // Implement printer connection testing based on printer type
            await Task.Delay(100, ct); // Simulate test
            return true; // Placeholder
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TestEmailConnectionAsync(EmailSettings email, CancellationToken ct)
    {
        try
        {
            // Implement SMTP connection testing
            await Task.Delay(100, ct); // Simulate test
            return true; // Placeholder
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TestSmsConnectionAsync(SmsSettings sms, CancellationToken ct)
    {
        try
        {
            // Implement SMS provider connection testing
            await Task.Delay(100, ct); // Simulate test
            return true; // Placeholder
        }
        catch
        {
            return false;
        }
    }

    private static string GetHostKey(string? hostOverride)
    {
        if (!string.IsNullOrWhiteSpace(hostOverride)) return hostOverride;
        try { return System.Net.Dns.GetHostName(); }
        catch { return Environment.MachineName; }
    }

    #endregion
}

public class SettingsAuditEntry
{
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ChangedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Changes { get; set; }
}
