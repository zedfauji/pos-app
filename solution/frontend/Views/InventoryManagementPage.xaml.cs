using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class InventoryManagementPage : Page, IToolbarConsumer
{
    private readonly InventoryDashboardViewModel _vm;

    public InventoryManagementPage()
    {
        try
        {
            this.InitializeComponent();
            
            // CRITICAL FIX: Ensure Api is initialized before creating ViewModel
            if (App.Api == null)
            {
                // Show error dialog instead of throwing exception
                ShowErrorDialog("API Not Initialized", "The API service is not available. Please restart the application or contact support.");
                return;
            }
            
            // Create services with proper HTTP clients and loggers
            var inventoryService = new InventoryService(new HttpClient(), new SimpleLogger<InventoryService>());
            var restockService = new RestockService(new HttpClient(), new SimpleLogger<RestockService>());
            var vendorOrderService = new VendorOrderService(new HttpClient(), new SimpleLogger<VendorOrderService>());
            var auditService = new AuditService(new HttpClient(), new SimpleLogger<AuditService>());
            
            _vm = new InventoryDashboardViewModel(inventoryService, restockService, vendorOrderService, auditService);
            
            this.DataContext = _vm;
            Loaded += InventoryManagementPage_Loaded;
        }
        catch (Exception ex)
        {
            // Show error dialog instead of letting the exception crash the app
            ShowErrorDialog("Initialization Error", $"Failed to initialize Inventory Management page: {ex.Message}");
        }
    }

    private async void InventoryManagementPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDashboardDataAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDashboardDataAsync();
    }

    // Navigation methods
    private void NavigateToInventory_Click(object sender, TappedRoutedEventArgs e)
    {
        // Navigate to Inventory CRUD page
        var frame = (Frame)this.Parent;
        frame.Navigate(typeof(InventoryCrudPage));
    }

    private void NavigateToVendors_Click(object sender, TappedRoutedEventArgs e)
    {
        var frame = (Frame)this.Parent;
        // Legacy VendorsPage removed - using new inventory system
    }

    private void NavigateToRestock_Click(object sender, TappedRoutedEventArgs e)
    {
        var frame = (Frame)this.Parent;
        frame.Navigate(typeof(RestockPage));
    }

    private void NavigateToVendorOrders_Click(object sender, TappedRoutedEventArgs e)
    {
        var frame = (Frame)this.Parent;
        frame.Navigate(typeof(VendorOrdersPage));
    }

    private void NavigateToAudit_Click(object sender, TappedRoutedEventArgs e)
    {
        var frame = (Frame)this.Parent;
        frame.Navigate(typeof(AuditReportsPage));
    }

    private void NavigateToSettings_Click(object sender, TappedRoutedEventArgs e)
    {
        var frame = (Frame)this.Parent;
        frame.Navigate(typeof(InventorySettingsPage));
    }

    private async void ShowComingSoonDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message },
            PrimaryButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void ShowErrorDialog(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                PrimaryButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // Fallback: Log error if dialog fails
            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
        }
    }

    // IToolbarConsumer implementation
    public void OnAdd()
    {
        NavigateToRestock_Click(this, new TappedRoutedEventArgs());
    }

    public void OnEdit()
    {
        NavigateToInventory_Click(this, new TappedRoutedEventArgs());
    }

    public void OnDelete()
    {
        // Not applicable for dashboard
    }

        public void OnRefresh()
        {
            Refresh_Click(this, new RoutedEventArgs());
        }
    }
