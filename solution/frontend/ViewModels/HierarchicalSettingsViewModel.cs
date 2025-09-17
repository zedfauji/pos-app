using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Shared.DTOs.Settings;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SharedInventorySettings = MagiDesk.Shared.DTOs.Settings.InventorySettings;

namespace MagiDesk.Frontend.ViewModels;

public class HierarchicalSettingsViewModel : INotifyPropertyChanged
{
    private readonly HierarchicalSettingsApiService _settingsApi;
    private readonly ILogger<HierarchicalSettingsViewModel> _logger;
    
    private HierarchicalSettings _settings;
    private HierarchicalSettings _originalSettings;
    private bool _isLoading;
    private string _statusMessage = "Ready";
    private string _hostKey = "default";

    public HierarchicalSettingsViewModel()
    {
        _logger = NullLoggerFactory.Create<HierarchicalSettingsViewModel>();
        _settingsApi = new HierarchicalSettingsApiService();
        _settings = new HierarchicalSettings();
        _originalSettings = new HierarchicalSettings();
    }

    #region Properties

    public HierarchicalSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string HostKey
    {
        get => _hostKey;
        set => SetProperty(ref _hostKey, value);
    }

    public bool HasUnsavedChanges => !AreSettingsEqual(_settings, _originalSettings);

    public bool HasCategoryChanges { get; private set; }

    #endregion

    #region Public Methods

    public async Task LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading settings...";

