using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using MagiDesk.Frontend.Services;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace MagiDesk.Frontend
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window window = Window.Current;
        public static Window? MainWindow { get; private set; }
        public static I18nService I18n { get; } = new I18nService();

        public static Services.ApiService? Api { get; private set; }
        public static Services.MenuApiService? Menu { get; private set; }
        public static Services.UserApiService? UsersApi { get; private set; }
        public static Services.PaymentApiService? Payments { get; private set; }
        public static Services.OrderApiService? OrdersApi { get; private set; }
        public static Services.VendorOrdersApiService? VendorOrders { get; private set; }
        public static Services.ReceiptService? ReceiptService { get; private set; }
        public static Services.SettingsApiService? SettingsApi { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            _ = InitializeApiAsync();
            ApplyThemeFromConfig();
        }

        public static void ReinitializeApi(string backendBase, string inventoryBase)
        {
            try
            {
                Api = new Services.ApiService(backendBase, inventoryBase);
            }
            catch { }
        }

        private void ApplyThemeFromConfig()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                var config = builder.Build();
                var theme = (config["UI:Theme"] ?? "System").Trim();
                ApplyThemeToRoot(theme);
            }
            catch { }
        }

        public static void ApplyThemeToRoot(string theme)
        {
            try
            {
                var isDark = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
                var isLight = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);

                if (MainWindow?.Content is FrameworkElement fe)
                {
                    fe.RequestedTheme = isDark ? ElementTheme.Dark : isLight ? ElementTheme.Light : ElementTheme.Default;
                }
                else
                {
                    // Fallback before window is ready
                    if (isDark) Application.Current.RequestedTheme = ApplicationTheme.Dark;
                    else if (isLight) Application.Current.RequestedTheme = ApplicationTheme.Light;
                }
            }
            catch { }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();
            window.Title = "MagiDesk";
            MainWindow = window;

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }
            // Always start at LoginPage; under isolation builds navigate to MainPage
#if XAML_ONLY_MAIN
            rootFrame.Navigate(typeof(MagiDesk.Frontend.Views.MainPage), e.Arguments);
#else
            rootFrame.Navigate(typeof(Views.LoginPage), e.Arguments);
#endif
            window.Activate();

            // Bring window to foreground to ensure visibility
            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                if (hwnd != IntPtr.Zero)
                {
                    ShowWindow(hwnd, SW_SHOW);
                    SetForegroundWindow(hwnd);
                }
            }
            catch { }
        }

        private async Task InitializeApiAsync()
        {
            try
            {
                var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true);
                var config = builder.Build();
                var backendBase = config["Api:BaseUrl"] ?? "https://localhost:7016";
                var inventoryBase = config["InventoryApi:BaseUrl"] ?? backendBase;
                var menuBase = config["MenuApi:BaseUrl"] ?? inventoryBase;
                var paymentBase = config["PaymentApi:BaseUrl"] ?? backendBase;
                var ordersBase = config["OrderApi:BaseUrl"] ?? backendBase;
                var vendorOrdersBase = config["VendorOrdersApi:BaseUrl"] ?? backendBase;
                Api = new Services.ApiService(backendBase, inventoryBase);

                var inner = new HttpClientHandler();
                inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logging = new Services.HttpLoggingHandler(inner);
                var http = new HttpClient(logging) { BaseAddress = new Uri(backendBase.TrimEnd('/') + "/") };
                UsersApi = new Services.UserApiService(http);

                var innerMenu = new HttpClientHandler();
                innerMenu.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logMenu = new Services.HttpLoggingHandler(innerMenu);
                Menu = new Services.MenuApiService(new HttpClient(logMenu) { BaseAddress = new Uri(menuBase.TrimEnd('/') + "/") });

                var innerPay = new HttpClientHandler();
                innerPay.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logPay = new Services.HttpLoggingHandler(innerPay);
                Payments = new Services.PaymentApiService(new HttpClient(logPay) { BaseAddress = new Uri(paymentBase.TrimEnd('/') + "/") });

                var innerOrders = new HttpClientHandler();
                innerOrders.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logOrders = new Services.HttpLoggingHandler(innerOrders);
                OrdersApi = new Services.OrderApiService(new HttpClient(logOrders) { BaseAddress = new Uri(ordersBase.TrimEnd('/') + "/") });

                var innerVendorOrders = new HttpClientHandler();
                innerVendorOrders.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logVendorOrders = new Services.HttpLoggingHandler(innerVendorOrders);
                VendorOrders = new Services.VendorOrdersApiService(new HttpClient(logVendorOrders) { BaseAddress = new Uri(vendorOrdersBase.TrimEnd('/') + "/") });
                
                // Initialize ReceiptService and SettingsApi
                var settingsBase = config["SettingsApi:BaseUrl"] ?? backendBase;
                var innerSettings = new HttpClientHandler();
                innerSettings.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logSettings = new Services.HttpLoggingHandler(innerSettings);
                SettingsApi = new Services.SettingsApiService(new HttpClient(logSettings) { BaseAddress = new Uri(settingsBase.TrimEnd('/') + "/") }, null);
                
                ReceiptService = new Services.ReceiptService(null, null);
                // ReceiptService will be initialized lazily when printing is needed
            }
            catch
            {
                Api = new Services.ApiService("https://localhost:7016", "https://localhost:7016");
                var inner = new HttpClientHandler();
                inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logging = new Services.HttpLoggingHandler(inner);
                UsersApi = new Services.UserApiService(new HttpClient(logging) { BaseAddress = new Uri("https://localhost:7016/") });

                var innerMenu = new HttpClientHandler();
                innerMenu.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logMenu = new Services.HttpLoggingHandler(innerMenu);
                Menu = new Services.MenuApiService(new HttpClient(logMenu) { BaseAddress = new Uri("https://localhost:7016/") });

                var innerPay = new HttpClientHandler();
                innerPay.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logPay = new Services.HttpLoggingHandler(innerPay);
                Payments = new Services.PaymentApiService(new HttpClient(logPay) { BaseAddress = new Uri("https://localhost:7016/") });

                var innerOrders = new HttpClientHandler();
                innerOrders.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logOrders = new Services.HttpLoggingHandler(innerOrders);
                OrdersApi = new Services.OrderApiService(new HttpClient(logOrders) { BaseAddress = new Uri("https://localhost:7016/") });

                var innerVendorOrders = new HttpClientHandler();
                innerVendorOrders.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logVendorOrders = new Services.HttpLoggingHandler(innerVendorOrders);
                VendorOrders = new Services.VendorOrdersApiService(new HttpClient(logVendorOrders) { BaseAddress = new Uri("https://localhost:7016/") });
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Log.Error($"Navigation failed to {e.SourcePageType.FullName}", e.Exception);
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName, e.Exception);
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Error("Unhandled UI exception", e.Exception);
            }
            catch { }
            // Let it crash after logging, or set e.Handled = true to try to continue
            // e.Handled = true; // Uncomment to prevent crash during debugging
        }

        // Win32 interop to bring window to front
        private const int SW_SHOW = 5;
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
