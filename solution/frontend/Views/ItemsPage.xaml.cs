using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Dialogs;
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
        App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        ApplyLanguage();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is VendorDto v)
        {
            _vm.SelectedVendor = v;
        }
    }

    private async void ItemsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyLanguage();
            await CheckConnectivityAsync();
            VendorNameText.Text = _vm.SelectedVendor is null ? string.Empty : $"for Vendor: {_vm.SelectedVendor.Name}";
            if (_vm.SelectedVendor != null)
            {
                await _vm.LoadAsync();
                ShowInfo("Loaded items.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load items", ex);
            ShowError($"Failed to load items: {ex.Message}");
        }
    }

    public async void OnAdd()
    {
        var dto = new ItemDto();
        var dlg = new ItemDialog(dto) { XamlRoot = this.XamlRoot };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var created = await _vm.CreateAsync(dto);
                if (created != null)
                {
                    await _vm.LoadAsync();
                    ShowInfo("Item created.");
                }
                else ShowError("Failed to create item.");
            }
            catch (Exception ex)
            {
                Log.Error("Create item failed", ex);
                ShowError($"Create failed: {ex.Message}");
            }
        }
    }

    public async void OnEdit()
    {
        if (ItemsGrid.SelectedItem is ItemDto i)
        {
            var dto = new ItemDto { Id = i.Id, Sku = i.Sku, Name = i.Name, Price = i.Price, Stock = i.Stock };
            var dlg = new ItemDialog(dto) { XamlRoot = this.XamlRoot };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var updated = await _vm.UpdateAsync(dto);
                    if (updated != null)
                    {
                        await _vm.LoadAsync();
                        ShowInfo("Item updated.");
                    }
                    else ShowError("Failed to update item.");
                }
                catch (Exception ex)
                {
                    Log.Error("Update item failed", ex);
                    ShowError($"Update failed: {ex.Message}");
                }
            }
        }
    }

    public async void OnDelete()
    {
        if (ItemsGrid.SelectedItem is ItemDto i)
        {
            try
            {
                var ok = await _vm.DeleteAsync(i);
                if (ok)
                {
                    await _vm.LoadAsync();
                    ShowInfo("Item deleted.");
                }
                else ShowError("Failed to delete item.");
            }
            catch (Exception ex)
            {
                Log.Error("Delete item failed", ex);
                ShowError($"Delete failed: {ex.Message}");
            }
        }
    }

    public async void OnRefresh() { await CheckConnectivityAsync(); await _vm.LoadAsync(); ShowInfo("Refreshed."); }

    private void Item_Edit_Click(object sender, RoutedEventArgs e) => OnEdit();

    private async void Item_Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ItemDto i)
        {
            var ok = await _vm.DeleteAsync(i);
            if (ok)
            {
                await _vm.LoadAsync();
                ShowInfo("Item deleted.");
            }
            else ShowError("Failed to delete item.");
        }
    }

    private void ShowInfo(string message)
    {
        FeedbackBar.Severity = InfoBarSeverity.Success;
        FeedbackBar.Message = message;
        FeedbackBar.IsOpen = true;
    }

    private void ShowError(string message)
    {
        FeedbackBar.Severity = InfoBarSeverity.Error;
        FeedbackBar.Message = message;
        FeedbackBar.IsOpen = true;
    }

    private void ApplyLanguage()
    {
        try
        {
            Title.Text = App.I18n.T("items_title");
            if (CtxEdit != null) CtxEdit.Text = App.I18n.T("edit");
            if (CtxDelete != null) CtxDelete.Text = App.I18n.T("delete");
        }
        catch { }
    }

    private async Task CheckConnectivityAsync()
    {
        try
        {
            var ok = await App.UsersApi!.PingAsync();
            if (!ok)
            {
                FeedbackBar.Severity = InfoBarSeverity.Warning;
                FeedbackBar.Message = "Backend is unavailable. Data may be limited.";
                FeedbackBar.IsOpen = true;
            }
        }
        catch
        {
            FeedbackBar.Severity = InfoBarSeverity.Warning;
            FeedbackBar.Message = "Backend is unavailable. Data may be limited.";
            FeedbackBar.IsOpen = true;
        }
    }
}
