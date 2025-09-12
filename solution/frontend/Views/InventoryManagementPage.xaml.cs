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
                    this.InitializeComponent();
                    
                    // CRITICAL FIX: Ensure Api is initialized before creating ViewModel
                    if (App.Api == null)
                    {
                        throw new InvalidOperationException("Api not initialized. Ensure App.InitializeApiAsync() has completed successfully.");
                    }
                    
                    // TODO: Implement proper dependency injection for services
                    // For now, create mock services
                    var inventoryService = new InventoryService(new HttpClient(), new SimpleLogger<InventoryService>());
                    var vendorService = new VendorService(new HttpClient(), new SimpleLogger<VendorService>());
                    var restockService = new RestockService(new HttpClient(), new SimpleLogger<RestockService>());
                    var vendorOrderService = new VendorOrderService(new HttpClient(), new SimpleLogger<VendorOrderService>());
                    var auditService = new AuditService(new HttpClient(), new SimpleLogger<AuditService>());
                    
                    _vm = new InventoryDashboardViewModel(inventoryService, vendorService, restockService, vendorOrderService, auditService);
                    
                    this.DataContext = _vm;
                    Loaded += InventoryManagementPage_Loaded;
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
        // TODO: Navigate to Vendors page
        ShowComingSoonDialog("Vendor Management", "Vendor profiles and relationship management will be implemented here.");
    }

    private void NavigateToRestock_Click(object sender, TappedRoutedEventArgs e)
    {
        // TODO: Navigate to Restock page
        ShowComingSoonDialog("Restock Management", "Restock requests and order management will be implemented here.");
    }

    private void NavigateToVendorOrders_Click(object sender, TappedRoutedEventArgs e)
    {
        // TODO: Navigate to Vendor Orders page
        ShowComingSoonDialog("Vendor Orders", "Purchase order tracking and management will be implemented here.");
    }

    private void NavigateToAudit_Click(object sender, TappedRoutedEventArgs e)
    {
        // TODO: Navigate to Audit & Reports page
        ShowComingSoonDialog("Audit & Reports", "Transaction logs and reporting functionality will be implemented here.");
    }

    private void NavigateToSettings_Click(object sender, TappedRoutedEventArgs e)
    {
        // TODO: Navigate to Settings page
        ShowComingSoonDialog("Inventory Settings", "Configuration and preferences will be implemented here.");
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
