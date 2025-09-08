using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;
using MagiDesk.Shared.DTOs;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;

namespace MagiDesk.Frontend.Views;

public sealed partial class OrderBuilderPage : Page
{
    private int _step = 1;
    private ComboBox _vendorCombo = null!;
    private TextBox _vendorIdBox = null!;
    private TextBox _itemNameBox = null!;
    private NumberBox _qtyBox = null!;
    private NumberBox _priceBox = null!;
    private ListView _cartList = null!;
    private NumberBox _editQtyBox = null!;
    private TextBlock _totalText = null!;
    private InfoBar _budgetBar = null!;

    public OrderDto? BuiltOrder { get; private set; }
    public CartDraftDto? BuiltDraft { get; private set; }

    private readonly ObservableCollection<OrderItemDto> _items = new();
    private MagiDesk.Shared.DTOs.VendorDto? _selectedVendor;
    private readonly ObservableCollection<MagiDesk.Shared.DTOs.ItemDto> _catalog = new();
    private List<MagiDesk.Shared.DTOs.ItemDto> _catalogAll = new();

    private readonly TaskCompletionSource<OrderDto?> _tcs = new();
    public Task<OrderDto?> Completion => _tcs.Task;

    public OrderBuilderPage()
    {
        this.InitializeComponent();
        _items.CollectionChanged += (s, e) => UpdateTotals();
        this.SizeChanged += (s, e) => { if (_step == 2) RenderStep(); };
        RenderStep();
    }

    public void LoadFromDraft(CartDraftDto draft)
    {
        _items.Clear();
        foreach (var it in draft.Items)
            _items.Add(new OrderItemDto { ItemId = it.ItemId, ItemName = it.ItemName, Quantity = it.Quantity, Price = it.Price });
        _selectedVendor = new MagiDesk.Shared.DTOs.VendorDto { Id = draft.VendorId, Name = draft.VendorName };
        NormalizeItems();
    }

    private bool ValidateStep(int step)
    {
        if (step == 1) return _selectedVendor != null || !string.IsNullOrWhiteSpace(_vendorIdBox.Text);
        if (step == 2) return _items.Count > 0;
        return true;
    }

    private void RenderStep()
    {
        switch (_step)
        {
            case 1:
                StepTitle.Text = "Step 1 – Vendor";
                StepFrame.Content = BuildVendorStep();
                NextButton.Content = "Next";
                BackButton.IsEnabled = false;
                break;
            case 2:
                StepTitle.Text = "Step 2 – Items";
                StepFrame.Content = BuildItemsStep();
                NextButton.Content = "Next";
                BackButton.IsEnabled = true;
                break;
            case 3:
                StepTitle.Text = "Step 3 – Review";
                StepFrame.Content = BuildReviewStep();
                NextButton.Content = "Finalize";
                BackButton.IsEnabled = true;
                break;
        }
    }

    private UIElement BuildVendorStep()
    {
        var panel = new StackPanel { Spacing = 8, Padding = new Thickness(8) };
        _vendorCombo = new ComboBox { Header = "Select Vendor", PlaceholderText = "Choose a vendor" };
        _vendorCombo.SelectionChanged += async (s, e) => { _selectedVendor = _vendorCombo.SelectedItem as MagiDesk.Shared.DTOs.VendorDto; await LoadItemsForVendorAsync(); };
        _vendorIdBox = new TextBox { Header = "Vendor ID (optional)" };
        panel.Children.Add(_vendorCombo);
        panel.Children.Add(_vendorIdBox);
        _ = LoadVendorsAsync();
        return panel;
    }

