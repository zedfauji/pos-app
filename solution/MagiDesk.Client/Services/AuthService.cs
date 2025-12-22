using MagiDesk.Shared.DTOs.Auth;
using System.Threading.Tasks;
using Refit;

namespace MagiDesk.Client.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
}

public class AuthService : IAuthService
{
    private readonly IAuthApi _authApi;
    private readonly ITokenService _tokenService;
    private bool _isAuthenticated;

    public bool IsAuthenticated => _isAuthenticated;

    public AuthService(IAuthApi authApi, ITokenService tokenService)
    {
        _authApi = authApi;
        _tokenService = tokenService;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var request = new LoginRequest { Username = username, Password = password };
            var response = await _authApi.LoginAsync(request);

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                // NOTE: LoginResponse from Backend (AuthController.cs) does NOT currently return a Token!
                // It returns { UserId, Username, Role, LastLoginAt }.
                // This is a legacy auth system (cookie based or simple trust).
                // However, for the NEW client, we need a way to authenticate subsequent requests if the backend required auth.
                // The current backend likely uses Cookies or nothing.
                // Since this is a rewrite, and we are "Strict API First", we should assume we need a token or we need to handle cookies.
                // Refit handles cookies if we use a specific HttpClientHandler.
                
                // For now, we will assume success = valid session.
                // If the backend returned a JWT in future (which it should), we would save it.
                // For now, we just mark as authenticated locally.
                
                _isAuthenticated = true;
                return true;
            }
        }
        catch
        {
            // Log error
        }
        
        _isAuthenticated = false;
        return false;
    }

    public async Task LogoutAsync()
    {
        _isAuthenticated = false;
        await _tokenService.ClearTokenAsync();
    }
}
