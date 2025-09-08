using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorOrdersPage : Page, IToolbarConsumer
{
    private readonly OrdersViewModel _vm;

    public VendorOrdersPage()
    {
        this.InitializeComponent();
        _vm = new OrdersViewModel(App.Api!);
        this.DataContext = _vm;
        Loaded += OrdersPage_Loaded;
        App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        ApplyLanguage();
    }

    private async void OrdersPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLanguage();
        await CheckConnectivityAsync();
        await _vm.LoadAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await CheckConnectivityAsync();
        await _vm.LoadAsync();
    }

    private async void Jobs_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshJobsAsync();
        var grid = new CommunityToolkit.WinUI.UI.Controls.DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            ItemsSource = _vm.Jobs
        };
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "JobId", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("JobId") } });
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Vendor", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("VendorName") } });
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Status", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("Status") } });
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Updated", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("UpdatedAt") } });

        var dlg = new ContentDialog
        {
            Title = "Order Jobs",
            Content = new Grid { Children = { grid } },
            PrimaryButtonText = "Close",
            XamlRoot = this.XamlRoot
        };
        await dlg.ShowAsync();
    }

    private async void Notifications_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshNotificationsAsync();
        var grid = new CommunityToolkit.WinUI.UI.Controls.DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            ItemsSource = _vm.Notifications
        };
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Event", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("EventType") } });
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Vendor", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("VendorName") } });
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Order", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("OrderId") } });
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Time", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("Timestamp") } });
        grid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn { Header = "Message", Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("StatusMessage") } });

        var dlg = new ContentDialog
        {
            Title = "Notifications",
            Content = new Grid { Children = { grid } },
            PrimaryButtonText = "Close",
            XamlRoot = this.XamlRoot
        };
        await dlg.ShowAsync();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        var order = await OpenOrderBuilderPageAsync(null);
        if (order != null)
        {
            var orderId = await _vm.FinalizeOrderAsync(order);
            if (!string.IsNullOrEmpty(orderId)) ShowInfo($"Order submitted: {orderId}"); else ShowError("Order submission failed");
        }
    }

    private async void OpenDraft_Click(object sender, RoutedEventArgs e)
    {
        // Draft picker: list recent drafts and allow selection
        var list = await App.Api!.GetDraftsAsync(20);
        if (list.Count == 0) { ShowError("No drafts available"); return; }
        var lv = new ListView { ItemsSource = list, SelectionMode = ListViewSelectionMode.Single };
        lv.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
            <StackPanel Padding='6' Spacing='2'>
                <TextBlock Text='{Binding CartId}' FontWeight='SemiBold'/>
                <TextBlock Text='{Binding VendorName}'/>
                <TextBlock Text='{Binding CreatedAt}'/>
                <StackPanel Orientation='Horizontal' Spacing='12'>
                    <TextBlock Text='{Binding Items.Count, StringFormat=Items: {0}}'/>
                    <TextBlock Text='{Binding TotalAmount, StringFormat=Total: {0}}'/>
                </StackPanel>
            </StackPanel>
        </DataTemplate>");
        var dlgPick = new ContentDialog
        {
            Title = "Open Draft",
            Content = lv,
            PrimaryButtonText = "Open",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };
        var res = await dlgPick.ShowAsync();
        if (res != ContentDialogResult.Primary) return;
        var draft = lv.SelectedItem as MagiDesk.Shared.DTOs.CartDraftDto;
        if (draft == null) { ShowError("Please select a draft"); return; }

        var order = await OpenOrderBuilderPageAsync(draft);
        if (order != null)
        {
            var orderId = await _vm.FinalizeOrderAsync(order);
            if (!string.IsNullOrEmpty(orderId)) ShowInfo($"Order submitted: {orderId}"); else ShowError("Order submission failed");
        }
    }

    private async void SaveDraft_Click(object sender, RoutedEventArgs e)
    {
        // Use the builder to collect a draft
        var order = await OpenOrderBuilderPageAsync(null);
        if (order != null)
        {
            var draft = new MagiDesk.Shared.DTOs.CartDraftDto
            {
                VendorId = order.VendorId,
                VendorName = order.VendorName,
                Items = order.Items,
                TotalAmount = order.TotalAmount,
                Status = "draft",
                CreatedAt = DateTime.UtcNow
            };
            var saved = await _vm.SaveDraftAsync(draft);
            if (saved != null) ShowInfo($"Draft saved: {saved.CartId}"); else ShowError("Save draft failed");
        }

    }

    private static async Task<MagiDesk.Shared.DTOs.OrderDto?> OpenOrderBuilderPageAsync(MagiDesk.Shared.DTOs.CartDraftDto? draft)
    {
        var page = new MagiDesk.Frontend.Views.OrderBuilderPage();
        if (draft != null) page.LoadFromDraft(draft);
        var win = new Microsoft.UI.Xaml.Window
        {
            Title = "Build Order",
            Content = page
        };
        win.Activate();
        try
        {
            return await page.Completion;
        }
        finally
        {
            win.Close();
        }
    }

    public async void OnAdd() => await _vm.LoadAsync();
    public async void OnEdit() => await _vm.LoadAsync();
    public async void OnDelete() => await _vm.LoadAsync();
    public async void OnRefresh() => await _vm.LoadAsync();

    private void ShowInfo(string msg)
    {
        if (this.FindName("Info") is InfoBar bar)
        {
            bar.Message = msg;
            bar.Severity = InfoBarSeverity.Success;
            bar.IsOpen = true;
        }
    }

    private void ShowError(string msg)
    {
        if (this.FindName("Info") is InfoBar bar)
        {
            bar.Message = msg;
            bar.Severity = InfoBarSeverity.Error;
            bar.IsOpen = true;
        }
    }

    private void ApplyLanguage()
    {
        try
        {
            if (this.FindName("Title") is TextBlock t) t.Text = App.I18n.T("orders_title");
            if (this.FindName("BtnStart") is Button b1) b1.Content = App.I18n.T("start_new_order");
            if (this.FindName("BtnSaveDraft") is Button b2) b2.Content = App.I18n.T("save_draft");
            if (this.FindName("BtnOpenDraft") is Button b3) b3.Content = App.I18n.T("open_draft");
            if (this.FindName("BtnRefresh") is Button b4) b4.Content = App.I18n.T("refresh");
            if (this.FindName("BtnJobs") is Button b5) b5.Content = App.I18n.T("job_history");
            if (this.FindName("BtnNotifications") is Button b6) b6.Content = App.I18n.T("notifications");
        }
        catch { }
    }

    private async Task CheckConnectivityAsync()
    {
        try
        {
            var ok = await App.UsersApi!.PingAsync();
            if (!ok && this.FindName("Info") is InfoBar bar)
            {
                bar.Message = "Backend is unavailable. Data may be limited.";
                bar.Severity = InfoBarSeverity.Warning;
                bar.IsOpen = true;
            }
        }
        catch
        {
            if (this.FindName("Info") is InfoBar bar)
            {
                bar.Message = "Backend is unavailable. Data may be limited.";
                bar.Severity = InfoBarSeverity.Warning;
                bar.IsOpen = true;
            }
        }
    }
}
