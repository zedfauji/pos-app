using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorOrdersPage : Page, IToolbarConsumer
{
    private readonly VendorOrdersViewModel _vm;

    public VendorOrdersPage()
    {
        this.InitializeComponent();
        _vm = new VendorOrdersViewModel();
        this.DataContext = _vm;
        Loaded += OrdersPage_Loaded;
        App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        ApplyLanguage();
    }

    // No navigation parameter required for Vendor Orders.

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
        ShowInfo("Job history not available in this view.");
    }

    private async void Notifications_Click(object sender, RoutedEventArgs e)
    {
        ShowInfo("Notifications not available in this view.");
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        ShowInfo("Start New Order: integrate with order builder flow.");
    }

    private async void OpenDraft_Click(object sender, RoutedEventArgs e)
    {
        // Draft picker: list recent drafts and allow selection
        ShowInfo("Open Draft: integrate with drafts flow.");
    }

    private async void SaveDraft_Click(object sender, RoutedEventArgs e)
    {
        // Use the builder to collect a draft
        ShowInfo("Save Draft: integrate with builder flow.");

    }

    // Order builder flow removed from Vendor Orders.

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
