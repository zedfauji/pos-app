using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MagiDesk.Shared.DTOs;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Data;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class OrderBuilderDialog : ContentDialog
{
    private int _step = 1;
    private ComboBox _vendorCombo = null!;
    private TextBox _vendorIdBox = null!;
    private TextBox _itemNameBox = null!;
    private NumberBox _qtyBox = null!;
    private NumberBox _priceBox = null!;
    private ListView _cartList = null!;
    private NumberBox _editQtyBox = null!;
    private Button _updateBtn = null!;
    private Button _removeBtn = null!;
    private TextBlock _totalText = null!;

    public OrderDto? BuiltOrder { get; private set; }
    public CartDraftDto? BuiltDraft { get; private set; }

    private ObservableCollection<OrderItemDto> _items = new();
    private MagiDesk.Shared.DTOs.VendorDto? _selectedVendor;
    private ObservableCollection<MagiDesk.Shared.DTOs.ItemDto> _catalog = new();
    private List<MagiDesk.Shared.DTOs.ItemDto> _catalogAll = new();

    private class ReviewItem
    {
        public string ItemName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    private class Group<K, T> : System.Collections.Generic.List<T>
    {
        public K Key { get; }
        public System.Collections.Generic.IList<T> Items => this;
        public Group(K key, System.Collections.Generic.IEnumerable<T> items) : base(items) { Key = key; }
    }

    public OrderBuilderDialog()
    {
        this.InitializeComponent();
        // Make dialog wider so Step 2 layout has room
        this.MinWidth = 1600;
        this.MaxWidth = 2560;
        this.Width = 1600;
        this.Height = 900;
        this.PrimaryButtonClick += OrderBuilderDialog_PrimaryButtonClick;
        this.SecondaryButtonClick += OrderBuilderDialog_SecondaryButtonClick;
        this.SizeChanged += (s, e) =>
        {
            // Re-render Step 2 layout when the dialog width changes
            if (_step == 2)
            {
                RenderStep();
            }
        };
        _items.CollectionChanged += (s, e) => UpdateTotals();
        RenderStep();
    }

    public void LoadFromDraft(CartDraftDto draft)
    {
        _items.Clear();
        foreach (var it in draft.Items)
            _items.Add(new OrderItemDto { ItemId = it.ItemId, ItemName = it.ItemName, Quantity = it.Quantity, Price = it.Price, VendorId = draft.VendorId, VendorName = draft.VendorName });
        _selectedVendor = new MagiDesk.Shared.DTOs.VendorDto { Id = draft.VendorId, Name = draft.VendorName };
        NormalizeItems();
    }

    private void OrderBuilderDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_step > 1)
        {
            _step--;
            RenderStep();
            args.Cancel = true;
        }
    }

    private void OrderBuilderDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_step < 3)
        {
            if (!ValidateStep(_step)) { args.Cancel = true; return; }
            _step++;
            RenderStep();
            args.Cancel = true;
        }
        else
        {
            // finalize
            if (!ValidateStep(_step)) { args.Cancel = true; return; }
            var total = _items.Sum(i => i.Price * i.Quantity);
            BuiltOrder = new OrderDto
            {
                VendorId = _selectedVendor?.Id ?? _vendorIdBox.Text?.Trim() ?? string.Empty,
                VendorName = _selectedVendor?.Name ?? string.Empty,
                Items = _items.ToList(),
                TotalAmount = total,
                Status = "submitted"
            };
        }
    }

    private bool ValidateStep(int step)
    {
        if (step == 1)
        {
            return _selectedVendor != null || !string.IsNullOrWhiteSpace(_vendorIdBox.Text);
        }
        if (step == 2)
        {
            return _items.Count > 0;
        }
        return true;
    }

    private void RenderStep()
    {
        switch (_step)
        {
            case 1:
                StepTitle.Text = "Step 1 – Vendor";
                StepFrame.Content = BuildVendorStep();
                this.PrimaryButtonText = "Next";
                this.SecondaryButtonText = "Back";
                break;
            case 2:
                StepTitle.Text = "Step 2 – Items";
                StepFrame.Content = BuildItemsStep();
                this.PrimaryButtonText = "Next";
                this.SecondaryButtonText = "Back";
                break;
            case 3:
                StepTitle.Text = "Step 3 – Review";
                StepFrame.Content = BuildReviewStep();
                this.PrimaryButtonText = "Finalize";
                this.SecondaryButtonText = "Back";
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
        // Ensure we don't show duplicates if any exist from prior state/drafts
        NormalizeItems();
        var availableWidth = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
        var narrow = availableWidth < 1200; // threshold for stacked layout

        Grid root;
        if (!narrow)
        {
            root = new Grid
            {
                Padding = new Thickness(20),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }, // custom add row
                    new RowDefinition { Height = GridLength.Auto }, // search
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // lists
                    new RowDefinition { Height = GridLength.Auto } // edit row
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
                    new RowDefinition { Height = GridLength.Auto }, // custom add row
                    new RowDefinition { Height = GridLength.Auto }, // search
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // catalog
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // cart
                    new RowDefinition { Height = GridLength.Auto } // edit row
                },
                ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } }
            };
        }

        // Top: custom item add row (spans 2 columns)
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0,0,0,8) };
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
        root.Children.Add(row);

        // Catalog search row (left column)
        var searchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0,0,0,8) };
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
        Grid.SetRow(searchRow, 1);
        Grid.SetColumn(searchRow, 0);
        if (narrow) Grid.SetColumnSpan(searchRow, 1); else Grid.SetColumnSpan(searchRow, 1);
        root.Children.Add(searchRow);

        // Left: catalog list
        var catalog = new ListView
        {
            ItemsSource = _catalog,
            IsItemClickEnabled = true,
            SelectionMode = ListViewSelectionMode.Single,
            ItemTemplate = BuildCatalogTemplate(),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            MinWidth = 900
        };
        catalog.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is MagiDesk.Shared.DTOs.ItemDto it)
            {
                AddOrIncrement(it.Name ?? "Item", it.Price, 1, it.Id ?? string.Empty, _selectedVendor?.Id, _selectedVendor?.Name);
            }
        };
        if (!narrow)
        {
            Grid.SetRow(catalog, 2);
            Grid.SetColumn(catalog, 0);
        }
        else
        {
            Grid.SetRow(catalog, 2);
            Grid.SetColumn(catalog, 0);
        }
        root.Children.Add(catalog);

        // Right: cart
        _cartList = new ListView
        {
            ItemTemplate = BuildCartTemplate(),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            MinWidth = 700
        };
        ApplyCartGrouping();
        _cartList.SelectionChanged += (s, e) =>
        {
            if (_cartList.SelectedItem is OrderItemDto it)
            {
                _editQtyBox.Value = it.Quantity;
            }
        };
        if (!narrow)
        {
            Grid.SetRow(_cartList, 2);
            Grid.SetColumn(_cartList, 1);
        }
        else
        {
            Grid.SetRow(_cartList, 3);
            Grid.SetColumn(_cartList, 0);
        }
        root.Children.Add(_cartList);

        var editRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0,8,0,0) };
        _editQtyBox = new NumberBox { Header = "Quantity", Minimum = 0, Maximum = 100000, Value = 1, Width = 120 };
        _updateBtn = new Button { Content = "Update" };
        _removeBtn = new Button { Content = "Remove" };
        var clearBtn = new Button { Content = "Clear Cart" };
        _updateBtn.Click += (s, e) =>
        {
            if (_cartList.SelectedItem is OrderItemDto it)
            {
                it.Quantity = (int)_editQtyBox.Value;
                // force refresh
                var idx = _items.IndexOf(it);
                if (idx >= 0) { _items.RemoveAt(idx); _items.Insert(idx, it); }
            }
        };
        _removeBtn.Click += (s, e) =>
        {
            if (_cartList.SelectedItem is OrderItemDto it)
            {
                _items.Remove(it);
            }
        };
        editRow.Children.Add(_editQtyBox);
        editRow.Children.Add(_updateBtn);
        editRow.Children.Add(_removeBtn);
        clearBtn.Click += (s, e) => { _items.Clear(); };
        editRow.Children.Add(clearBtn);
        if (!narrow)
        {
            Grid.SetRow(editRow, 3);
            Grid.SetColumn(editRow, 1);
        }
        else
        {
            Grid.SetRow(editRow, 4);
            Grid.SetColumn(editRow, 0);
        }
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
              <StackPanel Orientation='Horizontal'>
                <TextBlock Text='{Binding ItemName}' TextWrapping='WrapWholeWords'/>
                <TextBlock Text='  (x'/>
                <TextBlock Text='{Binding Quantity}'/>
                <TextBlock Text=')'/>
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

        // Build vendor-grouped review
        var groups = _items.GroupBy(i => i.VendorName ?? _selectedVendor?.Name ?? "Vendor")
                           .Select(g => new Group<string, OrderItemDto>(g.Key, g.ToList()))
                           .ToList();
        var cvs = new CollectionViewSource { IsSourceGrouped = true, Source = groups };
        cvs.ItemsPath = new PropertyPath("Items");
        var list = new ListView { ItemsSource = cvs.View, ItemTemplate = BuildReviewItemTemplate() };
        var gs = new GroupStyle();
        gs.HeaderTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Key}' FontWeight='SemiBold' FontSize='14' Margin='0,8,0,4'/></DataTemplate>");
        list.GroupStyle.Add(gs);

        panel.Children.Add(title);
        panel.Children.Add(list);
        panel.Children.Add(_totalText);
        return panel;
    }

    private DataTemplate BuildReviewItemTemplate()
    {
        var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
            <Grid Padding='4'>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width='*'/>
                <ColumnDefinition Width='Auto'/>
              </Grid.ColumnDefinitions>
              <TextBlock Text='{Binding ItemName}'/>
              <TextBlock Grid.Column='1' Text='{Binding Quantity}' Margin='12,0,0,0' HorizontalAlignment='Right' Width='60'/>
            </Grid>
        </DataTemplate>";
        return (DataTemplate)XamlReader.Load(xaml);
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
    }

    private void OnCartRemoveClick(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is OrderItemDto it)
        {
            _items.Remove(it);
        }
    }

    private void OnCartQtyChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (sender.DataContext is OrderItemDto it)
        {
            it.Quantity = (int)sender.Value;
            // Force refresh
            var idx = _items.IndexOf(it);
            if (idx >= 0) { _items.RemoveAt(idx); _items.Insert(idx, it); }
            UpdateTotals();
        }
    }

    private void UpdateTotals()
    {
        var total = _items.Sum(i => i.Price * i.Quantity);
        if (_totalText != null)
        {
            _totalText.Text = $"Total: {total:C}";
        }
    }

    private void AddOrIncrement(string name, decimal price, int increment = 1, string itemId = "", string? vendorId = null, string? vendorName = null)
    {
        var existing = _items.FirstOrDefault(x => string.Equals(x.ItemName, name, StringComparison.OrdinalIgnoreCase) && string.Equals(x.VendorId ?? "", vendorId ?? "", StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Quantity += increment;
            // force refresh
            var idx = _items.IndexOf(existing);
            if (idx >= 0) { _items.RemoveAt(idx); _items.Insert(idx, existing); }
        }
        else
        {
            _items.Add(new OrderItemDto { ItemId = itemId, ItemName = name, Quantity = increment, Price = price, VendorId = vendorId, VendorName = vendorName });
        }
        UpdateTotals();
        ApplyCartGrouping();
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
    }

    private void ApplyCartGrouping()
    {
        if (_cartList == null) return;
        var groups = _items.GroupBy(i => i.VendorName ?? _selectedVendor?.Name ?? "Vendor")
                           .Select(g => new Group<string, OrderItemDto>(g.Key, g.ToList()))
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
    }
}
