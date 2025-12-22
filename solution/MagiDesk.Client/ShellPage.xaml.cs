using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;

namespace MagiDesk.Client
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel { get; }

        public ShellPage(ShellViewModel viewModel)
        {
            this.InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;
            this.Loaded += ShellPage_Loaded;
        }

        private void ShellPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // Ensure we start at Login if not authenticated
            if (!ViewModel.AuthService.IsLoggedIn)
            {
                // Force navigation to Login
                ViewModel.NavigateToLogin();
                // Clear any auto-selected item in NavigationView
                NavView.SelectedItem = null;
            }

            // Force update of one-way bindings to ensure ContentControl reflects the ViewModel state
            this.Bindings.Update();
        }


        public Microsoft.UI.Xaml.Visibility ToVis(bool isVisible) =>
            isVisible ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                switch (item.Tag?.ToString())
                {
                    case "Tables":
                        ViewModel.NavigateToTables();
                        break;
                    case "Menu":
                        ViewModel.NavigateToMenu();
                        break;
                    case "MenuEditor":
                        ViewModel.NavigateToMenuEditor();
                        break;
                    case "Inventory":
                        ViewModel.NavigateToInventory();
                        break;
                    case "Reports":
                        ViewModel.NavigateToReports();
                        break;
                    case "Settings":
                        ViewModel.NavigateToSettings();
                        break;
                    case "PaymentHub":
                        ViewModel.NavigateToPaymentHub();
                        break;
                    case "ShiftController":
                        ViewModel.NavigateToShiftController();
                        break;
                    case "Logout":
                        ViewModel.LogoutCommand.Execute(null);
                        break;
                }
            }
        }
    }
}
