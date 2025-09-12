using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class InventoryCrudPage : Page, IToolbarConsumer
{
    private readonly InventoryCrudViewModel _vm;

    public InventoryCrudPage()
    {
        this.InitializeComponent();
        
        // Create services
        var inventoryService = new InventoryService(new HttpClient(), new SimpleLogger<InventoryService>());
        var vendorService = new VendorService(new HttpClient(), new SimpleLogger<VendorService>());
        
        _vm = new InventoryCrudViewModel(inventoryService, vendorService);
        this.DataContext = _vm;
        
        Loaded += InventoryCrudPage_Loaded;
    }

    private async void InventoryCrudPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
        await LoadFiltersAsync();
    }

    private async Task LoadFiltersAsync()
    {
        try
        {
            // Load categories
            var categories = await _vm.GetCategoriesAsync();
            CategoryFilter.Items.Clear();
            CategoryFilter.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = "" });
            foreach (var category in categories)
            {
                CategoryFilter.Items.Add(new ComboBoxItem { Content = category, Tag = category });
            }

            // Load vendors
            var vendors = await _vm.GetVendorsAsync();
            VendorFilter.Items.Clear();
            VendorFilter.Items.Add(new ComboBoxItem { Content = "All Vendors", Tag = "" });
            foreach (var vendor in vendors)
            {
                VendorFilter.Items.Add(new ComboBoxItem { Content = vendor.Name, Tag = vendor.Id });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading filters: {ex.Message}");
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private async void AddItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InventoryItemDialog();
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _vm.AddItemAsync(dialog.Item);
            await _vm.LoadDataAsync();
        }
    }

    private async void EditItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string itemId)
        {
            var item = await _vm.GetItemAsync(itemId);
            if (item != null)
            {
                var dialog = new InventoryItemDialog(item);
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await _vm.UpdateItemAsync(itemId, dialog.Item);
                    await _vm.LoadDataAsync();
                }
            }
        }
    }

    private async void AdjustStock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string itemId)
        {
            var dialog = new StockAdjustmentDialog(itemId);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _vm.AdjustStockAsync(itemId, dialog.Adjustment, dialog.Notes);
                await _vm.LoadDataAsync();
            }
        }
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string itemId)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Item",
                Content = "Are you sure you want to delete this item?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            
            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _vm.DeleteItemAsync(itemId);
                await _vm.LoadDataAsync();
            }
        }
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement CSV import functionality
        var dialog = new ContentDialog
        {
            Title = "Import Items",
            Content = "CSV import functionality will be implemented here.",
            PrimaryButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement CSV export functionality
        var dialog = new ContentDialog
        {
            Title = "Export Items",
            Content = "CSV export functionality will be implemented here.",
            PrimaryButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryFilter.SelectedItem is ComboBoxItem item && item.Tag is string category)
        {
            _vm.FilterByCategory(category);
        }
    }

    private void VendorFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VendorFilter.SelectedItem is ComboBoxItem item && item.Tag is string vendorId)
        {
            _vm.FilterByVendor(vendorId);
        }
    }

    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusFilter.SelectedItem is ComboBoxItem item && item.Tag is string status)
        {
            _vm.FilterByStatus(status);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _vm.Search(textBox.Text);
        }
    }

    private void InventoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection if needed
    }

    // IToolbarConsumer implementation
    public void OnAdd()
    {
        AddItem_Click(this, new RoutedEventArgs());
    }

    public void OnEdit()
    {
        if (InventoryList.SelectedItem is InventoryItemDto item)
        {
            EditItem_Click(this, new RoutedEventArgs());
        }
    }

    public void OnDelete()
    {
        if (InventoryList.SelectedItem is InventoryItemDto item)
        {
            DeleteItem_Click(this, new RoutedEventArgs());
        }
    }

    public void OnRefresh()
    {
        Refresh_Click(this, new RoutedEventArgs());
    }
}
