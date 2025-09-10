using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class SettingsPage : Page
{
    private IConfigurationRoot? _config;
    private SettingsApiService _settingsApi;
    private readonly ILogger<SettingsPage> _logger;

    // Fallback logger for when DI is not available
    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
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
            
            // LoadConfig will be called asynchronously after SettingsApi is initialized
            _logger.LogInformation("Config loading will be done asynchronously");
            
            // Rate controls removed in minimal layout
            // Prefer dedicated SettingsApi base URL from config if available
            var settingsBase = _config?["SettingsApi:BaseUrl"]
                ?? App.Api?.BackendBase?.ToString()
                ?? "https://magidesk-settings-904541739138.us-central1.run.app/";
            
            _logger.LogInformation($"Settings API base URL: {settingsBase}");
            
            try
            {
                _settingsApi = new SettingsApiService(new HttpClient { BaseAddress = new Uri(settingsBase) }, null);
                _logger.LogInformation("SettingsApiService created successfully");
            }
            catch (Exception ex)
            {
                // Fallback to a default settings service if URI is invalid
                _logger.LogError(ex, "Failed to create SettingsApiService with primary URL, using fallback");
                _settingsApi = new SettingsApiService(new HttpClient { BaseAddress = new Uri("https://localhost:5001/") }, null);
                System.Diagnostics.Debug.WriteLine($"Settings API initialization failed: {ex.Message}");
            }
            
            // Default to first category
            // Simplified layout - all panels visible by default
            _logger.LogInformation("Using simplified layout - all panels visible");
            
            // Safely initialize async operations
            try
            {
                _logger.LogInformation("Starting async initialization operations");
                _ = LoadFromBackendAsync();
                _ = UpdateAppSummaryAsync();
                _logger.LogInformation("Async initialization operations started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start async initialization operations");
                System.Diagnostics.Debug.WriteLine($"Settings page initialization error: {ex.Message}");
            }
            
            _logger.LogInformation("SettingsPage constructor completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Critical error in SettingsPage constructor");
            System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR in SettingsPage constructor: {ex}");
            throw; // Re-throw to see the crash in debugger
        }
    }

    private async Task LoadAuditAsync()
    {
        try
        {
            _logger.LogInformation("LoadAuditAsync started");
            
            var host = GetSettingsHost();
            _logger.LogInformation($"GetSettingsHost returned for audit: {host}");
            
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("Host is null or empty, setting empty audit list");
                AuditList.ItemsSource = new List<object>();
                return;
            }
            
            _logger.LogInformation("Calling GetAuditAsync");
            var list = await _settingsApi.GetAuditAsync(host, 50);
            _logger.LogInformation($"GetAuditAsync completed, result is null: {list == null}");
            
            if (list == null)
            {
                _logger.LogWarning("Audit list is null, setting empty audit list");
                AuditList.ItemsSource = new List<object>();
                return;
            }
            
            _logger.LogInformation($"Processing {list.Count} audit entries");
            // Display a simplified projection for readability
            var view = list.Select(x => new
            {
                section = x.ContainsKey("section") ? x["section"] : null,
                timestamp = x.ContainsKey("timestamp") ? x["timestamp"] : null,
                changes = x.ContainsKey("changes") ? System.Text.Json.JsonSerializer.Serialize(x["changes"]) : null
            }).ToList();
            
            AuditList.ItemsSource = view;
            _logger.LogInformation($"Set AuditList.ItemsSource with {view.Count} items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoadAuditAsync");
            if (StatusText != null)
                StatusText.Text = $"Audit error: {ex.Message}";
        }
    }

    private async void AuditRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadAuditAsync();
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            _logger.LogInformation("LoadConfigAsync started - loading from SettingsApi");
            
            // Load settings from SettingsApi instead of local config files
            var host = GetSettingsHost();
            _logger.LogInformation($"Loading settings for host: {host}");
            
            // Load frontend settings
            var frontendSettings = await _settingsApi.GetFrontendAsync(host);
            if (frontendSettings != null)
            {
                _logger.LogInformation("Loading frontend settings from SettingsApi");
                
                if (BaseUrlText != null && frontendSettings.ContainsKey("ApiBaseUrl"))
                {
                    BaseUrlText.Text = frontendSettings["ApiBaseUrl"]?.ToString() ?? "";
                    _logger.LogInformation($"Set BaseUrlText from SettingsApi: {BaseUrlText.Text}");
                }
                
                if (ThemeSelector != null && frontendSettings.ContainsKey("Theme"))
                {
                    var theme = frontendSettings["Theme"]?.ToString() ?? "System";
                    ThemeSelector.SelectedIndex = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
                            ? 2
                            : 0; // System
                    _logger.LogInformation($"Set ThemeSelector from SettingsApi: {theme}");
                }
            }
            
            // Load backend settings
            var backendSettings = await _settingsApi.GetBackendAsync(host);
            if (backendSettings != null)
            {
                _logger.LogInformation("Loading backend settings from SettingsApi");
                
                if (InventoryApiUrlText != null && backendSettings.ContainsKey("InventoryApiUrl"))
                {
                    InventoryApiUrlText.Text = backendSettings["InventoryApiUrl"]?.ToString() ?? "";
                    _logger.LogInformation($"Set InventoryApiUrlText from SettingsApi: {InventoryApiUrlText.Text}");
                }
                
                if (SettingsApiUrlText != null && backendSettings.ContainsKey("SettingsApiUrl"))
                {
                    SettingsApiUrlText.Text = backendSettings["SettingsApiUrl"]?.ToString() ?? "";
                    _logger.LogInformation($"Set SettingsApiUrlText from SettingsApi: {SettingsApiUrlText.Text}");
                }
                
                if (TablesApiUrlText != null && backendSettings.ContainsKey("TablesApiUrl"))
                {
                    TablesApiUrlText.Text = backendSettings["TablesApiUrl"]?.ToString() ?? "";
                    _logger.LogInformation($"Set TablesApiUrlText from SettingsApi: {TablesApiUrlText.Text}");
                }
            }
            
            // Load app settings for printer configuration
            var appSettings = await _settingsApi.GetAppAsync(host);
            if (appSettings?.ReceiptSettings != null)
            {
                _logger.LogInformation("Loading receipt/printer settings from SettingsApi");
                
                if (PrinterWidthCombo != null)
                {
                    var paperSize = appSettings.ReceiptSettings.ReceiptSize ?? "80mm";
                    PrinterWidthCombo.SelectedIndex = paperSize == "80mm" ? 1 : 0;
                    _logger.LogInformation($"Set PrinterWidthCombo from SettingsApi: {paperSize}");
                }
                
                if (TaxPercentText != null && !string.IsNullOrWhiteSpace(appSettings.ReceiptSettings.BusinessName))
                {
                    // We could add tax percentage to ReceiptSettings if needed
                    _logger.LogInformation($"Business name from SettingsApi: {appSettings.ReceiptSettings.BusinessName}");
                }
            }
            
            _logger.LogInformation("LoadConfigAsync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from SettingsApi");
            // Fallback to local config if SettingsApi fails
            await LoadLocalConfigFallback();
        }
    }
    
    private async Task LoadLocalConfigFallback()
    {
        try
        {
            _logger.LogWarning("Falling back to local config files");
            
            var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true);
            _config = builder.Build();
            
            // Set default values from local config
            if (BaseUrlText != null)
                BaseUrlText.Text = _config["Api:BaseUrl"] ?? "";
            if (InventoryApiUrlText != null)
                InventoryApiUrlText.Text = _config["InventoryApi:BaseUrl"] ?? "";
            if (SettingsApiUrlText != null)
                SettingsApiUrlText.Text = _config["SettingsApi:BaseUrl"] ?? "";
            if (TablesApiUrlText != null)
                TablesApiUrlText.Text = _config["TablesApi:BaseUrl"] ?? "";
                
            _logger.LogInformation("Local config fallback completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading local config fallback");
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Save_Click started - saving to SettingsApi");
            
            var host = GetSettingsHost();
            _logger.LogInformation($"Saving settings for host: {host}");
            
            // Save frontend settings
            var frontendSettings = new SettingsApiService.FrontendSettings();
            if (BaseUrlText != null && !string.IsNullOrWhiteSpace(BaseUrlText.Text))
                frontendSettings.ApiBaseUrl = BaseUrlText.Text;
            if (ThemeSelector != null)
            {
                var themeIndex = ThemeSelector.SelectedIndex;
                frontendSettings.Theme = themeIndex == 1 ? "Dark" : themeIndex == 2 ? "Light" : "System";
            }
            
            var frontendSuccess = await _settingsApi.SaveFrontendAsync(frontendSettings, host);
            _logger.LogInformation($"Frontend settings saved: {frontendSuccess}");
            
            // Save backend settings
            var backendSettings = new SettingsApiService.BackendSettings();
            if (InventoryApiUrlText != null && !string.IsNullOrWhiteSpace(InventoryApiUrlText.Text))
                backendSettings.InventoryApiUrl = InventoryApiUrlText.Text;
            if (SettingsApiUrlText != null && !string.IsNullOrWhiteSpace(SettingsApiUrlText.Text))
                backendSettings.SettingsApiUrl = SettingsApiUrlText.Text;
            if (TablesApiUrlText != null && !string.IsNullOrWhiteSpace(TablesApiUrlText.Text))
                backendSettings.TablesApiUrl = TablesApiUrlText.Text;
            
            var backendSuccess = await _settingsApi.SaveBackendAsync(backendSettings, host);
            _logger.LogInformation($"Backend settings saved: {backendSuccess}");
            
            // Save app settings (receipt/printer settings)
            var appSettings = new SettingsApiService.AppSettings();
            if (appSettings.ReceiptSettings == null)
                appSettings.ReceiptSettings = new SettingsApiService.ReceiptSettings();
                
            if (PrinterWidthCombo != null)
                appSettings.ReceiptSettings.ReceiptSize = PrinterWidthCombo.SelectedIndex == 1 ? "80mm" : "58mm";
            
            var appSuccess = await _settingsApi.SaveAppAsync(appSettings, host);
            _logger.LogInformation($"App settings saved: {appSuccess}");
            
            if (frontendSuccess && backendSuccess && appSuccess)
            {
                if (StatusText != null)
                    StatusText.Text = "Settings saved to SettingsApi";
                Toast("Settings saved to SettingsApi");
                _logger.LogInformation("All settings saved successfully to SettingsApi");
            }
            else
            {
                if (StatusText != null)
                    StatusText.Text = "Some settings failed to save";
                _logger.LogWarning("Some settings failed to save to SettingsApi");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to SettingsApi");
            if (StatusText != null)
                StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async Task LoadFromBackendAsync()
    {
        try
        {
            _logger.LogInformation("LoadFromBackendAsync started");
            
            if (LoadingRing != null)
            {
                LoadingRing.IsActive = true; 
                LoadingRing.Visibility = Visibility.Visible;
                _logger.LogInformation("LoadingRing activated");
            }
            else
            {
                _logger.LogWarning("LoadingRing is null");
            }
            
            // Load configuration from SettingsApi first
            await LoadConfigAsync();
            
            var host = GetSettingsHost();
            _logger.LogInformation($"GetSettingsHost returned: {host}");
            
            _logger.LogInformation("Calling GetFrontendAsync");
            var fe = await _settingsApi.GetFrontendAsync(host);
            _logger.LogInformation($"GetFrontendAsync completed, result is null: {fe == null}");
            if (fe != null)
            {
                _logger.LogInformation("Processing frontend settings");
                var frontendSettings = new SettingsApiService.FrontendSettings();
                foreach (var kvp in fe)
                {
                    frontendSettings[kvp.Key] = kvp.Value;
                }
                
                if (!string.IsNullOrWhiteSpace(frontendSettings.ApiBaseUrl) && BaseUrlText != null) 
                {
                    BaseUrlText.Text = frontendSettings.ApiBaseUrl;
                    _logger.LogInformation($"Set BaseUrlText to: {frontendSettings.ApiBaseUrl}");
                }
                    
                var theme = string.IsNullOrWhiteSpace(frontendSettings.Theme) ? "System" : frontendSettings.Theme!;
                if (ThemeSelector != null)
                {
                    ThemeSelector.SelectedIndex = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 1 : theme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                    _logger.LogInformation($"Set ThemeSelector to: {theme} (index: {ThemeSelector.SelectedIndex})");
                }
                    
                if (BackendSummaryText != null)
                {
                    BackendSummaryText.Text = $"Backend: {BaseUrlText?.Text ?? "Not set"}";
                    _logger.LogInformation($"Set BackendSummaryText");
                }
                if (ThemeSummaryText != null)
                {
                    ThemeSummaryText.Text = $"Theme: {theme}";
                    _logger.LogInformation($"Set ThemeSummaryText");
                }
                    
                if (frontendSettings.RatePerMinute.HasValue && CurrentRateText != null)
                {
                    CurrentRateText.Text = $"Current Rate: {frontendSettings.RatePerMinute.Value.ToString("0.##", CultureInfo.InvariantCulture)}";
                    _logger.LogInformation($"Set CurrentRateText to: {frontendSettings.RatePerMinute.Value}");
                }
            }
            else
            {
                _logger.LogInformation("Frontend settings is null, skipping frontend processing");
            }

            // Load backend connection settings
            _logger.LogInformation("Calling GetBackendAsync");
            var be = await _settingsApi.GetBackendAsync(host);
            _logger.LogInformation($"GetBackendAsync completed, result is null: {be == null}");
            if (be != null)
            {
                _logger.LogInformation("Processing backend settings");
                var backendSettings = new SettingsApiService.BackendSettings();
                foreach (var kvp in be)
                {
                    backendSettings[kvp.Key] = kvp.Value;
                }
                
                if (!string.IsNullOrWhiteSpace(backendSettings.BackendApiUrl) && BaseUrlText != null) 
                {
                    BaseUrlText.Text = backendSettings.BackendApiUrl;
                    _logger.LogInformation($"Set BaseUrlText to backend URL: {backendSettings.BackendApiUrl}");
                }
                if (!string.IsNullOrWhiteSpace(backendSettings.SettingsApiUrl) && SettingsApiUrlText != null) 
                {
                    SettingsApiUrlText.Text = backendSettings.SettingsApiUrl;
                    _logger.LogInformation($"Set SettingsApiUrlText to: {backendSettings.SettingsApiUrl}");
                }
                if (!string.IsNullOrWhiteSpace(backendSettings.TablesApiUrl) && TablesApiUrlText != null) 
                {
                    TablesApiUrlText.Text = backendSettings.TablesApiUrl;
                    _logger.LogInformation($"Set TablesApiUrlText to: {backendSettings.TablesApiUrl}");
                }
            }
            else
            {
                _logger.LogInformation("Backend settings is null, skipping backend processing");
            }
            
            if (LoadingRing != null)
            {
                LoadingRing.IsActive = false; 
                LoadingRing.Visibility = Visibility.Collapsed;
                _logger.LogInformation("LoadingRing deactivated");
            }
            
            _logger.LogInformation("LoadFromBackendAsync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoadFromBackendAsync");
            if (StatusText != null)
                StatusText.Text = $"Error: {ex.Message}";
            if (LoadingRing != null)
            {
                LoadingRing.IsActive = false; 
                LoadingRing.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async Task SaveToBackendAsync(string apiBaseUrl, string theme)
    {
        try
        {
            // Do not include rate here; edited via dialog and saved immediately
            var fe = new SettingsApiService.FrontendSettings { ApiBaseUrl = apiBaseUrl, Theme = theme };
            await _settingsApi.SaveFrontendAsync(fe, GetSettingsHost());
            BackendSummaryText.Text = $"Backend: {BaseUrlText.Text}";
            ThemeSummaryText.Text = $"Theme: {theme}";
        }
        catch { }
    }

    private async Task UpdateAppSummaryAsync()
    {
        try
        {
            var app = await _settingsApi.GetAppAsync(GetSettingsHost());
            if (app is null)
            {
                AppSummaryText.Text = "App Settings: -";
                return;
            }
            var locale = string.IsNullOrWhiteSpace(app.Locale) ? "(default)" : app.Locale;
            var notif = app.EnableNotifications == true ? "On" : "Off";
            AppSummaryText.Text = $"App Settings: Locale={locale}; Notifications={notif}";
            // Load session timers if present
            try
            {
                if (app.Extras != null)
                {
                    if (app.Extras.TryGetValue("tables.session.warnMinutes", out var warn) && warn != null)
                    {
                        if (int.TryParse(warn.ToString(), out var wm) && WarnMinutesBox != null) 
                            WarnMinutesBox.Text = wm.ToString();
                    }
                    if (app.Extras.TryGetValue("tables.session.autoStopMinutes", out var auto) && auto != null)
                    {
                        if (int.TryParse(auto.ToString(), out var am) && AutoStopMinutesBox != null) 
                            AutoStopMinutesBox.Text = am.ToString();
                    }
                }
            }
            catch { }
        }
        catch
        {
            AppSummaryText.Text = "App Settings: -";
        }
    }

    private void ApplyLanguage()
    {
        try
        {
            TitleText.Text = App.I18n.T("settings_title");
            // BackendUrlLabel removed in simplified layout
            SaveButton.Content = App.I18n.T("save");
        }
        catch { }
    }

    // Rate controls removed; no rate load/save in minimal layout

    private async void EditRate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get current value from backend
            decimal current = 0m;
            try
            {
                var fe = await _settingsApi.GetFrontendAsync();
                if (fe != null)
                {
                    var frontendSettings = new SettingsApiService.FrontendSettings();
                    foreach (var kvp in fe)
                    {
                        frontendSettings[kvp.Key] = kvp.Value;
                    }
                    if (frontendSettings.RatePerMinute is decimal r) current = r;
                }
            }
            catch { }

            // Build dialog UI in code to avoid adding complex XAML
            var panel = new StackPanel { Spacing = 8 };
            var tb = new TextBox { PlaceholderText = "0.50", Text = current == 0m ? string.Empty : current.ToString("0.##", CultureInfo.InvariantCulture), Width = 200 };
            panel.Children.Add(new TextBlock { Text = "Enter rate per minute (decimal):" });
            panel.Children.Add(tb);

            var dlg = new ContentDialog
            {
                Title = "Edit Rate",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = panel,
                XamlRoot = (App.MainWindow?.Content as FrameworkElement)?.XamlRoot
            };

            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                if (!decimal.TryParse(tb.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var newRate))
                {
                    StatusText.Text = App.I18n.T("error");
                    return;
                }

                // Save theme + baseUrl unchanged, only update rate
                string theme = ThemeSelector.SelectedIndex == 1 ? "Dark" : ThemeSelector.SelectedIndex == 2 ? "Light" : "System";
                var fe = new SettingsApiService.FrontendSettings
                {
                    ApiBaseUrl = BaseUrlText.Text,
                    Theme = theme,
                    RatePerMinute = newRate
                };
                await _settingsApi.SaveFrontendAsync(fe);
                StatusText.Text = App.I18n.T("saved");
                CurrentRateText.Text = $"Current Rate: {newRate.ToString("0.##", CultureInfo.InvariantCulture)}";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void EditAppSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var current = await _settingsApi.GetAppAsync(GetSettingsHost());
            var localeBox = new TextBox { Text = current?.Locale ?? string.Empty, PlaceholderText = "en-US", Width = 240 };
            var notifChk = new CheckBox { Content = "Enable Notifications", IsChecked = current?.EnableNotifications ?? false };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var localeLbl = new TextBlock { Text = "Locale:" };
            Grid.SetRow(localeLbl, 0); Grid.SetColumn(localeLbl, 0);
            Grid.SetRow(localeBox, 0); Grid.SetColumn(localeBox, 1);
            grid.Children.Add(localeLbl); grid.Children.Add(localeBox);

            var notifLbl = new TextBlock { Text = "Enable Notifications:" };
            Grid.SetRow(notifLbl, 1); Grid.SetColumn(notifLbl, 0);
            Grid.SetRow(notifChk, 1); Grid.SetColumn(notifChk, 1);
            grid.Children.Add(notifLbl); grid.Children.Add(notifChk);

            var dlg = new ContentDialog
            {
                Title = "App Settings",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = grid,
                XamlRoot = (App.MainWindow?.Content as FrameworkElement)?.XamlRoot
            };

            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                var updated = new SettingsApiService.AppSettings
                {
                    Locale = localeBox.Text,
                    EnableNotifications = notifChk.IsChecked ?? false
                };
                await _settingsApi.SaveAppAsync(updated, GetSettingsHost());
                StatusText.Text = App.I18n.T("saved");
                await UpdateAppSummaryAsync();
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private string? GetSettingsHost()
    {
        try
        {
            _logger.LogInformation("GetSettingsHost called");
            
            // Prefer explicit override from config if provided
            var host = _config?["SettingsApi:Host"];
            if (!string.IsNullOrWhiteSpace(host)) 
            {
                _logger.LogInformation($"Using host from config: {host}");
                return host;
            }
            
            // Fallback to machine name
            var machineName = Environment.MachineName;
            _logger.LogInformation($"Using machine name as host: {machineName}");
            return machineName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSettingsHost");
            return "unknown";
        }
    }

    private async void SaveApi_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Merge base + user preserving others
            var basePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            using var baseDoc = File.Exists(basePath) ? JsonDocument.Parse(File.ReadAllText(basePath)) : JsonDocument.Parse("{}\n");
            using var userDoc = File.Exists(path) ? JsonDocument.Parse(File.ReadAllText(path)) : JsonDocument.Parse("{}\n");

            var root = new System.Collections.Generic.Dictionary<string, object?>();
            void CopyOthers(JsonElement el)
            {
                foreach (var prop in el.EnumerateObject())
                {
                    if (!string.Equals(prop.Name, "Api", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(prop.Name, "SettingsApi", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(prop.Name, "TablesApi", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(prop.Name, "UI", StringComparison.OrdinalIgnoreCase) &&
                        !root.ContainsKey(prop.Name))
                    {
                        root[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                    }
                }
            }
            CopyOthers(baseDoc.RootElement);
            CopyOthers(userDoc.RootElement);

            // Validate inputs
            var paperTag = ((PrinterWidthCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()) ?? "58";
            if (paperTag != "58" && paperTag != "80") { StatusText.Text = "Paper width must be 58 or 80"; return; }
            var taxText = string.IsNullOrWhiteSpace(TaxPercentText.Text) ? "0" : TaxPercentText.Text.Trim();
            if (!decimal.TryParse(taxText, NumberStyles.Number, CultureInfo.InvariantCulture, out var _)) { StatusText.Text = "Tax % must be a decimal"; return; }

            root["Api"] = new { BaseUrl = BaseUrlText.Text };
            var theme = ThemeSelector.SelectedIndex == 1 ? "Dark" : ThemeSelector.SelectedIndex == 2 ? "Light" : "System";
            root["UI"] = new { Theme = theme };
            root["InventoryApi"] = new { BaseUrl = InventoryApiUrlText.Text };
            root["SettingsApi"] = new { BaseUrl = SettingsApiUrlText.Text };
            if (!string.IsNullOrWhiteSpace(SettingsHostText.Text))
            {
                root["SettingsApi"] = new { BaseUrl = SettingsApiUrlText.Text, Host = SettingsHostText.Text };
            }
            root["TablesApi"] = new { BaseUrl = TablesApiUrlText.Text };
            // Printer settings
            root["Printer"] = new { PaperWidthMm = paperTag, TaxPercent = taxText };

            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            BackendSummaryText.Text = $"Backend: {BaseUrlText.Text}";
            ThemeSummaryText.Text = $"Theme: {theme}";
            StatusText.Text = App.I18n.T("saved");
            // Re-init services using the new URLs immediately
            ReinitServicesFromUI();

            // Persist to backend settings (source of truth)
            var be = new SettingsApiService.BackendSettings
            {
                BackendApiUrl = BaseUrlText.Text?.Trim(),
                SettingsApiUrl = SettingsApiUrlText.Text?.Trim(),
                TablesApiUrl = TablesApiUrlText.Text?.Trim()
            };
            await _settingsApi.SaveBackendAsync(be, GetSettingsHost());
            Toast("Saved");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void ReinitServicesFromUI()
    {
        try
        {
            var settingsUrl = SettingsApiUrlText.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(settingsUrl))
            {
                _settingsApi = new SettingsApiService(new HttpClient { BaseAddress = new Uri(settingsUrl!) }, null);
            }
            // Recreate core ApiService with backend + inventory URLs
            var backendUrl = BaseUrlText.Text?.Trim();
            var inventoryUrl = string.IsNullOrWhiteSpace(InventoryApiUrlText.Text) ? backendUrl : InventoryApiUrlText.Text.Trim();
            if (!string.IsNullOrWhiteSpace(backendUrl))
            {
                MagiDesk.Frontend.App.ReinitializeApi(backendUrl!, inventoryUrl ?? backendUrl!);
            }
        }
        catch { }
    }

    private async void TestPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Read printer settings from current UI
            var paperTag = ((PrinterWidthCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()) ?? "58";
            int paper = 58; int.TryParse(paperTag, out paper);
            decimal tax = 0m; decimal.TryParse(TaxPercentText.Text?.Trim() ?? "0", System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out tax);

            // Build a sample bill
            var bill = new MagiDesk.Shared.DTOs.Tables.BillResult
            {
                BillId = Guid.NewGuid(),
                TableLabel = "Demo Table",
                ServerId = "demo",
                ServerName = "DEMO",
                StartTime = DateTime.UtcNow.AddMinutes(-17),
                EndTime = DateTime.UtcNow,
                TotalTimeMinutes = 17,
                Items = new System.Collections.Generic.List<MagiDesk.Shared.DTOs.Tables.ItemLine>
                {
                    new MagiDesk.Shared.DTOs.Tables.ItemLine{ itemId="COFFEE", name="Coffee", quantity=2, price=3.5m },
                    new MagiDesk.Shared.DTOs.Tables.ItemLine{ itemId="SODA", name="Soda", quantity=1, price=2m },
                },
                TimeCost = 0m,
                ItemsCost = 9m,
                TotalAmount = 9m
            };

            var view = Services.ReceiptFormatter.BuildReceiptView(bill, paper, tax);
            await Services.PrintService.PrintVisualAsync(view);
            Toast("Printed test receipt");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void PingApis_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler { ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
            { Timeout = TimeSpan.FromSeconds(6) };

            async Task<(string status, bool ok)> PingSettingsAsync(string? baseUrl)
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return ("-", false);
                try
                {
                    var u = baseUrl.TrimEnd('/') + "/api/settings/frontend";
                    using var res = await http.GetAsync(u);
                    return res.IsSuccessStatusCode ? ("Online", true) : ($"HTTP {(int)res.StatusCode}", true);
                }
                catch { return ("Err", false); }
            }

            async Task<(string status, bool ok)> PingTablesAsync(string? baseUrl)
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return ("-", false);
                try
                {
                    var u = baseUrl.TrimEnd('/') + "/health";
                    using var res = await http.GetAsync(u);
                    return res.IsSuccessStatusCode ? ("Online", true) : ($"HTTP {(int)res.StatusCode}", true);
                }
                catch { return ("Err", false); }
            }

            async Task<(string status, bool ok)> PingInventoryAsync(string? baseUrl)
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return ("-", false);
                try
                {
                    var u = baseUrl.TrimEnd('/') + "/health";
                    using var res = await http.GetAsync(u);
                    return res.IsSuccessStatusCode ? ("Online", true) : ($"HTTP {(int)res.StatusCode}", true);
                }
                catch { return ("Err", false); }
            }

            async Task<(string status, bool ok)> PingBackendAsync(string? baseUrl)
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return ("-", false);
                try
                {
                    var u = baseUrl.TrimEnd('/') + "/";
                    using var res = await http.GetAsync(u);
                    if (res.IsSuccessStatusCode) return ("Online", true);
                    // Try swagger as a fallback common endpoint
                    var swagger = baseUrl.TrimEnd('/') + "/swagger/index.html";
                    using var res2 = await http.GetAsync(swagger);
                    return res2.IsSuccessStatusCode ? ("Online", true) : ($"HTTP {(int)res.StatusCode}", true);
                }
                catch { return ("Err", false); }
            }

            var (settingsStatus, settingsOk) = await PingSettingsAsync(SettingsApiUrlText.Text);
            var (inventoryStatus, inventoryOk) = await PingInventoryAsync(InventoryApiUrlText.Text);
            var (tablesStatus, tablesOk) = await PingTablesAsync(TablesApiUrlText.Text);
            var (backendStatus, backendOk) = await PingBackendAsync(BaseUrlText.Text);

            SettingsStatusText.Text = $"Settings API: {settingsStatus}";
            InventoryStatusText.Text = $"Inventory API: {inventoryStatus}";
            TablesStatusText.Text = $"Tables API: {tablesStatus}";
            BackendSummaryText.Text = $"Backend: {BaseUrlText.Text} ({backendStatus})";

            SettingsStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(settingsOk ? Microsoft.UI.Colors.SeaGreen : Microsoft.UI.Colors.Firebrick);
            InventoryStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(inventoryOk ? Microsoft.UI.Colors.SeaGreen : Microsoft.UI.Colors.Firebrick);
            TablesStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(tablesOk ? Microsoft.UI.Colors.SeaGreen : Microsoft.UI.Colors.Firebrick);
            BackendStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(backendOk ? Microsoft.UI.Colors.SeaGreen : Microsoft.UI.Colors.Firebrick);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    // Category switching: show only selected panel
    // CategoryList_SelectionChanged method removed - using simplified layout with all panels visible

    private async void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadingRing.IsActive = true; LoadingRing.Visibility = Visibility.Visible;
            var fe = await _settingsApi.GetFrontendDefaultsAsync();
            var be = await _settingsApi.GetBackendDefaultsAsync();
            var app = await _settingsApi.GetAppDefaultsAsync();
            if (fe != null)
            {
                var frontendSettings = new SettingsApiService.FrontendSettings();
                foreach (var kvp in fe)
                {
                    frontendSettings[kvp.Key] = kvp.Value;
                }
                
                BaseUrlText.Text = frontendSettings.ApiBaseUrl ?? BaseUrlText.Text;
                var theme = string.IsNullOrWhiteSpace(frontendSettings.Theme) ? "System" : frontendSettings.Theme!;
                ThemeSelector.SelectedIndex = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 1 : theme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                if (frontendSettings.RatePerMinute.HasValue)
                {
                    CurrentRateText.Text = $"Current Rate: {frontendSettings.RatePerMinute.Value.ToString("0.##", CultureInfo.InvariantCulture)}";
                }
            }
            if (be != null)
            {
                var backendSettings = new SettingsApiService.BackendSettings();
                foreach (var kvp in be)
                {
                    backendSettings[kvp.Key] = kvp.Value;
                }
                
                if (!string.IsNullOrWhiteSpace(backendSettings.BackendApiUrl)) BaseUrlText.Text = backendSettings.BackendApiUrl;
                if (!string.IsNullOrWhiteSpace(backendSettings.SettingsApiUrl)) SettingsApiUrlText.Text = backendSettings.SettingsApiUrl;
                if (!string.IsNullOrWhiteSpace(backendSettings.TablesApiUrl)) TablesApiUrlText.Text = backendSettings.TablesApiUrl;
            }
            if (app != null && app.Extras != null)
            {
                if (app.Extras.TryGetValue("tables.session.warnMinutes", out var warn) && warn != null && int.TryParse(warn.ToString(), out var wm)) 
                {
                    if (WarnMinutesBox != null) WarnMinutesBox.Text = wm.ToString();
                }
                if (app.Extras.TryGetValue("tables.session.autoStopMinutes", out var auto) && auto != null && int.TryParse(auto.ToString(), out var am)) 
                {
                    if (AutoStopMinutesBox != null) AutoStopMinutesBox.Text = am.ToString();
                }
            }
            Toast("Defaults loaded");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            LoadingRing.IsActive = false; LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private void Toast(string message)
    {
        try
        {
            ToastBar.Message = message;
            ToastBar.IsOpen = true;
            var _ = Task.Delay(2500).ContinueWith(_ => DispatcherQueue.TryEnqueue(() => ToastBar.IsOpen = false));
        }
        catch { }
    }

    private async void RateLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var repo = new Services.TableRepository();
            var rate = await repo.GetRatePerMinuteAsync();
            if (rate.HasValue)
            {
                if (RatePerMinuteBox != null) 
                    RatePerMinuteBox.Text = rate.Value.ToString("0.##", CultureInfo.InvariantCulture);
                CurrentRateText.Text = $"Current Rate: {rate.Value.ToString("0.##", CultureInfo.InvariantCulture)}";
                Toast("Rate loaded");
            }
            else
            {
                StatusText.Text = "Rate not configured";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void RateSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var val = RatePerMinuteBox?.Text;
            if (string.IsNullOrWhiteSpace(val) || !decimal.TryParse(val, out var rate) || rate < 0)
            {
                StatusText.Text = "Rate must be >= 0";
                return;
            }
            var repo = new Services.TableRepository();
            var ok = await repo.SetRatePerMinuteAsync(rate);
            if (ok)
            {
                CurrentRateText.Text = $"Current Rate: {rate.ToString("0.##", CultureInfo.InvariantCulture)}";
                Toast("Rate saved");
            }
            else
            {
                StatusText.Text = "Failed to save rate (check Tables API URL)";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
