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
        public static Services.UserApiService? UsersApi { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            InitializeApi();
            ApplyThemeFromConfig();
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

        private void InitializeApi()
        {
            try
            {
                var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true);
                var config = builder.Build();
                var baseUrl = config["Api:BaseUrl"] ?? "https://localhost:7016";
                Api = new Services.ApiService(baseUrl);
                var inner = new HttpClientHandler();
                inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logging = new Services.HttpLoggingHandler(inner);
                var http = new HttpClient(logging) { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
                UsersApi = new Services.UserApiService(http);
            }
            catch
            {
                Api = new Services.ApiService("https://localhost:7016");
                var inner = new HttpClientHandler();
                inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logging = new Services.HttpLoggingHandler(inner);
                UsersApi = new Services.UserApiService(new HttpClient(logging) { BaseAddress = new Uri("https://localhost:7016/") });
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
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
