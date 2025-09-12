using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorOrdersPage : Page, IToolbarConsumer
{
    private readonly VendorOrdersViewModel _vm;

    public VendorOrdersPage()
    {
        this.InitializeComponent();
        
        // Create services with proper HTTP clients and loggers
        var vendorOrderService = new VendorOrderService(new HttpClient(), new SimpleLogger<VendorOrderService>());
        var inventoryService = new InventoryService(new HttpClient(), new SimpleLogger<InventoryService>());
        
        _vm = new VendorOrdersViewModel(vendorOrderService, inventoryService);
        this.DataContext = _vm;
        
        Loaded += VendorOrdersPage_Loaded;
    }

    private async void VendorOrdersPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private async void CreatePurchaseOrder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PurchaseOrderDialog();
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await _vm.CreateOrderAsync(dialog.Order);
            await _vm.LoadDataAsync();
        }
    }

    private async void ImportOrders_Click(object sender, RoutedEventArgs e)
    {
        var result = await ShowConfirmDialog("Import Orders", "This will import orders from external systems. Continue?");
        if (result == ContentDialogResult.Primary)
        {
            await _vm.ImportOrdersAsync();
            await _vm.LoadDataAsync();
        }
    }

    private async void ExportOrders_Click(object sender, RoutedEventArgs e)
    {
        await _vm.ExportOrdersAsync();
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

    private void OrdersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection if needed
    }

    private async void ViewOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string orderId)
        {
            var dialog = new OrderDetailsDialog(orderId);
            await dialog.ShowAsync();
        }
    }

    private async void EditOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string orderId)
        {
            var order = await _vm.GetOrderAsync(orderId);
            if (order != null)
            {
                var dialog = new PurchaseOrderDialog(order);
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    await _vm.UpdateOrderAsync(orderId, dialog.Order);
                    await _vm.LoadDataAsync();
                }
            }
        }
    }

    private async void SendOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string orderId)
        {
            var result = await ShowConfirmDialog("Send Order", "Are you sure you want to send this order to the vendor?");
            if (result == ContentDialogResult.Primary)
            {
                await _vm.SendOrderAsync(orderId);
                await _vm.LoadDataAsync();
            }
        }
    }

    private async void ReceiveOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string orderId)
        {
            var dialog = new OrderReceiptDialog(orderId);
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await _vm.ReceiveOrderAsync(orderId, dialog.ReceivedItems);
                await _vm.LoadDataAsync();
            }
        }
    }

    private async void CancelOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string orderId)
        {
            var result = await ShowConfirmDialog("Cancel Order", "Are you sure you want to cancel this order?");
            if (result == ContentDialogResult.Primary)
            {
                await _vm.CancelOrderAsync(orderId);
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
        CreatePurchaseOrder_Click(this, new RoutedEventArgs());
    }

    public void OnEdit()
    {
        if (OrdersList.SelectedItem is VendorOrderDto order)
        {
            EditOrder_Click(this, new RoutedEventArgs());
        }
    }

    public void OnDelete()
    {
        if (OrdersList.SelectedItem is VendorOrderDto order)
        {
            CancelOrder_Click(this, new RoutedEventArgs());
        }
    }

    public void OnRefresh()
    {
        Refresh_Click(this, new RoutedEventArgs());
    }
}