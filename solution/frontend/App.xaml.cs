using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using MagiDesk.Frontend.Services;
using System.Runtime.InteropServices;

namespace MagiDesk.Frontend
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static readonly object _initializationLock = new object();
        private static volatile bool _isInitialized = false;
        private static volatile bool _isInitializing = false;

        public static bool IsInitialized => _isInitialized;
        public static bool IsInitializing => _isInitializing;

        /// <summary>
        /// Wait for App initialization to complete
        /// </summary>
        public static async Task WaitForInitializationAsync()
        {
            while (_isInitializing)
            {
                await Task.Delay(10);
            }
        }

        private Window? window;
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
        public static Services.HeartbeatService? HeartbeatService { get; private set; }
        public static Services.InventorySettingsService? InventorySettingsService { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                Log.Info("App constructor started");
                this.InitializeComponent();
                Log.Info("InitializeComponent completed");
                this.UnhandledException += App_UnhandledException;
                
                // CRITICAL DEBUG: Enable first-chance exception logging
                AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
                {
                    if (e.Exception is System.Runtime.InteropServices.COMException comEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"FIRST-CHANCE COM EXCEPTION: {comEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"HRESULT: 0x{comEx.HResult:X8}");
                        System.Diagnostics.Debug.WriteLine($"Stack Trace: {comEx.StackTrace}");
                    }
                };
                
                // CRITICAL FIX: Initialize ReceiptService IMMEDIATELY in constructor
                // This prevents race conditions with InitializeApiAsync
                // Note: ReceiptService will be properly initialized in MainPage constructor
                try
                {
                    Log.Info("Initializing ReceiptService");
                    var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.ReceiptService>();
                    var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
                    ReceiptService = new Services.ReceiptService(logger, config);
                    Log.Info("ReceiptService initialized successfully");
                }
                catch (Exception ex)
                {
                    Log.Error("ReceiptService initialization failed", ex);
                    // Fallback to null - will be properly initialized later
                    ReceiptService = null;
                }
                
                Log.Info("Starting InitializeApiAsync");
                _ = InitializeApiAsync();
                Log.Info("ApplyThemeFromConfig started");
                ApplyThemeFromConfig();
                Log.Info("App constructor completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error("Critical error in App constructor", ex);
                throw; // Re-throw to prevent silent failure
            }
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
            try
            {
                Log.Info("OnLaunched started");
                window ??= new Window();
                window.Title = "MagiDesk";
                MainWindow = window;
                
                // Set minimum window size to ensure navigation pane is visible
                window.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
                
                // CRITICAL FIX: Remove Window.Current usage to prevent COM exceptions in WinUI 3 Desktop Apps
                // Window.Current is a Windows Runtime COM interop call that causes Marshal.ThrowExceptionForHR errors
                // We use App.MainWindow instead for thread-safe access
                System.Diagnostics.Debug.WriteLine("MainWindow set successfully");

                if (window.Content is not Frame rootFrame)
                {
                    Log.Info("Creating new Frame");
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    window.Content = rootFrame;
                }
                // Always start at LoginPage; under isolation builds navigate to MainPage
#if XAML_ONLY_MAIN
                Log.Info("Navigating to MainPage (XAML_ONLY_MAIN)");
                rootFrame.Navigate(typeof(MagiDesk.Frontend.Views.MainPage), e.Arguments);
#else
                Log.Info("Navigating to LoginPage");
                rootFrame.Navigate(typeof(Views.LoginPage), e.Arguments);
#endif
                Log.Info("Activating window");
                window.Activate();

                // Add window closing cleanup
                window.Closed += async (sender, e) =>
                {
                    try
                    {
                        Log.Info("Window closing, cleaning up sessions");
                        await CleanupActiveSessionsAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error during window cleanup", ex);
                    }
                };

                // CRITICAL FIX: Remove WindowNative COM interop calls to prevent Marshal.ThrowExceptionForHR errors
                // WindowNative.GetWindowHandle is a Windows Runtime COM interop call that causes COM exceptions in WinUI 3 Desktop Apps
                // Window activation is handled automatically by the system
                System.Diagnostics.Debug.WriteLine("Window launched successfully");
                Log.Info("OnLaunched completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error("Critical error in OnLaunched", ex);
                throw; // Re-throw to prevent silent failure
            }
        }

        private async Task InitializeApiAsync()
        {
            lock (_initializationLock)
            {
                if (_isInitialized || _isInitializing)
                    return;
                _isInitializing = true;
            }

            // Default values for fallback
            var backendBase = "https://localhost:7016";
            var inventoryBase = "https://localhost:7016";
            var menuBase = "https://localhost:7016";
            var paymentBase = "https://localhost:7016";
            var ordersBase = "https://localhost:7016";
            var vendorOrdersBase = "https://localhost:7016";
            var usersBase = "https://magidesk-users-23sbzjsxaq-pv.a.run.app";

            try
            {
                var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true);
                var config = builder.Build();
                backendBase = config["Api:BaseUrl"] ?? "https://localhost:7016";
                inventoryBase = config["InventoryApi:BaseUrl"] ?? backendBase;
                menuBase = config["MenuApi:BaseUrl"] ?? inventoryBase;
                paymentBase = config["PaymentApi:BaseUrl"] ?? backendBase;
                ordersBase = config["OrderApi:BaseUrl"] ?? backendBase;
                vendorOrdersBase = config["VendorOrdersApi:BaseUrl"] ?? backendBase;
                usersBase = config["UsersApi:BaseUrl"] ?? "https://magidesk-users-23sbzjsxaq-pv.a.run.app";
                
                // Debug logging
                Log.Info($"Configuration loaded:");
                Log.Info($"  UsersApi:BaseUrl = {config["UsersApi:BaseUrl"]}");
                Log.Info($"  usersBase = {usersBase}");
                Api = new Services.ApiService(backendBase, inventoryBase);

                var inner = new HttpClientHandler();
                inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logging = new Services.HttpLoggingHandler(inner);
                var http = new HttpClient(logging) { BaseAddress = new Uri(usersBase.TrimEnd('/') + "/") };
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
                
                // Initialize HeartbeatService
                HeartbeatService = new Services.HeartbeatService();
                
                // Initialize Inventory Settings Service
                var innerInventorySettings = new HttpClientHandler();
                innerInventorySettings.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logInventorySettings = new Services.HttpLoggingHandler(innerInventorySettings);
                InventorySettingsService = new Services.InventorySettingsService(new HttpClient(logInventorySettings) { BaseAddress = new Uri(inventoryBase.TrimEnd('/') + "/") }, new Services.SimpleLogger<Services.InventorySettingsService>());
                
                // ReceiptService is already initialized in constructor
            }
            catch (Exception ex)
            {
                Log.Error("Failed to initialize APIs from configuration, using fallback", ex);
                Api = new Services.ApiService(backendBase, inventoryBase);
                var inner = new HttpClientHandler();
                inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logging = new Services.HttpLoggingHandler(inner);
                var http = new HttpClient(logging) { BaseAddress = new Uri(usersBase.TrimEnd('/') + "/") };
                UsersApi = new Services.UserApiService(http);

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
                
                // Initialize HeartbeatService
                HeartbeatService = new Services.HeartbeatService();
                
                // Initialize Inventory Settings Service (fallback)
                var innerInventorySettingsFallback = new HttpClientHandler();
                innerInventorySettingsFallback.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                var logInventorySettingsFallback = new Services.HttpLoggingHandler(innerInventorySettingsFallback);
                InventorySettingsService = new Services.InventorySettingsService(new HttpClient(logInventorySettingsFallback) { BaseAddress = new Uri("https://localhost:7016/") }, new Services.SimpleLogger<Services.InventorySettingsService>());
                
                // ReceiptService is already initialized in constructor
            }
            finally
            {
                lock (_initializationLock)
                {
                    _isInitializing = false;
                    _isInitialized = true;
                }
                
                // After initialization, attempt to recover any orphaned sessions
                _ = Task.Run(async () => await RecoverActiveSessionsAsync());
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Log.Error($"Navigation failed to {e.SourcePageType.FullName}", e.Exception);
            
            // Handle navigation failure gracefully instead of throwing exception
            try
            {
                // Try to navigate to a safe fallback page
                if (MainWindow?.Content is Frame frame)
                {
                    frame.Navigate(typeof(Views.MainPage));
                }
            }
            catch (Exception fallbackEx)
            {
                Log.Error("Failed to navigate to fallback page", fallbackEx);
                // If even the fallback fails, just log it - don't crash the app
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Error("Unhandled UI exception", e.Exception);
                // Attempt cleanup before crash
                _ = Task.Run(async () => await CleanupActiveSessionsAsync());
            }
            catch { }
            // Let it crash after logging, or set e.Handled = true to try to continue
            // e.Handled = true; // Uncomment to prevent crash during debugging
        }


        private static async Task CleanupActiveSessionsAsync()
        {
            try
            {
                // Get current session context
                var sessionId = MagiDesk.Frontend.Services.OrderContext.CurrentSessionId;
                var billingId = MagiDesk.Frontend.Services.OrderContext.CurrentBillingId;
                
                if (!string.IsNullOrWhiteSpace(sessionId) && Guid.TryParse(sessionId, out var sessionGuid))
                {
                    // Close any open orders for this session
                    if (OrdersApi != null)
                    {
                        var openOrders = await OrdersApi.GetOrdersBySessionAsync(sessionGuid, includeHistory: false);
                        var ordersToClose = openOrders.Where(o => o.Status == "open").ToList();
                        
                        foreach (var order in ordersToClose)
                        {
                            try
                            {
                                await OrdersApi.CloseOrderAsync(order.Id);
                            }
                            catch { }
                        }
                    }
                }

                // Stop heartbeat
                HeartbeatService?.StopHeartbeat();

                // Clear context
                MagiDesk.Frontend.Services.OrderContext.CurrentSessionId = null;
                MagiDesk.Frontend.Services.OrderContext.CurrentBillingId = null;
                MagiDesk.Frontend.Services.OrderContext.CurrentOrderId = null;
            }
            catch { }
        }

        /// <summary>
        /// Recover active sessions on app startup - this is the key to handling crashes
        /// </summary>
        public static async Task RecoverActiveSessionsAsync()
        {
            try
            {
                if (Api == null) return;

                // Get all active sessions from the server
                var activeSessions = await Api.GetActiveSessionsAsync();
                
                if (activeSessions.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Found {activeSessions.Count} active sessions that need recovery:");
                    
                    foreach (var session in activeSessions)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Table: {session.TableId}, Session: {session.SessionId}, Server: {session.ServerName}");
                    }
                    
                    // Show recovery dialog to user
                    await ShowSessionRecoveryDialogAsync(activeSessions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Session recovery failed: {ex.Message}");
            }
        }

        private static async Task ShowSessionRecoveryDialogAsync(List<MagiDesk.Shared.DTOs.Tables.SessionOverview> sessions)
        {
            try
            {
                // Wait for main window to be ready
                await WaitForInitializationAsync();
                
                if (MainWindow?.Content is Frame frame && frame.Content is Views.MainPage mainPage)
                {
                    var dialog = new Dialogs.SessionRecoveryDialog();
                    dialog.SetSessions(sessions);
                    
                    var result = await dialog.ShowAsync();
                    
                    if (result == ContentDialogResult.Primary && dialog.SelectedSession != null)
                    {
                        // Resume selected session
                        await ResumeSessionAsync(dialog.SelectedSession.SessionId);
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        // Close all sessions
                        await CloseAllSessionsAsync(sessions);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Session recovery dialog failed: {ex.Message}");
            }
        }

        private static async Task ResumeSessionAsync(string sessionId)
        {
            try
            {
                if (Guid.TryParse(sessionId, out var sessionGuid))
                {
                    // Set the session context
                    MagiDesk.Frontend.Services.OrderContext.CurrentSessionId = sessionId;
                    
                    // Get session details and set billing ID
                    var sessions = await Api.GetActiveSessionsAsync();
                    var session = sessions.FirstOrDefault(s => s.SessionId == sessionGuid);
                    if (session != null)
                    {
                        MagiDesk.Frontend.Services.OrderContext.CurrentBillingId = session.BillingId?.ToString();
                    }
                    
                    // Start heartbeat for this session
                    HeartbeatService?.StartHeartbeat();
                    
                    System.Diagnostics.Debug.WriteLine($"Resumed session: {sessionId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to resume session {sessionId}: {ex.Message}");
            }
        }

        private static async Task CloseAllSessionsAsync(List<MagiDesk.Shared.DTOs.Tables.SessionOverview> sessions)
        {
            try
            {
                foreach (var session in sessions)
                {
                    try
                    {
                        // Close orders for this session
                        if (OrdersApi != null)
                        {
                            var orders = await OrdersApi.GetOrdersBySessionAsync(session.SessionId, includeHistory: false);
                            var openOrders = orders.Where(o => o.Status == "open").ToList();
                            
                            foreach (var order in openOrders)
                            {
                                await OrdersApi.CloseOrderAsync(order.Id);
                            }
                        }
                        
                        // Stop the session (this will create a bill)
                        var tablesApiUrl = Environment.GetEnvironmentVariable("TABLESAPI_BASEURL") ?? "https://magidesk-tables-904541739138.northamerica-south1.run.app";
                        using var tablesHttp = new HttpClient(new HttpClientHandler 
                        { 
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
                        })
                        {
                            BaseAddress = new Uri(tablesApiUrl.TrimEnd('/') + "/"),
                            Timeout = TimeSpan.FromSeconds(10)
                        };

                        await tablesHttp.PostAsync($"tables/{Uri.EscapeDataString(session.TableId)}/stop", new StringContent(""));
                        
                        System.Diagnostics.Debug.WriteLine($"Closed session: {session.SessionId} on table {session.TableId}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to close session {session.SessionId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to close all sessions: {ex.Message}");
            }
        }
    }
}
