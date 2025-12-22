using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SharedVendorOrderDto = MagiDesk.Shared.DTOs.VendorOrderDto;
using System.Linq;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorDetailsPage : Page
{
    private VendorDisplay _vendor = new VendorDisplay(new VendorDto { Name = "Loading..." });
    private readonly IVendorOrderService? _vendorOrderService;
    private readonly IInventoryService? _inventoryService;
    private readonly IVendorService? _vendorService;
    
    // Items tab state
    private List<ItemDto> _allItems = new List<ItemDto>();
    
    // Orders tab state
    private List<SharedVendorOrderDto> _allOrders = new List<SharedVendorOrderDto>();

    public VendorDetailsPage()
    {
        this.InitializeComponent();
        this.DataContext = this;

        // Initialize services
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var inventoryBase = config["InventoryApi:BaseUrl"] ?? "https://localhost:5001";
            var vendorOrdersBase = config["VendorOrdersApi:BaseUrl"] ?? inventoryBase;

            var innerVendor = new HttpClientHandler();
            innerVendor.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var logVendor = new Services.HttpLoggingHandler(innerVendor);
            var vendorHttpClient = new HttpClient(logVendor) 
            { 
                BaseAddress = new Uri(inventoryBase.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _vendorService = new VendorService(vendorHttpClient, new SimpleLogger<VendorService>());

            var vendorOrdersHttpClient = new HttpClient(logVendor) 
            { 
                BaseAddress = new Uri(vendorOrdersBase.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _vendorOrderService = new VendorOrderService(vendorOrdersHttpClient, new SimpleLogger<VendorOrderService>());

            var inventoryHandler = new HttpClientHandler();
            inventoryHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var inventoryHttpClient = new HttpClient(inventoryHandler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _inventoryService = new InventoryService(inventoryHttpClient, new SimpleLogger<InventoryService>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing services: {ex.Message}");
        }

        // Set up tab selection changed handler
        DetailsTabView.SelectionChanged += DetailsTabView_SelectionChanged;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is VendorDisplay vendor)
        {
            _vendor = vendor;
            await LoadVendorDataAsync();
        }
        else if (e.Parameter is string vendorId && _vendorService != null)
        {
            await LoadVendorByIdAsync(vendorId);
        }
        else
        {
            ShowError("Invalid vendor parameter");
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }

    private async Task LoadVendorByIdAsync(string vendorId)
    {
        try
        {
            if (_vendorService == null) return;

            var vendors = await _vendorService.GetVendorsAsync();
            var vendorDto = vendors.FirstOrDefault(v => v.Id == vendorId);
            
            if (vendorDto == null)
            {
                ShowError("Vendor not found");
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                return;
            }

            _vendor = new VendorDisplay(vendorDto);
            await LoadVendorDataAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Error loading vendor: {ex.Message}");
        }
    }

    private async Task LoadVendorDataAsync()
    {
        try
        {
            // Load order summaries
            if (_vendorOrderService != null && !string.IsNullOrWhiteSpace(_vendor.Id))
            {
                List<SharedVendorOrderDto> ordersList;
                try
                {
                    var orders = await _vendorOrderService.GetVendorOrdersByVendorIdAsync(_vendor.Id);
                    ordersList = orders != null ? orders.Cast<SharedVendorOrderDto>().ToList() : new List<SharedVendorOrderDto>();
                }
                catch (HttpRequestException httpEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Network error loading orders: {httpEx.Message}");
                    ordersList = new List<SharedVendorOrderDto>();
                    // Continue with empty list rather than failing completely
                }
                catch (TaskCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("Request timeout loading orders");
                    ordersList = new List<SharedVendorOrderDto>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading orders: {ex.Message}");
                    ordersList = new List<SharedVendorOrderDto>();
                }

                if (ordersList.Any())
                {
                    _vendor.TotalOrders = ordersList.Count;
                    _vendor.PendingOrders = ordersList.Count(o => o.Status == "Draft" || o.Status == "Sent" || o.Status == "Confirmed" || o.Status == "Shipped");
                    _vendor.TotalOrderValue = ordersList.Sum(o => o.TotalValue);
                    _vendor.LastOrderDate = ordersList.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;

                // Calculate performance metrics
                if (ordersList.Any())
                {
                    var deliveredOrders = ordersList.Where(o => o.Status == "Delivered" && o.ActualDeliveryDate.HasValue && o.ExpectedDeliveryDate.HasValue).ToList();
                    
                    if (deliveredOrders.Any())
                    {
                        var onTimeDeliveries = deliveredOrders.Count(o => o.ActualDeliveryDate.Value <= o.ExpectedDeliveryDate.Value);
                        _vendor.OnTimeDeliveryRate = (double)onTimeDeliveries / deliveredOrders.Count * 100;

                        var deliveryTimes = deliveredOrders
                            .Where(o => o.SentDate.HasValue && o.ActualDeliveryDate.HasValue)
                            .Select(o => (o.ActualDeliveryDate.Value - o.SentDate.Value).TotalDays)
                            .ToList();
                        if (deliveryTimes.Any())
                        {
                            _vendor.AverageDeliveryDays = deliveryTimes.Average();
                        }

                        _vendor.AverageOrderValue = deliveredOrders.Average(o => o.TotalValue);
                    }

                    var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                    var recentOrders = ordersList.Where(o => o.OrderDate >= thirtyDaysAgo).ToList();
                    _vendor.MonthlySpend = recentOrders.Sum(o => o.TotalValue);

                    var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    _vendor.OrdersThisMonth = ordersList.Count(o => o.OrderDate >= startOfMonth);
                }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vendor data: {ex.Message}");
            // Don't show error to user here - let individual tabs handle their own errors
        }
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
                case "AnalyticsTab":
                    await LoadAnalyticsAsync();
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

            if (_inventoryService == null || string.IsNullOrWhiteSpace(_vendor.Id))
            {
                ItemsLoadingText.Visibility = Visibility.Collapsed;
                ItemsEmptyText.Visibility = Visibility.Visible;
                return;
            }

            IEnumerable<ItemDto>? items = null;
            try
            {
                items = await _inventoryService.GetItemsAsync(vendorId: _vendor.Id);
            }
            catch (HttpRequestException httpEx)
            {
                ItemsLoadingText.Visibility = Visibility.Collapsed;
                ItemsEmptyText.Text = "Unable to connect to the server. Please check your network connection.";
                ItemsEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Network error loading items: {httpEx.Message}");
                return;
            }
            catch (TaskCanceledException)
            {
                ItemsLoadingText.Visibility = Visibility.Collapsed;
                ItemsEmptyText.Text = "Request timed out. Please try again.";
                ItemsEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("Request timeout loading items");
                return;
            }
            catch (Exception ex)
            {
                ItemsLoadingText.Visibility = Visibility.Collapsed;
                ItemsEmptyText.Text = $"Error loading items: {ex.Message}";
                ItemsEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
                return;
            }

            ItemsContent.Children.Clear();

            if (items == null || !items.Any())
            {
                ItemsLoadingText.Visibility = Visibility.Collapsed;
                ItemsEmptyText.Visibility = Visibility.Visible;
                return;
            }

            ItemsLoadingText.Visibility = Visibility.Collapsed;
            ItemsEmptyText.Visibility = Visibility.Collapsed;

            _allItems = items.ToList();
            ApplyItemsFilterAndSort();
        }
        catch (Exception ex)
        {
            ItemsLoadingText.Visibility = Visibility.Collapsed;
            ItemsEmptyText.Text = $"Unexpected error: {ex.Message}";
            ItemsEmptyText.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine($"Unexpected error in LoadItemsAsync: {ex.Message}");
        }
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            OrdersLoadingText.Visibility = Visibility.Visible;
            OrdersEmptyText.Visibility = Visibility.Collapsed;

            if (_vendorOrderService == null || string.IsNullOrWhiteSpace(_vendor.Id))
            {
                OrdersLoadingText.Visibility = Visibility.Collapsed;
                OrdersEmptyText.Visibility = Visibility.Visible;
                return;
            }

            IEnumerable<SharedVendorOrderDto>? orders = null;
            try
            {
                var orderResult = await _vendorOrderService.GetVendorOrdersByVendorIdAsync(_vendor.Id);
                orders = orderResult?.Cast<SharedVendorOrderDto>();
            }
            catch (HttpRequestException httpEx)
            {
                OrdersLoadingText.Visibility = Visibility.Collapsed;
                OrdersEmptyText.Text = "Unable to connect to the server. Please check your network connection.";
                OrdersEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Network error loading orders: {httpEx.Message}");
                return;
            }
            catch (TaskCanceledException)
            {
                OrdersLoadingText.Visibility = Visibility.Collapsed;
                OrdersEmptyText.Text = "Request timed out. Please try again.";
                OrdersEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("Request timeout loading orders");
                return;
            }
            catch (Exception ex)
            {
                OrdersLoadingText.Visibility = Visibility.Collapsed;
                OrdersEmptyText.Text = $"Error loading orders: {ex.Message}";
                OrdersEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Error loading orders: {ex.Message}");
                return;
            }

            OrdersContent.Children.Clear();

            if (orders == null || !orders.Any())
            {
                OrdersLoadingText.Visibility = Visibility.Collapsed;
                OrdersEmptyText.Visibility = Visibility.Visible;
                return;
            }

            OrdersLoadingText.Visibility = Visibility.Collapsed;
            OrdersEmptyText.Visibility = Visibility.Collapsed;

            _allOrders = orders.ToList();
            ApplyOrdersFilterAndSort();
        }
        catch (Exception ex)
        {
            OrdersLoadingText.Visibility = Visibility.Collapsed;
            OrdersEmptyText.Text = $"Unexpected error: {ex.Message}";
            OrdersEmptyText.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine($"Unexpected error in LoadOrdersAsync: {ex.Message}");
        }
    }

    private async void AddItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_vendor.Id))
            {
                ShowError("Vendor ID is missing. Cannot add item.");
                return;
            }

            var dialog = new Dialogs.ItemDialog(new ItemDto
            {
                Sku = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
            });
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Create item via vendor-specific endpoint
                    var createdItem = await CreateVendorItemAsync(_vendor.Id, dialog.Dto);
                    if (createdItem != null)
                    {
                        // Reload items
                        await LoadItemsAsync();
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to create item: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }

    private async Task<ItemDto?> CreateVendorItemAsync(string vendorId, ItemDto item)
    {
        try
        {
            if (_inventoryService == null) return null;

            // Use the vendor-specific endpoint
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var inventoryBase = config["InventoryApi:BaseUrl"] ?? "https://localhost:5001";
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(inventoryBase.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(item);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"api/vendors/{Uri.EscapeDataString(vendorId)}/items", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<ItemDto>(responseJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to create item: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating vendor item: {ex.Message}");
            throw;
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

        var mainStack = new StackPanel { Spacing = 8 };

        // Header row with name and stock
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameBlock = new TextBlock
        {
            Text = item.Name,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        Grid.SetColumn(nameBlock, 0);
        headerGrid.Children.Add(nameBlock);

        // Stock badge with color coding
        var stockColor = item.Stock <= 10 
            ? Windows.UI.Color.FromArgb(255, 232, 17, 35)  // Red for low stock
            : item.Stock <= 50 
                ? Windows.UI.Color.FromArgb(255, 255, 185, 0)  // Yellow for medium stock
                : Windows.UI.Color.FromArgb(255, 16, 124, 16);  // Green for good stock

        var stockBorder = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(stockColor),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center
        };

        var stockBlock = new TextBlock();
        var stockLabelRun = new Run { Text = "Stock: " };
        stockLabelRun.FontSize = 11;
        stockLabelRun.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(200, 0, 0, 0));
        stockBlock.Inlines.Add(stockLabelRun);
        
        var stockValueRun = new Run { Text = item.Stock.ToString() };
        stockValueRun.FontSize = 13;
        stockValueRun.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        stockValueRun.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        stockBlock.Inlines.Add(stockValueRun);
        
        stockBorder.Child = stockBlock;
        Grid.SetColumn(stockBorder, 1);
        headerGrid.Children.Add(stockBorder);

        mainStack.Children.Add(headerGrid);

        // Details row
        var detailsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        
        if (!string.IsNullOrWhiteSpace(item.Sku))
        {
            var skuBlock = new TextBlock();
            skuBlock.Inlines.Add(new Run { Text = "SKU: ", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
            skuBlock.Inlines.Add(new Run { Text = item.Sku, FontSize = 11 });
            detailsStack.Children.Add(skuBlock);
        }

        var priceBlock = new TextBlock();
        priceBlock.Inlines.Add(new Run { Text = "Price: ", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128)) });
        priceBlock.Inlines.Add(new Run { Text = item.Price.ToString("C"), FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        detailsStack.Children.Add(priceBlock);


        mainStack.Children.Add(detailsStack);

        border.Child = mainStack;
        
        // Make item card clickable for editing
        border.PointerPressed += (s, args) => { args.Handled = true; };
        border.DoubleTapped += async (s, args) => await EditItem_Click(item);
        
        return border;
    }

    private async Task EditItem_Click(ItemDto item)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_vendor.Id) || string.IsNullOrWhiteSpace(item.Id))
            {
                ShowError("Cannot edit item: Missing vendor or item ID.");
                return;
            }

            var dialog = new Dialogs.ItemDialog(item);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Update item via vendor-specific endpoint
                    var success = await UpdateVendorItemAsync(_vendor.Id, item.Id, dialog.Dto);
                    if (success)
                    {
                        // Reload items
                        await LoadItemsAsync();
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to update item: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }

    private async Task<bool> UpdateVendorItemAsync(string vendorId, string itemId, ItemDto item)
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var inventoryBase = config["InventoryApi:BaseUrl"] ?? "https://localhost:5001";
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(inventoryBase.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(item);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"api/vendors/{Uri.EscapeDataString(vendorId)}/items/{Uri.EscapeDataString(itemId)}", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating vendor item: {ex.Message}");
            throw;
        }
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

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Reload vendor data
            if (!string.IsNullOrWhiteSpace(_vendor.Id) && _vendorService != null)
            {
                await LoadVendorByIdAsync(_vendor.Id);
            }
            else
            {
                await LoadVendorDataAsync();
            }

            // Reload current tab if applicable
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
                    case "AnalyticsTab":
                        await LoadAnalyticsAsync();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to refresh: {ex.Message}");
        }
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new VendorCrudDialog(_vendor);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Reload vendor data after edit
                if (!string.IsNullOrWhiteSpace(_vendor.Id) && _vendorService != null)
                {
                    await LoadVendorByIdAsync(_vendor.Id);
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to edit vendor: {ex.Message}");
        }
    }

    private async Task LoadAnalyticsAsync()
    {
        try
        {
            AnalyticsLoadingText.Visibility = Visibility.Visible;
            AnalyticsEmptyText.Visibility = Visibility.Collapsed;

            if (_vendorOrderService == null || string.IsNullOrWhiteSpace(_vendor.Id))
            {
                AnalyticsLoadingText.Visibility = Visibility.Collapsed;
                AnalyticsEmptyText.Visibility = Visibility.Visible;
                return;
            }

            IEnumerable<SharedVendorOrderDto>? orders = null;
            try
            {
                var orderResult = await _vendorOrderService.GetVendorOrdersByVendorIdAsync(_vendor.Id);
                orders = orderResult?.Cast<SharedVendorOrderDto>();
            }
            catch (HttpRequestException httpEx)
            {
                AnalyticsLoadingText.Visibility = Visibility.Collapsed;
                AnalyticsEmptyText.Text = "Unable to connect to the server. Please check your network connection.";
                AnalyticsEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Network error loading analytics: {httpEx.Message}");
                return;
            }
            catch (TaskCanceledException)
            {
                AnalyticsLoadingText.Visibility = Visibility.Collapsed;
                AnalyticsEmptyText.Text = "Request timed out. Please try again.";
                AnalyticsEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("Request timeout loading analytics");
                return;
            }
            catch (Exception ex)
            {
                AnalyticsLoadingText.Visibility = Visibility.Collapsed;
                AnalyticsEmptyText.Text = $"Error loading analytics: {ex.Message}";
                AnalyticsEmptyText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Error loading analytics: {ex.Message}");
                return;
            }

            AnalyticsContent.Children.Clear();

            if (orders == null || !orders.Any())
            {
                AnalyticsLoadingText.Visibility = Visibility.Collapsed;
                AnalyticsEmptyText.Visibility = Visibility.Visible;
                return;
            }

            AnalyticsLoadingText.Visibility = Visibility.Collapsed;
            AnalyticsEmptyText.Visibility = Visibility.Collapsed;

            var ordersList = orders.ToList();

            // Spend Trends Section
            var spendTrendsCard = CreateSpendTrendsCard(ordersList);
            AnalyticsContent.Children.Add(spendTrendsCard);

            // Delivery Performance Section
            var deliveryPerformanceCard = CreateDeliveryPerformanceCard(ordersList);
            AnalyticsContent.Children.Add(deliveryPerformanceCard);

            // Order Statistics Section
            var orderStatsCard = CreateOrderStatisticsCard(ordersList);
            AnalyticsContent.Children.Add(orderStatsCard);

            // Monthly Comparison Section
            var monthlyComparisonCard = CreateMonthlyComparisonCard(ordersList);
            AnalyticsContent.Children.Add(monthlyComparisonCard);
        }
        catch (Exception ex)
        {
            AnalyticsLoadingText.Visibility = Visibility.Collapsed;
            AnalyticsEmptyText.Text = $"Unexpected error: {ex.Message}";
            AnalyticsEmptyText.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine($"Unexpected error in LoadAnalyticsAsync: {ex.Message}");
        }
    }

    private Border CreateSpendTrendsCard(List<SharedVendorOrderDto> orders)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 245)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };

        var stack = new StackPanel { Spacing = 12 };

        var titleBlock = new TextBlock
        {
            Text = "💰 Spend Trends (Last 6 Months)",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        stack.Children.Add(titleBlock);

        // Calculate monthly spend
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var monthlySpend = orders
            .Where(o => o.OrderDate >= sixMonthsAgo)
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                Total = g.Sum(o => o.TotalValue),
                Count = g.Count()
            })
            .ToList();

        if (monthlySpend.Any())
        {
            var maxSpend = monthlySpend.Max(m => m.Total);
            var chartGrid = new Grid();
            chartGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            chartGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var chartPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 150,
                Spacing = 8
            };

            foreach (var month in monthlySpend)
            {
                var monthStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var barHeight = maxSpend > 0 ? (double)(month.Total / maxSpend) * 130 : 0;
                var bar = new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)),
                    Height = barHeight,
                    CornerRadius = new CornerRadius(4, 4, 0, 0),
                    Margin = new Thickness(2, 0, 2, 0),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                monthStack.Children.Add(bar);

                var labelBlock = new TextBlock
                {
                    Text = month.Month.ToString("MMM"),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                monthStack.Children.Add(labelBlock);

                var valueBlock = new TextBlock
                {
                    Text = month.Total.ToString("C0"),
                    FontSize = 9,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0.7
                };
                monthStack.Children.Add(valueBlock);

                chartPanel.Children.Add(monthStack);
            }

            Grid.SetRow(chartPanel, 0);
            chartGrid.Children.Add(chartPanel);

            var totalBlock = new TextBlock
            {
                Text = $"Total: {monthlySpend.Sum(m => m.Total):C}",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(totalBlock, 1);
            chartGrid.Children.Add(totalBlock);

            stack.Children.Add(chartGrid);
        }
        else
        {
            var emptyBlock = new TextBlock
            {
                Text = "No spend data available for the last 6 months.",
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };
            stack.Children.Add(emptyBlock);
        }

        border.Child = stack;
        return border;
    }

    private Border CreateDeliveryPerformanceCard(List<SharedVendorOrderDto> orders)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 245)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };

        var stack = new StackPanel { Spacing = 12 };

        var titleBlock = new TextBlock
        {
            Text = "🚚 Delivery Performance",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        stack.Children.Add(titleBlock);

        var deliveredOrders = orders.Where(o => o.Status == "Delivered" && o.ActualDeliveryDate.HasValue && o.ExpectedDeliveryDate.HasValue).ToList();

        if (deliveredOrders.Any())
        {
            var onTimeCount = deliveredOrders.Count(o => o.ActualDeliveryDate!.Value <= o.ExpectedDeliveryDate!.Value);
            var onTimeRate = (double)onTimeCount / deliveredOrders.Count * 100;

            var statsGrid = new Grid();
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // On-Time Rate
            var onTimeStack = new StackPanel { Margin = new Thickness(0, 0, 8, 8) };
            var onTimeLabel = new TextBlock { Text = "On-Time Rate", FontSize = 11, Opacity = 0.7 };
            var onTimeValue = new TextBlock
            {
                Text = $"{onTimeRate:F1}%",
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 16))
            };
            onTimeStack.Children.Add(onTimeLabel);
            onTimeStack.Children.Add(onTimeValue);
            Grid.SetColumn(onTimeStack, 0);
            Grid.SetRow(onTimeStack, 0);
            statsGrid.Children.Add(onTimeStack);

            // Average Delivery Days
            var deliveryTimes = deliveredOrders
                .Where(o => o.SentDate.HasValue && o.ActualDeliveryDate.HasValue)
                .Select(o => (o.ActualDeliveryDate!.Value - o.SentDate!.Value).TotalDays)
                .ToList();
            var avgDays = deliveryTimes.Any() ? deliveryTimes.Average() : 0;

            var avgDaysStack = new StackPanel { Margin = new Thickness(8, 0, 0, 8) };
            var avgDaysLabel = new TextBlock { Text = "Avg. Delivery Days", FontSize = 11, Opacity = 0.7 };
            var avgDaysValue = new TextBlock
            {
                Text = $"{avgDays:F1}",
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            avgDaysStack.Children.Add(avgDaysLabel);
            avgDaysStack.Children.Add(avgDaysValue);
            Grid.SetColumn(avgDaysStack, 1);
            Grid.SetRow(avgDaysStack, 0);
            statsGrid.Children.Add(avgDaysStack);

            // Total Delivered
            var totalDeliveredStack = new StackPanel { Margin = new Thickness(0, 8, 8, 0) };
            var totalDeliveredLabel = new TextBlock { Text = "Total Delivered", FontSize = 11, Opacity = 0.7 };
            var totalDeliveredValue = new TextBlock
            {
                Text = deliveredOrders.Count.ToString(),
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            totalDeliveredStack.Children.Add(totalDeliveredLabel);
            totalDeliveredStack.Children.Add(totalDeliveredValue);
            Grid.SetColumn(totalDeliveredStack, 0);
            Grid.SetRow(totalDeliveredStack, 1);
            statsGrid.Children.Add(totalDeliveredStack);

            // Late Deliveries
            var lateCount = deliveredOrders.Count - onTimeCount;
            var lateStack = new StackPanel { Margin = new Thickness(8, 8, 0, 0) };
            var lateLabel = new TextBlock { Text = "Late Deliveries", FontSize = 11, Opacity = 0.7 };
            var lateValue = new TextBlock
            {
                Text = lateCount.ToString(),
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 17, 35))
            };
            lateStack.Children.Add(lateLabel);
            lateStack.Children.Add(lateValue);
            Grid.SetColumn(lateStack, 1);
            Grid.SetRow(lateStack, 1);
            statsGrid.Children.Add(lateStack);

            stack.Children.Add(statsGrid);
        }
        else
        {
            var emptyBlock = new TextBlock
            {
                Text = "No delivery data available.",
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };
            stack.Children.Add(emptyBlock);
        }

        border.Child = stack;
        return border;
    }

    private Border CreateOrderStatisticsCard(List<SharedVendorOrderDto> orders)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 245)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };

        var stack = new StackPanel { Spacing = 12 };

        var titleBlock = new TextBlock
        {
            Text = "📊 Order Statistics",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        stack.Children.Add(titleBlock);

        var statsGrid = new Grid();
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Status breakdown
        var statusGroups = orders.GroupBy(o => o.Status).ToList();

        int row = 0, col = 0;
        foreach (var statusGroup in statusGroups.Take(6))
        {
            var statusStack = new StackPanel
            {
                Margin = new Thickness(col == 0 ? 0 : 8, row == 0 ? 0 : 8, col == 1 ? 0 : 8, 0)
            };
            var statusLabel = new TextBlock
            {
                Text = statusGroup.Key,
                FontSize = 11,
                Opacity = 0.7
            };
            var statusValue = new TextBlock
            {
                Text = statusGroup.Count().ToString(),
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(GetStatusColor(statusGroup.Key))
            };
            statusStack.Children.Add(statusLabel);
            statusStack.Children.Add(statusValue);
            Grid.SetColumn(statusStack, col);
            Grid.SetRow(statusStack, row);
            statsGrid.Children.Add(statusStack);

            col++;
            if (col > 1)
            {
                col = 0;
                row++;
            }
        }

        stack.Children.Add(statsGrid);

        // Total value
        var totalValueBlock = new TextBlock
        {
            Text = $"Total Order Value: {orders.Sum(o => o.TotalValue):C}",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 0)
        };
        stack.Children.Add(totalValueBlock);

        border.Child = stack;
        return border;
    }

    private Border CreateMonthlyComparisonCard(List<SharedVendorOrderDto> orders)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 245)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };

        var stack = new StackPanel { Spacing = 12 };

        var titleBlock = new TextBlock
        {
            Text = "📅 This Month vs Last Month",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        stack.Children.Add(titleBlock);

        var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);
        var endOfLastMonth = startOfThisMonth.AddDays(-1);

        var thisMonthOrders = orders.Where(o => o.OrderDate >= startOfThisMonth).ToList();
        var lastMonthOrders = orders.Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate <= endOfLastMonth).ToList();

        var comparisonGrid = new Grid();
        comparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        comparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // This Month
        var thisMonthStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        var thisMonthLabel = new TextBlock
        {
            Text = "This Month",
            FontSize = 12,
            Opacity = 0.7
        };
        var thisMonthOrdersCount = new TextBlock
        {
            Text = thisMonthOrders.Count.ToString(),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        var thisMonthValue = new TextBlock
        {
            Text = thisMonthOrders.Sum(o => o.TotalValue).ToString("C"),
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215))
        };
        thisMonthStack.Children.Add(thisMonthLabel);
        thisMonthStack.Children.Add(thisMonthOrdersCount);
        thisMonthStack.Children.Add(thisMonthValue);
        Grid.SetColumn(thisMonthStack, 0);
        comparisonGrid.Children.Add(thisMonthStack);

        // Last Month
        var lastMonthStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        var lastMonthLabel = new TextBlock
        {
            Text = "Last Month",
            FontSize = 12,
            Opacity = 0.7
        };
        var lastMonthOrdersCount = new TextBlock
        {
            Text = lastMonthOrders.Count.ToString(),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        var lastMonthValue = new TextBlock
        {
            Text = lastMonthOrders.Sum(o => o.TotalValue).ToString("C"),
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        lastMonthStack.Children.Add(lastMonthLabel);
        lastMonthStack.Children.Add(lastMonthOrdersCount);
        lastMonthStack.Children.Add(lastMonthValue);
        Grid.SetColumn(lastMonthStack, 1);
        comparisonGrid.Children.Add(lastMonthStack);

        stack.Children.Add(comparisonGrid);

        // Change indicator
        if (lastMonthOrders.Any())
        {
            var thisMonthTotal = thisMonthOrders.Sum(o => o.TotalValue);
            var lastMonthTotal = lastMonthOrders.Sum(o => o.TotalValue);
            var change = ((thisMonthTotal - lastMonthTotal) / lastMonthTotal) * 100;

            var changeBlock = new TextBlock
            {
                Text = $"Change: {(change >= 0 ? "+" : "")}{change:F1}%",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(change >= 0 
                    ? Windows.UI.Color.FromArgb(255, 16, 124, 16)
                    : Windows.UI.Color.FromArgb(255, 232, 17, 35)),
                Margin = new Thickness(0, 8, 0, 0)
            };
            stack.Children.Add(changeBlock);
        }

        border.Child = stack;
        return border;
    }

    private void ItemsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyItemsFilterAndSort();
    }

    private void ItemsSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyItemsFilterAndSort();
    }

    private void ApplyItemsFilterAndSort()
    {
        ItemsContent.Children.Clear();

        if (!_allItems.Any())
        {
            ItemsEmptyText.Visibility = Visibility.Visible;
            ItemsCountText.Text = "0 items";
            return;
        }

        var filtered = _allItems.AsEnumerable();

        // Apply search filter
        var searchText = ItemsSearchBox?.Text?.ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(i => 
                i.Name.ToLowerInvariant().Contains(searchText) ||
                (i.Sku?.ToLowerInvariant().Contains(searchText) ?? false));
        }

        // Apply sorting
        var sortOption = (ItemsSortComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "NameAsc";
        filtered = sortOption switch
        {
            "NameDesc" => filtered.OrderByDescending(i => i.Name),
            "PriceAsc" => filtered.OrderBy(i => i.Price),
            "PriceDesc" => filtered.OrderByDescending(i => i.Price),
            "StockAsc" => filtered.OrderBy(i => i.Stock),
            "StockDesc" => filtered.OrderByDescending(i => i.Stock),
            _ => filtered.OrderBy(i => i.Name) // Default: NameAsc
        };

        var itemsList = filtered.ToList();

        // Update count
        ItemsCountText.Text = $"{itemsList.Count} item{(itemsList.Count != 1 ? "s" : "")}";

        if (!itemsList.Any())
        {
            ItemsEmptyText.Text = "No items match your search criteria.";
            ItemsEmptyText.Visibility = Visibility.Visible;
            return;
        }

        ItemsEmptyText.Visibility = Visibility.Collapsed;

        foreach (var item in itemsList)
        {
            var itemCard = CreateItemCard(item);
            ItemsContent.Children.Add(itemCard);
        }
    }

    private void OrdersSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyOrdersFilterAndSort();
    }

    private void OrdersStatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyOrdersFilterAndSort();
    }

    private void OrdersSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyOrdersFilterAndSort();
    }

    private void ApplyOrdersFilterAndSort()
    {
        OrdersContent.Children.Clear();

        if (!_allOrders.Any())
        {
            OrdersEmptyText.Visibility = Visibility.Visible;
            OrdersCountText.Text = "0 orders";
            return;
        }

        var filtered = _allOrders.AsEnumerable();

        // Apply search filter
        var searchText = OrdersSearchBox?.Text?.ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(o => 
                o.OrderId.ToLowerInvariant().Contains(searchText) ||
                o.Status.ToLowerInvariant().Contains(searchText));
        }

        // Apply status filter
        var statusFilter = (OrdersStatusFilterComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            filtered = filtered.Where(o => o.Status == statusFilter);
        }

        // Apply sorting
        var sortOption = (OrdersSortComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "DateDesc";
        filtered = sortOption switch
        {
            "DateAsc" => filtered.OrderBy(o => o.OrderDate),
            "ValueDesc" => filtered.OrderByDescending(o => o.TotalValue),
            "ValueAsc" => filtered.OrderBy(o => o.TotalValue),
            "Status" => filtered.OrderBy(o => o.Status),
            _ => filtered.OrderByDescending(o => o.OrderDate) // Default: DateDesc
        };

        var ordersList = filtered.ToList();

        // Update count
        OrdersCountText.Text = $"{ordersList.Count} order{(ordersList.Count != 1 ? "s" : "")}";

        if (!ordersList.Any())
        {
            OrdersEmptyText.Text = "No orders match your search criteria.";
            OrdersEmptyText.Visibility = Visibility.Visible;
            return;
        }

        OrdersEmptyText.Visibility = Visibility.Collapsed;

        foreach (var order in ordersList)
        {
            var orderCard = CreateOrderCard(order);
            OrdersContent.Children.Add(orderCard);
        }
    }

    private async void ShowError(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            PrimaryButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}

