using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Refit;
using System;
using System.Net.Http;
using Polly;
using MagiDesk.Client.Services;
using MagiDesk.Client.ViewModels;

namespace MagiDesk.Client
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }
        private Window m_window;
        public Window MainWindow => m_window;

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            
            // Handle exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "logs", "log-.txt"), rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .WriteTo.Debug()
                .CreateLogger();

            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
        }

        private void AppDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception ?? new Exception("Unknown exception"), "AppDomain Unhandled Exception. IsTerminating: {IsTerminating}", e.IsTerminating);
            
#if DEBUG
            // In Debug, allow the exception to propagate so Visual Studio can break
            // Don't set e.IsTerminating to false as it's read-only
#endif
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<ITokenService, WindowsTokenService>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>(); // New Service
            services.AddSingleton<IDialogService, ContentDialogService>();
            services.AddSingleton<IPrinterService, EscPosPrinterService>();
            services.AddTransient<LoggingHandler>();

            // API Clients (Refit)
            // Ports: Users=55161/55162(HTTP), Tables=53503, Menu=5227
            
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true };

            // Using HTTP port 55162 for UsersApi locally to avoid HTTPS issues if any
            services.AddRefitClient<IAuthApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:55162")) 
                .AddHttpMessageHandler<LoggingHandler>()
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddRefitClient<ITableApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:53503"))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true })
                .AddHttpMessageHandler<LoggingHandler>()
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddRefitClient<IShiftApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:53503"))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true })
                .AddHttpMessageHandler<LoggingHandler>()
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddRefitClient<IMenuApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5227"))
                .AddHttpMessageHandler<LoggingHandler>()
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddRefitClient<IInventoryApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5117"))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true })
                .AddHttpMessageHandler<LoggingHandler>()
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddRefitClient<IPaymentApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5002"))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true })
                .AddHttpMessageHandler<LoggingHandler>()
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // ViewModels (ShellViewModel must be registered before NavigationService)
            services.AddSingleton<ShellViewModel>();
            
            // Navigation Service (uses Lazy to break circular dependency with ShellViewModel)
            services.AddSingleton<INavigationService>(sp => 
            {
                // Use Lazy<T> to defer ShellViewModel resolution until first navigation
                var lazyShellVm = new Lazy<ShellViewModel>(() => sp.GetRequiredService<ShellViewModel>());
                return new NavigationService(sp, lazyShellVm);
            });
            
            services.AddTransient<LoginViewModel>();
            services.AddTransient<TableViewModel>();
            services.AddTransient<MenuViewModel>();
            services.AddTransient<OrderViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<MenuEditorViewModel>();
            services.AddTransient<SettingsViewModel>(); // New Phase 17
            services.AddTransient<DayCloseViewModel>(); // Phase 2: Reporting
            services.AddTransient<PaymentHubViewModel>(); // Payment Workspace Redesign Phase 1
            services.AddTransient<PaymentWorkspaceViewModel>(); // Payment Workspace Redesign Phase 2
            services.AddTransient<TableWorkspaceViewModel>(); // GAP-08: Operational ViewModel
            services.AddTransient<ShiftControllerViewModel>(); // Shift Controller
            services.AddTransient<TableManagementViewModel>(); // Admin Table CRUD

            services.AddRefitClient<IReportingApi>()
                 .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5228"))
                 .AddHttpMessageHandler<LoggingHandler>()
                 .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // Views
            services.AddTransient<ShellPage>();
            services.AddTransient<LoginPage>();
            services.AddTransient<OrderPage>();
            services.AddTransient<InventoryPage>();
            services.AddTransient<ReportsPage>();
            services.AddTransient<MenuEditorPage>();
            services.AddTransient<SettingsPage>(); // New Phase 17
            services.AddTransient<PaymentHubPage>(); // Payment Workspace Redesign Phase 1
            services.AddTransient<PaymentWorkspacePage>(); // Payment Workspace Redesign Phase 2
            services.AddTransient<TableWorkspacePage>(); // GAP-08: Operational Page
            services.AddTransient<TableManagementPage>(); // Admin Table CRUD Page
        }



        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                Log.Information("Application launching...");
                
                m_window = new Window();
                m_window.Title = "MagiDesk POS";
                
                Log.Information("Window created, resolving ShellPage...");

                // Resolve ShellPage with detailed error handling
                ShellPage shellPage;
                try
                {
                    // Debug: Try to resolve each dependency of ShellViewModel manually first
                    Log.Information("Step 1: Testing ShellViewModel dependencies one by one...");
                    
                    Log.Information("Step 1a: Resolving IAuthenticationService...");
                    var authService = Services.GetRequiredService<IAuthenticationService>();
                    Log.Information("Step 1a: IAuthenticationService OK");
                    
                    Log.Information("Step 1b: Resolving IShiftApi...");
                    var shiftApi = Services.GetRequiredService<IShiftApi>();
                    Log.Information("Step 1b: IShiftApi OK");
                    
                    Log.Information("Step 1c: Resolving IDialogService...");
                    var dialogService = Services.GetRequiredService<IDialogService>();
                    Log.Information("Step 1c: IDialogService OK");
                    
                    Log.Information("Step 1d: Resolving INavigationService...");
                    var navService = Services.GetRequiredService<INavigationService>();
                    Log.Information("Step 1d: INavigationService OK");
                    
                    Log.Information("Step 1e: Resolving ShellViewModel...");
                    var shellVm = Services.GetRequiredService<ShellViewModel>();
                    Log.Information("Step 1e: ShellViewModel OK");
                    
                    Log.Information("Step 2: Attempting to resolve ShellPage via DI...");
                    shellPage = Services.GetRequiredService<ShellPage>();
                    Log.Information("Step 2: ShellPage resolved successfully");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Failed to resolve ShellPage from DI container. Exception: {ExType} - {ExMsg}", ex.GetType().Name, ex.Message);
                    if (ex.InnerException != null)
                    {
                        Log.Fatal(ex.InnerException, "Inner Exception: {InnerType} - {InnerMsg}", ex.InnerException.GetType().Name, ex.InnerException.Message);
                    }
                    throw;
                }
                
                Log.Information("Setting ShellPage as window content...");
                try
                {
                    m_window.Content = shellPage;
                    Log.Information("ShellPage content set successfully");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Failed to set ShellPage as window content. This may indicate a XAML initialization error.");
                    throw;
                }
                
                Log.Information("Triggering initial navigation to Login...");
                try
                {
                    // Trigger initial navigation
                    shellPage.ViewModel.NavigateToLogin();
                    Log.Information("Initial navigation to Login completed");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Failed to navigate to Login page");
                    throw;
                }

                Log.Information("Activating window...");
                m_window.Activate();
                
                Log.Information("Application launched successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error during application launch");
