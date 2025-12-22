using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsApiService _settingsApi;
    private readonly ILogger<SettingsViewModel> _logger;
    private bool _isLoading;
    private string _statusMessage = "Ready";

    public SettingsViewModel(SettingsApiService settingsApi, ILogger<SettingsViewModel> logger)
    {
        _settingsApi = settingsApi;
        _logger = logger;
    }

    #region Loading and Status
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
    #endregion

    #region General Settings
    private string _hostKey = "";
    public string HostKey
    {
        get => _hostKey;
        set => SetProperty(ref _hostKey, value);
    }

    private string _theme = "System";
    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    private decimal _ratePerMinute = 0.50m;
    public decimal RatePerMinute
    {
        get => _ratePerMinute;
        set => SetProperty(ref _ratePerMinute, value);
    }

    private string _locale = "en-US";
    public string Locale
    {
        get => _locale;
        set => SetProperty(ref _locale, value);
    }

    private bool _enableNotifications = true;
    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => SetProperty(ref _enableNotifications, value);
    }
    #endregion

    #region API Connection Settings
    private string _backendApiUrl = "";
    public string BackendApiUrl
    {
        get => _backendApiUrl;
        set => SetProperty(ref _backendApiUrl, value);
    }

    private string _settingsApiUrl = "";
    public string SettingsApiUrl
    {
        get => _settingsApiUrl;
        set => SetProperty(ref _settingsApiUrl, value);
    }

    private string _tablesApiUrl = "";
    public string TablesApiUrl
    {
        get => _tablesApiUrl;
        set => SetProperty(ref _tablesApiUrl, value);
    }

    private string _menuApiUrl = "";
    public string MenuApiUrl
    {
        get => _menuApiUrl;
        set => SetProperty(ref _menuApiUrl, value);
    }

    private string _orderApiUrl = "";
    public string OrderApiUrl
    {
        get => _orderApiUrl;
        set => SetProperty(ref _orderApiUrl, value);
    }

    private string _paymentApiUrl = "";
    public string PaymentApiUrl
    {
        get => _paymentApiUrl;
        set => SetProperty(ref _paymentApiUrl, value);
    }

    private string _inventoryApiUrl = "";
    public string InventoryApiUrl
    {
        get => _inventoryApiUrl;
        set => SetProperty(ref _inventoryApiUrl, value);
    }

    private string _vendorOrdersApiUrl = "";
    public string VendorOrdersApiUrl
    {
        get => _vendorOrdersApiUrl;
        set => SetProperty(ref _vendorOrdersApiUrl, value);
    }
    #endregion

    #region Business/Receipt Settings
    private string _businessName = "MagiDesk Billiard Club";
    public string BusinessName
    {
        get => _businessName;
        set => SetProperty(ref _businessName, value);
    }

    private string _businessAddress = "123 Main Street, City, State 12345";
    public string BusinessAddress
    {
        get => _businessAddress;
        set => SetProperty(ref _businessAddress, value);
    }

    private string _businessPhone = "(555) 123-4567";
    public string BusinessPhone
    {
        get => _businessPhone;
        set => SetProperty(ref _businessPhone, value);
    }

    private string _defaultPrinter = "";
    public string DefaultPrinter
    {
        get => _defaultPrinter;
        set => SetProperty(ref _defaultPrinter, value);
    }

    private string _receiptSize = "80mm";
    public string ReceiptSize
    {
        get => _receiptSize;
        set => SetProperty(ref _receiptSize, value);
    }

    private bool _autoPrintOnPayment = true;
    public bool AutoPrintOnPayment
    {
        get => _autoPrintOnPayment;
        set => SetProperty(ref _autoPrintOnPayment, value);
    }

    private bool _previewBeforePrint = true;
    public bool PreviewBeforePrint
    {
        get => _previewBeforePrint;
        set => SetProperty(ref _previewBeforePrint, value);
    }

    private bool _printProForma = true;
    public bool PrintProForma
    {
        get => _printProForma;
        set => SetProperty(ref _printProForma, value);
    }

    private bool _printFinalReceipt = true;
    public bool PrintFinalReceipt
    {
        get => _printFinalReceipt;
        set => SetProperty(ref _printFinalReceipt, value);
    }

    private int _copiesForFinalReceipt = 2;
    public int CopiesForFinalReceipt
    {
        get => _copiesForFinalReceipt;
        set => SetProperty(ref _copiesForFinalReceipt, value);
    }
    #endregion

    #region Table Settings
    private int _warnMinutes = 30;
    public int WarnMinutes
    {
        get => _warnMinutes;
        set => SetProperty(ref _warnMinutes, value);
    }

    private int _autoStopMinutes = 120;
    public int AutoStopMinutes
    {
        get => _autoStopMinutes;
        set => SetProperty(ref _autoStopMinutes, value);
    }

    private decimal _taxPercent = 8.5m;
    public decimal TaxPercent
    {
        get => _taxPercent;
        set => SetProperty(ref _taxPercent, value);
    }
    #endregion

    #region API Status
    private bool _backendApiStatus = false;
    public bool BackendApiStatus
    {
        get => _backendApiStatus;
        set => SetProperty(ref _backendApiStatus, value);
    }

    private bool _settingsApiStatus = false;
    public bool SettingsApiStatus
    {
        get => _settingsApiStatus;
        set => SetProperty(ref _settingsApiStatus, value);
    }

    private bool _tablesApiStatus = false;
    public bool TablesApiStatus
    {
        get => _tablesApiStatus;
        set => SetProperty(ref _tablesApiStatus, value);
    }

    private bool _menuApiStatus = false;
    public bool MenuApiStatus
    {
        get => _menuApiStatus;
        set => SetProperty(ref _menuApiStatus, value);
    }

    private bool _orderApiStatus = false;
    public bool OrderApiStatus
    {
        get => _orderApiStatus;
        set => SetProperty(ref _orderApiStatus, value);
    }

    private bool _paymentApiStatus = false;
    public bool PaymentApiStatus
    {
        get => _paymentApiStatus;
        set => SetProperty(ref _paymentApiStatus, value);
    }

    private bool _inventoryApiStatus = false;
    public bool InventoryApiStatus
    {
        get => _inventoryApiStatus;
        set => SetProperty(ref _inventoryApiStatus, value);
    }

    private bool _vendorOrdersApiStatus = false;
    public bool VendorOrdersApiStatus
    {
        get => _vendorOrdersApiStatus;
        set => SetProperty(ref _vendorOrdersApiStatus, value);
    }
    #endregion

    #region Commands
    public async Task LoadSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading settings...";
        
        try
        {
            // Try to load settings from API, but handle offline mode gracefully
            try
            {
                // Load Frontend settings
                var frontendSettings = await _settingsApi.GetFrontendAsync(HostKey);
                if (frontendSettings != null)
                {
                    if (frontendSettings.TryGetValue("theme", out var theme))
                        Theme = theme?.ToString() ?? "System";
                    if (frontendSettings.TryGetValue("ratePerMinute", out var rate) && 
                        decimal.TryParse(rate?.ToString(), out var rateValue))
                        RatePerMinute = rateValue;
                }

                // Load Backend settings
                var backendSettings = await _settingsApi.GetBackendAsync(HostKey);
            if (backendSettings != null)
            {
                if (backendSettings.TryGetValue("backendApiUrl", out var url))
                    BackendApiUrl = url?.ToString() ?? "";
                if (backendSettings.TryGetValue("settingsApiUrl", out var settingsUrl))
                    SettingsApiUrl = settingsUrl?.ToString() ?? "";
                if (backendSettings.TryGetValue("tablesApiUrl", out var tablesUrl))
                    TablesApiUrl = tablesUrl?.ToString() ?? "";
            }

            // Load App settings
            var appSettings = await _settingsApi.GetAppAsync(HostKey);
            if (appSettings != null)
            {
                Locale = appSettings.Locale ?? "en-US";
                EnableNotifications = appSettings.EnableNotifications ?? true;
                
                if (appSettings.ReceiptSettings != null)
                {
                    BusinessName = appSettings.ReceiptSettings.BusinessName ?? "MagiDesk Billiard Club";
                    BusinessAddress = appSettings.ReceiptSettings.BusinessAddress ?? "123 Main Street, City, State 12345";
                    BusinessPhone = appSettings.ReceiptSettings.BusinessPhone ?? "(555) 123-4567";
                    DefaultPrinter = appSettings.ReceiptSettings.DefaultPrinter ?? "";
                    ReceiptSize = appSettings.ReceiptSettings.ReceiptSize ?? "80mm";
                    AutoPrintOnPayment = appSettings.ReceiptSettings.AutoPrintOnPayment ?? true;
                    PreviewBeforePrint = appSettings.ReceiptSettings.PreviewBeforePrint ?? true;
                    PrintProForma = appSettings.ReceiptSettings.PrintProForma ?? true;
                    PrintFinalReceipt = appSettings.ReceiptSettings.PrintFinalReceipt ?? true;
                    CopiesForFinalReceipt = appSettings.ReceiptSettings.CopiesForFinalReceipt ?? 2;
                }
            }

                StatusMessage = "Settings loaded successfully";
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("No connection"))
            {
                // Backend is offline - this is expected
                _logger.LogInformation("Backend is offline, using default settings");
                StatusMessage = "Backend offline - using default settings";
                
                // Initialize with default values for offline mode
                await InitializeOfflineModeAsync();
            }
            catch (Exception apiEx)
            {
                _logger.LogWarning(apiEx, "API error, falling back to offline mode");
                StatusMessage = "Using offline mode - backend unavailable";
                await InitializeOfflineModeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            StatusMessage = $"Error loading settings: {ex.Message}";
            
            // Initialize with defaults even on error
            try
            {
                await InitializeOfflineModeAsync();
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to initialize offline mode");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "Saving settings...";
        
        try
        {
            // Save Frontend settings
            var frontendSettings = new SettingsApiService.FrontendSettings
            {
                ApiBaseUrl = BackendApiUrl,
                Theme = Theme,
                RatePerMinute = RatePerMinute
            };
            await _settingsApi.SaveFrontendAsync(frontendSettings, HostKey);

            // Save Backend settings
            var backendSettings = new SettingsApiService.BackendSettings
            {
                BackendApiUrl = BackendApiUrl,
                SettingsApiUrl = SettingsApiUrl,
                TablesApiUrl = TablesApiUrl,
                InventoryApiUrl = InventoryApiUrl
            };
            await _settingsApi.SaveBackendAsync(backendSettings, HostKey);

            // Save App settings
            var appSettings = new SettingsApiService.AppSettings
            {
                Locale = Locale,
                EnableNotifications = EnableNotifications,
                ReceiptSettings = new SettingsApiService.ReceiptSettings
                {
                    BusinessName = BusinessName,
                    BusinessAddress = BusinessAddress,
                    BusinessPhone = BusinessPhone,
                    DefaultPrinter = DefaultPrinter,
                    ReceiptSize = ReceiptSize,
                    AutoPrintOnPayment = AutoPrintOnPayment,
                    PreviewBeforePrint = PreviewBeforePrint,
                    PrintProForma = PrintProForma,
                    PrintFinalReceipt = PrintFinalReceipt,
                    CopiesForFinalReceipt = CopiesForFinalReceipt
                }
            };
            await _settingsApi.SaveAppAsync(appSettings, HostKey);

            StatusMessage = "Settings saved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task TestApiConnectionsAsync()
    {
        IsLoading = true;
        StatusMessage = "Testing API connections...";
        
        try
        {
            // Test each API endpoint
            BackendApiStatus = await TestApiConnection(BackendApiUrl);
            SettingsApiStatus = await TestApiConnection(SettingsApiUrl);
            TablesApiStatus = await TestApiConnection(TablesApiUrl);
            MenuApiStatus = await TestApiConnection(MenuApiUrl);
            OrderApiStatus = await TestApiConnection(OrderApiUrl);
            PaymentApiStatus = await TestApiConnection(PaymentApiUrl);
            InventoryApiStatus = await TestApiConnection(InventoryApiUrl);
            VendorOrdersApiStatus = await TestApiConnection(VendorOrdersApiUrl);

            var connectedCount = new[] { BackendApiStatus, SettingsApiStatus, TablesApiStatus, 
                                       MenuApiStatus, OrderApiStatus, PaymentApiStatus, 
                                       InventoryApiStatus, VendorOrdersApiStatus }.Count(x => x);
            
            StatusMessage = $"API test completed. {connectedCount}/8 APIs connected";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test API connections");
            StatusMessage = $"Error testing APIs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<bool> TestApiConnection(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync($"{url}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task LoadDefaultsAsync()
    {
        try
        {
            var frontendDefaults = await _settingsApi.GetFrontendDefaultsAsync();
            var backendDefaults = await _settingsApi.GetBackendDefaultsAsync();
            var appDefaults = await _settingsApi.GetAppDefaultsAsync();

            // Apply defaults
            if (frontendDefaults != null)
            {
                if (frontendDefaults.TryGetValue("theme", out var theme))
                    Theme = theme?.ToString() ?? "System";
                if (frontendDefaults.TryGetValue("ratePerMinute", out var rate) && 
                    decimal.TryParse(rate?.ToString(), out var rateValue))
                    RatePerMinute = rateValue;
            }

            if (backendDefaults != null)
            {
                if (backendDefaults.TryGetValue("backendApiUrl", out var url))
                    BackendApiUrl = url?.ToString() ?? "";
                if (backendDefaults.TryGetValue("settingsApiUrl", out var settingsUrl))
                    SettingsApiUrl = settingsUrl?.ToString() ?? "";
                if (backendDefaults.TryGetValue("tablesApiUrl", out var tablesUrl))
                    TablesApiUrl = tablesUrl?.ToString() ?? "";
            }

            if (appDefaults != null)
            {
                Locale = appDefaults.Locale ?? "en-US";
                EnableNotifications = appDefaults.EnableNotifications ?? true;
                
                if (appDefaults.ReceiptSettings != null)
                {
                    BusinessName = appDefaults.ReceiptSettings.BusinessName ?? "MagiDesk Billiard Club";
                    BusinessAddress = appDefaults.ReceiptSettings.BusinessAddress ?? "123 Main Street, City, State 12345";
                    BusinessPhone = appDefaults.ReceiptSettings.BusinessPhone ?? "(555) 123-4567";
                    ReceiptSize = appDefaults.ReceiptSettings.ReceiptSize ?? "80mm";
                    AutoPrintOnPayment = appDefaults.ReceiptSettings.AutoPrintOnPayment ?? true;
                    PreviewBeforePrint = appDefaults.ReceiptSettings.PreviewBeforePrint ?? true;
                    PrintProForma = appDefaults.ReceiptSettings.PrintProForma ?? true;
                    PrintFinalReceipt = appDefaults.ReceiptSettings.PrintFinalReceipt ?? true;
                    CopiesForFinalReceipt = appDefaults.ReceiptSettings.CopiesForFinalReceipt ?? 2;
                }
            }

            StatusMessage = "Defaults loaded successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load defaults");
            StatusMessage = $"Error loading defaults: {ex.Message}";
        }
    }

    public async Task InitializeOfflineModeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Initializing offline mode...";

            // Set default values for offline mode
            HostKey = "store-1";
            Theme = "System";
            Locale = "en-US";
            EnableNotifications = true;

            // API URLs - use default cloud URLs
            BackendApiUrl = "https://magidesk-backend-904541739138.us-central1.run.app";
            SettingsApiUrl = "https://magidesk-settings-904541739138.us-central1.run.app";
            MenuApiUrl = "https://magidesk-menu-904541739138.northamerica-south1.run.app";
            OrderApiUrl = "https://magidesk-order-904541739138.northamerica-south1.run.app";
            PaymentApiUrl = "https://magidesk-payment-904541739138.northamerica-south1.run.app";
            TablesApiUrl = "https://magidesk-tables-904541739138.northamerica-south1.run.app";
            InventoryApiUrl = "http://localhost:5001";
            VendorOrdersApiUrl = "https://magidesk-vendororders-904541739138.northamerica-south1.run.app";

            // Business settings defaults
            BusinessName = "MagiDesk POS";
            BusinessAddress = "123 Business St";
            BusinessPhone = "+1-555-0123";
            DefaultPrinter = "Default Printer";
            ReceiptSize = "80mm";

            StatusMessage = "Offline mode initialized - settings ready";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize offline mode");
            StatusMessage = $"Error initializing offline mode: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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