    private UIElement BuildItemsStep()
    {
        NormalizeItems();
        var availableWidth = this.ActualWidth > 0 ? this.ActualWidth : 1200;
        var narrow = availableWidth < 1200;

        Grid root;
        if (!narrow)
        {
            root = new Grid
            {
                Padding = new Thickness(20),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(10, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(7, GridUnitType.Star) }
                }
            };
            root.ColumnSpacing = 24;
        }
        else
        {
            root = new Grid
            {
                Padding = new Thickness(20),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } }
            };
        }

        // Budget warning bar (top)
        _budgetBar = new InfoBar { IsOpen = false, Severity = InfoBarSeverity.Warning };
        Grid.SetRow(_budgetBar, 0);
        Grid.SetColumnSpan(_budgetBar, narrow ? 1 : 2);
        root.Children.Add(_budgetBar);

        // Custom add row
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 0, 0, 8) };
        _itemNameBox = new TextBox { PlaceholderText = "Custom item name", Width = 700 };
        _qtyBox = new NumberBox { PlaceholderText = "Qty", Minimum = 1, Maximum = 100000, Value = 1, Width = 160 };
        _priceBox = new NumberBox { PlaceholderText = "Price", Minimum = 0, Maximum = 1000000, Value = 0, Width = 200 };
        var addBtn = new Button { Content = "Add Custom" };
        addBtn.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(_itemNameBox.Text) && _qtyBox.Value > 0)
            {
                AddOrIncrement(_itemNameBox.Text.Trim(), (decimal)_priceBox.Value, (int)_qtyBox.Value, string.Empty, _selectedVendor?.Id, _selectedVendor?.Name);
                _itemNameBox.Text = string.Empty; _qtyBox.Value = 1; _priceBox.Value = 0;
            }
        };
        row.Children.Add(_itemNameBox);
        row.Children.Add(_qtyBox);
        row.Children.Add(_priceBox);
        row.Children.Add(addBtn);
        if (!narrow) Grid.SetColumnSpan(row, 2);
        Grid.SetRow(row, 1);
        root.Children.Add(row);

        // Search row
        var searchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 8) };
        var searchBox = new TextBox { PlaceholderText = "Search items...", Width = 1000 };
        searchBox.TextChanged += (s, e) =>
        {
            var q = searchBox.Text?.Trim() ?? string.Empty;
            _catalog.Clear();
            IEnumerable<MagiDesk.Shared.DTOs.ItemDto> src = _catalogAll;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLowerInvariant();
                src = src.Where(it => (it.Name ?? it.Sku ?? "").ToLowerInvariant().Contains(qq));
            }
            foreach (var it in src) _catalog.Add(it);
        };
        searchRow.Children.Add(searchBox);
        Grid.SetRow(searchRow, 2);
        Grid.SetColumn(searchRow, 0);
        root.Children.Add(searchRow);

        // Catalog
        var catalog = new ListView
        {
            ItemsSource = _catalog,
            IsItemClickEnabled = true,
            SelectionMode = ListViewSelectionMode.Single,
            ItemTemplate = BuildCatalogTemplate(),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        catalog.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is MagiDesk.Shared.DTOs.ItemDto it)
            {
                AddOrIncrement(it.Name ?? "Item", it.Price, 1, it.Id ?? string.Empty, _selectedVendor?.Id, _selectedVendor?.Name);
            }
        };
        if (!narrow) { Grid.SetRow(catalog, 3); Grid.SetColumn(catalog, 0); }
        else { Grid.SetRow(catalog, 3); Grid.SetColumn(catalog, 0); }
        root.Children.Add(catalog);

        // Cart
        _cartList = new ListView
        {
            ItemTemplate = BuildCartTemplate(),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        ApplyCartGrouping();
        _cartList.SelectionChanged += (s, e) =>
        {
            if (_cartList.SelectedItem is OrderItemDto it) { _editQtyBox.Value = it.Quantity; }
        };
        if (!narrow) { Grid.SetRow(_cartList, 3); Grid.SetColumn(_cartList, 1); }
        else { Grid.SetRow(_cartList, 4); Grid.SetColumn(_cartList, 0); }
        root.Children.Add(_cartList);

        // Edit row
        var editRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        _editQtyBox = new NumberBox { Header = "Quantity", Minimum = 0, Maximum = 100000, Value = 1, Width = 120 };
        var updateBtn = new Button { Content = "Update" };
        var removeBtn = new Button { Content = "Remove" };
        var clearBtn = new Button { Content = "Clear Cart" };
        updateBtn.Click += (s, e) =>
        {
            if (_cartList.SelectedItem is OrderItemDto it)
            {
                it.Quantity = (int)_editQtyBox.Value;
                var idx = _items.IndexOf(it);
                if (idx >= 0) { _items.RemoveAt(idx); _items.Insert(idx, it); }
            }
        };
        removeBtn.Click += (s, e) => { if (_cartList.SelectedItem is OrderItemDto it) { _items.Remove(it); CheckBudgetAndWarn(); } };
        clearBtn.Click += (s, e) => { _items.Clear(); CheckBudgetAndWarn(); };
        editRow.Children.Add(_editQtyBox);
        editRow.Children.Add(updateBtn);
        editRow.Children.Add(removeBtn);
        editRow.Children.Add(clearBtn);
        if (!narrow) { Grid.SetRow(editRow, 4); Grid.SetColumn(editRow, 1); }
        else { Grid.SetRow(editRow, 5); Grid.SetColumn(editRow, 0); }
        root.Children.Add(editRow);

        return root;
    }

    private DataTemplate BuildCartTemplate()
    {
        var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
            <Grid Padding='4'>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width='*'/>
                <ColumnDefinition Width='Auto'/>
              </Grid.ColumnDefinitions>
              <StackPanel Orientation='Horizontal' Spacing='6'>
                <TextBlock Text='{Binding ItemName}' TextWrapping='WrapWholeWords'/>
                <TextBlock Text='  (x'/>
                <TextBlock Text='{Binding Quantity}'/>
                <TextBlock Text=')'/>
                <TextBlock Text=' - ' Opacity='0.6'/>
                <TextBlock Text='{Binding VendorName}' Opacity='0.6'/>
              </StackPanel>
              <Button Grid.Column='1' Content='Remove' Click='OnCartRemoveClick'/>
            </Grid>
        </DataTemplate>";
        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
    }

    private DataTemplate BuildCatalogTemplate()
    {
        var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
            <StackPanel Padding='8' Spacing='2'>
              <TextBlock Text='{Binding Name}' FontWeight='SemiBold' TextWrapping='WrapWholeWords' />
              <TextBlock Text='{Binding Price}' Opacity='0.7'/>
            </StackPanel>
        </DataTemplate>";
        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
    }

    private UIElement BuildReviewStep()
    {
        var panel = new StackPanel { Spacing = 8 };
        var title = new TextBlock { Text = "Review Order", FontWeight = FontWeights.SemiBold, FontSize = 16 };
        _totalText = new TextBlock { };
        UpdateTotals();

        // Group by vendor in review
        var groups = _items.GroupBy(i => i.VendorName ?? _selectedVendor?.Name ?? "Vendor")
                           .Select(g => new { Key = g.Key, Items = g.ToList() })
                           .ToList();
        var cvs = new CollectionViewSource { IsSourceGrouped = true, Source = groups };
        cvs.ItemsPath = new PropertyPath("Items");
        var list = new ListView { ItemsSource = cvs.View };
        list.ItemTemplate = (DataTemplate)XamlReader.Load(@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
            <StackPanel Orientation='Horizontal' Padding='4'>
              <TextBlock Text='{Binding ItemName}'/>
              <TextBlock Text='  (x'/>
              <TextBlock Text='{Binding Quantity}'/>
              <TextBlock Text=')'/>
            </StackPanel>
        </DataTemplate>");
        var gs = new GroupStyle();
        gs.HeaderTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Key}' FontWeight='SemiBold' FontSize='14' Margin='0,8,0,4'/></DataTemplate>");
        list.GroupStyle.Add(gs);

        panel.Children.Add(title);
        panel.Children.Add(list);
        panel.Children.Add(_totalText);
        return panel;
    }

    private async Task LoadVendorsAsync()
    {
        try
        {
            var vendors = await App.Api!.GetVendorsAsync();
            _vendorCombo.ItemsSource = vendors;
            _vendorCombo.DisplayMemberPath = nameof(MagiDesk.Shared.DTOs.VendorDto.Name);
            _vendorCombo.SelectedValuePath = nameof(MagiDesk.Shared.DTOs.VendorDto.Id);
        }
        catch { }
    }

    private async Task LoadItemsForVendorAsync()
    {
        try
        {
            _catalog.Clear();
            _catalogAll.Clear();
            if (_selectedVendor?.Id is string vid && !string.IsNullOrWhiteSpace(vid))
            {
                var list = await App.Api!.GetItemsByVendorAsync(vid);
                _catalogAll = list.ToList();
                foreach (var it in _catalogAll) _catalog.Add(it);
            }
        }
        catch { }
        CheckBudgetAndWarn();
    }

    private void OnCartRemoveClick(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is OrderItemDto it)
        {
            _items.Remove(it);
        }
    }

    private void UpdateTotals()
    {
        var total = _items.Sum(i => i.Price * i.Quantity);
        if (_totalText != null) _totalText.Text = $"Total: {total:C}";
    }

    private void CheckBudgetAndWarn()
    {
        try
        {
            if (_budgetBar == null) return;
            var budget = _selectedVendor?.Budget ?? 0m;
            if (budget <= 0m) { _budgetBar.IsOpen = false; return; }
            var vid = _selectedVendor?.Id ?? string.Empty;
            var totalForVendor = _items.Where(i => string.Equals(i.VendorId ?? string.Empty, vid, StringComparison.OrdinalIgnoreCase))
                                       .Sum(i => i.Price * i.Quantity);
            if (totalForVendor > budget)
            {
                _budgetBar.Message = $"Budget exceeded for {_selectedVendor?.Name}: {totalForVendor:C} > {budget:C}";
                _budgetBar.Severity = InfoBarSeverity.Warning;
                _budgetBar.IsOpen = true;
            }
            else
            {
                _budgetBar.IsOpen = false;
            }
        }
        catch { }
    }

    private void AddOrIncrement(string name, decimal price, int increment = 1, string itemId = "", string? vendorId = null, string? vendorName = null)
    {
        var existing = _items.FirstOrDefault(x => string.Equals(x.ItemName, name, System.StringComparison.OrdinalIgnoreCase) && string.Equals(x.VendorId ?? "", vendorId ?? "", System.StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Quantity += increment;
            var idx = _items.IndexOf(existing);
            if (idx >= 0) { _items.RemoveAt(idx); _items.Insert(idx, existing); }
        }
        else
        {
            _items.Add(new OrderItemDto { ItemId = itemId, ItemName = name, Quantity = increment, Price = price, VendorId = vendorId, VendorName = vendorName });
        }
        UpdateTotals();
        ApplyCartGrouping();
        CheckBudgetAndWarn();
    }

    private void NormalizeItems()
    {
        if (_items.Count <= 1) return;
        var grouped = _items
            .GroupBy(x => $"{x.VendorId}|{x.ItemName}")
            .Select(g => new OrderItemDto
            {
                ItemId = g.First().ItemId,
                ItemName = g.First().ItemName,
                VendorId = g.First().VendorId,
                VendorName = g.First().VendorName,
                Quantity = g.Sum(x => x.Quantity),
                Price = g.First().Price
            })
            .ToList();
        _items.Clear();
        foreach (var it in grouped) _items.Add(it);
        ApplyCartGrouping();
        CheckBudgetAndWarn();
    }

    private void ApplyCartGrouping()
    {
        if (_cartList == null) return;
        var groups = _items.GroupBy(i => i.VendorName ?? _selectedVendor?.Name ?? "Vendor")
                           .Select(g => new { Key = g.Key, Items = g.ToList() })
                           .ToList();
        var cvs = new CollectionViewSource { IsSourceGrouped = true, Source = groups };
        cvs.ItemsPath = new PropertyPath("Items");
        _cartList.ItemsSource = cvs.View;
        if (_cartList.GroupStyle.Count == 0)
        {
            var gs = new GroupStyle();
            gs.HeaderTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Key}' FontWeight='SemiBold' Margin='0,6,0,2'/></DataTemplate>");
            _cartList.GroupStyle.Add(gs);
        }
        CheckBudgetAndWarn();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_step > 1) { _step--; RenderStep(); }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_step < 3)
        {
            if (!ValidateStep(_step)) return;
            _step++; RenderStep();
        }
        else
        {
            if (!ValidateStep(_step)) return;
            var total = _items.Sum(i => i.Price * i.Quantity);
            BuiltOrder = new OrderDto
            {
                VendorId = _selectedVendor?.Id ?? _vendorIdBox.Text?.Trim() ?? string.Empty,
                VendorName = _selectedVendor?.Name ?? string.Empty,
                Items = _items.ToList(),
                TotalAmount = total,
                Status = "submitted"
            };
            _tcs.TrySetResult(BuiltOrder);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _tcs.TrySetResult(null);
    }
}
