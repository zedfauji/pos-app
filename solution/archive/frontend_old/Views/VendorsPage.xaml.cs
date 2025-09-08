using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorsPage : Page, IToolbarConsumer
{
    private readonly VendorsViewModel _vm;

    public VendorsPage()
    {
        this.InitializeComponent();
        _vm = new VendorsViewModel(App.Api!);
        DataContext = _vm;
        Loaded += VendorsPage_Loaded;
    }

    private async void VendorsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
        VendorsList.ItemsSource = _vm.Vendors;
    }

    public async void OnAdd()
    {
        var dto = new VendorDto();
        var dlg = new VendorDialog(dto)
        {
            XamlRoot = this.XamlRoot
        };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var created = await _vm.CreateAsync(dto);
            if (created != null) await _vm.LoadAsync();
        }
    }

    public async void OnEdit()
    {
        if (VendorsList.SelectedItem is VendorDto v)
        {
            var dto = new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                ContactInfo = v.ContactInfo,
                Status = v.Status
            };
            var dlg = new VendorDialog(dto)
            {
                XamlRoot = this.XamlRoot
            };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var updated = await _vm.UpdateAsync(dto);
                if (updated != null) await _vm.LoadAsync();
            }
        }
    }

    public async void OnDelete()
    {
        if (VendorsList.SelectedItem is VendorDto v)
        {
            var ok = await _vm.DeleteAsync(v);
            if (ok) await _vm.LoadAsync();
        }
    }

    public async void OnRefresh() => await _vm.LoadAsync();

    private async void VendorsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is VendorDto v)
        {
            var dto = new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                ContactInfo = v.ContactInfo,
                Status = v.Status
            };
            var dlg = new VendorDialog(dto)
            {
                XamlRoot = this.XamlRoot
            };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var updated = await _vm.UpdateAsync(dto);
                if (updated != null) await _vm.LoadAsync();
            }
        }
    }

    private void Vendor_Edit_Click(object sender, RoutedEventArgs e) => OnEdit();

    private async void Vendor_Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is VendorDto v)
        {
            var ok = await _vm.DeleteAsync(v);
            if (ok) await _vm.LoadAsync();
        }
    }

    private void Vendor_ViewItems_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to Items page filtered by vendor
        if ((sender as FrameworkElement)?.DataContext is VendorDto v)
        {
            Frame.Navigate(typeof(ItemsPage), v);
        }
    }
}
