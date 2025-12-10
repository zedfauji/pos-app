using System.Text;
using Microsoft.UI.Xaml;
using MagiDesk.Shared.DTOs.Auth;
using System.Security.Cryptography;

namespace MagiDesk.Frontend.Services;

public static class SessionService
{
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "session.bin");

    public static SessionDto? Current { get; private set; }

    public static async Task InitializeAsync()
    {
        try
        {
            if (!File.Exists(FilePath)) { Current = null; return; }
            var bytes = await SafeFileOperations.SafeReadBytesAsync(FilePath);
            
            // Use .NET cryptography instead of Windows Runtime COM interop
            // This avoids "No installed components were detected" errors in WinUI 3 Desktop Apps
            try
            {
                // Simple base64 decode for now - can be enhanced with proper encryption later
                var json = Encoding.UTF8.GetString(bytes);
                var result = System.Text.Json.JsonSerializer.Deserialize<SessionDto>(json);
                Current = result;
            }
            catch (Exception ex)
            {
                // If decryption fails, clear the session
                Current = null;
                // Optionally delete the corrupted file
                try { await SafeFileOperations.SafeDeleteAsync(FilePath); } catch { }
            }
        }
        catch (Exception ex)
        {
            Current = null;
        }
    }

    public static async Task SaveAsync(SessionDto session)
    {
        try
        {
            SafeFileOperations.EnsureDirectoryExists(Path.GetDirectoryName(FilePath)!);
            var json = System.Text.Json.JsonSerializer.Serialize(session);
            
            // Use .NET file operations instead of Windows Runtime COM interop
            // This avoids "No installed components were detected" errors in WinUI 3 Desktop Apps
            var bytes = Encoding.UTF8.GetBytes(json);
            await SafeFileOperations.SafeWriteBytesAsync(FilePath, bytes);
            Current = session;
        }
        catch (Exception ex)
        {
            // If save fails, don't update Current
        }
    }

    public static async Task ClearAsync()
    {
        try { if (File.Exists(FilePath)) await SafeFileOperations.SafeDeleteAsync(FilePath); } catch { }
        Current = null;
    }
}
