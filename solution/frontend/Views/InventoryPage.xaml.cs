using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class InventoryPage : Page, IToolbarConsumer
{
    private readonly InventoryViewModel _vm;

    public InventoryPage()
    {
        this.InitializeComponent();
        
        // CRITICAL FIX: Ensure Api is initialized before creating ViewModel
        if (App.Api == null)
        {
            throw new InvalidOperationException("Api not initialized. Ensure App.InitializeApiAsync() has completed successfully.");
        }
        _vm = new InventoryViewModel(App.Api);
        
        this.DataContext = _vm;
        Loaded += InventoryPage_Loaded;
    }

    private async void InventoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
        await _vm.RefreshJobsAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
        await _vm.RefreshJobsAsync();
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        // Confirm if a job is already running
        if (await _vm.HasRunningSyncAsync())
        {
            var confirm = new ContentDialog
            {
                Title = "A sync is already running",
                Content = new TextBlock { Text = "Launch another sync?" },
                PrimaryButtonText = "Launch",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var res = await confirm.ShowAsync();
            if (res != ContentDialogResult.Primary) return;
        }

        var dlg = new ContentDialog
        {
            Title = "Syncing Product Names",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new ProgressRing{ IsActive = true, Width=32, Height=32},
                    new TextBlock{ Text = "Please wait...", Margin = new Thickness(0,8,0,0)}
                }
            },
            PrimaryButtonText = "Hide",
            IsPrimaryButtonEnabled = false,
            XamlRoot = this.XamlRoot
        };
        _ = dlg.ShowAsync();
        await _vm.LaunchSyncAsync(() => { DispatcherQueue.TryEnqueue(() => { /* tick */ }); return Task.CompletedTask; });
        dlg.Hide();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchText = (sender as TextBox)?.Text;
        _vm.ApplyFilter();
    }

    private async void JobHistory_Click(object sender, RoutedEventArgs e)
    {
        await _vm.RefreshJobsAsync();

        // CRITICAL FIX: Replace CommunityToolkit DataGrid with native WinUI 3 ListView
        // CommunityToolkit DataGrid causes COM exceptions in WinUI 3 Desktop Apps
        var listView = new ListView
        {
            ItemsSource = _vm.JobHistory,
            SelectionMode = ListViewSelectionMode.None
        };

        var dlg = new ContentDialog
        {
            Title = "Job History",
            Content = listView,
            PrimaryButtonText = "Close",
            XamlRoot = this.XamlRoot,
            IsSecondaryButtonEnabled = false
        };
        await dlg.ShowAsync();
    }

    public async void OnAdd() => await _vm.LoadAsync();
    public async void OnEdit() => await _vm.LoadAsync();
    public async void OnDelete() => await _vm.LoadAsync();
    public async void OnRefresh() { await _vm.LoadAsync(); await _vm.RefreshJobsAsync(); }
}