#if DEBUG
                // In Debug, re-throw so Visual Studio can break
                throw;
#endif
            }
        }

        public static T GetService<T>() where T : class
        {
            var app = Application.Current as App;
            if (app == null)
            {
                throw new InvalidOperationException("Application.Current is null. Ensure App is initialized before accessing services.");
            }
            if (app.Services == null)
            {
                throw new InvalidOperationException("App.Services is null. Ensure services are configured.");
            }
            return app.Services.GetService(typeof(T)) as T;
        }

        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "Unhandled Exception");
            
            // Log binding-related errors with additional context
            if (e.Exception?.Message?.Contains("Binding", StringComparison.OrdinalIgnoreCase) == true ||
                e.Exception?.Message?.Contains("DataContext", StringComparison.OrdinalIgnoreCase) == true ||
                e.Exception?.Source?.Contains("Xaml", StringComparison.OrdinalIgnoreCase) == true)
            {
                Log.Error(e.Exception, "Binding or XAML-related error detected. Check DataContext and binding paths.");
            }
            
#if DEBUG
            // In Debug builds, don't mark as handled so Visual Studio can break on the exception
            // This allows proper debugging with breakpoints
            e.Handled = false;
            return;
#else
            // In Release builds, mark as handled and show user-friendly error
            e.Handled = true;
            
            try 
            {
                if (m_window != null && Services != null)
                {
                    // Attempt to resolve dialog service to show error
                    var dialogService = Services.GetService<IDialogService>();
                    if (dialogService != null)
                    {
                         await dialogService.ShowMessageAsync("Critical Error", $"An unexpected error occurred: {e.Exception.Message}");
                    }
                }
            }
            catch 
            { 
               // Worst case: just log (already done) 
            }
#endif
        }
    }
}
