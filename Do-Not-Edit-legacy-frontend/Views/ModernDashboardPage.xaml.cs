using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class ModernDashboardPage : Page
{
    private ModernDashboardViewModel? _viewModel;
    private NotificationService? _notificationService;

    public ModernDashboardPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (_viewModel == null)
        {
                var tableRepository = new TableRepository();
            _notificationService = new NotificationService();
            _viewModel = new ModernDashboardViewModel(App.Api!, tableRepository, _notificationService);
            DataContext = _viewModel;
        }
        
        await _viewModel.RefreshDataAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _viewModel?.Dispose();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshDataAsync();
        }
    }

    private void ViewNotifications_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to notifications page or show notifications flyout
        ShowNotificationsFlyout();
    }

    private void StartSession_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to tables page using the MainPage's navigation
        NavigateToPage(typeof(TablesPage));
    }

    private void ViewOrders_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to orders page using the MainPage's navigation
        NavigateToPage(typeof(OrdersPage));
    }

    private void GenerateReport_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to reports page or show report dialog using the MainPage's navigation
        NavigateToPage(typeof(ReportGenerationDialog));
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to settings page using the MainPage's navigation
        NavigateToPage(typeof(SettingsPage));
    }

    private void NavigateToPage(Type pageType)
    {
        // Use the MainPage's navigation by finding it through the visual tree
        var mainPage = FindMainPage();
        if (mainPage != null)
        {
            // Use reflection to access the ContentFrame
            var contentFrameField = typeof(MainPage).GetField("ContentFrame", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (contentFrameField?.GetValue(mainPage) is Frame frame)
            {
                frame.Navigate(pageType);
            }
        }
    }

    private MainPage? FindMainPage()
    {
        // Find the MainPage by traversing up the visual tree
        var current = this as DependencyObject;
        while (current != null)
        {
            if (current is MainPage mainPage)
            {
                return mainPage;
            }
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void ShowNotificationsFlyout()
    {
        if (_viewModel?.Notifications != null)
        {
            var flyout = new Flyout();
            var listView = new ListView
            {
                ItemsSource = _viewModel.Notifications,
                SelectionMode = ListViewSelectionMode.None
            };
            
            flyout.Content = listView;
            flyout.ShowAt(this);
        }
    }
}
