using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorsPage : Page, IToolbarConsumer
{
    private readonly VendorsViewModel _vm;

    public VendorsPage()
    {
        this.InitializeComponent();
        
        if (App.Api == null)
        {
            throw new InvalidOperationException("Api not initialized. Ensure App.InitializeApiAsync() has completed successfully.");
        }
        
        // Create services
        var vendorService = new VendorService(new HttpClient(), new SimpleLogger<VendorService>());
        var vendorOrderService = new VendorOrderService(new HttpClient(), new SimpleLogger<VendorOrderService>());
        
        _vm = new VendorsViewModel(vendorService, vendorOrderService);
        this.DataContext = _vm;
        
        Loaded += VendorsPage_Loaded;
    }

    private async void VendorsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private async void AddVendor_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VendorDialog();
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await _vm.AddVendorAsync(dialog.Vendor);
        }
    }

    private async void EditVendor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string vendorId)
        {
            var vendor = await _vm.GetVendorAsync(vendorId);
            if (vendor != null)
            {
                var dialog = new VendorDialog(vendor);
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    await _vm.UpdateVendorAsync(vendorId, dialog.Vendor);
                }
            }
        }
    }

    private async void DeleteVendor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string vendorId)
        {
            var vendor = await _vm.GetVendorAsync(vendorId);
            if (vendor != null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Delete Vendor",
                    Content = $"Are you sure you want to delete vendor '{vendor.Name}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close
                };
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await _vm.DeleteVendorAsync(vendorId);
                }
            }
        }
    }

    private async void ViewOrders_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string vendorId)
        {
            // TODO: Navigate to vendor orders page
            var dialog = new ContentDialog
            {
                Title = "Vendor Orders",
                Content = $"Orders for vendor {vendorId} will be shown here.",
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _vm.Search(textBox.Text);
        }
    }

    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            _vm.FilterByStatus(item.Tag?.ToString() ?? string.Empty);
        }
    }

    private void VendorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection if needed
    }

    // IToolbarConsumer implementation
    public void OnAdd()
    {
        AddVendor_Click(this, new RoutedEventArgs());
    }

    public void OnEdit()
    {
        if (VendorsList.SelectedItem is ExtendedVendorDto vendor)
        {
            EditVendor_Click(this, new RoutedEventArgs());
        }
    }

    public void OnDelete()
    {
        if (VendorsList.SelectedItem is ExtendedVendorDto vendor)
        {
            DeleteVendor_Click(this, new RoutedEventArgs());
        }
    }

    public void OnRefresh()
    {
        Refresh_Click(this, new RoutedEventArgs());
    }
}