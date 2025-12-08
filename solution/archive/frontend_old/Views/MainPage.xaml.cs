using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavView.MenuItems.Count > 0)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
            }
            ContentFrame.Navigate(typeof(DashboardPage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
            {
                switch (tag)
                {
                    case "DashboardPage":
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;
                    case "VendorsPage":
                        ContentFrame.Navigate(typeof(VendorsPage));
                        break;
                    case "ItemsPage":
                        ContentFrame.Navigate(typeof(ItemsPage));
                        break;
                    case "SettingsPage":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }
    }
}
