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

namespace MagiDesk.Frontend.Views;

public sealed partial class SettingsPage : Page
{
    private IConfigurationRoot? _config;
    private SettingsApiService _settingsApi;

    public SettingsPage()
    {
        this.InitializeComponent();
        ApplyLanguage();
        App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        LoadConfig();
        // Rate controls removed in minimal layout
        // Prefer dedicated SettingsApi base URL from config if available
        var settingsBase = _config?["SettingsApi:BaseUrl"]
            ?? App.Api?.BackendBase?.ToString()
            ?? "https://magidesk-settings-904541739138.us-central1.run.app/";
        _settingsApi = new SettingsApiService(settingsBase);
        // Default to first category
        try { CategoryList.SelectedIndex = 0; } catch { }
        _ = LoadFromBackendAsync();
        _ = UpdateAppSummaryAsync();
    }

    private async Task LoadAuditAsync()
    {
        try
        {
            var list = await _settingsApi.GetAuditAsync(GetSettingsHost(), 50);
            // Display a simplified projection for readability
            var view = list.Select(x => new
            {
                section = x.ContainsKey("section") ? x["section"] : null,
                timestamp = x.ContainsKey("timestamp") ? x["timestamp"] : null,
                changes = x.ContainsKey("changes") ? System.Text.Json.JsonSerializer.Serialize(x["changes"]) : null
            }).ToList();
            AuditList.ItemsSource = view;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Audit error: {ex.Message}";
        }
    }

