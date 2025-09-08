using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

    public TablesPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        Loaded += TablesPage_Loaded;
        _pollTimer.Tick += PollTimer_Tick;
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
        return Services.ReceiptFormatter.BuildReceiptView(bill, paper, tax);
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
        ApplyFilter();
        UpdateStatus();
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

    private async void PollTimer_Tick(object sender, object e)
    {
        await RefreshFromStoreAsync();
    }

    private async Task RefreshFromStoreAsync()
    {
        var rows = await _repo.GetAllAsync();
        var dict = rows.ToDictionary(r => r.Label, r => r, StringComparer.OrdinalIgnoreCase);
        void reconcile(ObservableCollection<TableItem> list, string type)
        {
            foreach (var item in list)
            {
                if (dict.TryGetValue(item.Label, out var rec))
                {
                    item.Occupied = rec.Occupied;
                    item.OrderId = rec.OrderId; item.StartTime = rec.StartTime; item.Server = rec.Server;
                }
            }
        }
        reconcile(BilliardTables, "billiard");
        reconcile(BarTables, "bar");
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
        await RefreshFromStoreAsync();
    }

    private async void StopMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
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
                var (paper, tax) = ReadPrinterConfig();
                var view = Services.ReceiptFormatter.BuildReceiptView(bill, paper, tax);
                await Services.PrintService.PrintVisualAsync(view);
            }
        }
    }

    private async void AddItemsMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
        // Load inventory
        var inventory = await FetchInventoryAsync();
        // Load existing session items
        var existing = await _repo.GetSessionItemsAsync(item.Label);

        // Build UI
        var stack = new StackPanel { Spacing = 8 };
        var listView = new ListView { SelectionMode = ListViewSelectionMode.None, Height = 320 };
        var data = inventory.Select(inv => new AddItemRow
        {
            ItemId = inv.Id ?? inv.clave ?? inv.productname ?? Guid.NewGuid().ToString(),
            Name = inv.productname ?? inv.clave ?? "(Unnamed)",
            Price = 0m,
            Quantity = 0
        }).ToList();
        // prefill existing
        foreach (var ex in existing)
        {
            var row = data.FirstOrDefault(d => string.Equals(d.ItemId, ex.itemId, StringComparison.OrdinalIgnoreCase) || string.Equals(d.Name, ex.name, StringComparison.OrdinalIgnoreCase));
            if (row != null) { row.Price = ex.price; row.Quantity = ex.quantity; }
            else data.Add(new AddItemRow { ItemId = ex.itemId, Name = ex.name, Price = ex.price, Quantity = ex.quantity });
        }
        listView.ItemsSource = data;
        listView.ItemTemplate = BuildAddItemTemplate();

        stack.Children.Add(new TextBlock { Text = $"Add items to {item.Label}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        stack.Children.Add(listView);

        var dlg = new ContentDialog
        {
            Title = "Add Items",
            Content = stack,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        var res = await dlg.ShowAsync();
        if (res != ContentDialogResult.Primary) return;

        var lines = data.Where(d => d.Quantity > 0)
                        .Select(d => new ItemLine { itemId = d.ItemId, name = d.Name, quantity = d.Quantity, price = d.Price })
                        .ToList();
        var ok = await _repo.ReplaceSessionItemsAsync(item.Label, lines);
        if (!ok)
        {
            await new ContentDialog { Title = "Failed", Content = "Could not save items.", CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
            return;
        }

        // update ItemsSummary
        item.ItemsSummary = BuildItemsSummary(lines);
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

    private async Task<List<InventoryItem>> FetchInventoryAsync()
    {
        try
        {
            var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true)
                .Build();
            var apiBase = cfg["Api:BaseUrl"] ?? string.Empty;
            var inner = new HttpClientHandler();
            inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var logging = new HttpLoggingHandler(inner);
            using var http = new HttpClient(logging) { BaseAddress = new Uri(apiBase.TrimEnd('/') + "/") };
            var res = await http.GetAsync("api/inventory");
            res.EnsureSuccessStatusCode();
            var list = await res.Content.ReadFromJsonAsync<List<InventoryItem>>() ?? new();
            return list;
        }
        catch { return new List<InventoryItem>(); }
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

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