            _settings = await _settingsApi.GetSettingsAsync(_hostKey);
            _originalSettings = CloneSettings(_settings);
            
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            
            StatusMessage = "Settings loaded successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load hierarchical settings");
            StatusMessage = $"Error loading settings: {ex.Message}";
            
            // Load defaults on error
            _settings = GetDefaultSettings();
            _originalSettings = CloneSettings(_settings);
            OnPropertyChanged(nameof(Settings));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> SaveAllSettingsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Saving all settings...";

            var success = await _settingsApi.SaveSettingsAsync(_settings, _hostKey);
            
            if (success)
            {
                _originalSettings = CloneSettings(_settings);
                OnPropertyChanged(nameof(HasUnsavedChanges));
                StatusMessage = "All settings saved successfully";
                return true;
            }
            else
            {
                StatusMessage = "Failed to save settings";
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save all settings");
            StatusMessage = $"Error saving settings: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> SaveCategoryAsync(string category)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Saving {category} settings...";

            var categorySettings = GetCategorySettings(category);
            if (categorySettings == null)
            {
                StatusMessage = $"Invalid category: {category}";
                return false;
            }

            var success = await _settingsApi.SaveSettingsCategoryAsync(category, categorySettings, _hostKey);
            
            if (success)
            {
                // Update original settings for this category
                SetCategoryInOriginalSettings(category, categorySettings);
                OnPropertyChanged(nameof(HasUnsavedChanges));
                StatusMessage = $"{category} settings saved successfully";
                return true;
            }
            else
            {
                StatusMessage = $"Failed to save {category} settings";
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save category {Category}", category);
            StatusMessage = $"Error saving {category} settings: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ResetToDefaultsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Resetting to defaults...";

            var success = await _settingsApi.ResetToDefaultsAsync(_hostKey);
            
            if (success)
            {
                await LoadSettingsAsync();
                StatusMessage = "Settings reset to defaults successfully";
                return true;
            }
            else
            {
                StatusMessage = "Failed to reset settings";
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings to defaults");
            StatusMessage = $"Error resetting settings: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ResetCategoryAsync(string category)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Resetting {category} to defaults...";

            var defaultSettings = GetDefaultSettings();
            var defaultCategorySettings = GetCategorySettings(category, defaultSettings);
            
            if (defaultCategorySettings == null)
            {
                StatusMessage = $"Invalid category: {category}";
                return false;
            }

            var success = await _settingsApi.SaveSettingsCategoryAsync(category, defaultCategorySettings, _hostKey);
            
            if (success)
            {
                SetCategorySettings(category, defaultCategorySettings);
                SetCategoryInOriginalSettings(category, defaultCategorySettings);
                OnPropertyChanged(nameof(Settings));
                OnPropertyChanged(nameof(HasUnsavedChanges));
                StatusMessage = $"{category} settings reset successfully";
                return true;
            }
            else
            {
                StatusMessage = $"Failed to reset {category} settings";
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset category {Category}", category);
            StatusMessage = $"Error resetting {category} settings: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<Dictionary<string, bool>> TestConnectionsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Testing connections...";

            var results = await _settingsApi.TestConnectionsAsync(_settings);
            
            StatusMessage = "Connection tests completed";
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connections");
            StatusMessage = $"Error testing connections: {ex.Message}";
            return new Dictionary<string, bool>();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public object? GetCategorySettings(string category)
    {
        return GetCategorySettings(category, _settings);
    }

    public void UpdateCategorySettings(string category, object settings)
    {
        SetCategorySettings(category, settings);
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        HasCategoryChanges = true;
        OnPropertyChanged(nameof(HasCategoryChanges));
    }

    public async Task<string> ExportSettingsToJsonAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Exporting settings...";

            var json = await _settingsApi.ExportSettingsToJsonAsync(_hostKey);
            
            StatusMessage = "Settings exported successfully";
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings to JSON");
            StatusMessage = $"Error exporting settings: {ex.Message}";
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ImportSettingsFromFileAsync(string filePath)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Importing settings...";

            var success = await _settingsApi.ImportSettingsFromFileAsync(filePath, _hostKey);
            
            if (success)
            {
                await LoadSettingsAsync();
                StatusMessage = "Settings imported successfully";
            }
            else
            {
                StatusMessage = "Failed to import settings";
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import settings from file {FilePath}", filePath);
            StatusMessage = $"Error importing settings: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Methods

    private object? GetCategorySettings(string category, HierarchicalSettings? settings = null)
    {
        var source = settings ?? _settings;
        
        return category.ToLowerInvariant() switch
        {
            "general" => source.General,
            "pos" => source.Pos,
            "inventory" => source.Inventory,
            "customers" => source.Customers,
            "payments" => source.Payments,
            "printers" => source.Printers,
            "notifications" => source.Notifications,
            "security" => source.Security,
            "integrations" => source.Integrations,
            "system" => source.System,
            _ => null
        };
    }

    private void SetCategorySettings(string category, object settings)
    {
        switch (category.ToLowerInvariant())
        {
            case "general":
                if (settings is GeneralSettings general) _settings.General = general;
                break;
            case "pos":
                if (settings is PosSettings pos) _settings.Pos = pos;
                break;
            case "inventory":
                if (settings is SharedInventorySettings inventory) _settings.Inventory = inventory;
                break;
            case "customers":
                if (settings is CustomerSettings customers) _settings.Customers = customers;
                break;
            case "payments":
                if (settings is PaymentSettings payments) _settings.Payments = payments;
                break;
            case "printers":
                if (settings is PrinterSettings printers) _settings.Printers = printers;
                break;
            case "notifications":
                if (settings is NotificationSettings notifications) _settings.Notifications = notifications;
                break;
            case "security":
                if (settings is SecuritySettings security) _settings.Security = security;
                break;
            case "integrations":
                if (settings is IntegrationSettings integrations) _settings.Integrations = integrations;
                break;
            case "system":
                if (settings is SystemSettings system) _settings.System = system;
                break;
        }
    }

    private void SetCategoryInOriginalSettings(string category, object settings)
    {
        switch (category.ToLowerInvariant())
        {
            case "general":
                if (settings is GeneralSettings general) _originalSettings.General = CloneObject(general);
                break;
            case "pos":
                if (settings is PosSettings pos) _originalSettings.Pos = CloneObject(pos);
                break;
            case "inventory":
                if (settings is SharedInventorySettings inventory) _originalSettings.Inventory = CloneObject(inventory);
                break;
            case "customers":
                if (settings is CustomerSettings customers) _originalSettings.Customers = CloneObject(customers);
                break;
            case "payments":
                if (settings is PaymentSettings payments) _originalSettings.Payments = CloneObject(payments);
                break;
            case "printers":
                if (settings is PrinterSettings printers) _originalSettings.Printers = CloneObject(printers);
                break;
            case "notifications":
                if (settings is NotificationSettings notifications) _originalSettings.Notifications = CloneObject(notifications);
                break;
            case "security":
                if (settings is SecuritySettings security) _originalSettings.Security = CloneObject(security);
                break;
            case "integrations":
                if (settings is IntegrationSettings integrations) _originalSettings.Integrations = CloneObject(integrations);
                break;
            case "system":
                if (settings is SystemSettings system) _originalSettings.System = CloneObject(system);
                break;
        }
    }

    private static HierarchicalSettings CloneSettings(HierarchicalSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        return JsonSerializer.Deserialize<HierarchicalSettings>(json) ?? new HierarchicalSettings();
    }

    private static T CloneObject<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    private static bool AreSettingsEqual(HierarchicalSettings settings1, HierarchicalSettings settings2)
    {
        try
        {
            var json1 = JsonSerializer.Serialize(settings1);
            var json2 = JsonSerializer.Serialize(settings2);
            return json1 == json2;
        }
        catch
        {
            return false;
        }
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

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

