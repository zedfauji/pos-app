using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using System;
using System.Threading.Tasks;

namespace MagiDesk.Client.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;
        private readonly ShellViewModel _shellViewModel;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoginViewModel(IAuthenticationService authService, IDialogService dialogService, ShellViewModel shellViewModel)
        {
            _authService = authService;
            _dialogService = dialogService;
            _shellViewModel = shellViewModel;
        }

        [RelayCommand]
        public async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var success = await _authService.LoginAsync(Username, Password);
                if (success)
                {
                    _shellViewModel.OnLoginSuccess();
                }
                else
                {
                    ErrorMessage = "Invalid credentials. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Login failed: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
