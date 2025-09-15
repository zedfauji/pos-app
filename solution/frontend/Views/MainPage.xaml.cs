using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views
{
    /// <summary>
    /// App shell with NavigationView and content frame.
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            
            // CRITICAL: Initialize ReceiptService asynchronously after InitializeComponent
            // This ensures it's ready before any other pages can create PaymentDialog
            _ = InitializeReceiptServiceAsync();
            
            Loaded += MainPage_Loaded;
            NavView.BackRequested += NavView_BackRequested;
            ContentFrame.Navigated += ContentFrame_Navigated;
            _ = StartBackendMonitorAsync();
            App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        }

        private async Task InitializeReceiptServiceAsync()
        {
            try
            {
                if (App.ReceiptService != null)
                {
                    var printingPanel = this.FindName("PrintingContainer") as Panel;
                    if (printingPanel != null)
                    {
                        // CRITICAL FIX: Use App.MainWindow instead of Window.Current to avoid race condition
                        // Window.Current might be null during MainPage constructor
                        var window = App.MainWindow;
                        
                        // Get DispatcherQueue with fallback
                        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                        if (dispatcherQueue == null && window != null)
                        {
                            dispatcherQueue = window.DispatcherQueue;
                        }
                        
                        if (dispatcherQueue != null)
                        {
                            // CRITICAL: Use async initialization
                            await App.ReceiptService.InitializeAsync(printingPanel, dispatcherQueue, window);
                            Log.Info("ReceiptService initialized asynchronously in MainPage");
                        }
                        else
                        {
                            Log.Error("DispatcherQueue not available in MainPage");
                        }
                    }
                    else
                    {
                        Log.Error("PrintingContainer not found in MainPage");
                    }
                }
                else
                {
                    Log.Error("App.ReceiptService is null in MainPage");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize ReceiptService in MainPage: {ex.Message}");
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure navigation pane is always visible and anchored
                NavView.IsPaneOpen = true;
                NavView.PaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Left;
                
                // Force refresh the navigation pane layout
                NavView.UpdateLayout();
                
                // ReceiptService is already initialized in constructor
                // Just do the initial setup here
                
                // Initial connectivity check
                _ = CheckBackendAsync();
                // Apply language to UI
                ApplyLanguage();
                // Role-based menu: add Users item only for admins
                var role = Services.SessionService.Current?.Role ?? "employee";
                bool isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) || 
                              string.Equals(role, "administrator", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);
                // Ensure Users menu exists for admins
                var usersItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (i.Content?.ToString() ?? "") == "Users");
                if (isAdmin && usersItem == null)
                {
                    usersItem = new NavigationViewItem { Content = "Users", Tag = "UsersPage", Icon = new SymbolIcon(Symbol.Contact) };
                    NavView.MenuItems.Insert(1, usersItem); // near Vendors
                }
                if (!isAdmin && usersItem != null)
                {
                    NavView.MenuItems.Remove(usersItem);
                }
                if (NavView.MenuItems.Count > 0)
                {
                    NavView.SelectedItem = NavView.MenuItems[0];
                }
                Log.Info("Navigating to ModernDashboardPage on load");
#if XAML_ONLY_MAIN
                // In isolation builds, avoid referencing other pages
                // Optionally, navigate to self-hosted placeholder
#else
                ContentFrame.Navigate(typeof(ModernDashboardPage));
#endif
            }
            catch (Exception ex)
            {
                Log.Error("Initial navigation failed", ex);
            }
        }

        private void ApplyLanguage()
        {
            try
            {
                // Toolbar
                AddBtn.Label = App.I18n.T("add");
                EditBtn.Label = App.I18n.T("edit");
                DeleteBtn.Label = App.I18n.T("delete");
                RefreshBtn.Label = App.I18n.T("refresh");
                DarkToggle.Label = App.I18n.T("dark_mode");
                LogoutBtn.Label = App.I18n.T("logout");

                // Backend info message
                BackendInfo.Message = App.I18n.T("backend_unavailable");

                // Navigation items content
                foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
                {
                    var tag = item.Tag?.ToString();
                    switch (tag)
                    {
                        case "DashboardPage": item.Content = App.I18n.T("dashboard"); break;
                        case "MenuManagementPage": item.Content = "Menu Management"; break;
                        case "VendorOrdersPage": item.Content = App.I18n.T("vendor-orders"); break;
                        case "InventoryManagementPage": item.Content = App.I18n.T("inventory"); break;
                        case "CashFlowPage": item.Content = App.I18n.T("cash_flow"); break;
                        case "VendorsInventoryPage": item.Content = "Vendors Inventory"; break;
                        case "BillingPage": item.Content = "Billing"; break;
                        case "TablesPage": item.Content = App.I18n.T("tables"); break;
                        case "SettingsPage": item.Content = App.I18n.T("settings"); break;
                        case "UsersPage": item.Content = App.I18n.T("users"); break;
                    }
                }
            }
            catch { }
        }

        private async Task StartBackendMonitorAsync()
        {
            while (true)
            {
                try { await CheckBackendAsync(); } catch { }
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        private async Task CheckBackendAsync()
        {
            try
            {
                var ok = await App.UsersApi!.PingAsync();
                BackendInfo.IsOpen = !ok;
            }
            catch
            {
                BackendInfo.IsOpen = true;
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            try
            {
                if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
                {
                    Log.Info($"NavView invoked: {tag}");
                    switch (tag)
                    {
                        case "MenuManagementPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(MenuManagementPage));
#endif
                            break;
                        case "EnhancedMenuManagementPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(EnhancedMenuManagementPage));
#endif
                            break;
                        case "DashboardPage":
#if XAML_ONLY_MAIN
                            // skip navigation in isolation
#else
                            ContentFrame.Navigate(typeof(ModernDashboardPage));
#endif
                            break;
                        case "VendorOrdersPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(VendorOrdersPage));
#endif
                            break;
                        case "VendorsInventoryPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(VendorsInventoryPage));
#endif
                            break;
                        case "InventoryManagementPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(InventoryManagementPage));
#endif
                            break;
                        case "TablesPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(TablesPage));
#endif
                            break;
                        case "OrdersManagementPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(OrdersManagementPage));
#endif
                            break;
                        case "OrdersPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(OrdersPage));
#endif
                            break;
                        case "SessionsPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(SessionsPage));
#endif
                            break;
                        case "CashFlowPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(CashFlowPage));
#endif
                            break;
                        case "SettingsPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(SettingsPage));
#endif
                            break;
                        case "UsersPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(UsersPage));
#endif
                            break;
                        case "BillingPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(BillingPage));
#endif
                            break;
                        case "PaymentPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(PaymentPage));
#endif
                            break;
                        case "AllPaymentsPage":
#if XAML_ONLY_MAIN
#else
                            ContentFrame.Navigate(typeof(AllPaymentsPage));
#endif
                            break;
                    }
                    // Update back button after navigation
                    NavView.IsBackEnabled = ContentFrame.CanGoBack;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Navigation failed", ex);
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            try
            {
                if (ContentFrame.CanGoBack)
                {
                    ContentFrame.GoBack();
                    NavView.IsBackEnabled = ContentFrame.CanGoBack;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Back navigation failed", ex);
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // keep back button state in sync
            NavView.IsBackEnabled = ContentFrame.CanGoBack;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content is IToolbarConsumer c) c.OnAdd();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content is IToolbarConsumer c) c.OnEdit();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content is IToolbarConsumer c) c.OnDelete();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content is IToolbarConsumer c) c.OnRefresh();
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isDark = (sender as AppBarToggleButton)?.IsChecked ?? false;
            ThemeService.Apply(isDark);
        }

        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Services.SessionService.ClearAsync();
                // Navigate to LoginPage
                if (this.Parent is Frame f)
                {
                    #if XAML_ONLY_MAIN
                    // Skip navigation in isolation
                    #else
                    f.Navigate(typeof(LoginPage));
                    #endif
                }
                else
                {
                    var frame = new Frame();
                    #if XAML_ONLY_MAIN
                    // Skip navigation in isolation
                    #else
                    frame.Navigate(typeof(LoginPage));
                    #endif
                    (App.MainWindow!).Content = frame;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Logout failed", ex);
            }
        }
    }
}
