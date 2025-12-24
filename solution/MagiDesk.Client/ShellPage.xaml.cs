using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;
using Serilog;

namespace MagiDesk.Client
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel { get; }

        public ShellPage(ShellViewModel viewModel)
        {
            Log.Information("ShellPage: Constructor entry - ViewModel is {Status}", viewModel != null ? "not null" : "NULL");
            try
            {
                Log.Information("ShellPage constructor: About to call InitializeComponent...");
                Log.Information("ShellPage constructor: Current App.Current is {Status}", Microsoft.UI.Xaml.Application.Current != null ? "not null" : "NULL");
                
                this.InitializeComponent();
                Log.Information("ShellPage constructor: InitializeComponent completed");
                
                Log.Information("ShellPage constructor: Setting ViewModel property...");
                ViewModel = viewModel;
                Log.Information("ShellPage constructor: ViewModel property set");
                
                Log.Information("ShellPage constructor: Setting DataContext...");
                this.DataContext = ViewModel;
                Log.Information("ShellPage constructor: DataContext set");
                
                Log.Information("ShellPage constructor: Subscribing to Loaded event...");
                this.Loaded += ShellPage_Loaded;
                Log.Information("ShellPage constructor: Completed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "ShellPage constructor: Exception during construction. Type: {ExType}, Message: {ExMsg}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    Log.Fatal(ex.InnerException, "ShellPage constructor: Inner exception: {InnerType} - {InnerMsg}", ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                throw;
            }
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

            // Note: {Binding} updates automatically when properties change (INotifyPropertyChanged)
            // No need to call Bindings.Update() which is only for x:Bind
        }

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
                    case "TableManagement":
                        ViewModel.NavigateToTableManagement();
                        break;
                    case "Logout":
                        ViewModel.LogoutCommand.Execute(null);
                        break;
                }
            }
        }
    }
}
