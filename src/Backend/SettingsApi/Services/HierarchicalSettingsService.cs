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
    Task<bool> ExportSettingsAsync(string? hostKey = null, CancellationToken ct = default);
    Task<string> ExportSettingsToJsonAsync(string? hostKey = null, CancellationToken ct = default);
    Task<bool> ImportSettingsFromJsonAsync(string jsonSettings, string? hostKey = null, CancellationToken ct = default);
    Task<bool> ImportSettingsFromFileAsync(string filePath, string? hostKey = null, CancellationToken ct = default);
    Task<bool> ValidateUserAccessAsync(string category, string userId, string action, CancellationToken ct = default);
    Task<List<string>> GetUserAccessibleCategoriesAsync(string userId, CancellationToken ct = default);
    Task<bool> SetUserCategoryAccessAsync(string userId, string category, bool canView, bool canEdit, CancellationToken ct = default);
    Task<List<SettingsAuditEntry>> GetAuditHistoryAsync(string? hostKey = null, string? category = null, int limit = 100, CancellationToken ct = default);
    Task<List<SettingsAuditEntry>> GetAuditHistoryByUserAsync(string userId, string? hostKey = null, int limit = 100, CancellationToken ct = default);
    Task<bool> BulkResetSettingsAsync(List<string> categories, string? hostKey = null, CancellationToken ct = default);
    Task<string> BulkExportSettingsAsync(List<string> categories, string? hostKey = null, CancellationToken ct = default);
    Task<Dictionary<string, List<string>>> BulkValidateSettingsAsync(HierarchicalSettings settings, CancellationToken ct = default);
    Task<bool> ValidateSettingsAsync(HierarchicalSettings settings, CancellationToken ct = default);
    Task<Dictionary<string, object>> TestConnectionsAsync(HierarchicalSettings settings, CancellationToken ct = default);
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

                // Get existing settings for change comparison
                var existingSettings = await GetSettingsAsync(host, ct);
                var changes = new Dictionary<string, object>();
                
                if (existingSettings != null)
                {
                    changes = CompareSettings(existingSettings, settings);
                }

                // Log audit entry with detailed change tracking
                _logger.LogInformation("Logging audit entry");
                await LogAuditEntryAsync(host, "settings_updated", 
                    $"Settings updated for host {host}", 
                    category: "all",
                    changedBy: "system", // TODO: Get from authentication context
                    changes: changes.Count > 0 ? changes : null,
                    ct: ct);

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
                    Changes = reader.IsDBNull(reader.GetOrdinal("changes_json")) ? null : 
                        JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(reader.GetOrdinal("changes_json")), _jsonOptions)
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
            settings.Inventory ??= new InventorySettings();
            settings.Customers ??= new CustomerSettings();
            settings.Payments ??= new PaymentSettings();
            settings.Notifications ??= new NotificationSettings();
            settings.Integrations ??= new IntegrationSettings();
            settings.System ??= new SystemSettings();

            // Validate General Settings
            await ValidateGeneralSettingsAsync(settings.General, ct);

            // Validate POS Settings
            await ValidatePosSettingsAsync(settings.Pos, ct);

            // Validate Inventory Settings
            await ValidateInventorySettingsAsync(settings.Inventory, ct);

            // Validate Customer Settings
            await ValidateCustomerSettingsAsync(settings.Customers, ct);

            // Validate Payment Settings
            await ValidatePaymentSettingsAsync(settings.Payments, ct);

            // Validate Printer Settings
            await ValidatePrinterSettingsAsync(settings.Printers, ct);

            // Validate Notification Settings
            await ValidateNotificationSettingsAsync(settings.Notifications, ct);

            // Validate Security Settings
            await ValidateSecuritySettingsAsync(settings.Security, ct);

            // Validate Integration Settings
            await ValidateIntegrationSettingsAsync(settings.Integrations, ct);

            // Validate System Settings
            await ValidateSystemSettingsAsync(settings.System, ct);

            _logger.LogInformation("Settings validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings validation failed with exception");
            return false;
        }
    }

    public async Task<bool> ExportSettingsAsync(string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var settings = await GetSettingsAsync(hostKey, ct);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            
            var host = GetHostKey(hostKey);
            var fileName = $"magidesk-settings-{host}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            
            await File.WriteAllTextAsync(filePath, json, ct);
            
            _logger.LogInformation("Settings exported to {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings for host {HostKey}", hostKey);
            return false;
        }
    }

    public async Task<string> ExportSettingsToJsonAsync(string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var settings = await GetSettingsAsync(hostKey, ct);
            return JsonSerializer.Serialize(settings, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings to JSON for host {HostKey}", hostKey);
            throw;
        }
    }

    public async Task<bool> ImportSettingsFromJsonAsync(string jsonSettings, string? hostKey = null, CancellationToken ct = default)
    {
        var host = GetHostKey(hostKey);
        
        try
        {
            var settings = JsonSerializer.Deserialize<HierarchicalSettings>(jsonSettings, _jsonOptions);
            if (settings == null)
            {
                _logger.LogWarning("Failed to deserialize settings JSON");
                return false;
            }

            // Validate the imported settings
            var isValid = await ValidateSettingsAsync(settings, ct);
            if (!isValid)
            {
                _logger.LogWarning("Imported settings failed validation");
                return false;
            }

            // Save the validated settings
            var success = await SaveSettingsAsync(settings, host, ct);
            if (success)
            {
                await LogAuditEntryAsync(host, "settings_imported", "Settings imported from JSON", ct: ct);
                _logger.LogInformation("Settings imported successfully for host {HostKey}", host);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import settings from JSON for host {HostKey}", host);
            return false;
        }
    }

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
            _logger.LogError(ex, "Failed to import settings from file {FilePath}", filePath);
            return false;
        }
    }

    #region Validation Methods

    private async Task ValidateGeneralSettingsAsync(GeneralSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Business information validation
        if (string.IsNullOrWhiteSpace(settings.BusinessName))
        {
            settings.BusinessName = "MagiDesk POS";
            _logger.LogInformation("Using default business name");
        }

        if (string.IsNullOrWhiteSpace(settings.BusinessPhone))
        {
            settings.BusinessPhone = "";
            _logger.LogInformation("Business phone not set");
        }

        if (string.IsNullOrWhiteSpace(settings.BusinessAddress))
        {
            settings.BusinessAddress = "";
            _logger.LogInformation("Business address not set");
        }

        // Email validation
        if (!string.IsNullOrWhiteSpace(settings.BusinessEmail) && !IsValidEmail(settings.BusinessEmail))
        {
            settings.BusinessEmail = "";
            _logger.LogWarning("Invalid business email format, cleared");
        }

        // Theme validation
        var validThemes = new[] { "System", "Light", "Dark" };
        if (!validThemes.Contains(settings.Theme))
        {
            settings.Theme = "System";
            _logger.LogInformation("Using default theme: System");
        }

        // Language validation
        var validLanguages = new[] { "en-US", "es-ES", "fr-FR" };
        if (!validLanguages.Contains(settings.Language))
        {
            settings.Language = "en-US";
            _logger.LogInformation("Using default language: en-US");
        }

        // Currency validation
        var validCurrencies = new[] { "USD", "CAD", "EUR", "GBP" };
        if (!validCurrencies.Contains(settings.Currency))
        {
            settings.Currency = "USD";
            _logger.LogInformation("Using default currency: USD");
        }

        // Timezone validation
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(settings.Timezone);
        }
        catch
        {
            settings.Timezone = "America/New_York";
            _logger.LogInformation("Using default timezone: America/New_York");
        }
    }

    private async Task ValidatePosSettingsAsync(PosSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Cash Drawer validation
        settings.CashDrawer ??= new CashDrawerSettings();
        if (settings.CashDrawer.BaudRate < 1200 || settings.CashDrawer.BaudRate > 115200)
        {
            settings.CashDrawer.BaudRate = 9600;
            _logger.LogInformation("Using default baud rate: 9600");
        }

        // Table Layout validation
        settings.TableLayout ??= new TableLayoutSettings();
        if (settings.TableLayout.RatePerMinute < 0.01m || settings.TableLayout.RatePerMinute > 999.99m)
        {
            settings.TableLayout.RatePerMinute = 0.50m;
            _logger.LogInformation("Using default rate per minute: $0.50");
        }

        if (settings.TableLayout.WarnAfterMinutes < 1 || settings.TableLayout.WarnAfterMinutes > 1440)
        {
            settings.TableLayout.WarnAfterMinutes = 30;
            _logger.LogInformation("Using default warn after minutes: 30");
        }

        if (settings.TableLayout.AutoStopAfterMinutes < 1 || settings.TableLayout.AutoStopAfterMinutes > 1440)
        {
            settings.TableLayout.AutoStopAfterMinutes = 120;
            _logger.LogInformation("Using default auto-stop after minutes: 120");
        }

        // Tax validation
        settings.Tax ??= new TaxSettings();
        if (settings.Tax.DefaultTaxRate < 0 || settings.Tax.DefaultTaxRate > 100)
        {
            settings.Tax.DefaultTaxRate = 0;
            _logger.LogInformation("Using default tax rate: 0%");
        }

        if (string.IsNullOrWhiteSpace(settings.Tax.TaxDisplayName))
        {
            settings.Tax.TaxDisplayName = "Tax";
            _logger.LogInformation("Using default tax display name: Tax");
        }

        // Validate additional tax rates
        settings.Tax.AdditionalTaxRates ??= new List<TaxRate>();
        foreach (var taxRate in settings.Tax.AdditionalTaxRates)
        {
            if (taxRate.Rate < 0 || taxRate.Rate > 100)
            {
                taxRate.Rate = 0;
                _logger.LogWarning("Invalid tax rate for {Name}, set to 0%", taxRate.Name);
            }
        }
    }

    private async Task ValidateInventorySettingsAsync(InventorySettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Stock validation
        settings.Stock ??= new StockSettings();
        if (settings.Stock.LowStockThreshold < 1 || settings.Stock.LowStockThreshold > 1000)
        {
            settings.Stock.LowStockThreshold = 10;
            _logger.LogInformation("Using default low stock threshold: 10");
        }

        if (settings.Stock.CriticalStockThreshold < 1 || settings.Stock.CriticalStockThreshold > 1000)
        {
            settings.Stock.CriticalStockThreshold = 5;
            _logger.LogInformation("Using default critical stock threshold: 5");
        }

        // Reorder validation
        settings.Reorder ??= new ReorderSettings();
        if (settings.Reorder.ReorderLeadTimeDays < 1 || settings.Reorder.ReorderLeadTimeDays > 365)
        {
            settings.Reorder.ReorderLeadTimeDays = 7;
            _logger.LogInformation("Using default reorder lead time: 7 days");
        }

        if (settings.Reorder.SafetyStockMultiplier < 1.0m || settings.Reorder.SafetyStockMultiplier > 10.0m)
        {
            settings.Reorder.SafetyStockMultiplier = 1.5m;
            _logger.LogInformation("Using default safety stock multiplier: 1.5");
        }

        // Vendor validation
        settings.Vendors ??= new VendorSettings();
        if (settings.Vendors.DefaultPaymentTerms < 1 || settings.Vendors.DefaultPaymentTerms > 90)
        {
            settings.Vendors.DefaultPaymentTerms = 30;
            _logger.LogInformation("Using default payment terms: 30 days");
        }

        var validCurrencies = new[] { "USD", "CAD", "EUR", "GBP" };
        if (!validCurrencies.Contains(settings.Vendors.DefaultCurrency))
        {
            settings.Vendors.DefaultCurrency = "USD";
            _logger.LogInformation("Using default vendor currency: USD");
        }
    }

    private async Task ValidateCustomerSettingsAsync(CustomerSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Membership validation
        settings.Membership ??= new MembershipSettings();
        if (settings.Membership.DefaultExpiryDays < 1 || settings.Membership.DefaultExpiryDays > 365)
        {
            settings.Membership.DefaultExpiryDays = 365;
            _logger.LogInformation("Using default membership expiry: 365 days");
        }

        // Validate membership tiers
        settings.Membership.Tiers ??= new List<MembershipTier>();
        foreach (var tier in settings.Membership.Tiers)
        {
            if (tier.DiscountPercentage < 0 || tier.DiscountPercentage > 100)
            {
                tier.DiscountPercentage = 0;
                _logger.LogWarning("Invalid discount percentage for tier {Name}, set to 0%", tier.Name);
            }

            if (tier.MinimumSpend < 0)
            {
                tier.MinimumSpend = 0;
                _logger.LogWarning("Invalid minimum spend for tier {Name}, set to $0", tier.Name);
            }
        }

        // Wallet validation
        settings.Wallet ??= new WalletSettings();
        if (settings.Wallet.MaxWalletBalance < 0 || settings.Wallet.MaxWalletBalance > 10000)
        {
            settings.Wallet.MaxWalletBalance = 1000;
            _logger.LogInformation("Using default max wallet balance: $1000");
        }

        if (settings.Wallet.MinTopUpAmount < 1 || settings.Wallet.MinTopUpAmount > 100)
        {
            settings.Wallet.MinTopUpAmount = 10;
            _logger.LogInformation("Using default min top-up amount: $10");
        }

        // Loyalty validation
        settings.Loyalty ??= new LoyaltySettings();
        if (settings.Loyalty.PointsPerDollar < 0.01m || settings.Loyalty.PointsPerDollar > 10.0m)
        {
            settings.Loyalty.PointsPerDollar = 1.0m;
            _logger.LogInformation("Using default points per dollar: 1.0");
        }

        if (settings.Loyalty.PointValue < 0.01m || settings.Loyalty.PointValue > 1.0m)
        {
            settings.Loyalty.PointValue = 0.01m;
            _logger.LogInformation("Using default point value: $0.01");
        }

        if (settings.Loyalty.MinPointsForRedemption < 1 || settings.Loyalty.MinPointsForRedemption > 10000)
        {
            settings.Loyalty.MinPointsForRedemption = 100;
            _logger.LogInformation("Using default min points for redemption: 100");
        }
    }

    private async Task ValidatePaymentSettingsAsync(PaymentSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Payment Methods validation
        settings.EnabledMethods ??= new List<PaymentMethod>();
        if (!settings.EnabledMethods.Any())
        {
            settings.EnabledMethods.AddRange(new[]
            {
                new PaymentMethod { Name = "Cash", Type = "Cash", IsEnabled = true },
                new PaymentMethod { Name = "Credit Card", Type = "Card", IsEnabled = true }
            });
            _logger.LogInformation("Added default payment methods");
        }

        // Discounts validation
        settings.Discounts ??= new DiscountSettings();
        if (settings.Discounts.MaxDiscountPercentage < 0 || settings.Discounts.MaxDiscountPercentage > 100)
        {
            settings.Discounts.MaxDiscountPercentage = 50;
            _logger.LogInformation("Using default max discount percentage: 50%");
        }

        // Surcharges validation
        settings.Surcharges ??= new SurchargeSettings();
        if (settings.Surcharges.CardSurchargePercentage < 0 || settings.Surcharges.CardSurchargePercentage > 25)
        {
            settings.Surcharges.CardSurchargePercentage = 5;
            _logger.LogInformation("Using default max surcharge percentage: 5%");
        }
    }

    private async Task ValidatePrinterSettingsAsync(PrinterSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Receipt validation
        settings.Receipt ??= new ReceiptPrinterSettings();
        if (settings.Receipt.CopiesForFinalReceipt < 1 || settings.Receipt.CopiesForFinalReceipt > 5)
        {
            settings.Receipt.CopiesForFinalReceipt = 1;
            _logger.LogInformation("Using default copies for final receipt: 1");
        }

        var validPaperSizes = new[] { "58mm", "80mm", "A4" };
        if (!validPaperSizes.Contains(settings.Receipt.PaperSize))
        {
            settings.Receipt.PaperSize = "80mm";
            _logger.LogInformation("Using default paper size: 80mm");
        }

        // Kitchen validation
        settings.Kitchen ??= new KitchenPrinterSettings();
        if (settings.Kitchen.CopiesPerOrder < 1 || settings.Kitchen.CopiesPerOrder > 3)
        {
            settings.Kitchen.CopiesPerOrder = 1;
            _logger.LogInformation("Using default copies for kitchen order: 1");
        }
    }

    private async Task ValidateNotificationSettingsAsync(NotificationSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Email validation
        settings.Email ??= new EmailSettings();
        if (settings.Email.SmtpPort < 1 || settings.Email.SmtpPort > 65535)
        {
            settings.Email.SmtpPort = 587;
            _logger.LogInformation("Using default SMTP port: 587");
        }

        if (!string.IsNullOrWhiteSpace(settings.Email.FromAddress) && !IsValidEmail(settings.Email.FromAddress))
        {
            settings.Email.FromAddress = "";
            _logger.LogWarning("Invalid from email address, cleared");
        }

        // SMS validation
        settings.Sms ??= new SmsSettings();
        var validProviders = new[] { "Twilio", "AWS SNS", "Azure Communication Services" };
        if (!string.IsNullOrWhiteSpace(settings.Sms.Provider) && !validProviders.Contains(settings.Sms.Provider))
        {
            settings.Sms.Provider = "Twilio";
            _logger.LogInformation("Using default SMS provider: Twilio");
        }

        // Alert validation
        settings.Alerts ??= new AlertSettings();
        if (settings.Alerts.AlertVolume < 1 || settings.Alerts.AlertVolume > 100)
        {
            settings.Alerts.AlertVolume = 50;
            _logger.LogInformation("Using default alert volume: 50%");
        }

        // Validate threshold alerts
        settings.Alerts.ThresholdAlerts ??= new List<ThresholdAlert>();
        foreach (var alert in settings.Alerts.ThresholdAlerts)
        {
            if (alert.Threshold < 0)
            {
                alert.Threshold = 0;
                _logger.LogWarning("Invalid threshold for alert {Name}, set to 0", alert.Name);
            }

            var validTypes = new[] { "Stock", "Revenue", "Orders" };
            if (!validTypes.Contains(alert.Type))
            {
                alert.Type = "Stock";
                _logger.LogWarning("Invalid type for alert {Name}, set to Stock", alert.Name);
            }
        }
    }

    private async Task ValidateSecuritySettingsAsync(SecuritySettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // RBAC validation
        settings.Rbac ??= new RbacSettings();

        // Login validation
        settings.Login ??= new LoginSettings();
        if (settings.Login.MaxLoginAttempts < 3 || settings.Login.MaxLoginAttempts > 10)
        {
            settings.Login.MaxLoginAttempts = 5;
            _logger.LogInformation("Using default max login attempts: 5");
        }

        if (settings.Login.LockoutDurationMinutes < 5 || settings.Login.LockoutDurationMinutes > 60)
        {
            settings.Login.LockoutDurationMinutes = 15;
            _logger.LogInformation("Using default lockout duration: 15 minutes");
        }

        // Session validation
        settings.Sessions ??= new SessionSettings();
        if (settings.Sessions.SessionTimeoutMinutes < 5 || settings.Sessions.SessionTimeoutMinutes > 480)
        {
            settings.Sessions.SessionTimeoutMinutes = 60;
            _logger.LogInformation("Using default session timeout: 60 minutes");
        }

        if (settings.Sessions.MaxConcurrentSessions < 1 || settings.Sessions.MaxConcurrentSessions > 10)
        {
            settings.Sessions.MaxConcurrentSessions = 3;
            _logger.LogInformation("Using default max concurrent sessions: 3");
        }

        // Audit validation
        settings.Audit ??= new AuditSettings();
        if (settings.Audit.RetentionDays < 30 || settings.Audit.RetentionDays > 365)
        {
            settings.Audit.RetentionDays = 90;
            _logger.LogInformation("Using default audit retention: 90 days");
        }
    }

    private async Task ValidateIntegrationSettingsAsync(IntegrationSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Payment Gateway validation
        settings.PaymentGateways ??= new PaymentGatewaySettings();
        var validGateways = new[] { "Stripe", "Square", "PayPal" };
        if (!string.IsNullOrWhiteSpace(settings.PaymentGateways.DefaultGateway) && 
            !validGateways.Contains(settings.PaymentGateways.DefaultGateway))
        {
            settings.PaymentGateways.DefaultGateway = "Stripe";
            _logger.LogInformation("Using default payment gateway: Stripe");
        }

        // Validate payment gateways
        settings.PaymentGateways.Gateways ??= new List<PaymentGateway>();
        foreach (var gateway in settings.PaymentGateways.Gateways)
        {
            if (!validGateways.Contains(gateway.Provider))
            {
                gateway.Provider = "Stripe";
                _logger.LogWarning("Invalid provider for gateway {Name}, set to Stripe", gateway.Name);
            }
        }

        // Webhook validation
        settings.Webhooks ??= new WebhookSettings();
        if (settings.Webhooks.TimeoutMs < 1000 || settings.Webhooks.TimeoutMs > 30000)
        {
            settings.Webhooks.TimeoutMs = 10000;
            _logger.LogInformation("Using default webhook timeout: 10000ms");
        }

        if (settings.Webhooks.MaxRetries < 1 || settings.Webhooks.MaxRetries > 10)
        {
            settings.Webhooks.MaxRetries = 3;
            _logger.LogInformation("Using default webhook max retries: 3");
        }

        // CRM validation
        settings.Crm ??= new CrmSettings();
        var validCrmProviders = new[] { "Salesforce", "HubSpot", "Pipedrive" };
        if (!string.IsNullOrWhiteSpace(settings.Crm.CrmProvider) && 
            !validCrmProviders.Contains(settings.Crm.CrmProvider))
        {
            settings.Crm.CrmProvider = "Salesforce";
            _logger.LogInformation("Using default CRM provider: Salesforce");
        }

        if (settings.Crm.SyncIntervalMinutes < 5 || settings.Crm.SyncIntervalMinutes > 1440)
        {
            settings.Crm.SyncIntervalMinutes = 60;
            _logger.LogInformation("Using default CRM sync interval: 60 minutes");
        }

        // API validation
        settings.Api ??= new ApiSettings();
        if (settings.Api.DefaultTimeoutMs < 1000 || settings.Api.DefaultTimeoutMs > 60000)
        {
            settings.Api.DefaultTimeoutMs = 10000;
            _logger.LogInformation("Using default API timeout: 10000ms");
        }

        if (settings.Api.DefaultRetries < 1 || settings.Api.DefaultRetries > 10)
        {
            settings.Api.DefaultRetries = 3;
            _logger.LogInformation("Using default API retries: 3");
        }
    }

    private async Task ValidateSystemSettingsAsync(SystemSettings settings, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Logging validation
        settings.Logging ??= new LoggingSettings();
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        if (!validLogLevels.Contains(settings.Logging.LogLevel))
        {
            settings.Logging.LogLevel = "Information";
            _logger.LogInformation("Using default log level: Information");
        }

        if (settings.Logging.MaxLogFileSizeMB < 1 || settings.Logging.MaxLogFileSizeMB > 100)
        {
            settings.Logging.MaxLogFileSizeMB = 10;
            _logger.LogInformation("Using default max log file size: 10MB");
        }

        if (settings.Logging.MaxLogFiles < 1 || settings.Logging.MaxLogFiles > 100)
        {
            settings.Logging.MaxLogFiles = 10;
            _logger.LogInformation("Using default max log files: 10");
        }

        if (settings.Logging.LogRetentionDays < 1 || settings.Logging.LogRetentionDays > 365)
        {
            settings.Logging.LogRetentionDays = 30;
            _logger.LogInformation("Using default log retention: 30 days");
        }

        // Tracing validation
        settings.Tracing ??= new TracingSettings();

        // Background Jobs validation
        settings.BackgroundJobs ??= new BackgroundJobSettings();
        if (settings.BackgroundJobs.HeartbeatIntervalMinutes < 1 || settings.BackgroundJobs.HeartbeatIntervalMinutes > 60)
        {
            settings.BackgroundJobs.HeartbeatIntervalMinutes = 5;
            _logger.LogInformation("Using default heartbeat interval: 5 minutes");
        }

        if (settings.BackgroundJobs.CleanupIntervalMinutes < 1 || settings.BackgroundJobs.CleanupIntervalMinutes > 1440)
        {
            settings.BackgroundJobs.CleanupIntervalMinutes = 60;
            _logger.LogInformation("Using default cleanup interval: 60 minutes");
        }

        if (settings.BackgroundJobs.BackupIntervalMinutes < 1 || settings.BackgroundJobs.BackupIntervalMinutes > 1440)
        {
            settings.BackgroundJobs.BackupIntervalMinutes = 240;
            _logger.LogInformation("Using default backup interval: 240 minutes");
        }

        // Performance validation
        settings.Performance ??= new PerformanceSettings();
        if (settings.Performance.DatabaseConnectionPoolSize < 10 || settings.Performance.DatabaseConnectionPoolSize > 1000)
        {
            settings.Performance.DatabaseConnectionPoolSize = 50;
            _logger.LogInformation("Using default connection pool size: 50");
        }

        if (settings.Performance.CacheExpirationMinutes < 1 || settings.Performance.CacheExpirationMinutes > 60)
        {
            settings.Performance.CacheExpirationMinutes = 15;
            _logger.LogInformation("Using default cache expiration: 15 minutes");
        }

        if (settings.Performance.MaxConcurrentRequests < 100 || settings.Performance.MaxConcurrentRequests > 10000)
        {
            settings.Performance.MaxConcurrentRequests = 1000;
            _logger.LogInformation("Using default max concurrent requests: 1000");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    #endregion

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
            General = new GeneralSettings
            {
                BusinessName = "MagiDesk POS",
                BusinessPhone = "",
                BusinessAddress = "",
                BusinessEmail = "",
                BusinessWebsite = "",
                Theme = "System",
                Language = "en-US",
                Timezone = "America/New_York",
                Currency = "USD"
            },
            Pos = new PosSettings
            {
                CashDrawer = new CashDrawerSettings
                {
                    AutoOpenOnSale = true,
                    AutoOpenOnRefund = false,
                    ComPort = "COM1",
                    BaudRate = 9600,
                    RequireManagerOverride = false
                },
                TableLayout = new TableLayoutSettings
                {
                    RatePerMinute = 0.50m,
                    WarnAfterMinutes = 30,
                    AutoStopAfterMinutes = 120,
                    EnableAutoStop = true,
                    ShowTimerOnTables = true
                },
                Shifts = new ShiftSettings
                {
                    ShiftStartTime = new TimeSpan(9, 0, 0), // 9:00 AM
                    ShiftEndTime = new TimeSpan(17, 0, 0),  // 5:00 PM
                    RequireShiftReports = true,
                    AutoCloseShift = false
                },
                Tax = new TaxSettings
                {
                    DefaultTaxRate = 0m,
                    TaxDisplayName = "Tax",
                    TaxInclusivePricing = false,
                    AdditionalTaxRates = new List<TaxRate>()
                }
            },
            Inventory = new InventorySettings
            {
                Stock = new StockSettings
                {
                    LowStockThreshold = 10,
                    CriticalStockThreshold = 5,
                    EnableStockAlerts = true,
                    AutoDeductStock = true,
                    AllowNegativeStock = false
                },
                Reorder = new ReorderSettings
                {
                    EnableAutoReorder = false,
                    ReorderLeadTimeDays = 7,
                    SafetyStockMultiplier = 1.5m
                },
                Vendors = new VendorSettings
                {
                    DefaultPaymentTerms = 30,
                    DefaultCurrency = "USD",
                    RequireApprovalForOrders = false
                }
            },
            Customers = new CustomerSettings
            {
                Membership = new MembershipSettings
                {
                    DefaultExpiryDays = 365,
                    AutoRenewMemberships = false,
                    RequireEmailForMembership = false,
                    Tiers = new List<MembershipTier>
                    {
                        new() { Name = "Basic", DiscountPercentage = 0, MinimumSpend = 0, Color = "#007ACC" },
                        new() { Name = "Premium", DiscountPercentage = 10, MinimumSpend = 100, Color = "#FFD700" },
                        new() { Name = "VIP", DiscountPercentage = 20, MinimumSpend = 500, Color = "#FF6B6B" }
                    }
                },
                Wallet = new WalletSettings
                {
                    EnableWalletSystem = false,
                    MaxWalletBalance = 1000m,
                    MinTopUpAmount = 10m,
                    AllowNegativeBalance = false,
                    RequireIdForWalletUse = true
                },
                Loyalty = new LoyaltySettings
                {
                    EnableLoyaltyProgram = false,
                    PointsPerDollar = 1.0m,
                    PointValue = 0.01m,
                    MinPointsForRedemption = 100
                }
            },
            Payments = new PaymentSettings
            {
                EnabledMethods = new List<PaymentMethod>
                {
                    new() { Name = "Cash", Type = "Cash", IsEnabled = true },
                    new() { Name = "Credit Card", Type = "Card", IsEnabled = true },
                    new() { Name = "Debit Card", Type = "Card", IsEnabled = true }
                },
                Discounts = new DiscountSettings
                {
                    MaxDiscountPercentage = 50m,
                    RequireManagerApproval = true,
                    AllowStackingDiscounts = false
                },
                Surcharges = new SurchargeSettings
                {
                    EnableSurcharges = false,
                    CardSurchargePercentage = 2.5m,
                    ShowSurchargeOnReceipt = true
                },
                SplitPayments = new SplitPaymentSettings
                {
                    EnableSplitPayments = true,
                    MaxSplitCount = 4,
                    AllowUnevenSplits = true
                }
            },
            Printers = new PrinterSettings
            {
                Receipt = new ReceiptPrinterSettings
                {
                    DefaultPrinter = "",
                    FallbackPrinter = "",
                    PaperSize = "80mm",
                    AutoPrintOnPayment = true,
                    PreviewBeforePrint = true,
                    PrintProForma = true,
                    PrintFinalReceipt = true,
                    CopiesForFinalReceipt = 1,
                    PrintCustomerCopy = true,
                    PrintMerchantCopy = true
                },
                Kitchen = new KitchenPrinterSettings
                {
                    CopiesPerOrder = 1,
                    PrintOrderNumbers = true,
                    PrintTimestamps = true,
                    PrintSpecialInstructions = true
                },
                Devices = new PrinterDeviceSettings
                {
                    AvailablePrinters = new List<PrinterDevice>(),
                    AutoDetectPrinters = true,
                    DefaultComPort = "COM1",
                    DefaultBaudRate = 9600
                },
                Jobs = new PrintJobSettings
                {
                    MaxRetries = 3,
                    TimeoutMs = 5000,
                    LogPrintJobs = true,
                    QueueFailedJobs = true,
                    MaxQueueSize = 50
                }
            },
            Notifications = new NotificationSettings
            {
                Email = new EmailSettings
                {
                    EnableEmail = false,
                    SmtpServer = "",
                    SmtpPort = 587,
                    Username = "",
                    Password = "",
                    UseSsl = true,
                    FromAddress = "",
                    FromName = ""
                },
                Sms = new SmsSettings
                {
                    EnableSms = false,
                    Provider = "Twilio",
                    ApiKey = "",
                    ApiSecret = "",
                    FromNumber = ""
                },
                Push = new PushSettings
                {
                    EnablePush = true,
                    ShowStockAlerts = true,
                    ShowOrderAlerts = true,
                    ShowPaymentAlerts = true,
                    ShowSystemAlerts = true
                },
                Alerts = new AlertSettings
                {
                    EnableSoundAlerts = true,
                    AlertVolume = 50,
                    AlertSoundPath = "",
                    ThresholdAlerts = new List<ThresholdAlert>()
                }
            },
            Security = new SecuritySettings
            {
                Rbac = new RbacSettings
                {
                    EnforceRolePermissions = true,
                    AllowRoleInheritance = true,
                    RequireManagerOverride = true,
                    RestrictedOperations = new List<string>()
                },
                Login = new LoginSettings
                {
                    MaxLoginAttempts = 5,
                    LockoutDurationMinutes = 15,
                    RequireStrongPasswords = true,
                    PasswordExpiryDays = 90,
                    EnableTwoFactor = false
                },
                Sessions = new SessionSettings
                {
                    SessionTimeoutMinutes = 60,
                    MaxConcurrentSessions = 3,
                    RequireReauthForSensitive = true
                },
                Audit = new AuditSettings
                {
                    EnableAuditLogging = true,
                    LogUserActions = true,
                    LogSystemEvents = true,
                    RetentionDays = 90
                }
            },
            Integrations = new IntegrationSettings
            {
                PaymentGateways = new PaymentGatewaySettings
                {
                    DefaultGateway = "Stripe",
                    EnableTestMode = true,
                    Gateways = new List<PaymentGateway>()
                },
                Webhooks = new WebhookSettings
                {
                    EnableWebhooks = false,
                    TimeoutMs = 10000,
                    MaxRetries = 3,
                    Endpoints = new List<WebhookEndpoint>()
                },
                Crm = new CrmSettings
                {
                    EnableCrmSync = false,
                    CrmProvider = "Salesforce",
                    ApiEndpoint = "",
                    ApiKey = "",
                    SyncCustomers = false,
                    SyncOrders = false,
                    SyncIntervalMinutes = 60
                },
                Api = new ApiSettings
                {
                    DefaultTimeoutMs = 10000,
                    DefaultRetries = 3,
                    EnableApiLogging = true,
                    Endpoints = new List<ApiEndpoint>
                    {
                        new() { Name = "MenuApi", BaseUrl = "https://magidesk-menu-904541739138.northamerica-south1.run.app", IsEnabled = true },
                        new() { Name = "OrderApi", BaseUrl = "https://magidesk-order-904541739138.northamerica-south1.run.app", IsEnabled = true },
                        new() { Name = "PaymentApi", BaseUrl = "https://magidesk-payment-904541739138.northamerica-south1.run.app", IsEnabled = true },
                        new() { Name = "InventoryApi", BaseUrl = "https://magidesk-inventory-904541739138.northamerica-south1.run.app", IsEnabled = true },
                        new() { Name = "TablesApi", BaseUrl = "https://magidesk-tables-904541739138.northamerica-south1.run.app", IsEnabled = true },
                        new() { Name = "UsersApi", BaseUrl = "https://magidesk-users-23sbzjsxaq-pv.a.run.app", IsEnabled = true },
                        new() { Name = "SettingsApi", BaseUrl = "https://magidesk-settings-904541739138.us-central1.run.app", IsEnabled = true },
                        new() { Name = "VendorOrdersApi", BaseUrl = "https://magidesk-vendororders-904541739138.northamerica-south1.run.app", IsEnabled = true }
                    }
                }
            },
            System = new SystemSettings
            {
                Logging = new LoggingSettings
                {
                    LogLevel = "Information",
                    EnableFileLogging = true,
                    EnableConsoleLogging = true,
                    LogFilePath = "logs/magidesk.log",
                    MaxLogFileSizeMB = 10,
                    MaxLogFiles = 10,
                    LogRetentionDays = 30
                },
                Tracing = new TracingSettings
                {
                    EnableTracing = false,
                    TraceApiCalls = false,
                    TraceDbQueries = false,
                    TraceUserActions = false,
                    TracingEndpoint = ""
                },
                BackgroundJobs = new BackgroundJobSettings
                {
                    EnableBackgroundJobs = true,
                    HeartbeatIntervalMinutes = 5,
                    CleanupIntervalMinutes = 60,
                    BackupIntervalMinutes = 240
                },
                Performance = new PerformanceSettings
                {
                    DatabaseConnectionPoolSize = 50,
                    CacheExpirationMinutes = 15,
                    EnableResponseCompression = true,
                    EnableResponseCaching = true,
                    MaxConcurrentRequests = 1000
                }
            }
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

    private async Task LogAuditEntryAsync(string hostKey, string action, string description, string? category = null, string? changedBy = null, object? changes = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            const string sql = @"
                INSERT INTO settings.settings_audit (host_key, action, description, category, changes_json, changed_by, created_at, ip_address, user_agent)
                VALUES (@hostKey, @action, @description, @category, @changesJson, @changedBy, @createdAt, @ipAddress, @userAgent)";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("hostKey", hostKey);
            command.Parameters.AddWithValue("action", action);
            command.Parameters.AddWithValue("description", description);
            command.Parameters.AddWithValue("category", category ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("changesJson", changes != null ? JsonSerializer.Serialize(changes, _jsonOptions) : (object)DBNull.Value);
            command.Parameters.AddWithValue("changedBy", changedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("ipAddress", ipAddress ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("userAgent", userAgent ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry: {Action} - {Description}", action, description);
        }
    }

    private static string GetHostKey(string? hostOverride)
    {
        if (!string.IsNullOrWhiteSpace(hostOverride)) return hostOverride;
        try { return System.Net.Dns.GetHostName(); }
        catch { return Environment.MachineName; }
    }

    private Dictionary<string, object> CompareSettings(HierarchicalSettings existing, HierarchicalSettings updated)
    {
        var changes = new Dictionary<string, object>();
        
        try
        {
            // Compare each category
            var existingJson = JsonSerializer.Serialize(existing, _jsonOptions);
            var updatedJson = JsonSerializer.Serialize(updated, _jsonOptions);
            
            if (existingJson != updatedJson)
            {
                changes["summary"] = "Settings have been modified";
                changes["timestamp"] = DateTime.UtcNow;
                
                // Compare individual categories
                if (JsonSerializer.Serialize(existing.General, _jsonOptions) != JsonSerializer.Serialize(updated.General, _jsonOptions))
                    changes["general"] = "General settings modified";
                    
                if (JsonSerializer.Serialize(existing.Pos, _jsonOptions) != JsonSerializer.Serialize(updated.Pos, _jsonOptions))
                    changes["pos"] = "POS settings modified";
                    
                if (JsonSerializer.Serialize(existing.Inventory, _jsonOptions) != JsonSerializer.Serialize(updated.Inventory, _jsonOptions))
                    changes["inventory"] = "Inventory settings modified";
                    
                if (JsonSerializer.Serialize(existing.Customers, _jsonOptions) != JsonSerializer.Serialize(updated.Customers, _jsonOptions))
                    changes["customers"] = "Customer settings modified";
                    
                if (JsonSerializer.Serialize(existing.Payments, _jsonOptions) != JsonSerializer.Serialize(updated.Payments, _jsonOptions))
                    changes["payments"] = "Payment settings modified";
                    
                if (JsonSerializer.Serialize(existing.Printers, _jsonOptions) != JsonSerializer.Serialize(updated.Printers, _jsonOptions))
                    changes["printers"] = "Printer settings modified";
                    
                if (JsonSerializer.Serialize(existing.Notifications, _jsonOptions) != JsonSerializer.Serialize(updated.Notifications, _jsonOptions))
                    changes["notifications"] = "Notification settings modified";
                    
                if (JsonSerializer.Serialize(existing.Security, _jsonOptions) != JsonSerializer.Serialize(updated.Security, _jsonOptions))
                    changes["security"] = "Security settings modified";
                    
                if (JsonSerializer.Serialize(existing.Integrations, _jsonOptions) != JsonSerializer.Serialize(updated.Integrations, _jsonOptions))
                    changes["integrations"] = "Integration settings modified";
                    
                if (JsonSerializer.Serialize(existing.System, _jsonOptions) != JsonSerializer.Serialize(updated.System, _jsonOptions))
                    changes["system"] = "System settings modified";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare settings");
            changes["error"] = "Failed to compare settings";
        }
        
        return changes;
    }

    public async Task<List<SettingsAuditEntry>> GetAuditHistoryAsync(string? hostKey = null, string? category = null, int limit = 100, CancellationToken ct = default)
    {
        try
        {
            var host = GetHostKey(hostKey);
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                SELECT id, host_key, action, description, category, changes_json, changed_by, created_at, ip_address, user_agent
                FROM settings.settings_audit 
                WHERE host_key = @hostKey";
            
            if (!string.IsNullOrEmpty(category))
            {
                sql += " AND category = @category";
            }
            
            sql += " ORDER BY created_at DESC LIMIT @limit";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("hostKey", host);
            command.Parameters.AddWithValue("limit", limit);
            
            if (!string.IsNullOrEmpty(category))
            {
                command.Parameters.AddWithValue("category", category);
            }

            var auditEntries = new List<SettingsAuditEntry>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            
            while (await reader.ReadAsync(ct))
            {
                var entry = new SettingsAuditEntry
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    HostKey = reader.GetString(reader.GetOrdinal("host_key")),
                    Action = reader.GetString(reader.GetOrdinal("action")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString(reader.GetOrdinal("category")),
                    ChangedBy = reader.IsDBNull(reader.GetOrdinal("changed_by")) ? null : reader.GetString(reader.GetOrdinal("changed_by")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString(reader.GetOrdinal("ip_address")),
                    UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? null : reader.GetString(reader.GetOrdinal("user_agent"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("changes_json")))
                {
                    var changesJson = reader.GetString(reader.GetOrdinal("changes_json"));
                    entry.Changes = JsonSerializer.Deserialize<Dictionary<string, object>>(changesJson, _jsonOptions);
                }

                auditEntries.Add(entry);
            }

            return auditEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit history for host {HostKey}", hostKey);
            return new List<SettingsAuditEntry>();
        }
    }

    public async Task<List<SettingsAuditEntry>> GetAuditHistoryByUserAsync(string userId, string? hostKey = null, int limit = 100, CancellationToken ct = default)
    {
        try
        {
            var host = GetHostKey(hostKey);
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            const string sql = @"
                SELECT id, host_key, action, description, category, changes_json, changed_by, created_at, ip_address, user_agent
                FROM settings.settings_audit 
                WHERE host_key = @hostKey AND changed_by = @userId
                ORDER BY created_at DESC LIMIT @limit";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("hostKey", host);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("limit", limit);

            var auditEntries = new List<SettingsAuditEntry>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            
            while (await reader.ReadAsync(ct))
            {
                var entry = new SettingsAuditEntry
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    HostKey = reader.GetString(reader.GetOrdinal("host_key")),
                    Action = reader.GetString(reader.GetOrdinal("action")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString(reader.GetOrdinal("category")),
                    ChangedBy = reader.IsDBNull(reader.GetOrdinal("changed_by")) ? null : reader.GetString(reader.GetOrdinal("changed_by")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString(reader.GetOrdinal("ip_address")),
                    UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? null : reader.GetString(reader.GetOrdinal("user_agent"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("changes_json")))
                {
                    var changesJson = reader.GetString(reader.GetOrdinal("changes_json"));
                    entry.Changes = JsonSerializer.Deserialize<Dictionary<string, object>>(changesJson, _jsonOptions);
                }

                auditEntries.Add(entry);
            }

            return auditEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit history for user {UserId} and host {HostKey}", userId, hostKey);
            return new List<SettingsAuditEntry>();
        }
    }

    public async Task<bool> BulkResetSettingsAsync(List<string> categories, string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var host = GetHostKey(hostKey);
            var defaults = GetDefaultSettings();
            
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            try
            {
                var resetCategories = new List<string>();
                
                foreach (var category in categories)
                {
                    object? settingsData = category switch
                    {
                        "general" => defaults.General,
                        "pos" => defaults.Pos,
                        "inventory" => defaults.Inventory,
                        "customers" => defaults.Customers,
                        "payments" => defaults.Payments,
                        "printers" => defaults.Printers,
                        "notifications" => defaults.Notifications,
                        "security" => defaults.Security,
                        "integrations" => defaults.Integrations,
                        "system" => defaults.System,
                        _ => null
                    };

                    if (settingsData != null)
                    {
                        var settingsJson = JsonSerializer.Serialize(settingsData, _jsonOptions);
                        await SaveCategoryAsync(connection, transaction, host, category, settingsData, ct);
                        resetCategories.Add(category);
                    }
                }

                // Log audit entry
                await LogAuditEntryAsync(host, "bulk_reset", 
                    $"Bulk reset completed for categories: {string.Join(", ", resetCategories)}", 
                    category: "bulk",
                    changedBy: "system", // TODO: Get from authentication context
                    changes: new { categories = resetCategories },
                    ct: ct);

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
            _logger.LogError(ex, "Failed to bulk reset settings for host {HostKey}", hostKey);
            return false;
        }
    }

    public async Task<string> BulkExportSettingsAsync(List<string> categories, string? hostKey = null, CancellationToken ct = default)
    {
        try
        {
            var host = GetHostKey(hostKey);
            var settings = await GetSettingsAsync(host, ct);
            
            if (settings == null)
            {
                settings = GetDefaultSettings();
            }

            // Create filtered settings based on requested categories
            var filteredSettings = new HierarchicalSettings
            {
                General = categories.Contains("general") ? settings.General : new GeneralSettings(),
                Pos = categories.Contains("pos") ? settings.Pos : new PosSettings(),
                Inventory = categories.Contains("inventory") ? settings.Inventory : new InventorySettings(),
                Customers = categories.Contains("customers") ? settings.Customers : new CustomerSettings(),
                Payments = categories.Contains("payments") ? settings.Payments : new PaymentSettings(),
                Printers = categories.Contains("printers") ? settings.Printers : new PrinterSettings(),
                Notifications = categories.Contains("notifications") ? settings.Notifications : new NotificationSettings(),
                Security = categories.Contains("security") ? settings.Security : new SecuritySettings(),
                Integrations = categories.Contains("integrations") ? settings.Integrations : new IntegrationSettings(),
                System = categories.Contains("system") ? settings.System : new SystemSettings()
            };

            var json = JsonSerializer.Serialize(filteredSettings, _jsonOptions);
            
            // Log audit entry
            await LogAuditEntryAsync(host, "bulk_export", 
                $"Bulk export completed for categories: {string.Join(", ", categories)}", 
                category: "bulk",
                changedBy: "system", // TODO: Get from authentication context
                changes: new { categories = categories },
                ct: ct);

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk export settings for host {HostKey}", hostKey);
            throw;
        }
    }

    public async Task<Dictionary<string, List<string>>> BulkValidateSettingsAsync(HierarchicalSettings settings, CancellationToken ct = default)
    {
        var validationResults = new Dictionary<string, List<string>>();
        
        try
        {
            // Validate each category
            var categories = new (string, object)[]
            {
                ("general", settings.General),
                ("pos", settings.Pos),
                ("inventory", settings.Inventory),
                ("customers", settings.Customers),
                ("payments", settings.Payments),
                ("printers", settings.Printers),
                ("notifications", settings.Notifications),
                ("security", settings.Security),
                ("integrations", settings.Integrations),
                ("system", settings.System)
            };

            foreach ((string categoryName, object categorySettings) in categories)
            {
                var errors = new List<string>();
                
                try
                {
                    // Validate the specific category
                    var isValid = await ValidateCategoryAsync(categoryName, categorySettings, ct);
                    if (!isValid)
                    {
                        errors.Add($"Validation failed for {categoryName} category");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error validating {categoryName}: {ex.Message}");
                }

                if (errors.Any())
                {
                    validationResults[categoryName] = errors;
                }
            }

            // Log audit entry
            var totalErrors = validationResults.Values.Sum(e => e.Count);
            await LogAuditEntryAsync(GetHostKey(null), "bulk_validation", 
                $"Bulk validation completed with {totalErrors} errors across {validationResults.Count} categories", 
                category: "bulk",
                changedBy: "system", // TODO: Get from authentication context
                changes: new { 
                    totalErrors = totalErrors,
                    categoriesWithErrors = validationResults.Keys.ToList(),
                    errorCounts = validationResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count)
                },
                ct: ct);

            return validationResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk validate settings");
            validationResults["system"] = new List<string> { $"Bulk validation failed: {ex.Message}" };
            return validationResults;
        }
    }

    private async Task<bool> ValidateCategoryAsync(string categoryName, object categorySettings, CancellationToken ct)
    {
        try
        {
            // This is a simplified validation - in a real implementation,
            // you would have specific validation logic for each category
            return categorySettings != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValidateUserAccessAsync(string category, string userId, string action, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            const string sql = @"
                SELECT 
                    CASE 
                        WHEN @action = 'view' THEN can_view
                        WHEN @action = 'edit' THEN can_edit
                        ELSE false
                    END as has_access
                FROM settings.user_category_access 
                WHERE user_id = @userId AND category = @category
                UNION ALL
                SELECT 
                    CASE 
                        WHEN @action = 'view' THEN true
                        WHEN @action = 'edit' THEN false
                        ELSE false
                    END as has_access
                WHERE NOT EXISTS (
                    SELECT 1 FROM settings.user_category_access 
                    WHERE user_id = @userId AND category = @category
                ) AND @category IN ('general', 'pos', 'printers')";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("category", category);
            command.Parameters.AddWithValue("action", action);

            var result = await command.ExecuteScalarAsync(ct);
            return result is bool hasAccess && hasAccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate user access for category {Category}, user {UserId}, action {Action}", 
                category, userId, action);
            return false;
        }
    }

    public async Task<List<string>> GetUserAccessibleCategoriesAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            const string sql = @"
                SELECT category 
                FROM settings.user_category_access 
                WHERE user_id = @userId AND can_view = true
                UNION
                SELECT category 
                FROM (VALUES ('general'), ('pos'), ('printers')) AS default_categories(category)
                WHERE NOT EXISTS (
                    SELECT 1 FROM settings.user_category_access 
                    WHERE user_id = @userId AND category = default_categories.category
                )";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", userId);

            var categories = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                categories.Add(reader.GetString(reader.GetOrdinal("category")));
            }

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get accessible categories for user {UserId}", userId);
            return new List<string> { "general", "pos", "printers" }; // Default accessible categories
        }
    }

    public async Task<bool> SetUserCategoryAccessAsync(string userId, string category, bool canView, bool canEdit, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            try
            {
                const string sql = @"
                    INSERT INTO settings.user_category_access (user_id, category, can_view, can_edit, updated_at)
                    VALUES (@userId, @category, @canView, @canEdit, @updatedAt)
                    ON CONFLICT (user_id, category) 
                    DO UPDATE SET 
                        can_view = @canView,
                        can_edit = @canEdit,
                        updated_at = @updatedAt";

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("category", category);
                command.Parameters.AddWithValue("canView", canView);
                command.Parameters.AddWithValue("canEdit", canEdit);
                command.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync(ct);
                await LogAuditEntryAsync(connection, transaction, GetHostKey(null), "access_updated", 
                    $"User {userId} access updated for category {category}", ct);

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
            _logger.LogError(ex, "Failed to set user category access for user {UserId}, category {Category}", userId, category);
            return false;
        }
    }

    #endregion

    public async Task<Dictionary<string, object>> TestConnectionsAsync(HierarchicalSettings settings, CancellationToken ct = default)
    {
        var results = new Dictionary<string, object>();
        
        try
        {
            // Test database connection
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            results["database"] = new { status = "connected", timestamp = DateTime.UtcNow };
            
            // Test email settings if configured
            if (settings.Notifications.Email.EnableEmail && !string.IsNullOrEmpty(settings.Notifications.Email.SmtpServer))
            {
                results["email"] = new { status = "configured", server = settings.Notifications.Email.SmtpServer };
            }
            else
            {
                results["email"] = new { status = "not_configured" };
            }
            
            // Test SMS settings if configured
            if (settings.Notifications.Sms.EnableSms && !string.IsNullOrEmpty(settings.Notifications.Sms.ApiKey))
            {
                results["sms"] = new { status = "configured", provider = settings.Notifications.Sms.Provider };
            }
            else
            {
                results["sms"] = new { status = "not_configured" };
            }
            
            // Test payment gateway settings
            var enabledGateways = settings.Integrations.PaymentGateways.Gateways.Where(g => g.IsEnabled).ToList();
            results["payment_gateways"] = new { 
                status = enabledGateways.Any() ? "configured" : "not_configured",
                count = enabledGateways.Count 
            };
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connections");
            results["error"] = ex.Message;
            return results;
        }
    }
}

public class SettingsAuditEntry
{
    public long Id { get; set; }
    public string HostKey { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? Changes { get; set; }
}
