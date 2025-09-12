using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text.Json;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Configuration;
using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.Views;

public sealed partial class TablesPage : Page
{
    public ObservableCollection<TableItem> BilliardTables { get; } = new();
    public ObservableCollection<TableItem> BarTables { get; } = new();
    public ObservableCollection<TableItem> FilteredBilliardTables { get; } = new();
    public ObservableCollection<TableItem> FilteredBarTables { get; } = new();
    private TableRepository _repo = new TableRepository();
    private DispatcherTimer _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
    private readonly string _timerCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "timers.json");
    private readonly string _thresholdsCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "thresholds.json");

    public TablesPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        Loaded += TablesPage_Loaded;
        _pollTimer.Tick += PollTimer_Tick;
    }

    private Dictionary<string, int> LoadThresholdsCache()
    {
        try
        {
            var dir = Path.GetDirectoryName(_thresholdsCachePath)!;
            Directory.CreateDirectory(dir);
            if (!File.Exists(_thresholdsCachePath)) return new();
            var json = File.ReadAllText(_thresholdsCachePath);
            var map = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
            return map ?? new();
        }
        catch { return new(); }
    }

    private void SaveThresholdsCache(Dictionary<string, int> map)
    {
        try
        {
            var dir = Path.GetDirectoryName(_thresholdsCachePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_thresholdsCachePath, json);
        }
        catch { }
    }

    private async void SetThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        // Prompt for minutes
        var panel = new StackPanel { Spacing = 8 };
        var nb = new NumberBox { SmallChange = 5, Minimum = 1, Maximum = 720, Value = item.ThresholdMinutes.HasValue ? item.ThresholdMinutes.Value : 30 };
        panel.Children.Add(new TextBlock { Text = $"Set threshold for {item.Label} (minutes):" });
        panel.Children.Add(nb);
        var dlg = new ContentDialog
        {
            Title = "Set Threshold",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        var res = await dlg.ShowAsync();
        if (res != ContentDialogResult.Primary) return;
        item.ThresholdMinutes = (int)Math.Round(nb.Value);
        // Persist immediately
        var thresholds = LoadThresholdsCache();
        thresholds[item.Label] = item.ThresholdMinutes.Value;
        SaveThresholdsCache(thresholds);
        // Refresh visuals
        item.Tick();
    }

    private void ClearThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        item.ThresholdMinutes = null;
        var thresholds = LoadThresholdsCache();
        if (thresholds.ContainsKey(item.Label)) { thresholds.Remove(item.Label); SaveThresholdsCache(thresholds); }
        item.Tick();
    }

    private async void MoveMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        // List available tables
        var available = await _repo.GetAvailableTableLabelsAsync();
        // Remove current label if it somehow appears as available
        available.RemoveAll(l => string.Equals(l, item.Label, StringComparison.OrdinalIgnoreCase));
        if (available.Count == 0)
        {
            await new ContentDialog { Title = "No available tables", Content = "There are no free tables to move to.", CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
            return;
        }
        var combo = new ComboBox { ItemsSource = available, SelectedIndex = 0, Width = 280 };
        var dlg = new ContentDialog
        {
            Title = $"Move {item.Label} to:",
            Content = combo,
            PrimaryButtonText = "Move",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        var res = await dlg.ShowAsync();
        if (res != ContentDialogResult.Primary) return;
        var target = combo.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(target)) return;

        var move = await _repo.MoveSessionAsync(item.Label, target!);
        if (!move.ok)
        {
            await new ContentDialog { Title = "Move failed", Content = move.message, CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
            return;
        }
        await RefreshFromStoreAsync();
    }

    private (int paperMm, decimal taxPercent) ReadPrinterConfig()
    {
        try
        {
            var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true)
                .Build();
            var paperStr = cfg["Printer:PaperWidthMm"] ?? "58";
            var taxStr = cfg["Printer:TaxPercent"] ?? "0";
            int paper = 58;
            int.TryParse(paperStr, out paper);
            decimal tax = 0m;
            decimal.TryParse(taxStr, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out tax);
            return (paper, tax);
        }
        catch { return (58, 0m); }
    }

    private async Task<FrameworkElement> BuildReceiptPreviewAsync(MagiDesk.Shared.DTOs.Tables.BillResult bill)
    {
        await Task.Yield();
        var (paper, tax) = ReadPrinterConfig();
        // Use new PDFSharp-based preview instead of old ReceiptFormatter
        // For now, return a simple text preview since PDFSharp generates files, not UI elements
        var panel = new StackPanel { Spacing = 4, Padding = new Thickness(6) };
        panel.Children.Add(new TextBlock { Text = $"Bill Preview for {bill.BillId}", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
        panel.Children.Add(new TextBlock { Text = $"Table: {bill.TableLabel}" });
        panel.Children.Add(new TextBlock { Text = $"Server: {bill.ServerName}" });
        panel.Children.Add(new TextBlock { Text = $"Total: {bill.TotalAmount:C}" });
        panel.Children.Add(new TextBlock { Text = "Note: Full receipt will be generated as PDF" });
        return panel;
    }

public class AddItemRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _itemId = string.Empty;
    public string ItemId { get => _itemId; set { _itemId = value; OnPropertyChanged(); } }

    private string _name = string.Empty;
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

    private decimal _price;
    public decimal Price { get => _price; set { _price = value; OnPropertyChanged(); } }

    private int _quantity;
    public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); } }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

    private async void TablesPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        FilterCombo.SelectedIndex = 0; // All
        await LoadFromDatabaseAsync();
        // Leverage the unified refresh to load caches, rehydrate, and persist
        await RefreshFromStoreAsync();
    }

    private Dictionary<string, DateTimeOffset> LoadTimersCache()
    {
        try
        {
            var dir = Path.GetDirectoryName(_timerCachePath)!;
            Directory.CreateDirectory(dir);
            if (!File.Exists(_timerCachePath)) return new();
            var json = File.ReadAllText(_timerCachePath);
            var map = JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(json);
            return map ?? new();
        }
        catch { return new(); }
    }

    private void SaveTimersCache(Dictionary<string, DateTimeOffset> map)
    {
        try
        {
            var dir = Path.GetDirectoryName(_timerCachePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_timerCachePath, json);
        }
        catch { }
    }

    private async Task LoadFromDatabaseAsync()
    {
        BilliardTables.Clear();
        BarTables.Clear();
        await _repo.EnsureSchemaAsync();
        var rows = await _repo.GetAllAsync();
        foreach (var r in rows)
        {
            var item = new TableItem(r.Label) { Occupied = r.Occupied };
            item.OrderId = r.OrderId; item.StartTime = r.StartTime; item.Server = r.Server;
            if (r.Type == "billiard") BilliardTables.Add(item);
            else BarTables.Add(item);
        }
        // Seed if empty
        if (rows.Count == 0)
        {
            var toInsert = new System.Collections.Generic.List<TableStatusDto>();
            for (int i = 1; i <= 5; i++) toInsert.Add(new TableStatusDto { Label = $"Billiard {i}", Type = "billiard", Occupied = false });
            for (int i = 1; i <= 10; i++) toInsert.Add(new TableStatusDto { Label = $"Bar {i}", Type = "bar", Occupied = false });
            await _repo.UpsertManyAsync(toInsert);
            // Populate UI immediately
            foreach (var rec in toInsert)
            {
                var item = new TableItem(rec.Label) { Occupied = rec.Occupied };
                if (rec.Type == "billiard") BilliardTables.Add(item); else BarTables.Add(item);
            }
        }
        // Safety: if still empty for any reason, seed in-memory and persist
        if (BilliardTables.Count == 0 && BarTables.Count == 0)
        {
            var toInsert = new System.Collections.Generic.List<TableStatusDto>();
            for (int i = 1; i <= 5; i++) toInsert.Add(new TableStatusDto { Label = $"Billiard {i}", Type = "billiard", Occupied = false });
            for (int i = 1; i <= 10; i++) toInsert.Add(new TableStatusDto { Label = $"Bar {i}", Type = "bar", Occupied = false });
            foreach (var rec in toInsert)
            {
                var item = new TableItem(rec.Label) { Occupied = rec.Occupied };
                if (rec.Type == "billiard") BilliardTables.Add(item); else BarTables.Add(item);
            }
            try { await _repo.UpsertManyAsync(toInsert); } catch { }
        }
        ApplyFilter();
    }

    // Click disabled per request; no handler needed.

    private void ApplyFilter()
    {
        FilteredBilliardTables.Clear();
        FilteredBarTables.Clear();
        string mode = (FilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        bool include(TableItem t) => mode == "All" || (mode == "Available" && !t.Occupied) || (mode == "Occupied" && t.Occupied);
        foreach (var t in BilliardTables) if (include(t)) FilteredBilliardTables.Add(t);
        foreach (var t in BarTables) if (include(t)) FilteredBarTables.Add(t);
        UpdateStatus();
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private async Task ShowSafeContentDialog(string title, string content, string closeButtonText)
    {
        try
        {
            // CRITICAL FIX: Use App.MainWindow instead of Window.Current to avoid COM exceptions
            // Window.Current can be null during initialization in WinUI 3 Desktop Apps
            var xamlRoot = App.MainWindow?.Content is FrameworkElement mainContent 
                ? mainContent.XamlRoot 
                : this.XamlRoot;

            await new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = closeButtonText,
                XamlRoot = xamlRoot
            }.ShowAsync();
        }
        catch (Exception ex)
        {
            // Fallback: Log error instead of showing dialog to prevent COM exceptions
            Log.Error($"Failed to show ContentDialog: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ContentDialog Error: {title} - {content}");
        }
    }

    private async void PollTimer_Tick(object sender, object e)
    {
        await RefreshFromStoreAsync();
        try
        {
            foreach (var t in FilteredBilliardTables)
            {
                t.Tick();
            }
        }
        catch { }
    }

    private async Task RefreshFromStoreAsync()
    {
        // Load latest tables
        var rows = await _repo.GetAllAsync();
        var dict = rows.ToDictionary(r => r.Label, r => r, StringComparer.OrdinalIgnoreCase);
        // Load local timers cache for offline resiliency
        var timers = LoadTimersCache();
        var thresholds = LoadThresholdsCache();
        void reconcile(ObservableCollection<TableItem> list, string type)
        {
            foreach (var item in list)
            {
                if (dict.TryGetValue(item.Label, out var rec))
                {
                    item.Occupied = rec.Occupied;
                    item.OrderId = rec.OrderId; item.StartTime = rec.StartTime; item.Server = rec.Server;
                    // If occupied but start time missing, try local cache
                    if (item.Occupied && !item.StartTime.HasValue && timers.TryGetValue(item.Label, out var cached))
                    {
                        item.StartTime = cached;
                    }
                    // Load threshold if present for this table
                    if (thresholds.TryGetValue(item.Label, out var thVal))
                    {
                        item.ThresholdMinutes = thVal;
                    }
                    else
                    {
                        item.ThresholdMinutes = null; // default: no threshold
                    }
                }
            }
        }
        reconcile(BilliardTables, "billiard");
        reconcile(BarTables, "bar");
        // Recovery: if any occupied billiard table has null StartTime, try to rehydrate from active sessions API
        try
        {
            var needsStart = BilliardTables.Where(t => t.Occupied && !t.StartTime.HasValue).Select(t => t.Label).ToList();
            if (needsStart.Count > 0)
            {
                var actives = await _repo.GetActiveSessionsAsync();
                if (actives != null && actives.Count > 0)
                {
                    var map = actives.Where(a => !string.IsNullOrWhiteSpace(a.TableId))
                                     .ToDictionary(a => a.TableId, a => a, StringComparer.OrdinalIgnoreCase);
                    foreach (var t in BilliardTables)
                    {
                        if (t.Occupied && !t.StartTime.HasValue && map.TryGetValue(t.Label, out var s))
                        {
                            t.StartTime = new DateTimeOffset(s.StartTime, TimeSpan.Zero);
                        }
                    }
                }
            }
        }
        catch { }
        ApplyFilter();
        UpdateStatus();
    }

    private void LiveToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (LiveToggle.IsOn) _pollTimer.Start(); else _pollTimer.Stop();
    }

    private void UpdateStatus()
    {
        try
        {
            SourceText.Text = _repo.SourceLabel;
            CountsText.Text = $"Billiard: {BilliardTables.Count}  |  Bar: {BarTables.Count}";
        }
        catch { }
    }

    // Context menu handlers
    private async void StartMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        // Prompt for server
        var (ok, user) = await PromptSelectEmployeeAsync();
        if (!ok || user == null) return;

        var startResult = await _repo.StartSessionAsync(item.Label, user.UserId ?? string.Empty, user.Username ?? string.Empty);
        if (!startResult.ok)
        {
            await new ContentDialog
            {
                Title = "Unable to start",
                Content = startResult.message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
            return;
        }
        // Set context and auto-create/find order
        try
        {
            if (!string.IsNullOrWhiteSpace(startResult.sessionId)) OrderContext.CurrentSessionId = startResult.sessionId;
            if (!string.IsNullOrWhiteSpace(startResult.billingId)) OrderContext.CurrentBillingId = startResult.billingId;
            // Find existing order by session
            var ordersSvc = App.OrdersApi;
            if (ordersSvc != null && !string.IsNullOrWhiteSpace(startResult.sessionId) && Guid.TryParse(startResult.sessionId, out var sessionGuid))
            {
                var list = await ordersSvc.GetOrdersBySessionAsync(sessionGuid);
                var current = list.FirstOrDefault(o => o.SessionId == sessionGuid);
                if (current == null)
                {
                    // Create empty order
                    Guid? billingGuid = null;
                    if (!string.IsNullOrWhiteSpace(startResult.billingId) && Guid.TryParse(startResult.billingId, out var parsedBillingId))
                        billingGuid = parsedBillingId;
                    var req = new Services.OrderApiService.CreateOrderRequestDto(sessionGuid, billingGuid, item.Label, user.UserId ?? string.Empty, user.Username ?? string.Empty, Array.Empty<Services.OrderApiService.CreateOrderItemDto>());
                    var created = await ordersSvc.CreateOrderAsync(req);
                    if (created != null) current = created;
                }
                if (current != null)
                {
                    OrderContext.CurrentOrderId = current.Id;
                }
            }
        }
        catch { }

        await RefreshFromStoreAsync();
    }

    private async void StopMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        
        // First, close any open orders for this table's session
        try
        {
            var ordersSvc = App.OrdersApi;
            if (ordersSvc != null)
            {
                // Get the current session ID for this table
                var (activeSessionId, activeBillingId) = await _repo.GetActiveSessionForTableAsync(item.Label);
                if (activeSessionId.HasValue)
                {
                    // Get all open orders for this session
                    var openOrders = await ordersSvc.GetOrdersBySessionAsync(activeSessionId.Value, includeHistory: false);
                    var ordersToClose = openOrders.Where(o => o.Status == "open").ToList();
                    
                    // Close each open order
                    foreach (var order in ordersToClose)
                    {
                        try
                        {
                            await ordersSvc.CloseOrderAsync(order.Id);
                        }
                        catch (Exception ex)
                        {
                            // Log the error but continue closing other orders
                            System.Diagnostics.Debug.WriteLine($"Failed to close order {order.Id}: {ex.Message}");
                        }
                    }
                    
                    if (ordersToClose.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Closed {ordersToClose.Count} orders for session {activeSessionId.Value}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but continue with table stopping
            System.Diagnostics.Debug.WriteLine($"Error closing orders: {ex.Message}");
        }
        
        // Now proceed with normal table stopping
        var stopResult = await _repo.StopSessionAsync(item.Label);
        if (!stopResult.ok)
        {
            // Offer recovery if there is no active session in DB
            bool canRecover = (stopResult.message ?? string.Empty).IndexOf("no active session", StringComparison.OrdinalIgnoreCase) >= 0;
            if (canRecover)
            {
                var dlg = new ContentDialog
                {
                    Title = "No active session",
                    Content = "The table shows as occupied but there is no active session. Force free this table?",
                    PrimaryButtonText = "Force Free",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };
                var choice = await dlg.ShowAsync();
                if (choice == ContentDialogResult.Primary)
                {
                    var freed = await _repo.ForceFreeAsync(item.Label);
                    if (!freed)
                    {
                        await new ContentDialog { Title = "Failed", Content = "Could not force free this table.", CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
                    }
                    await RefreshFromStoreAsync();
                }
            }
            else
            {
                await new ContentDialog
                {
                    Title = "Unable to stop",
                    Content = stopResult.message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            return;
        }

        await RefreshFromStoreAsync();

        if (stopResult.bill != null)
        {
            var bill = stopResult.bill;
            var itemsText = bill.Items != null && bill.Items.Count > 0
                ? ("\nItems:\n" + string.Join("\n", bill.Items.Select(i => $" - {i.name} x{i.quantity} @ {i.price:C2}")))
                : string.Empty;
            var text = $"Stopped {item.Label}. Total minutes: {bill.TotalTimeMinutes}{itemsText}";

            var dlg = new ContentDialog
            {
                Title = "Session Stopped",
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = "Print",
                SecondaryButtonText = "View",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Secondary)
            {
                // Show receipt preview only
                var preview = await BuildReceiptPreviewAsync(bill);
                await new ContentDialog
                {
                    Title = "Receipt Preview",
                    Content = preview,
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            else if (res == ContentDialogResult.Primary)
            {
                // Use new PDFSharp-based printing
                try
                {
                    if (App.ReceiptService == null)
                    {
                        Log.Error("App.ReceiptService is null");
                        await ShowSafeContentDialog("Print Error", "ReceiptService not initialized", "Close");
                        return;
                    }
                    
                    var migrationService = new Services.ReceiptMigrationService(App.ReceiptService);
                    var (paper, tax) = ReadPrinterConfig();
                    await migrationService.PrintBillAsync(bill, paper, tax, "Default Printer", isProForma: false);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to print bill using PDFSharp", ex);
                    await ShowSafeContentDialog("Print Error", $"Failed to print receipt: {ex.Message}", "Close");
                }
            }
        }
    }

    private async void AddItemsMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;

        try
        {
            // Get active session for this table
            var (sessionId, billingId) = await _repo.GetActiveSessionForTableAsync(item.Label);
            if (!sessionId.HasValue)
            {
                await new ContentDialog
                {
                    Title = "No Active Session",
                    Content = $"Table {item.Label} does not have an active session. Please start a session first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return;
            }

            // Navigate to menu selection page
            var menuParams = new MenuSelectionParams
            {
                TableLabel = item.Label,
                SessionId = sessionId.Value,
                OrderId = OrderContext.CurrentOrderId
            };

            Frame.Navigate(typeof(MenuSelectionPage), menuParams);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening menu dialog: {ex.Message}");
            await new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to open menu: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }

    private DataTemplate BuildAddItemTemplate()
    {
        var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
            <Grid Padding='6'>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width='*'/>
                    <ColumnDefinition Width='100'/>
                    <ColumnDefinition Width='100'/>
                </Grid.ColumnDefinitions>
                <TextBlock Text='{Binding Name}'/>
                <TextBox Grid.Column='1' Text='{Binding Price, Mode=TwoWay}' Header='Price' />
                <NumberBox Grid.Column='2' Value='{Binding Quantity, Mode=TwoWay}' SmallChange='1' Minimum='0' Header='Qty'/>
            </Grid>
        </DataTemplate>";
        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
    }

    private async Task<List<MagiDesk.Frontend.Services.MenuApiService.MenuItemDto>> FetchMenuItemsAsync()
    {
        try
        {
            var svc = App.Menu;
            if (svc == null) return new();
            var items = await svc.ListItemsAsync(new MagiDesk.Frontend.Services.MenuApiService.ItemsQuery(null, null, null, true));
            return items.ToList();
        }
        catch { return new List<MagiDesk.Frontend.Services.MenuApiService.MenuItemDto>(); }
    }

    private string BuildItemsSummary(List<ItemLine> lines)
    {
        if (lines == null || lines.Count == 0) return string.Empty;
        var parts = lines.Select(l => $"{l.name} x{l.quantity}");
        var total = lines.Sum(l => l.price * l.quantity);
        return $"Items: {string.Join(", ", parts)} · {total:C2}";
    }

    private async Task<(bool ok, UserDto? user)> PromptSelectEmployeeAsync()
    {
        // Build API base URL from config
        var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true)
            .Build();
        var apiBase = cfg["Api:BaseUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiBase)) return (false, null);

        var inner = new HttpClientHandler();
        inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var logging = new HttpLoggingHandler(inner);
        using var http = new HttpClient(logging) { BaseAddress = new Uri(apiBase.TrimEnd('/') + "/") };
        var userApi = new UserApiService(http);
        var users = await userApi.GetUsersAsync();
        var employees = users.Where(u => string.Equals(u.Role, "employee", StringComparison.OrdinalIgnoreCase)).ToList();
        if (employees.Count == 0)
        {
            await new ContentDialog { Title = "No employees found", Content = "Please add employees first.", CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
            return (false, null);
        }

        var combo = new ComboBox { ItemsSource = employees, DisplayMemberPath = nameof(UserDto.Username), SelectedIndex = 0, Width = 320 };
        var dlg = new ContentDialog
        {
            Title = "Select Server",
            Content = combo,
            PrimaryButtonText = "Start",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary && combo.SelectedItem is UserDto selected)
            return (true, selected);
        return (false, null);
    }
}

public class TableItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public TableItem(string label)
    {
        _label = label;
        UpdateBrush();
    }

    private string _label;
    public string Label
    {
        get => _label;
        set { _label = value; OnPropertyChanged(); }
    }

    private bool _occupied;
    public bool Occupied
    {
        get => _occupied;
        set { if (_occupied != value) { _occupied = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAvailable)); OnPropertyChanged(nameof(StatusText)); UpdateBrush(); } }
    }

    private string? _orderId;
    public string? OrderId { get => _orderId; set { _orderId = value; OnPropertyChanged(); } }

    private DateTimeOffset? _startTime;
    public DateTimeOffset? StartTime { get => _startTime; set { _startTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); } }

    private string? _server;
    public string? Server { get => _server; set { _server = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); } }

    private Brush _backgroundBrush = new SolidColorBrush(Microsoft.UI.Colors.SeaGreen);
    public Brush BackgroundBrush
    {
        get => _backgroundBrush;
        private set { _backgroundBrush = value; OnPropertyChanged(); }
    }

    public bool IsAvailable => !Occupied;

    public string StatusText
    {
        get
        {
            if (!Occupied) return "Available";
            var start = StartTime?.ToLocalTime().ToString("t") ?? "?";
            var who = string.IsNullOrWhiteSpace(Server) ? "-" : Server;
            return $"Occupied · {who} · {start}";
        }
    }

    // Shows only for billiard tables when a session is running
    public bool HasTimer => Occupied && StartTime.HasValue;

    public string ElapsedMinutesText
    {
        get
        {
            if (!HasTimer) return string.Empty;
            var mins = (int)Math.Floor((DateTimeOffset.Now - StartTime!.Value).TotalMinutes);
            if (mins < 0) mins = 0;
            return $"{Label}: {mins} min elapsed";
        }
    }

    public Brush ChipBackground
    {
        get
        {
            // Default: semi-transparent black
            var dark = Microsoft.UI.Colors.Black;
            dark.A = 0xB3;
            if (!HasTimer) return new SolidColorBrush(dark);
            var mins = (int)Math.Floor((DateTimeOffset.Now - StartTime!.Value).TotalMinutes);
            // If a per-table threshold is set, use it; otherwise no threshold highlighting
            if (ThresholdMinutes.HasValue && mins >= ThresholdMinutes.Value)
            {
                var amber = Microsoft.UI.Colors.Orange;
                amber.A = 0xCC;
                return new SolidColorBrush(amber);
            }
            return new SolidColorBrush(dark);
        }
    }

    // Optional per-table threshold in minutes; when null, feature is off
    private int? _thresholdMinutes;
    public int? ThresholdMinutes
    {
        get => _thresholdMinutes;
        set { _thresholdMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(ChipBackground)); }
    }

    private string _itemsSummary = string.Empty;
    public string ItemsSummary
    {
        get => _itemsSummary;
        set { _itemsSummary = value; OnPropertyChanged(); }
    }

    private void UpdateBrush()
    {
        BackgroundBrush = new SolidColorBrush(_occupied ? Microsoft.UI.Colors.Firebrick : Microsoft.UI.Colors.SeaGreen);
    }

    public void Tick()
    {
        OnPropertyChanged(nameof(ElapsedMinutesText));
        OnPropertyChanged(nameof(ChipBackground));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
