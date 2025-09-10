using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Dialogs;
using MagiDesk.Shared.DTOs;
using Windows.Storage.Pickers;
using WinRT.Interop;

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
        App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        ApplyLanguage();
    }

    private async void VendorsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyLanguage();
            await CheckConnectivityAsync();
            await _vm.LoadAsync();
            ShowInfo("Loaded vendors.");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load vendors: {ex.Message}");
        }
    }

    public async void OnAdd()
    {
        var dto = new VendorDto();
        var dlg = new VendorDialog(dto) { XamlRoot = this.XamlRoot };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var created = await _vm.CreateAsync(dto);
                if (created != null)
                {
                    await _vm.LoadAsync();
                    ShowInfo("Vendor created.");
                }
                else ShowError("Failed to create vendor.");
            }
            catch (Exception ex)
            {
                ShowError($"Create failed: {ex.Message}");
            }
        }
    }

    public async void OnEdit()
    {
        var v = VendorsGrid.SelectedItem as VendorDto;
        if (v is VendorDto)
        {
            var dto = new VendorDto { Id = v.Id, Name = v.Name, ContactInfo = v.ContactInfo, Status = v.Status };
            var dlg = new VendorDialog(dto) { XamlRoot = this.XamlRoot };
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var updated = await _vm.UpdateAsync(dto);
                    if (updated != null)
                    {
                        await _vm.LoadAsync();
                        ShowInfo("Vendor updated.");
                    }
                    else ShowError("Failed to update vendor.");
                }
                catch (Exception ex)
                {
                    ShowError($"Update failed: {ex.Message}");
                }
            }
        }
    }

    public async void OnDelete()
    {
        if (VendorsGrid.SelectedItem is VendorDto v)
        {
            try
            {
                var confirm = await ConfirmDeleteVendorAsync(v);
                if (!confirm) return;
                var ok = await _vm.DeleteAsync(v);
                if (ok)
                {
                    await _vm.LoadAsync();
                    ShowInfo("Vendor deleted.");
                }
                else ShowError("Failed to delete vendor.");
            }
            catch (Exception ex)
            {
                ShowError($"Delete failed: {ex.Message}");
            }
        }
    }

    public async void OnRefresh()
    {
        await CheckConnectivityAsync();
        await _vm.LoadAsync();
    }

    private async void Vendor_Edit_Click(object sender, RoutedEventArgs e)
    {
        var v = (sender as FrameworkElement)?.Tag as VendorDto
                ?? VendorsGrid.SelectedItem as VendorDto
                ?? (sender as FrameworkElement)?.DataContext as VendorDto;
        if (v is null) { ShowError("No vendor selected."); return; }
        var dto = new VendorDto { Id = v.Id, Name = v.Name, ContactInfo = v.ContactInfo, Status = v.Status };
        var dlg = new VendorDialog(dto) { XamlRoot = this.XamlRoot };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var updated = await _vm.UpdateAsync(dto);
                if (updated != null)
                {
                    await _vm.LoadAsync();
                    ShowInfo("Vendor updated.");
                }
                else ShowError("Failed to update vendor.");
            }
            catch (Exception ex)
            {
                ShowError($"Update failed: {ex.Message}");
            }
        }
    }

    private async void Vendor_Delete_Click(object sender, RoutedEventArgs e)
    {
        var v = (sender as FrameworkElement)?.Tag as VendorDto
                ?? VendorsGrid.SelectedItem as VendorDto
                ?? (sender as FrameworkElement)?.DataContext as VendorDto;
        if (v is VendorDto)
        {
            var confirm = await ConfirmDeleteVendorAsync(v);
            if (!confirm) return;
            var ok = await _vm.DeleteAsync(v);
            if (ok)
            {
                await _vm.LoadAsync();
                ShowInfo("Vendor deleted.");
            }
            else ShowError("Failed to delete vendor.");
        }
    }

    private void Vendor_ViewItems_Click(object sender, RoutedEventArgs e)
    {
        var v = (sender as FrameworkElement)?.Tag as VendorDto
                ?? VendorsGrid.SelectedItem as VendorDto
                ?? (sender as FrameworkElement)?.DataContext as VendorDto;
        if (v is null) { ShowError("No vendor selected."); return; }
        Frame.Navigate(typeof(ItemsPage), v);
    }

    private void VendorsGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        try
        {
            var fe = e.OriginalSource as FrameworkElement;
            var v = fe?.DataContext as VendorDto ?? VendorsGrid.SelectedItem as VendorDto;
            if (v is null) { return; }
            Frame.Navigate(typeof(ItemsPage), v);
        }
        catch (Exception ex)
        {
            Log.Error("DoubleTap navigation failed", ex);
            ShowError($"Open items failed: {ex.Message}");
        }
    }

    private async void Vendor_ExportCsv_Click(object sender, RoutedEventArgs e)
        => await ExportVendorAsync(sender, "csv");

    private async void Vendor_ExportJson_Click(object sender, RoutedEventArgs e)
        => await ExportVendorAsync(sender, "json");

    private async void Vendor_ExportYaml_Click(object sender, RoutedEventArgs e)
        => await ExportVendorAsync(sender, "yaml");

    private async Task ExportVendorAsync(object sender, string format)
    {
        try
        {
            var v = (sender as FrameworkElement)?.Tag as VendorDto
                    ?? VendorsGrid.SelectedItem as VendorDto
                    ?? (sender as FrameworkElement)?.DataContext as VendorDto;
            if (v is null || string.IsNullOrWhiteSpace(v.Id)) { ShowError("No vendor selected."); return; }

            var content = await App.Api!.ExportVendorItemsAsync(v.Id!, format);
            if (content is null) { ShowError("Export failed."); return; }

            // CRITICAL FIX: Ensure FileSavePicker is called from UI thread
            // This is a COM interop call that requires UI thread context
            var picker = new FileSavePicker();
            var windowHandle = WindowNative.GetWindowHandle(App.MainWindow!);
            InitializeWithWindow.Initialize(picker, windowHandle);
            picker.SuggestedFileName = $"{v.Name}-items";
            switch (format)
            {
                case "csv": picker.FileTypeChoices.Add("CSV", new List<string> { ".csv" }); break;
                case "json": picker.FileTypeChoices.Add("JSON", new List<string> { ".json" }); break;
                case "yaml": picker.FileTypeChoices.Add("YAML", new List<string> { ".yml", ".yaml" }); break;
            }
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            
            // CRITICAL FIX: Ensure Windows.Storage.FileIO.WriteTextAsync() is called from UI thread
            // This is a COM interop call that requires UI thread context
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
            {
                var tcs = new TaskCompletionSource<bool>();
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await Windows.Storage.FileIO.WriteTextAsync(file, content);
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                await tcs.Task;
            }
            else
            {
                // Fallback: try direct call (might fail if not on UI thread)
                await Windows.Storage.FileIO.WriteTextAsync(file, content);
            }
            ShowInfo($"Exported {format.ToUpper()} for {v.Name}.");
        }
        catch (Exception ex)
        {
            Log.Error("Export failed", ex);
            ShowError($"Export failed: {ex.Message}");
        }
    }

    private async void Vendor_ImportCsv_Click(object sender, RoutedEventArgs e)
        => await ImportVendorAsync(sender, new[] { ".csv" }, "csv");

    private async void Vendor_ImportJson_Click(object sender, RoutedEventArgs e)
        => await ImportVendorAsync(sender, new[] { ".json" }, "json");

    private async void Vendor_ImportYaml_Click(object sender, RoutedEventArgs e)
        => await ImportVendorAsync(sender, new[] { ".yml", ".yaml" }, "yaml");

    private async Task ImportVendorAsync(object sender, string[] extensions, string format)
    {
        try
        {
            var v = (sender as FrameworkElement)?.Tag as VendorDto
                    ?? VendorsGrid.SelectedItem as VendorDto
                    ?? (sender as FrameworkElement)?.DataContext as VendorDto;
            if (v is null || string.IsNullOrWhiteSpace(v.Id)) { ShowError("No vendor selected."); return; }

            // CRITICAL FIX: Ensure FileOpenPicker is called from UI thread
            // This is a COM interop call that requires UI thread context
            var picker = new FileOpenPicker();
            var windowHandle = WindowNative.GetWindowHandle(App.MainWindow!);
            InitializeWithWindow.Initialize(picker, windowHandle);
            foreach (var ext in extensions) picker.FileTypeFilter.Add(ext);
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            using var stream = await file.OpenStreamForReadAsync();
            var ok = await App.Api!.ImportVendorItemsAsync(v.Id!, format, stream);
            if (ok)
            {
                ShowInfo($"Imported items for {v.Name}.");
            }
            else ShowError("Import failed.");
        }
        catch (Exception ex)
        {
            Log.Error("Import failed", ex);
            ShowError($"Import failed: {ex.Message}");
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

    private void ApplyLanguage()
    {
        try
        {
            Title.Text = App.I18n.T("vendors_title");
        }
        catch { }
    }

    private async Task<bool> ConfirmDeleteVendorAsync(VendorDto v)
    {
        var tb = new TextBox { PlaceholderText = "Type vendor name to confirm", Margin = new Thickness(0,8,0,0) };
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock
        {
            Text = $"Are you sure you want to delete vendor '{v.Name}'? This action cannot be undone.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock { Text = "Confirmation:", Opacity = 0.8 });
        panel.Children.Add(tb);

        var dlg = new ContentDialog
        {
            Title = "Delete Vendor",
            Content = panel,
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            IsPrimaryButtonEnabled = false
        };

        tb.TextChanged += (s, e) =>
        {
            dlg.IsPrimaryButtonEnabled = string.Equals(tb.Text?.Trim(), v.Name?.Trim(), StringComparison.OrdinalIgnoreCase);
        };

        var res = await dlg.ShowAsync();
        return res == ContentDialogResult.Primary;
    }
}
