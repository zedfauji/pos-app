using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MagiDesk.Client.Views;

public sealed partial class PaymentHubPage : Page
{
    public PaymentHubViewModel ViewModel { get; }

    public PaymentHubPage()
    {
        this.InitializeComponent();
        
        // Get ViewModel from DI container
        ViewModel = App.GetService<PaymentHubViewModel>();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Load sessions when navigating to this page
        await ViewModel.LoadSessionsCommand.ExecuteAsync(null);
    }
}
