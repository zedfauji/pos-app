using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using MagiDesk.Client.Services;
using Serilog;

namespace MagiDesk.Client.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private string _title = "MagiDesk";

    public IAuthenticationService AuthService { get; }

    public bool IsLoggedIn => AuthService.IsLoggedIn;
    public bool IsAdmin => AuthService.IsAdmin;

    public ShellViewModel(IServiceProvider serviceProvider, IAuthenticationService authService)
    {
        _serviceProvider = serviceProvider;
        AuthService = authService;

        if (AuthService is System.ComponentModel.INotifyPropertyChanged notifyService)
        {
            notifyService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsLoggedIn))
                {
                    OnPropertyChanged(nameof(IsLoggedIn));
                    // Re-evaluate Admin status when login changes
                    OnPropertyChanged(nameof(IsAdmin)); 
                }
                else if (e.PropertyName == "CurrentRole") // Role changes affect IsAdmin
                {
                    OnPropertyChanged(nameof(IsAdmin));
                }
            };
        }
    }

    [RelayCommand]
    public void NavigateToLogin() => NavigateTo<LoginViewModel>();

    [RelayCommand]
    public void NavigateToTables() => NavigateTo<TableViewModel>();

    [RelayCommand]
    public void NavigateToMenu() => NavigateTo<MenuViewModel>();

    [RelayCommand]
    public void NavigateToInventory() => NavigateTo<InventoryViewModel>();

    [RelayCommand]
    public void NavigateToMenuEditor() => NavigateTo<MenuEditorViewModel>();

    [RelayCommand]
    public void NavigateToReports() => NavigateTo<ReportsViewModel>();

    [RelayCommand]
    public void NavigateToSettings() => NavigateTo<SettingsViewModel>();

    [RelayCommand]
    public void NavigateToPaymentHub() => NavigateTo<PaymentHubViewModel>();

    [RelayCommand]
    public void Logout()
    {
        AuthService.Logout();
        NavigateToLogin();
    }

    public void OnLoginSuccess()
    {
        // Go to default page
        NavigateToTables();
    }

    public async void NavigateToOrder(string tableLabel)
    {
        var vm = _serviceProvider.GetRequiredService<OrderViewModel>();
        await vm.InitializeAsync(tableLabel);
        CurrentViewModel = vm;
    }

    public void NavigateToPaymentWorkspace(object navParams)
    {
        // PaymentWorkspacePage expects navigation via its OnNavigatedTo
        // We'll store the params and trigger navigation via the view
        var page = _serviceProvider.GetRequiredService<Views.PaymentWorkspacePage>();
        // This is a workaround since we can't directly pass parameters through ContentControl
        // The page will need to be navigated to properly
        
        // For now, create a temporary navigation mechanism
        System.Diagnostics.Debug.WriteLine($"Navigate to PaymentWorkspace with params: {navParams}");
        
        // We'll need a different approach - let's use a static property or event
        Views.PaymentWorkspacePage.PendingNavParams = navParams as Views.PaymentWorkspaceNavParams;
        
        var vm = _serviceProvider.GetRequiredService<PaymentWorkspaceViewModel>();
        CurrentViewModel = vm;
    }

    private void NavigateTo<T>() where T : ObservableObject
    {
        var vmName = typeof(T).Name;
        Log.Information("NAVIGATING to {ViewModel}", vmName);
        CurrentViewModel = _serviceProvider.GetRequiredService<T>();
    }
}
