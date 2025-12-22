using System;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace MagiDesk.Client.Services;

public interface ITokenService
{
    Task SaveTokenAsync(string token);
    Task<string?> GetTokenAsync();
    Task ClearTokenAsync();
}

public class WindowsTokenService : ITokenService
{
    private const string ResourceName = "MagiDesk.Client";
    private const string UserName = "AuthToken";

    public Task SaveTokenAsync(string token)
    {
        var vault = new PasswordVault();
        var cred = new PasswordCredential(ResourceName, UserName, token);
        vault.Add(cred);
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync()
    {
        try
        {
            var vault = new PasswordVault();
            var cred = vault.Retrieve(ResourceName, UserName);
            cred.RetrievePassword();
            return Task.FromResult<string?>(cred.Password);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task ClearTokenAsync()
    {
        try
        {
            var vault = new PasswordVault();
            var cred = vault.Retrieve(ResourceName, UserName);
            vault.Remove(cred);
        }
        catch { }
        return Task.CompletedTask;
    }
}
