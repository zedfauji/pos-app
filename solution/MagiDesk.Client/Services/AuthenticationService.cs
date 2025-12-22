using CommunityToolkit.Mvvm.ComponentModel;
using MagiDesk.Client.Services.Dtos;
using MagiDesk.Shared.DTOs.Auth;
using System;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services
{
    public interface IAuthenticationService
    {
        bool IsLoggedIn { get; }
        string? CurrentUserId { get; }
        string? CurrentUsername { get; }
        string? CurrentRole { get; }
        bool IsAdmin { get; }
        
        Task<bool> LoginAsync(string username, string password);
        void Logout();
    }

    public partial class AuthenticationService : ObservableObject, IAuthenticationService
    {
        private readonly IAuthApi _authApi;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAdmin))]
        private bool _isLoggedIn;

        [ObservableProperty]
        private string? _currentUserId;

        [ObservableProperty]
        private string? _currentUsername;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAdmin))]
        private string? _currentRole;

        public AuthenticationService(IAuthApi authApi)
        {
            _authApi = authApi;
        }

        public bool IsAdmin => CurrentRole?.ToLower() == "administrator" || CurrentRole?.ToLower() == "manager" || CurrentRole?.ToLower() == "owner";

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var request = new LoginRequest { Username = username, Password = password };
                var response = await _authApi.LoginAsync(request);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    CurrentUserId = response.Content.UserId;
                    CurrentUsername = response.Content.Username;
                    CurrentRole = response.Content.Role;
                    IsLoggedIn = true;
                    return true;
                }
            }
            catch (Exception)
            {
                // Log error?
            }
            return false;
        }

        public void Logout()
        {
            CurrentUserId = null;
            CurrentUsername = null;
            CurrentRole = null;
            IsLoggedIn = false;
        }
    }
}
