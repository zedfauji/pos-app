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

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
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

            // ViewModels
            services.AddSingleton<ShellViewModel>();
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
            // Register Converters globally in Resources (if not done in XAML)
            // Or better yet, ensure they are in App.xaml resources.
            // Since I cannot easily edit App.xaml resources block blindly without replacing it all,
            // I will check if I can rely on them being there or add them to the Page resources.
            // Actually, let's just make sure the converters exist in the project first.
            
            m_window = new Window();
            m_window.Title = "MagiDesk POS";

            // Resolve ShellPage
            var shellPage = Services.GetRequiredService<ShellPage>();
            m_window.Content = shellPage;
            
            // Trigger initial navigation
            shellPage.ViewModel.NavigateToLogin();

            m_window.Activate();
        }

        public static T GetService<T>() where T : class
        {
            return ((App)Application.Current).Services.GetService(typeof(T)) as T;
        }

        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Log.Fatal(e.Exception, "Unhandled Exception");
            
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
        }
    }
}