    private async void AuditRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadAuditAsync();
    }

    private void LoadConfig()
    {
        try
        {
            var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true);
            _config = builder.Build();

            var baseUrl = _config["Api:BaseUrl"] ?? string.Empty;
            BaseUrlText.Text = baseUrl;
            InventoryApiUrlText.Text = _config["InventoryApi:BaseUrl"] ?? InventoryApiUrlText.Text;
            // Prepopulate Settings/Tables API URLs when available
            SettingsApiUrlText.Text = _config["SettingsApi:BaseUrl"] ?? SettingsApiUrlText.Text;
            SettingsHostText.Text = _config["SettingsApi:Host"] ?? SettingsHostText.Text;
            TablesApiUrlText.Text = _config["TablesApi:BaseUrl"] ?? TablesApiUrlText.Text;
            // Printer settings
            var paper = _config["Printer:PaperWidthMm"];
            if (paper == "80") PrinterWidthCombo.SelectedIndex = 1; else PrinterWidthCombo.SelectedIndex = 0;
            var tax = _config["Printer:TaxPercent"];
            if (!string.IsNullOrWhiteSpace(tax)) TaxPercentText.Text = tax;

            // Apply Theme from config when available
            var theme = (_config["UI:Theme"] ?? "System").Trim();
            ThemeSelector.SelectedIndex = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? 1
                : theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : 0; // System
        }
        catch
        {
            // ignore config errors for now in UI
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string theme = ThemeSelector.SelectedIndex == 1 ? "Dark" : ThemeSelector.SelectedIndex == 2 ? "Light" : "System";

            // Merge base appsettings.json with existing user settings to preserve other sections
            var basePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            using var baseDoc = File.Exists(basePath) ? JsonDocument.Parse(File.ReadAllText(basePath)) : JsonDocument.Parse("{}\n");
            using var userDoc = File.Exists(path) ? JsonDocument.Parse(File.ReadAllText(path)) : JsonDocument.Parse("{}\n");

            // Build a new combined JSON with Api.BaseUrl and UI.Theme
            var root = new System.Collections.Generic.Dictionary<string, object?>();

            // Preserve any existing sections we don't manage explicitly
            void CopyOthers(JsonElement el)
            {
                foreach (var prop in el.EnumerateObject())
                {
                    if (!string.Equals(prop.Name, "Api", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(prop.Name, "UI", StringComparison.OrdinalIgnoreCase) &&
                        !root.ContainsKey(prop.Name))
                    {
                        root[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                    }
                }
            }
            CopyOthers(baseDoc.RootElement);
            CopyOthers(userDoc.RootElement);

            root["Api"] = new { BaseUrl = BaseUrlText.Text };
            root["UI"] = new { Theme = theme };

            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            try { App.ApplyThemeToRoot(theme); }
            catch (Exception applyEx) { StatusText.Text = $"Error: {applyEx}"; return; }

            // Save to backend as the source of truth
            _ = SaveToBackendAsync(BaseUrlText.Text, theme);
            // Also persist session timers via App settings Extras
            try
            {
                var extras = new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["tables.session.warnMinutes"] = (int)(WarnMinutesBox.Value <= 0 ? 0 : WarnMinutesBox.Value),
                    ["tables.session.autoStopMinutes"] = (int)(AutoStopMinutesBox.Value <= 0 ? 0 : AutoStopMinutesBox.Value)
                };
                var appSet = new SettingsApiService.AppSettings { Extras = extras };
                _ = _settingsApi.SaveAppAsync(appSet, GetSettingsHost());
            }
            catch { }
            StatusText.Text = App.I18n.T("saved");
            Toast("Saved");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex}";
        }
    }

    private async Task LoadFromBackendAsync()
    {
        try
        {
            LoadingRing.IsActive = true; LoadingRing.Visibility = Visibility.Visible;
            var host = GetSettingsHost();
            var fe = await _settingsApi.GetFrontendAsync(host);
            if (fe != null)
            {
                if (!string.IsNullOrWhiteSpace(fe.ApiBaseUrl)) BaseUrlText.Text = fe.ApiBaseUrl;
                var theme = string.IsNullOrWhiteSpace(fe.Theme) ? "System" : fe.Theme!;
                ThemeSelector.SelectedIndex = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 1 : theme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                BackendSummaryText.Text = $"Backend: {BaseUrlText.Text}";
                ThemeSummaryText.Text = $"Theme: {theme}";
                if (fe.RatePerMinute.HasValue)
                {
                    CurrentRateText.Text = $"Current Rate: {fe.RatePerMinute.Value.ToString("0.##", CultureInfo.InvariantCulture)}";
                }
            }

            // Load backend connection settings
            var be = await _settingsApi.GetBackendAsync(host);
            if (be != null)
            {
                if (!string.IsNullOrWhiteSpace(be.BackendApiUrl)) BaseUrlText.Text = be.BackendApiUrl;
                if (!string.IsNullOrWhiteSpace(be.SettingsApiUrl)) SettingsApiUrlText.Text = be.SettingsApiUrl;
                if (!string.IsNullOrWhiteSpace(be.TablesApiUrl)) TablesApiUrlText.Text = be.TablesApiUrl;
            }
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
                        if (int.TryParse(warn.ToString(), out var wm)) WarnMinutesBox.Value = wm;
                    }
                    if (app.Extras.TryGetValue("tables.session.autoStopMinutes", out var auto) && auto != null)
                    {
                        if (int.TryParse(auto.ToString(), out var am)) AutoStopMinutesBox.Value = am;
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
            BackendUrlLabel.Text = App.I18n.T("backend_base_url");
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
                if (fe?.RatePerMinute is decimal r) current = r;
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
            // Prefer explicit override from config if provided
            var host = _config?["SettingsApi:Host"];
            if (!string.IsNullOrWhiteSpace(host)) return host;
            // Fallback to machine name
            return Environment.MachineName;
        }
        catch { return Environment.MachineName; }
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
                _settingsApi = new SettingsApiService(settingsUrl!);
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
    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var idx = CategoryList.SelectedIndex;
            GeneralPanel.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
            ConnectionsPanel.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
            BillingPanel.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
            TablesPanel.Visibility = idx == 3 ? Visibility.Visible : Visibility.Collapsed;
            AuditPanel.Visibility = idx == 4 ? Visibility.Visible : Visibility.Collapsed;
            if (idx == 3)
            {
                // Prefill rate from Tables API
                _ = DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        var repo = new Services.TableRepository();
                        var rate = await repo.GetRatePerMinuteAsync();
                        if (rate.HasValue)
                        {
                            RatePerMinuteBox.Value = (double)rate.Value;
                            CurrentRateText.Text = $"Current Rate: {rate.Value.ToString("0.##", CultureInfo.InvariantCulture)}";
                        }
                    }
                    catch { }
                });
            }
            else if (idx == 4)
            {
                // Auto-load audit on first show
                _ = DispatcherQueue.TryEnqueue(async () =>
                {
                    try { await LoadAuditAsync(); } catch { }
                });
            }
        }
        catch { }
    }

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
                BaseUrlText.Text = fe.ApiBaseUrl ?? BaseUrlText.Text;
                var theme = string.IsNullOrWhiteSpace(fe.Theme) ? "System" : fe.Theme!;
                ThemeSelector.SelectedIndex = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 1 : theme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                if (fe.RatePerMinute.HasValue)
                {
                    CurrentRateText.Text = $"Current Rate: {fe.RatePerMinute.Value.ToString("0.##", CultureInfo.InvariantCulture)}";
                }
            }
            if (be != null)
            {
                if (!string.IsNullOrWhiteSpace(be.BackendApiUrl)) BaseUrlText.Text = be.BackendApiUrl;
                if (!string.IsNullOrWhiteSpace(be.SettingsApiUrl)) SettingsApiUrlText.Text = be.SettingsApiUrl;
                if (!string.IsNullOrWhiteSpace(be.TablesApiUrl)) TablesApiUrlText.Text = be.TablesApiUrl;
            }
            if (app != null && app.Extras != null)
            {
                if (app.Extras.TryGetValue("tables.session.warnMinutes", out var warn) && warn != null && int.TryParse(warn.ToString(), out var wm)) WarnMinutesBox.Value = wm;
                if (app.Extras.TryGetValue("tables.session.autoStopMinutes", out var auto) && auto != null && int.TryParse(auto.ToString(), out var am)) AutoStopMinutesBox.Value = am;
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
                RatePerMinuteBox.Value = (double)rate.Value;
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
            var val = RatePerMinuteBox.Value;
            if (val < 0)
            {
                StatusText.Text = "Rate must be >= 0";
                return;
            }
            var repo = new Services.TableRepository();
            var ok = await repo.SetRatePerMinuteAsync(Convert.ToDecimal(val));
            if (ok)
            {
                CurrentRateText.Text = $"Current Rate: {Convert.ToDecimal(val).ToString("0.##", CultureInfo.InvariantCulture)}";
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
