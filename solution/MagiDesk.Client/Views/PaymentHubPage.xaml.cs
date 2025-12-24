using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MagiDesk.Client.Views;

public sealed partial class PaymentHubPage : Page
{
    private PaymentHubViewModel? _viewModel;
    public PaymentHubViewModel ViewModel => _viewModel ??= App.GetService<PaymentHubViewModel>()
        ?? throw new InvalidOperationException("PaymentHubViewModel could not be resolved from DI container");

    public PaymentHubPage()
    {
        this.InitializeComponent();
        // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
        this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Load bills when navigating to this page
        await ViewModel.LoadBillsCommand.ExecuteAsync(null);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Refresh bills on page load
        await ViewModel.LoadBillsCommand.ExecuteAsync(null);
    }
}
