using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Logging;
using SharedVendorOrderDto = MagiDesk.Shared.DTOs.VendorOrderDto;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class VendorDetailsDialog : ContentDialog
{
    public VendorDisplay Vendor { get; set; }
    private readonly IVendorOrderService? _vendorOrderService;
    private readonly IInventoryService? _inventoryService;

    public VendorDetailsDialog(VendorDisplay vendor, IVendorOrderService? vendorOrderService = null, IInventoryService? inventoryService = null)
    {
        this.InitializeComponent();
        Vendor = vendor;
        _vendorOrderService = vendorOrderService;
        _inventoryService = inventoryService;

        // Set up tab selection changed handler
        DetailsTabView.SelectionChanged += DetailsTabView_SelectionChanged;
    }

    private async void DetailsTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DetailsTabView.SelectedItem is TabViewItem selectedTab)
        {
            switch (selectedTab.Tag?.ToString())
            {
                case "ItemsTab":
                    await LoadItemsAsync();
                    break;
                case "OrdersTab":
                    await LoadOrdersAsync();
                    break;
            }
        }
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            ItemsLoadingText.Visibility = Visibility.Visible;
            ItemsEmptyText.Visibility = Visibility.Collapsed;

            if (_inventoryService == null || string.IsNullOrWhiteSpace(Vendor.Id))
            {
                ItemsLoadingText.Visibility = Visibility.Collapsed;
                ItemsEmptyText.Visibility = Visibility.Visible;
                return;
            }

            var items = await _inventoryService.GetItemsAsync(vendorId: Vendor.Id);
            ItemsContent.Children.Clear();

            if (items == null || !items.Any())
            {
                ItemsLoadingText.Visibility = Visibility.Collapsed;
                ItemsEmptyText.Visibility = Visibility.Visible;
                return;
            }

            ItemsLoadingText.Visibility = Visibility.Collapsed;
            ItemsEmptyText.Visibility = Visibility.Collapsed;

            foreach (var item in items)
            {
                var itemCard = CreateItemCard(item);
                ItemsContent.Children.Add(itemCard);
            }
        }
        catch (Exception ex)
        {
            ItemsLoadingText.Visibility = Visibility.Collapsed;
            ItemsEmptyText.Text = $"Error loading items: {ex.Message}";
            ItemsEmptyText.Visibility = Visibility.Visible;
        }
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            OrdersLoadingText.Visibility = Visibility.Visible;
            OrdersEmptyText.Visibility = Visibility.Collapsed;

            if (_vendorOrderService == null || string.IsNullOrWhiteSpace(Vendor.Id))
            {
                OrdersLoadingText.Visibility = Visibility.Collapsed;
                OrdersEmptyText.Visibility = Visibility.Visible;
                return;
            }

            var orders = await _vendorOrderService.GetVendorOrdersByVendorIdAsync(Vendor.Id);
            OrdersContent.Children.Clear();

            if (orders == null || !orders.Any())
            {
                OrdersLoadingText.Visibility = Visibility.Collapsed;
                OrdersEmptyText.Visibility = Visibility.Visible;
                return;
            }

            OrdersLoadingText.Visibility = Visibility.Collapsed;
            OrdersEmptyText.Visibility = Visibility.Collapsed;

            // Cast to shared DTOs since the service deserializes from API which returns shared DTOs
            var sharedOrders = orders.Cast<SharedVendorOrderDto>();
            foreach (var order in sharedOrders.OrderByDescending(o => o.OrderDate))
            {
                var orderCard = CreateOrderCard(order);
                OrdersContent.Children.Add(orderCard);
            }
        }
        catch (Exception ex)
        {
            OrdersLoadingText.Visibility = Visibility.Collapsed;
            OrdersEmptyText.Text = $"Error loading orders: {ex.Message}";
            OrdersEmptyText.Visibility = Visibility.Visible;
        }
    }

    private Border CreateItemCard(ItemDto item)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 245)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leftStack = new StackPanel { Spacing = 4 };
        var nameBlock = new TextBlock
        {
            Text = item.Name,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        leftStack.Children.Add(nameBlock);

        var detailsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var skuBlock = new TextBlock();
        skuBlock.Inlines.Add(new Run { Text = "SKU: ", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
        skuBlock.Inlines.Add(new Run { Text = item.Sku });
        detailsStack.Children.Add(skuBlock);

        var priceBlock = new TextBlock();
        priceBlock.Inlines.Add(new Run { Text = "Price: ", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
        priceBlock.Inlines.Add(new Run { Text = item.Price.ToString("C") });
        detailsStack.Children.Add(priceBlock);

        leftStack.Children.Add(detailsStack);
        Grid.SetColumn(leftStack, 0);
        grid.Children.Add(leftStack);

        var stockBorder = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 185, 0)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center
        };

        var stockBlock = new TextBlock();
        var stockLabelRun = new Run { Text = "Stock: " };
        stockLabelRun.FontSize = 12;
        stockLabelRun.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(200, 0, 0, 0));
        stockBlock.Inlines.Add(stockLabelRun);
        
        var stockValueRun = new Run { Text = item.Stock.ToString() };
        stockValueRun.FontSize = 14;
        stockValueRun.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        stockValueRun.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        stockBlock.Inlines.Add(stockValueRun);
        
        stockBorder.Child = stockBlock;
        Grid.SetColumn(stockBorder, 1);
        grid.Children.Add(stockBorder);

        border.Child = grid;
        return border;
    }

    private Border CreateOrderCard(SharedVendorOrderDto order)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 245)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var stack = new StackPanel { Spacing = 8 };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var orderIdBlock = new TextBlock
        {
            Text = $"Order #{order.OrderId}",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        Grid.SetColumn(orderIdBlock, 0);
        headerGrid.Children.Add(orderIdBlock);

        var statusBorder = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(GetStatusColor(order.Status))
        };
        var statusBlock = new TextBlock
        {
            Text = order.Status,
            FontSize = 10,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
        };
        statusBorder.Child = statusBlock;
        Grid.SetColumn(statusBorder, 1);
        headerGrid.Children.Add(statusBorder);

        stack.Children.Add(headerGrid);

        var detailsGrid = new Grid();
        detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var dateBlock = new TextBlock();
        dateBlock.Inlines.Add(new Run { Text = "Date: ", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
        dateBlock.Inlines.Add(new Run { Text = order.OrderDate.ToString("MM/dd/yyyy"), FontSize = 11 });
        Grid.SetColumn(dateBlock, 0);
        Grid.SetRow(dateBlock, 0);
        detailsGrid.Children.Add(dateBlock);

        var valueBlock = new TextBlock();
        valueBlock.Inlines.Add(new Run { Text = "Value: ", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
        valueBlock.Inlines.Add(new Run { Text = order.TotalValue.ToString("C"), FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        Grid.SetColumn(valueBlock, 1);
        Grid.SetRow(valueBlock, 0);
        detailsGrid.Children.Add(valueBlock);

        var itemsBlock = new TextBlock();
        itemsBlock.Inlines.Add(new Run { Text = "Items: ", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
        itemsBlock.Inlines.Add(new Run { Text = order.ItemCount.ToString(), FontSize = 11 });
        Grid.SetColumn(itemsBlock, 0);
        Grid.SetRow(itemsBlock, 1);
        detailsGrid.Children.Add(itemsBlock);

        if (order.ExpectedDeliveryDate.HasValue)
        {
            var deliveryBlock = new TextBlock();
            deliveryBlock.Inlines.Add(new Run { Text = "Expected: ", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
            deliveryBlock.Inlines.Add(new Run { Text = order.ExpectedDeliveryDate.Value.ToString("MM/dd/yyyy"), FontSize = 11 });
            Grid.SetColumn(deliveryBlock, 1);
            Grid.SetRow(deliveryBlock, 1);
            detailsGrid.Children.Add(deliveryBlock);
        }

        stack.Children.Add(detailsGrid);
        border.Child = stack;
        return border;
    }

    private Windows.UI.Color GetStatusColor(string status)
    {
        return status switch
        {
            "Draft" => Windows.UI.Color.FromArgb(255, 128, 128, 128),
            "Sent" => Windows.UI.Color.FromArgb(255, 0, 120, 215),
            "Confirmed" => Windows.UI.Color.FromArgb(255, 177, 70, 194),
            "Shipped" => Windows.UI.Color.FromArgb(255, 255, 140, 0),
            "Delivered" => Windows.UI.Color.FromArgb(255, 16, 124, 16),
            "Cancelled" => Windows.UI.Color.FromArgb(255, 232, 17, 35),
            _ => Windows.UI.Color.FromArgb(255, 128, 128, 128)
        };
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        this.Tag = "Edit";
        this.Hide();
    }
}

