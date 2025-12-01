using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Documents;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorsManagementPage : Page, IToolbarConsumer
{
    private readonly VendorsManagementViewModel _vm;

    public VendorsManagementPage()
    {
        try
        {
            this.InitializeComponent();

            // CRITICAL FIX: Ensure Api is initialized before creating services
            if (App.Api == null)
            {
                ShowErrorDialog("API Not Initialized", "The API service is not available. Please restart the application or contact support.");
                return;
            }

            // Create services with proper HTTP clients and loggers
            var inventoryService = new InventoryService(new HttpClient(), new SimpleLogger<InventoryService>());
            
            // Get InventoryApi base URL from appsettings
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            
            var inventoryBase = config["InventoryApi:BaseUrl"] ?? "https://localhost:5001";
            var vendorOrdersBase = config["VendorOrdersApi:BaseUrl"] ?? inventoryBase;
            
            var innerVendor = new HttpClientHandler();
            innerVendor.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var logVendor = new Services.HttpLoggingHandler(innerVendor);
            var vendorHttpClient = new HttpClient(logVendor) { BaseAddress = new Uri(inventoryBase.TrimEnd('/') + "/") };
            var vendorService = new VendorService(vendorHttpClient, new SimpleLogger<VendorService>());

            // Create VendorOrderService
            var vendorOrdersHttpClient = new HttpClient(logVendor) { BaseAddress = new Uri(vendorOrdersBase.TrimEnd('/') + "/") };
            var vendorOrderService = new VendorOrderService(vendorOrdersHttpClient, new SimpleLogger<VendorOrderService>());

            _vm = new VendorsManagementViewModel(vendorService, inventoryService, vendorOrderService);
            this.DataContext = _vm;

            // Subscribe to property changes to update TotalBudgetText
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_vm.TotalBudget))
                {
                    TotalBudgetText.Text = _vm.TotalBudget.ToString("C");
                }
            };

            Loaded += VendorsManagementPage_Loaded;
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Initialization Error", $"Failed to initialize Vendors Management page: {ex.Message}");
        }
    }

    private async void VendorsManagementPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.LoadVendorsAsync();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to load vendors: {ex.Message}");
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.LoadVendorsAsync();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to refresh vendors: {ex.Message}");
        }
    }

    private async void AddVendor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new VendorCrudDialog();
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var dto = dialog.Item.ToDto();
                await _vm.CreateVendorAsync(dto);
                await _vm.LoadVendorsAsync(); // Reload to get the new vendor with ID
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to create vendor: {ex.Message}");
        }
    }

    private void Vendor_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is VendorDisplay vendor)
        {
            _vm.SelectedVendor = vendor;
        }
    }

    private async void Vendor_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is VendorDisplay vendor)
        {
            _vm.SelectedVendor = vendor;
            await EditVendorAsync(vendor);
        }
    }

    private async void EditVendor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is VendorDisplay vendor)
        {
            await EditVendorAsync(vendor);
        }
        else if (_vm.SelectedVendor != null)
        {
            await EditVendorAsync(_vm.SelectedVendor);
        }
        else
        {
            ShowErrorDialog("No Selection", "Please select a vendor to edit.");
        }
    }

    private async Task EditVendorAsync(VendorDisplay vendor)
    {
        try
        {
            var dialog = new VendorCrudDialog(vendor);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var dto = dialog.Item.ToDto();
                await _vm.UpdateVendorAsync(vendor.Id, dto);
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to update vendor: {ex.Message}");
        }
    }

    private async void DeleteVendor_Click(object sender, RoutedEventArgs e)
    {
        VendorDisplay? vendor = null;

        if (sender is MenuFlyoutItem item && item.Tag is VendorDisplay v)
        {
            vendor = v;
        }
        else if (_vm.SelectedVendor != null)
        {
            vendor = _vm.SelectedVendor;
        }

        if (vendor == null)
        {
            ShowErrorDialog("No Selection", "Please select a vendor to delete.");
            return;
        }

        var confirmDialog = new ContentDialog
        {
            Title = "Delete Vendor",
            Content = $"Are you sure you want to delete '{vendor.Name}'? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirmDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var success = await _vm.DeleteVendorAsync(vendor.Id);
                if (!success)
                {
                    ShowErrorDialog("Error", "Failed to delete vendor. It may have already been deleted.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to delete vendor: {ex.Message}");
            }
        }
    }

    private async void ViewItems_Click(object sender, RoutedEventArgs e)
    {
        VendorDisplay? vendor = null;

        if (sender is MenuFlyoutItem item && item.Tag is VendorDisplay v)
        {
            vendor = v;
        }
        else if (_vm.SelectedVendor != null)
        {
            vendor = _vm.SelectedVendor;
        }

        if (vendor == null)
        {
            ShowErrorDialog("No Selection", "Please select a vendor to view items.");
            return;
        }

        try
        {
            var items = await _vm.GetVendorItemsAsync(vendor.Id);
            var itemsList = items.ToList();
            
            UIElement content;
            
            if (itemsList.Any())
            {
                // Create a StackPanel with item cards
                var stackPanel = new StackPanel { Spacing = 8 };
                
                foreach (var itemDto in itemsList)
                {
                    var card = CreateItemCard(itemDto);
                    stackPanel.Children.Add(card);
                }
                
                content = new ScrollViewer
                {
                    Content = stackPanel,
                    MaxHeight = 500,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
            }
            else
            {
                // Empty state
                var emptyStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 12,
                    Padding = new Thickness(40, 60, 40, 60)
                };
                emptyStack.Children.Add(new SymbolIcon
                {
                    Symbol = Symbol.Document,
                    Width = 48,
                    Height = 48,
                    Opacity = 0.3
                });
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "No Items Found",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "This vendor doesn't have any items yet.",
                    FontSize = 14,
                    Opacity = 0.7,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                });
                
                content = emptyStack;
            }

            var itemsDialog = new ContentDialog
            {
                Title = $"Items from {vendor.Name} ({itemsList.Count})",
                Content = content,
                PrimaryButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await itemsDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to load vendor items: {ex.Message}");
        }
    }

    private Border CreateItemCard(ItemDto item)
    {
        var border = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 4),
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left side - Item details
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

        // Right side - Stock badge
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

    private void VendorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is VendorDisplay vendor)
        {
            _vm.SelectedVendor = vendor;
        }
    }

    private void SortDirectionButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.ToggleSort(_vm.SortBy);
    }

    private void PageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagValue)
        {
            if (int.TryParse(tagValue, out int pageSize))
            {
                _vm.PageSize = pageSize;
            }
        }
    }

    private void FirstPage_Click(object sender, RoutedEventArgs e)
    {
        _vm.GoToPage(1);
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        _vm.PreviousPage();
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        _vm.NextPage();
    }

    private void LastPage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.TotalPages > 0)
        {
            _vm.GoToPage(_vm.TotalPages);
        }
    }

    private async void ShowErrorDialog(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
        }
    }

    // IToolbarConsumer implementation
    public void OnAdd()
    {
        AddVendor_Click(this, new RoutedEventArgs());
    }

    public void OnEdit()
    {
        if (_vm.SelectedVendor != null)
        {
            EditVendorAsync(_vm.SelectedVendor);
        }
        else
        {
            ShowErrorDialog("No Selection", "Please select a vendor to edit.");
        }
    }

    public void OnDelete()
    {
        if (_vm.SelectedVendor != null)
        {
            DeleteVendor_Click(this, new RoutedEventArgs());
        }
        else
        {
            ShowErrorDialog("No Selection", "Please select a vendor to delete.");
        }
    }

    public void OnRefresh()
    {
        Refresh_Click(this, new RoutedEventArgs());
    }
}

