using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.Views;

public sealed partial class ItemsPage : Page, IToolbarConsumer
{
    private readonly ItemsViewModel _vm;

    public ItemsPage()
    {
        this.InitializeComponent();
        _vm = new ItemsViewModel(App.Api!);
        DataContext = _vm;
        Loaded += ItemsPage_Loaded;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is VendorDto v)
        {
            _vm.SelectedVendor = v;
        }
    }

    private async void ItemsPage_Loaded(object sender, RoutedEventArgs e)
    {
        VendorNameText.Text = _vm.SelectedVendor is null ? string.Empty : $"for Vendor: {_vm.SelectedVendor.Name}";
        if (_vm.SelectedVendor != null)
        {
            await _vm.LoadAsync();
            ItemsList.ItemsSource = _vm.Items;
        }
    }

    public async void OnAdd()
    {
        var dto = new ItemDto();
        var dlg = new ItemDialog(dto)
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
        if (ItemsList.SelectedItem is ItemDto i)
        {
            var dto = new ItemDto
            {
                Id = i.Id,
                Sku = i.Sku,
                Name = i.Name,
                Price = i.Price,
                Stock = i.Stock
            };
            var dlg = new ItemDialog(dto)
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
        if (ItemsList.SelectedItem is ItemDto i)
        {
            var ok = await _vm.DeleteAsync(i);
            if (ok) await _vm.LoadAsync();
        }
    }

    public async void OnRefresh() => await _vm.LoadAsync();

    private async void ItemsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ItemDto i)
        {
            var dto = new ItemDto
            {
                Id = i.Id,
                Sku = i.Sku,
                Name = i.Name,
                Price = i.Price,
                Stock = i.Stock
            };
            var dlg = new ItemDialog(dto)
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

    private void Item_Edit_Click(object sender, RoutedEventArgs e) => OnEdit();

    private async void Item_Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ItemDto i)
        {
            var ok = await _vm.DeleteAsync(i);
            if (ok) await _vm.LoadAsync();
        }
    }
}
