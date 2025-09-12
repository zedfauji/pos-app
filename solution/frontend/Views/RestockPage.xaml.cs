using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class RestockPage : Page, IToolbarConsumer
{
    private readonly RestockViewModel _vm;

    public RestockPage()
    {
        this.InitializeComponent();
        
        // Create services
        var restockService = new RestockService(new HttpClient(), new SimpleLogger<RestockService>());
        var inventoryService = new InventoryService(new HttpClient(), new SimpleLogger<InventoryService>());
        var vendorService = new VendorService(new HttpClient(), new SimpleLogger<VendorService>());
        
        _vm = new RestockViewModel(restockService, inventoryService, vendorService);
        this.DataContext = _vm;
        
        Loaded += RestockPage_Loaded;
    }

    private async void RestockPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private async void CreateRestockRequest_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RestockRequestDialog();
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await _vm.AddRestockRequestAsync(dialog.Request);
            await _vm.LoadDataAsync();
        }
    }

    private async void AutoRestock_Click(object sender, RoutedEventArgs e)
    {
        var result = await ShowConfirmDialog("Auto-Restock", "This will create restock requests for all items below minimum stock levels. Continue?");
        if (result == ContentDialogResult.Primary)
        {
            await _vm.AutoRestockLowStockAsync();
            await _vm.LoadDataAsync();
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchText = SearchBox.Text;
        _vm.ApplyFilters();
    }

    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = StatusFilter.SelectedItem as ComboBoxItem;
        _vm.StatusFilter = selectedItem?.Tag?.ToString() ?? string.Empty;
        _vm.ApplyFilters();
    }

    private void RestockList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection if needed
    }

    private async void ApproveRequest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string requestId)
        {
            await _vm.ApproveRequestAsync(requestId);
            await _vm.LoadDataAsync();
        }
    }

    private async void MarkAsOrdered_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string requestId)
        {
            await _vm.MarkAsOrderedAsync(requestId);
            await _vm.LoadDataAsync();
        }
    }

    private async void MarkAsReceived_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string requestId)
        {
            await _vm.MarkAsReceivedAsync(requestId);
            await _vm.LoadDataAsync();
        }
    }

    private async void CancelRequest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string requestId)
        {
            var result = await ShowConfirmDialog("Cancel Request", "Are you sure you want to cancel this restock request?");
            if (result == ContentDialogResult.Primary)
            {
                await _vm.CancelRequestAsync(requestId);
                await _vm.LoadDataAsync();
            }
        }
    }

    private async Task<ContentDialogResult> ShowConfirmDialog(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            XamlRoot = this.XamlRoot
        };
        
        return await dialog.ShowAsync();
    }

    // IToolbarConsumer implementation
    public void OnAdd()
    {
        CreateRestockRequest_Click(this, new RoutedEventArgs());
    }

    public void OnEdit()
    {
        if (RestockList.SelectedItem is RestockRequestDto request)
        {
            // TODO: Implement edit functionality
        }
    }

    public void OnDelete()
    {
        if (RestockList.SelectedItem is RestockRequestDto request)
        {
            CancelRequest_Click(this, new RoutedEventArgs());
        }
    }

    public void OnRefresh()
    {
        Refresh_Click(this, new RoutedEventArgs());
    }
}
