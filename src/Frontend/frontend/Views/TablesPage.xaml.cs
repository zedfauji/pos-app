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
using MagiDesk.Shared.DTOs.Users;
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

    private async void DiagnosticsMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        
        try
        {
            // Run comprehensive diagnostics
            var diagnostics = await DiagnoseTableIssuesAsync(item.Label);
            var diagnosticsDialog = new ContentDialog
            {
                Title = $"Table Diagnostics - {item.Label}",
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = diagnostics,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    },
                    MaxHeight = 500
                },
                PrimaryButtonText = "Copy to Clipboard",
                SecondaryButtonText = "Refresh Data",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            var choice = await diagnosticsDialog.ShowAsync();
            if (choice == ContentDialogResult.Primary)
            {
                // Copy diagnostics to clipboard
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(diagnostics);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                
                await new ContentDialog
                {
                    Title = "Copied",
                    Content = "Diagnostics copied to clipboard.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            else if (choice == ContentDialogResult.Secondary)
            {
                // Refresh data and run diagnostics again
                await RefreshFromStoreAsync();
                var refreshedDiagnostics = await DiagnoseTableIssuesAsync(item.Label);
                var refreshedDialog = new ContentDialog
                {
                    Title = $"Table Diagnostics - {item.Label} (Refreshed)",
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = refreshedDiagnostics,
                            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap
                        },
                        MaxHeight = 500
                    },
                    PrimaryButtonText = "Copy to Clipboard",
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };
                var refreshChoice = await refreshedDialog.ShowAsync();
                if (refreshChoice == ContentDialogResult.Primary)
                {
                    var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dataPackage.SetText(refreshedDiagnostics);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                }
            }
        }
        catch (Exception ex)
        {
            await new ContentDialog
            {
                Title = "Diagnostics Error",
                Content = $"Failed to run diagnostics: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
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
            var item = new TableItem(r.Label, r.Type) { Occupied = r.Occupied };
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
                var item = new TableItem(rec.Label, rec.Type) { Occupied = rec.Occupied };
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
                var item = new TableItem(rec.Label, rec.Type) { Occupied = rec.Occupied };
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
        string mode = (FilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Tables";
        bool include(TableItem t) => mode == "All Tables" || (mode == "Available Only" && !t.Occupied) || (mode == "Occupied Only" && t.Occupied);
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
            foreach (var t in FilteredBarTables)
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
                    item.Type = rec.Type; // Update table type
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

    // Comprehensive diagnostic method for table management issues
    private async Task<string> DiagnoseTableIssuesAsync(string tableLabel)
    {
        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine($"=== Table Diagnostics for '{tableLabel}' ===");
        diagnostics.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        diagnostics.AppendLine();

        try
        {
            // Check local table state
            var localTable = BilliardTables.Concat(BarTables).FirstOrDefault(t => t.Label == tableLabel);
            if (localTable != null)
            {
                diagnostics.AppendLine("Local Table State:");
                diagnostics.AppendLine($"  Label: {localTable.Label}");
                diagnostics.AppendLine($"  Occupied: {localTable.Occupied}");
                diagnostics.AppendLine($"  StartTime: {localTable.StartTime}");
                diagnostics.AppendLine($"  Server: {localTable.Server}");
                diagnostics.AppendLine($"  OrderId: {localTable.OrderId}");
                diagnostics.AppendLine($"  ThresholdMinutes: {localTable.ThresholdMinutes}");
            }
            else
            {
                diagnostics.AppendLine("Local Table State: NOT FOUND");
            }
            diagnostics.AppendLine();

            // Check API connectivity
            diagnostics.AppendLine("API Connectivity:");
            diagnostics.AppendLine($"  TablesApi BaseUrl: {_repo.SourceLabel}");
            diagnostics.AppendLine($"  OrdersApi Available: {App.OrdersApi != null}");
            diagnostics.AppendLine();

            // Check active sessions
            try
            {
                var (sessionId, billingId) = await _repo.GetActiveSessionForTableAsync(tableLabel);
                diagnostics.AppendLine("Active Session Check:");
                diagnostics.AppendLine($"  SessionId: {sessionId}");
                diagnostics.AppendLine($"  BillingId: {billingId}");
                
                // Additional detailed check
                var allActiveSessions = await _repo.GetActiveSessionsAsync();
                diagnostics.AppendLine($"  Total Active Sessions in API: {allActiveSessions.Count}");
                foreach (var session in allActiveSessions)
                {
                    diagnostics.AppendLine($"    - Table: '{session.TableId}', SessionId: {session.SessionId}, BillingId: {session.BillingId}");
                }
                diagnostics.AppendLine();
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"Active Session Check: ERROR - {ex.Message}");
                diagnostics.AppendLine();
            }

            // Check open orders
            try
            {
                var ordersSvc = App.OrdersApi;
                if (ordersSvc != null)
                {
                    var (sessionId, _) = await _repo.GetActiveSessionForTableAsync(tableLabel);
                    if (sessionId.HasValue)
                    {
                        var orders = await ordersSvc.GetOrdersBySessionAsync(sessionId.Value, includeHistory: false);
                        var openOrders = orders.Where(o => o.Status == "open").ToList();
                        diagnostics.AppendLine("Open Orders Check:");
                        diagnostics.AppendLine($"  Total Orders: {orders.Count}");
                        diagnostics.AppendLine($"  Open Orders: {openOrders.Count}");
                        foreach (var order in openOrders)
                        {
                            diagnostics.AppendLine($"    Order {order.Id}: {order.Status} (Total: {order.Total:C2})");
                        }
                    }
                    else
                    {
                        diagnostics.AppendLine("Open Orders Check: No active session found");
                    }
                }
                else
                {
                    diagnostics.AppendLine("Open Orders Check: OrdersApi not available");
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"Open Orders Check: ERROR - {ex.Message}");
            }
            diagnostics.AppendLine();

            // Check database schema
            try
            {
                await _repo.EnsureSchemaAsync();
                diagnostics.AppendLine("Database Schema: OK");
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"Database Schema: ERROR - {ex.Message}");
            }
            diagnostics.AppendLine();

            // Check table status consistency
            try
            {
                var allTables = await _repo.GetAllAsync();
                var apiTableState = allTables.FirstOrDefault(t => string.Equals(t.Label, tableLabel, StringComparison.OrdinalIgnoreCase));
                if (apiTableState != null)
                {
                    diagnostics.AppendLine("API Table State:");
                    diagnostics.AppendLine($"  Label: {apiTableState.Label}");
                    diagnostics.AppendLine($"  Occupied: {apiTableState.Occupied}");
                    diagnostics.AppendLine($"  StartTime: {apiTableState.StartTime}");
                    diagnostics.AppendLine($"  Server: {apiTableState.Server}");
                    diagnostics.AppendLine($"  OrderId: {apiTableState.OrderId}");
                    
                    // Compare with local state
                    if (localTable != null)
                    {
                        diagnostics.AppendLine("State Comparison:");
                        diagnostics.AppendLine($"  Occupied Match: {localTable.Occupied == apiTableState.Occupied}");
                        diagnostics.AppendLine($"  StartTime Match: {localTable.StartTime == apiTableState.StartTime}");
                        diagnostics.AppendLine($"  Server Match: {localTable.Server == apiTableState.Server}");
                    }
                }
                else
                {
                    diagnostics.AppendLine("API Table State: NOT FOUND");
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"API Table State Check: ERROR - {ex.Message}");
            }
            diagnostics.AppendLine();

            // Provide recovery recommendations
            diagnostics.AppendLine("=== RECOVERY RECOMMENDATIONS ===");
            if (localTable != null)
            {
                var (sessionId, billingId) = await _repo.GetActiveSessionForTableAsync(tableLabel);
                if (localTable.Occupied && !sessionId.HasValue)
                {
                    diagnostics.AppendLine("🚨 ISSUE DETECTED: Table shows as occupied but no active session found");
                    diagnostics.AppendLine("📋 RECOMMENDED ACTION: Use automatic session state repair");
                    diagnostics.AppendLine("💡 EXPLANATION: This happens when:");
                    diagnostics.AppendLine("   • Session was started but session tracking failed");
                    diagnostics.AppendLine("   • Database connectivity issues occurred during session start");
                    diagnostics.AppendLine("   • Session was manually cleared from database");
                    diagnostics.AppendLine("   • Race condition between table status and session creation");
                    diagnostics.AppendLine("   • Time synchronization issues between frontend and backend");
                    diagnostics.AppendLine();
                    diagnostics.AppendLine("🔧 AUTOMATIC RECOVERY STEPS:");
                    diagnostics.AppendLine("   1. Right-click the table → 'Stop Session'");
                    diagnostics.AppendLine("   2. System will automatically detect the inconsistency");
                    diagnostics.AppendLine("   3. Choose 'Repair' when prompted for automatic recovery");
                    diagnostics.AppendLine("   4. System will attempt to recreate the session");
                    diagnostics.AppendLine("   5. If recreation fails, table will be safely freed");
                    diagnostics.AppendLine("   6. Table will be available for new sessions");
                    diagnostics.AppendLine();
                    diagnostics.AppendLine("✅ BENEFITS:");
                    diagnostics.AppendLine("   • No manual intervention required");
                    diagnostics.AppendLine("   • Maintains session integrity");
                    diagnostics.AppendLine("   • Automatic retry with proper session handling");
                    diagnostics.AppendLine("   • Detailed diagnostics if repair fails");
                }
                else if (localTable.Occupied && sessionId.HasValue)
                {
                    diagnostics.AppendLine("✅ STATUS: Table and session are properly synchronized");
                    diagnostics.AppendLine("📋 ACTION: Normal stop session should work");
                }
                else if (!localTable.Occupied)
                {
                    diagnostics.AppendLine("✅ STATUS: Table is available");
                    diagnostics.AppendLine("📋 ACTION: No recovery needed");
                }
            }
            diagnostics.AppendLine();

        }
        catch (Exception ex)
        {
            diagnostics.AppendLine($"Diagnostic Error: {ex.Message}");
        }

        return diagnostics.ToString();
    }

    // Session State Management - Robust synchronization
    private async Task<bool> EnsureSessionStateConsistencyAsync(string tableLabel)
    {
        try
        {
            
            // Step 1: Get current table state
            var allTables = await _repo.GetAllAsync();
            var tableState = allTables.FirstOrDefault(t => string.Equals(t.Label, tableLabel, StringComparison.OrdinalIgnoreCase));
            
            if (tableState == null)
            {
                return false;
            }
            
            // Step 2: Get active session for this table
            var (sessionId, billingId) = await _repo.GetActiveSessionForTableAsync(tableLabel);
            
            // Step 3: Check consistency
            bool isConsistent = true;
            string issueDescription = "";
            
            if (tableState.Occupied && !sessionId.HasValue)
            {
                isConsistent = false;
                issueDescription = "Table marked as occupied but no active session found";
            }
            else if (!tableState.Occupied && sessionId.HasValue)
            {
                isConsistent = false;
                issueDescription = "Table marked as available but active session exists";
            }
            
            if (!isConsistent)
            {
                
                // Step 4: Attempt automatic repair
                bool repairSuccess = await AttemptSessionStateRepairAsync(tableLabel, tableState, sessionId, issueDescription);
                if (repairSuccess)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    
    private async Task<bool> AttemptSessionStateRepairAsync(string tableLabel, TableStatusDto tableState, Guid? sessionId, string issueDescription)
    {
        try
        {
            
            if (tableState.Occupied && !sessionId.HasValue)
            {
                // Case 1: Table shows occupied but no session - try to recreate session
                
                // Get server info from table state
                var serverName = tableState.Server ?? "SYSTEM";
                var serverId = "SYSTEM"; // Default server ID
                
                // Attempt to start session again
                var (ok, newSessionId, newBillingId, message) = await _repo.StartSessionAsync(tableLabel, serverId, serverName);
                if (ok)
                {
                    return true;
                }
                else
                {
                    
                    // If session recreation fails, we need to free the table
                    await _repo.UpsertAsync(new TableStatusDto 
                    { 
                        Label = tableLabel, 
                        Type = tableState.Type, 
                        Occupied = false, 
                        StartTime = null, 
                        Server = null,
                        OrderId = null
                    });
                    return true;
                }
            }
            else if (!tableState.Occupied && sessionId.HasValue)
            {
                // Case 2: Table shows available but session exists - clean up orphaned session
                
                // This is a backend issue - the session should be cleaned up by the API
                // For now, we'll mark the table as occupied to match the session
                await _repo.UpsertAsync(new TableStatusDto 
                { 
                    Label = tableLabel, 
                    Type = tableState.Type, 
                    Occupied = true, 
                    StartTime = tableState.StartTime ?? DateTimeOffset.UtcNow, 
                    Server = tableState.Server ?? "SYSTEM",
                    OrderId = tableState.OrderId
                });
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    // Context menu handlers
    private async void StartMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        
        // Step 1: Check if table is already occupied
        if (item.Occupied)
        {
            await new ContentDialog
            {
                Title = "Table Already Occupied",
                Content = $"Table '{item.Label}' is already occupied. Please stop the current session before starting a new one.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
            return;
        }
        
        // Step 2: Ensure session state consistency before starting
        var consistencyResult = await EnsureSessionStateConsistencyAsync(item.Label);
        if (!consistencyResult)
        {
            await new ContentDialog
            {
                Title = "Session State Issue",
                Content = $"Unable to resolve session state inconsistency for table '{item.Label}'.\n\n" +
                         $"Please run diagnostics to get detailed information about the issue.",
                PrimaryButtonText = "Run Diagnostics",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            }.ShowAsync();
            return;
        }
        
        // Step 3: Prompt for server
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
        
        
        // Step 4: Verify session was created properly
        var (verifySessionId, verifyBillingId) = await _repo.GetActiveSessionForTableAsync(item.Label);
        if (!verifySessionId.HasValue)
        {
            await new ContentDialog
            {
                Title = "Session Warning",
                Content = $"Session started for table '{item.Label}' but session tracking may have failed.\n\n" +
                         $"The table is marked as occupied but no session record was found.\n\n" +
                         $"This may cause issues when trying to stop the session.",
                PrimaryButtonText = "Run Diagnostics",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            }.ShowAsync();
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
        
        // Show progress dialog
        var progressDialog = new ContentDialog
        {
            Title = "Stopping Session",
            Content = $"Stopping session for table {item.Label}...",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };
        progressDialog.ShowAsync(); // Don't await, just show
        
        try
        {
            // Step 1: Validate table state
            
            if (!item.Occupied)
            {
                progressDialog.Hide();
                await new ContentDialog
                {
                    Title = "Table Not Occupied",
                    Content = $"Table {item.Label} is not currently occupied.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return;
            }
            
            // Step 2: Close any open orders for this table's session
            var ordersClosed = 0;
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
                                ordersClosed++;
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        
                        if (ordersClosed > 0)
                        {
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                // Continue with table stopping even if order closing fails
            }
            
               // Step 3: Ensure session state consistency before stopping
               var consistencyResult = await EnsureSessionStateConsistencyAsync(item.Label);
               if (!consistencyResult)
               {
                   progressDialog.Hide();
                   
                   await new ContentDialog
                   {
                       Title = "Session State Issue",
                       Content = $"Unable to resolve session state inconsistency for table '{item.Label}'.\n\n" +
                                $"Please run diagnostics to get detailed information about the issue.",
                       PrimaryButtonText = "Run Diagnostics",
                       CloseButtonText = "Cancel",
                       DefaultButton = ContentDialogButton.Primary,
                       XamlRoot = this.XamlRoot
                   }.ShowAsync();
                   return;
               }

               // Step 4: Stop the table session
               var stopResult = await _repo.StopSessionAsync(item.Label);

               progressDialog.Hide(); // Hide progress dialog

               if (!stopResult.ok)
               {

                   // Enhanced error handling with automatic recovery
                   var errorMessage = stopResult.message ?? "Unknown error occurred";
                   var canRecover = errorMessage.IndexOf("no active session", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  errorMessage.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  errorMessage.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0;

                   if (canRecover)
                   {
                       var dlg = new ContentDialog
                       {
                           Title = "Automatic Recovery",
                           Content = $"The table '{item.Label}' shows as occupied but there is no active session in the database.\n\n" +
                                    $"The system will attempt to automatically repair the session state.\n\n" +
                                    $"This can happen when:\n" +
                                    $"• Session tracking failed during start\n" +
                                    $"• Database connectivity issues occurred\n" +
                                    $"• Session was manually cleared\n\n" +
                                    $"Would you like to attempt automatic repair?",
                           PrimaryButtonText = "Repair",
                           CloseButtonText = "Cancel",
                           DefaultButton = ContentDialogButton.Primary,
                           XamlRoot = this.XamlRoot
                       };
                       var choice = await dlg.ShowAsync();
                       if (choice == ContentDialogResult.Primary)
                       {
                           
                           // Try to repair the session state
                           var repairResult = await EnsureSessionStateConsistencyAsync(item.Label);
                           if (repairResult)
                           {
                               
                               // Try stopping again after repair
                               var retryStopResult = await _repo.StopSessionAsync(item.Label);
                               if (retryStopResult.ok)
                               {
                                   await RefreshFromStoreAsync();
                                   return;
                               }
                           }
                           
                           // If repair failed, show diagnostics option
                           await new ContentDialog
                           {
                               Title = "Repair Failed",
                               Content = $"Could not automatically repair the session state for table '{item.Label}'.\n\n" +
                                        $"Please run diagnostics to get detailed information about the issue.",
                               PrimaryButtonText = "Run Diagnostics",
                               CloseButtonText = "OK",
                               DefaultButton = ContentDialogButton.Primary,
                               XamlRoot = this.XamlRoot
                           }.ShowAsync();
                       }
                   }
                else
                {
                    // Show detailed error information
                    var errorDialog = new ContentDialog
                    {
                        Title = "Unable to Stop Session",
                        Content = $"Failed to stop session for table '{item.Label}'.\n\n" +
                                 $"Error: {errorMessage}\n\n" +
                                 $"Possible causes:\n" +
                                 $"• Database connection issues\n" +
                                 $"• API service unavailable\n" +
                                 $"• Network connectivity problems\n" +
                                 $"• Server-side processing error\n\n" +
                                 $"Please check your connection and try again.",
                        PrimaryButtonText = "Run Diagnostics",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };
                    var errorChoice = await errorDialog.ShowAsync();
                    if (errorChoice == ContentDialogResult.Primary)
                    {
                        // Run comprehensive diagnostics
                        var diagnostics = await DiagnoseTableIssuesAsync(item.Label);
                        var diagnosticsDialog = new ContentDialog
                        {
                            Title = "Table Diagnostics",
                            Content = new ScrollViewer
                            {
                                Content = new TextBlock
                                {
                                    Text = diagnostics,
                                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                                    FontSize = 12,
                                    TextWrapping = TextWrapping.Wrap
                                },
                                MaxHeight = 400
                            },
                            PrimaryButtonText = "Copy to Clipboard",
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = this.XamlRoot
                        };
                        var diagChoice = await diagnosticsDialog.ShowAsync();
                        if (diagChoice == ContentDialogResult.Primary)
                        {
                            // Copy diagnostics to clipboard
                            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                            dataPackage.SetText(diagnostics);
                            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                        }
                    }
                }
                return;
            }
            
            
            // Step 4: Refresh the UI
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
        catch (Exception ex)
        {
            progressDialog.Hide();
            await new ContentDialog
            {
                Title = "Unexpected Error",
                Content = $"An unexpected error occurred while stopping table '{item.Label}':\n\n{ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }

    private async void TableStatusMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;

        try
        {
            
            var dialog = new Dialogs.TableStatusDialog
            {
                XamlRoot = this.XamlRoot
            };

            await dialog.LoadTableStatusAsync(item.Label);
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error("TableStatusMenu_Click failed", ex);
            await ShowSafeContentDialog("Error", $"Failed to load table status: {ex.Message}", "OK");
        }
    }

    // Bar table specific handlers
    private void OpenTableMenu_Click(object sender, RoutedEventArgs e)
    {
        // Same as StartMenu_Click but for bar tables
        StartMenu_Click(sender, e);
    }

    private void CloseTableMenu_Click(object sender, RoutedEventArgs e)
    {
        // Same as StopMenu_Click but for bar tables
        StopMenu_Click(sender, e);
    }

    private async void CheckSummaryMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        
        if (!item.Occupied)
        {
            await new ContentDialog
            {
                Title = "Table Not Occupied",
                Content = $"Table {item.Label} is not currently occupied.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
            return;
        }

        try
        {
            // Get session info
            var (sessionId, billingId) = await _repo.GetActiveSessionForTableAsync(item.Label);
            
            // Get items from session (legacy items stored in table_sessions)
            var sessionItems = new List<MagiDesk.Shared.DTOs.Tables.ItemLine>();
            if (!string.IsNullOrWhiteSpace(item.Label))
            {
                sessionItems = await _repo.GetSessionItemsAsync(item.Label);
            }

            // Get orders with their items if session exists
            var orders = new List<Services.OrderApiService.OrderDto>();
            var allOrderItems = new List<(long OrderId, Services.OrderApiService.OrderItemDto Item)>();
            
            if (sessionId.HasValue)
            {
                var ordersSvc = App.OrdersApi;
                if (ordersSvc != null)
                {
                    var orderList = await ordersSvc.GetOrdersBySessionAsync(sessionId.Value, includeHistory: true);
                    orders = orderList.ToList();
                    
                    // Collect all items from all orders
                    foreach (var order in orders)
                    {
                        if (order.Items != null)
                        {
                            foreach (var orderItem in order.Items)
                            {
                                allOrderItems.Add((order.Id, orderItem));
                            }
                        }
                    }
                }
            }

            // Build summary content
            var summaryContent = new StackPanel { Spacing = 16 };
            
            // Table Info Section
            var tableInfoBorder = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Microsoft.UI.Xaml.Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Microsoft.UI.Xaml.Thickness(12)
            };
            var tableInfoPanel = new StackPanel { Spacing = 4 };
            tableInfoPanel.Children.Add(new TextBlock
            {
                Text = "Table Information",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });
            tableInfoPanel.Children.Add(new TextBlock
            {
                Text = $"Table: {item.Label}",
                FontSize = 14
            });
            tableInfoPanel.Children.Add(new TextBlock
            {
                Text = $"Server: {item.Server ?? "N/A"}",
                FontSize = 14
            });
            tableInfoPanel.Children.Add(new TextBlock
            {
                Text = $"Start Time: {item.StartTime?.ToLocalTime().ToString("g") ?? "N/A"}",
                FontSize = 14
            });
            if (sessionId.HasValue)
            {
                tableInfoPanel.Children.Add(new TextBlock
                {
                    Text = $"Session ID: {sessionId.Value:D}",
                    FontSize = 12,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 0)
                });
            }
            tableInfoBorder.Child = tableInfoPanel;
            summaryContent.Children.Add(tableInfoBorder);

            // Orders Section
            if (orders.Count > 0)
            {
                var ordersHeader = new TextBlock
                {
                    Text = $"Orders ({orders.Count})",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 8)
                };
                summaryContent.Children.Add(ordersHeader);

                foreach (var order in orders)
                {
                    var orderBorder = new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                        BorderThickness = new Microsoft.UI.Xaml.Thickness(1),
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                        Padding = new Microsoft.UI.Xaml.Thickness(12),
                        Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 8)
                    };
                    var orderPanel = new StackPanel { Spacing = 8 };
                    
                    // Order Header
                    var orderHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                    orderHeader.Children.Add(new TextBlock
                    {
                        Text = $"Order #{order.Id}",
                        FontSize = 14,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    });
                    orderHeader.Children.Add(new TextBlock
                    {
                        Text = $"Status: {order.Status}",
                        FontSize = 12,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                    });
                    orderPanel.Children.Add(orderHeader);

                    // Order Items
                    if (order.Items != null && order.Items.Count > 0)
                    {
                        var itemsHeader = new TextBlock
                        {
                            Text = "Items:",
                            FontSize = 12,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                            Margin = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 4)
                        };
                        orderPanel.Children.Add(itemsHeader);

                        var itemsList = new StackPanel { Spacing = 4, Margin = new Microsoft.UI.Xaml.Thickness(8, 0, 0, 0) };
                        decimal orderTotal = 0;
                        foreach (var orderItem in order.Items)
                        {
                            var itemPrice = orderItem.BasePrice + orderItem.PriceDelta;
                            var itemTotal = itemPrice * orderItem.Quantity;
                            orderTotal += itemTotal;
                            
                            var itemText = new TextBlock
                            {
                                Text = $"  • Item #{orderItem.MenuItemId?.ToString() ?? orderItem.ComboId?.ToString() ?? "Unknown"} x{orderItem.Quantity} @ {itemPrice:C2} = {itemTotal:C2}",
                                FontSize = 12,
                                TextWrapping = TextWrapping.Wrap
                            };
                            itemsList.Children.Add(itemText);
                        }
                        orderPanel.Children.Add(itemsList);

                        var orderTotalText = new TextBlock
                        {
                            Text = $"Order Total: {orderTotal:C2}",
                            FontSize = 13,
                            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
                        };
                        orderPanel.Children.Add(orderTotalText);
                    }
                    else
                    {
                        orderPanel.Children.Add(new TextBlock
                        {
                            Text = "No items in this order.",
                            FontSize = 12,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                            Margin = new Microsoft.UI.Xaml.Thickness(8, 4, 0, 0)
                        });
                    }

                    orderBorder.Child = orderPanel;
                    summaryContent.Children.Add(orderBorder);
                }
            }

            // Session Items Section (legacy items not in orders)
            if (sessionItems.Count > 0)
            {
                var sessionItemsHeader = new TextBlock
                {
                    Text = "Session Items (Legacy)",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 8)
                };
                summaryContent.Children.Add(sessionItemsHeader);

                var sessionItemsBorder = new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                    BorderThickness = new Microsoft.UI.Xaml.Thickness(1),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Microsoft.UI.Xaml.Thickness(12)
                };
                var sessionItemsList = new StackPanel { Spacing = 4 };
                decimal sessionTotal = 0;
                foreach (var itemLine in sessionItems)
                {
                    var itemTotal = itemLine.price * itemLine.quantity;
                    sessionTotal += itemTotal;
                    var itemText = new TextBlock
                    {
                        Text = $"  • {itemLine.name} x{itemLine.quantity} @ {itemLine.price:C2} = {itemTotal:C2}",
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    };
                    sessionItemsList.Children.Add(itemText);
                }
                sessionItemsList.Children.Add(new TextBlock
                {
                    Text = $"Session Items Total: {sessionTotal:C2}",
                    FontSize = 13,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
                });
                sessionItemsBorder.Child = sessionItemsList;
                summaryContent.Children.Add(sessionItemsBorder);
            }

            // Grand Total
            if (orders.Count > 0 || sessionItems.Count > 0)
            {
                decimal grandTotal = orders.Sum(o => o.Items?.Sum(i => (i.BasePrice + i.PriceDelta) * i.Quantity) ?? 0m) +
                                    sessionItems.Sum(i => i.price * i.quantity);
                
                var grandTotalBorder = new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkBlue),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Microsoft.UI.Xaml.Thickness(12),
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
                };
                var grandTotalText = new TextBlock
                {
                    Text = $"Grand Total: {grandTotal:C2}",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                };
                grandTotalBorder.Child = grandTotalText;
                summaryContent.Children.Add(grandTotalBorder);
            }

            // Empty State
            if (orders.Count == 0 && sessionItems.Count == 0)
            {
                var emptyState = new TextBlock
                {
                    Text = "No orders or items found for this table.",
                    FontSize = 14,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 20, 0, 0)
                };
                summaryContent.Children.Add(emptyState);
            }

            var scrollViewer = new ScrollViewer
            {
                Content = summaryContent,
                MaxHeight = 600,
                VerticalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto
            };

            await new ContentDialog
            {
                Title = $"Table Summary - {item.Label}",
                Content = scrollViewer,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            }.ShowAsync();
        }
        catch (Exception ex)
        {
            await new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to load table summary: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }

    private async void AssignToBilliardMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        
        if (!item.Occupied)
        {
            await new ContentDialog
            {
                Title = "Table Not Occupied",
                Content = $"Table {item.Label} is not currently occupied.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
            return;
        }

        // Get free billiard tables
        var allTables = await _repo.GetAllAsync();
        var freeBilliardTables = allTables
            .Where(t => t.Type?.ToLower() == "billiard" && !t.Occupied)
            .Select(t => t.Label)
            .OrderBy(l => l)
            .ToList();

        if (freeBilliardTables.Count == 0)
        {
            await new ContentDialog
            {
                Title = "No Free Billiard Tables",
                Content = "There are no available billiard tables to assign to.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
            return;
        }

        var combo = new ComboBox 
        { 
            ItemsSource = freeBilliardTables, 
            SelectedIndex = 0, 
            Width = 280,
            Header = "Select Billiard Table:"
        };
        
        var dlg = new ContentDialog
        {
            Title = $"Assign {item.Label} to Billiard Table",
            Content = combo,
            PrimaryButtonText = "Assign",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        
        var res = await dlg.ShowAsync();
        if (res == ContentDialogResult.Primary && combo.SelectedItem is string targetBilliardTable)
        {
            try
            {
                // Move table (this will move the session and items)
                var moveResult = await _repo.MoveSessionAsync(item.Label, targetBilliardTable);
                
                if (moveResult.ok)
                {
                    await RefreshFromStoreAsync();
                    await new ContentDialog
                    {
                        Title = "Success",
                        Content = $"Table {item.Label} has been assigned to {targetBilliardTable}.\n\nAll items and session data have been moved.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
                else
                {
                    await new ContentDialog
                    {
                        Title = "Assignment Failed",
                        Content = moveResult.message ?? "Failed to assign table to billiard table.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to assign table: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
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
        try
        {
            // Use the existing UsersApi service instead of creating a new one
            if (App.UsersApi == null)
            {
                Log.Error("UsersApi is not initialized");
                await ShowSafeContentDialog("Error", "User service is not available. Please restart the application.", "OK");
                return (false, null);
            }

            Log.Info("PromptSelectEmployeeAsync: Getting users from UsersApi");
            var users = await App.UsersApi.GetUsersPagedAsync(new MagiDesk.Shared.DTOs.Users.UserSearchRequest { Page = 1, PageSize = 100 });
            // Filter for users who can serve tables (Server, Cashier, Manager, Owner, Administrator)
            var employees = users.Items.Where(u => 
                string.Equals(u.Role, "Server", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Role, "Cashier", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Role, "Manager", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Role, "Owner", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Role, "Administrator", StringComparison.OrdinalIgnoreCase)
            ).Where(u => u.IsActive).ToList();
            
            Log.Info($"PromptSelectEmployeeAsync: Found {employees.Count} employees");
            if (employees.Count == 0)
            {
                await ShowSafeContentDialog("No employees found", "Please add employees first.", "OK");
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
            {
                Log.Info($"PromptSelectEmployeeAsync: Selected employee {selected.Username}");
                return (true, selected);
            }
            return (false, null);
        }
        catch (Exception ex)
        {
            Log.Error("PromptSelectEmployeeAsync failed", ex);
            await ShowSafeContentDialog("Error", $"Failed to load employees: {ex.Message}", "OK");
            return (false, null);
        }
    }
}

public class TableItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public TableItem(string label, string type = "billiard")
    {
        _label = label;
        _type = type;
        UpdateBrush();
    }

    private string _label;
    public string Label
    {
        get => _label;
        set { _label = value; OnPropertyChanged(); }
    }

    private string _type = "billiard";
    public string Type
    {
        get => _type;
        set 
        { 
            _type = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(HasTimer));
            OnPropertyChanged(nameof(IsBarTable));
            OnPropertyChanged(nameof(IsBilliardTable));
        }
    }

    public bool IsBarTable => Type.ToLower() == "bar";
    public bool IsBilliardTable => Type.ToLower() == "billiard";

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
    public bool HasTimer => Type.ToLower() == "billiard" && Occupied && StartTime.HasValue;

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
