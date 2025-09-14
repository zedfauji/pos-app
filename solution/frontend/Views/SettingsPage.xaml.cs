using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MagiDesk.Frontend.Views;

public sealed partial class SettingsPage : Page
{
    private IConfigurationRoot? _config;
    private SettingsApiService _settingsApi = null!;
    private SettingsViewModel _viewModel = null!;
    private readonly ILogger<SettingsPage> _logger;

    // Fallback logger for when DI is not available
    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            System.Diagnostics.Debug.WriteLine($"[{logLevel}] {formatter(state, exception)}");
            if (exception != null)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {exception}");
            }
        }
    }

    public SettingsPage()
    {
        try
        {
            _logger = new NullLogger<SettingsPage>();
            _logger.LogInformation("SettingsPage constructor started");
            
            this.InitializeComponent();
            _logger.LogInformation("InitializeComponent completed");
            
            ApplyLanguage();
            _logger.LogInformation("ApplyLanguage completed");
            
            if (App.I18n != null)
            {
                App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
                _logger.LogInformation("LanguageChanged event handler registered");
            }
            else
            {
                _logger.LogWarning("App.I18n is null, skipping language event handler");
            }
            
            // Initialize SettingsApi
            InitializeSettingsApi();
            
            // Initialize ViewModel
            InitializeViewModel();
            
            // Load initial settings
            _ = LoadSettingsAsync();
            
            _logger.LogInformation("SettingsPage constructor completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SettingsPage constructor");
            System.Diagnostics.Debug.WriteLine($"SettingsPage initialization error: {ex.Message}");
        }
    }

    private void InitializeSettingsApi()
    {
        try
        {
            // Initialize configuration if not already done
            if (_config == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                _config = builder.Build();
            }

            var settingsBase = _config?["SettingsApi:BaseUrl"]
                ?? "https://magidesk-settings-904541739138.us-central1.run.app/";
            
            _logger.LogInformation($"Settings API base URL: {settingsBase}");
            
            _settingsApi = new SettingsApiService(new HttpClient { BaseAddress = new Uri(settingsBase) }, null);
            _logger.LogInformation("SettingsApiService created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SettingsApiService, using fallback");
            _settingsApi = new SettingsApiService(new HttpClient { BaseAddress = new Uri("https://magidesk-settings-904541739138.us-central1.run.app/") }, null);
            System.Diagnostics.Debug.WriteLine($"Settings API initialization failed: {ex.Message}");
        }
    }

    private void InitializeViewModel()
    {
        try
        {
            var logger = new NullLogger<SettingsViewModel>();
            _viewModel = new SettingsViewModel(_settingsApi, logger);
            
            // Bind ViewModel to UI
            this.DataContext = _viewModel;
            
            // Bind status properties
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            _logger.LogInformation("ViewModel initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ViewModel");
            System.Diagnostics.Debug.WriteLine($"ViewModel initialization error: {ex.Message}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            // Update UI based on ViewModel changes
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsLoading):
                    LoadingRing.IsActive = _viewModel.IsLoading;
                    LoadingRing.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(_viewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ViewModel property change");
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // Try to load settings from API, but don't fail if backend is unavailable
            try
            {
                await _viewModel.LoadSettingsAsync();
                UpdateUIFromViewModel();
                ShowStatusMessage("Settings loaded successfully");
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("No connection"))
            {
                // Backend is offline - this is expected
                _logger.LogInformation("Backend is offline, using local settings mode");
                ShowStatusMessage("Backend offline - using local settings", "Warning");
                
                // Initialize with default values for offline mode
                await _viewModel.InitializeOfflineModeAsync();
                UpdateUIFromViewModel();
            }
            catch (Exception apiEx)
            {
                _logger.LogWarning(apiEx, "API error, falling back to offline mode");
                ShowStatusMessage("Using offline mode - backend unavailable", "Warning");
                await _viewModel.InitializeOfflineModeAsync();
                UpdateUIFromViewModel();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in settings initialization");
            ShowStatusMessage($"Error initializing settings: {ex.Message}", "Error");
            
            // Initialize with defaults even on error
            try
            {
                await _viewModel.InitializeOfflineModeAsync();
                UpdateUIFromViewModel();
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to initialize offline mode");
            }
        }
    }

    private void UpdateUIFromViewModel()
    {
        try
        {
            // Update UI controls with ViewModel values
            if (SettingsHostText != null) SettingsHostText.Text = _viewModel.HostKey;
            if (ThemeSelector != null) SetThemeSelection(_viewModel.Theme);
            if (LocaleText != null) LocaleText.Text = _viewModel.Locale;
            if (EnableNotificationsCheck != null) EnableNotificationsCheck.IsChecked = _viewModel.EnableNotifications;
            
            // API URLs
            if (BaseUrlText != null) BaseUrlText.Text = _viewModel.BackendApiUrl;
            if (SettingsApiUrlText != null) SettingsApiUrlText.Text = _viewModel.SettingsApiUrl;
            if (TablesApiUrlText != null) TablesApiUrlText.Text = _viewModel.TablesApiUrl;
            if (MenuApiUrlText != null) MenuApiUrlText.Text = _viewModel.MenuApiUrl;
            if (OrderApiUrlText != null) OrderApiUrlText.Text = _viewModel.OrderApiUrl;
            if (PaymentApiUrlText != null) PaymentApiUrlText.Text = _viewModel.PaymentApiUrl;
            if (InventoryApiUrlText != null) InventoryApiUrlText.Text = _viewModel.InventoryApiUrl;
            if (VendorOrdersApiUrlText != null) VendorOrdersApiUrlText.Text = _viewModel.VendorOrdersApiUrl;
            
            // Business Information
            if (BusinessNameText != null) BusinessNameText.Text = _viewModel.BusinessName;
            if (BusinessAddressText != null) BusinessAddressText.Text = _viewModel.BusinessAddress;
            if (BusinessPhoneText != null) BusinessPhoneText.Text = _viewModel.BusinessPhone;
            
            // Receipt Settings
            if (DefaultPrinterText != null) DefaultPrinterText.Text = _viewModel.DefaultPrinter;
            if (ReceiptSizeCombo != null) SetReceiptSizeSelection(_viewModel.ReceiptSize);
            if (AutoPrintOnPaymentCheck != null) AutoPrintOnPaymentCheck.IsChecked = _viewModel.AutoPrintOnPayment;
            if (PreviewBeforePrintCheck != null) PreviewBeforePrintCheck.IsChecked = _viewModel.PreviewBeforePrint;
            if (PrintProFormaCheck != null) PrintProFormaCheck.IsChecked = _viewModel.PrintProForma;
            if (PrintFinalReceiptCheck != null) PrintFinalReceiptCheck.IsChecked = _viewModel.PrintFinalReceipt;
            if (CopiesForFinalReceiptText != null) CopiesForFinalReceiptText.Text = _viewModel.CopiesForFinalReceipt.ToString();
            if (TaxPercentText != null) TaxPercentText.Text = _viewModel.TaxPercent.ToString("F1");
            
            // Tables Settings
            if (RatePerMinuteBox != null) RatePerMinuteBox.Text = _viewModel.RatePerMinute.ToString("F2");
            if (WarnMinutesBox != null) WarnMinutesBox.Text = _viewModel.WarnMinutes.ToString();
            if (AutoStopMinutesBox != null) AutoStopMinutesBox.Text = _viewModel.AutoStopMinutes.ToString();
            
            // Update summaries
            UpdateSummaries();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating UI from ViewModel");
        }
    }

    private void UpdateViewModelFromUI()
    {
        try
        {
            // Update ViewModel with UI values
            _viewModel.HostKey = SettingsHostText?.Text ?? "";
            _viewModel.Theme = GetSelectedTheme();
            _viewModel.Locale = LocaleText?.Text ?? "en-US";
            _viewModel.EnableNotifications = EnableNotificationsCheck?.IsChecked ?? true;
            
            // API URLs
            _viewModel.BackendApiUrl = BaseUrlText?.Text ?? "";
            _viewModel.SettingsApiUrl = SettingsApiUrlText?.Text ?? "";
            _viewModel.TablesApiUrl = TablesApiUrlText?.Text ?? "";
            _viewModel.MenuApiUrl = MenuApiUrlText?.Text ?? "";
            _viewModel.OrderApiUrl = OrderApiUrlText?.Text ?? "";
            _viewModel.PaymentApiUrl = PaymentApiUrlText?.Text ?? "";
            _viewModel.InventoryApiUrl = InventoryApiUrlText?.Text ?? "";
            _viewModel.VendorOrdersApiUrl = VendorOrdersApiUrlText?.Text ?? "";
            
            // Business Information
            _viewModel.BusinessName = BusinessNameText?.Text ?? "";
            _viewModel.BusinessAddress = BusinessAddressText?.Text ?? "";
            _viewModel.BusinessPhone = BusinessPhoneText?.Text ?? "";
            
            // Receipt Settings
            _viewModel.DefaultPrinter = DefaultPrinterText?.Text ?? "";
            _viewModel.ReceiptSize = GetSelectedReceiptSize();
            _viewModel.AutoPrintOnPayment = AutoPrintOnPaymentCheck?.IsChecked ?? true;
            _viewModel.PreviewBeforePrint = PreviewBeforePrintCheck?.IsChecked ?? true;
            _viewModel.PrintProForma = PrintProFormaCheck?.IsChecked ?? true;
            _viewModel.PrintFinalReceipt = PrintFinalReceiptCheck?.IsChecked ?? true;
            if (int.TryParse(CopiesForFinalReceiptText?.Text, out var copies))
                _viewModel.CopiesForFinalReceipt = copies;
            if (decimal.TryParse(TaxPercentText?.Text, out var tax))
                _viewModel.TaxPercent = tax;
            
            // Tables Settings
            if (decimal.TryParse(RatePerMinuteBox?.Text, out var rate))
                _viewModel.RatePerMinute = rate;
            if (int.TryParse(WarnMinutesBox?.Text, out var warn))
                _viewModel.WarnMinutes = warn;
            if (int.TryParse(AutoStopMinutesBox?.Text, out var auto))
                _viewModel.AutoStopMinutes = auto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ViewModel from UI");
        }
    }

    private void SetThemeSelection(string theme)
    {
        if (ThemeSelector == null) return;
        
        ThemeSelector.SelectedIndex = theme switch
        {
            "Dark" => 1,
            "Light" => 2,
            _ => 0 // System
        };
    }

    private string GetSelectedTheme()
    {
        return ThemeSelector?.SelectedIndex switch
        {
            1 => "Dark",
            2 => "Light",
            _ => "System"
        };
    }

    private void SetReceiptSizeSelection(string size)
    {
        if (ReceiptSizeCombo == null) return;
        
        ReceiptSizeCombo.SelectedIndex = size switch
        {
            "58mm" => 0,
            "80mm" => 1,
            _ => 1 // Default to 80mm
        };
    }

    private string GetSelectedReceiptSize()
    {
        if (ReceiptSizeCombo?.SelectedItem is ComboBoxItem item)
        {
            return item.Tag?.ToString() ?? "80mm";
        }
        return "80mm";
    }

    private void UpdateSummaries()
    {
        try
        {
            if (ThemeSummaryText != null)
                ThemeSummaryText.Text = $"Theme: {_viewModel.Theme}";
            
            if (AppSummaryText != null)
                AppSummaryText.Text = $"Locale: {_viewModel.Locale}, Notifications: {(_viewModel.EnableNotifications ? "On" : "Off")}";
            
            if (CurrentRateText != null)
                CurrentRateText.Text = $"Current Rate: ${_viewModel.RatePerMinute:F2}/min";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating summaries");
        }
    }

    private void ShowStatusMessage(string message, string severity = "Success")
    {
        try
        {
            if (ToastBar != null)
            {
                ToastBar.Message = message;
                ToastBar.Severity = severity switch
                {
                    "Error" => InfoBarSeverity.Error,
                    "Warning" => InfoBarSeverity.Warning,
                    "Info" => InfoBarSeverity.Informational,
                    _ => InfoBarSeverity.Success
                };
                ToastBar.IsOpen = true;
                
                // Auto-hide after 3 seconds
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s, e) =>
                {
                    ToastBar.IsOpen = false;
                    timer.Stop();
                };
                timer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing status message");
        }
    }

    #region Event Handlers

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateViewModelFromUI();
            await _viewModel.SaveSettingsAsync();
            ShowStatusMessage("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            ShowStatusMessage($"Error saving settings: {ex.Message}", "Error");
        }
    }

    private async void SaveApi_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateViewModelFromUI();
            await _viewModel.SaveSettingsAsync();
            ShowStatusMessage("API URLs saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving API URLs");
            ShowStatusMessage($"Error saving API URLs: {ex.Message}", "Error");
        }
    }

    private async void PingApis_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateViewModelFromUI();
            await _viewModel.TestApiConnectionsAsync();
            UpdateApiStatusIndicators();
            ShowStatusMessage("API connection test completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API connections");
            ShowStatusMessage($"Error testing APIs: {ex.Message}", "Error");
        }
    }

    private async void TestPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Implement test print functionality
            ShowStatusMessage("Test print functionality not yet implemented", "Info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing print");
            ShowStatusMessage($"Error testing print: {ex.Message}", "Error");
        }
    }

    private async void LoadDefaults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.LoadDefaultsAsync();
            UpdateUIFromViewModel();
            ShowStatusMessage("Default settings loaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading defaults");
            ShowStatusMessage($"Error loading defaults: {ex.Message}", "Error");
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadSettingsAsync();
            ShowStatusMessage("Settings refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing settings");
            ShowStatusMessage($"Error refreshing settings: {ex.Message}", "Error");
        }
    }

    private async void RateLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.LoadSettingsAsync();
            UpdateUIFromViewModel();
            ShowStatusMessage("Rate loaded from server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading rate");
            ShowStatusMessage($"Error loading rate: {ex.Message}", "Error");
        }
    }

    private async void RateSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateViewModelFromUI();
            await _viewModel.SaveSettingsAsync();
            ShowStatusMessage("Rate saved to server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rate");
            ShowStatusMessage($"Error saving rate: {ex.Message}", "Error");
        }
    }

    private async void AuditRefresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Implement audit log refresh
            ShowStatusMessage("Audit log functionality not yet implemented", "Info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing audit log");
            ShowStatusMessage($"Error refreshing audit log: {ex.Message}", "Error");
        }
    }

    #endregion

    private void UpdateApiStatusIndicators()
    {
        try
        {
            UpdateStatusIndicator(BackendStatusDot, BackendSummaryText, _viewModel.BackendApiStatus, "Backend API");
            UpdateStatusIndicator(SettingsStatusDot, SettingsStatusText, _viewModel.SettingsApiStatus, "Settings API");
            UpdateStatusIndicator(TablesStatusDot, TablesStatusText, _viewModel.TablesApiStatus, "Tables API");
            UpdateStatusIndicator(MenuStatusDot, MenuStatusText, _viewModel.MenuApiStatus, "Menu API");
            UpdateStatusIndicator(OrderStatusDot, OrderStatusText, _viewModel.OrderApiStatus, "Order API");
            UpdateStatusIndicator(PaymentStatusDot, PaymentStatusText, _viewModel.PaymentApiStatus, "Payment API");
            UpdateStatusIndicator(InventoryStatusDot, InventoryStatusText, _viewModel.InventoryApiStatus, "Inventory API");
            UpdateStatusIndicator(VendorOrdersStatusDot, VendorOrdersStatusText, _viewModel.VendorOrdersApiStatus, "Vendor Orders API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API status indicators");
        }
    }

    private void UpdateStatusIndicator(Ellipse dot, TextBlock text, bool isConnected, string apiName)
    {
        if (dot != null)
        {
            dot.Fill = isConnected ? 
                Application.Current.Resources["SystemAccentColor"] as Microsoft.UI.Xaml.Media.Brush ?? 
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green) : 
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
        
        if (text != null)
        {
            text.Text = $"{apiName}: {(isConnected ? "Connected" : "Disconnected")}";
        }
    }

    private void ApplyLanguage()
    {
        try
        {
            if (App.I18n != null)
            {
                TitleText.Text = "Settings"; // App.I18n.GetString("Settings");
                // Add more translations as needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying language");
        }
    }
}