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
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _title = "MagiDesk";

    public IAuthenticationService AuthService { get; }

    public bool IsLoggedIn => AuthService.IsLoggedIn;
    public bool IsAdmin => AuthService.IsAdmin;

    private readonly IShiftApi _shiftApi;
    private readonly IDialogService _dialogService;

    public ShellViewModel(
        IServiceProvider serviceProvider, 
        IAuthenticationService authService, 
        IShiftApi shiftApi, 
        IDialogService dialogService,
        INavigationService navigationService)
    {
        try
        {
            Log.Information("ShellViewModel: Constructor starting...");
            _serviceProvider = serviceProvider;
            Log.Information("ShellViewModel: ServiceProvider assigned");
            
            AuthService = authService;
            Log.Information("ShellViewModel: AuthService assigned");
            
            _shiftApi = shiftApi;
            Log.Information("ShellViewModel: ShiftApi assigned");
            
            _dialogService = dialogService;
            Log.Information("ShellViewModel: DialogService assigned");
            
            _navigationService = navigationService;
            Log.Information("ShellViewModel: NavigationService assigned");

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
            Log.Information("ShellViewModel: Constructor completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "ShellViewModel: Constructor FAILED");
            throw;
        }
    }

    [RelayCommand]
    public void NavigateToLogin() => _navigationService.NavigateTo<LoginViewModel>();

    [RelayCommand]
    public void NavigateToShiftController() => _navigationService.NavigateTo<ShiftControllerViewModel>();

    [RelayCommand]
    public async void NavigateToTables()
    {
        if (await EnsureShiftOpenAsync())
        {
            _navigationService.NavigateTo<TableViewModel>();
        }
    }

    [RelayCommand]
    public void NavigateToMenu() => _navigationService.NavigateTo<MenuViewModel>();

    [RelayCommand]
    public void NavigateToInventory() => _navigationService.NavigateTo<InventoryViewModel>();

    [RelayCommand]
    public void NavigateToMenuEditor() => _navigationService.NavigateTo<MenuEditorViewModel>();

    [RelayCommand]
    public void NavigateToReports() => _navigationService.NavigateTo<ReportsViewModel>();

    [RelayCommand]
    public void NavigateToSettings() => _navigationService.NavigateTo<SettingsViewModel>();

    [RelayCommand]
    public void NavigateToTableManagement() => _navigationService.NavigateTo<TableManagementViewModel>();

    [RelayCommand]
    public async void NavigateToPaymentHub()
    {
        if (await EnsureShiftOpenAsync())
        {
            _navigationService.NavigateTo<PaymentHubViewModel>();
        }
    }

    [RelayCommand]
    public void Logout()
    {
        AuthService.Logout();
        NavigateToLogin();
    }

    public async void OnLoginSuccess()
    {
        // Check shift status on login
        // If shift is closed, go to Shift Controller
        var validation = await _shiftApi.ValidateAsync();
        if (!validation.CanOperate)
        {
             NavigateToShiftController();
             _dialogService.ShowMessageAsync("Shift Required", "A valid shift must be open to perform operations.");
        }
        else
        {
             NavigateToTables();
        }
    }

    private async Task<bool> EnsureShiftOpenAsync()
    {
        try
        {
            var validation = await _shiftApi.ValidateAsync();
            if (!validation.CanOperate)
            {
                await _dialogService.ShowMessageAsync("Shift Closed", "You must open a shift to access this feature.");
                NavigateToShiftController();
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            // If API fails, default to closed for safety, but log it
            Log.Error(ex, "Failed to validate shift status");
            await _dialogService.ShowMessageAsync("Connection Error", "Could not verify shift status. Please check connection.");
            NavigateToShiftController(); 
            return false;
        }
    }


    public async void NavigateToOrder(string tableLabel)
    {
        // Use NavigationService with parameter
        _navigationService.NavigateTo<TableWorkspaceViewModel>(tableLabel);
    }

    public void NavigateToPaymentWorkspace(object navParams)
    {
        // Use NavigationService with parameter - no more static workaround!
        _navigationService.NavigateTo<PaymentWorkspaceViewModel>(navParams);
    }
}
