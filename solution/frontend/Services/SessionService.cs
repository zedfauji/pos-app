using System.Text;
using Microsoft.UI.Xaml;
using MagiDesk.Shared.DTOs.Auth;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;

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
            var bytes = await File.ReadAllBytesAsync(FilePath);
            
            // CRITICAL FIX: Ensure Windows Runtime COM calls are made from UI thread
            // CryptographicBuffer and DataProtectionProvider are COM interop calls
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
            {
                var tcs = new TaskCompletionSource<SessionDto?>();
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        var enc = CryptographicBuffer.CreateFromByteArray(bytes);
                        var provider = new DataProtectionProvider();
                        var dec = await provider.UnprotectAsync(enc);
                        var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, dec);
                        var result = System.Text.Json.JsonSerializer.Deserialize<SessionDto>(json);
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                
                Current = await tcs.Task;
            }
            else
            {
                // Fallback: try direct call (might fail if not on UI thread)
                var enc = CryptographicBuffer.CreateFromByteArray(bytes);
                var provider = new DataProtectionProvider();
                var dec = await provider.UnprotectAsync(enc);
                var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, dec);
                Current = System.Text.Json.JsonSerializer.Deserialize<SessionDto>(json);
            }
        }
        catch { Current = null; }
    }

    public static async Task SaveAsync(SessionDto session)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = System.Text.Json.JsonSerializer.Serialize(session);
        
        // CRITICAL FIX: Ensure Windows Runtime COM calls are made from UI thread
        // CryptographicBuffer and DataProtectionProvider are COM interop calls
        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue != null)
        {
            var tcs = new TaskCompletionSource<bool>();
            dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var buf = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
                    var provider = new DataProtectionProvider("LOCAL=user");
                    var enc = await provider.ProtectAsync(buf);
                    CryptographicBuffer.CopyToByteArray(enc, out byte[] protectedBytes);
                    await File.WriteAllBytesAsync(FilePath, protectedBytes);
                    Current = session;
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            await tcs.Task;
        }
        else
        {
            // Fallback: try direct call (might fail if not on UI thread)
            var buf = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
            var provider = new DataProtectionProvider("LOCAL=user");
            var enc = await provider.ProtectAsync(buf);
            CryptographicBuffer.CopyToByteArray(enc, out byte[] protectedBytes);
            await File.WriteAllBytesAsync(FilePath, protectedBytes);
            Current = session;
        }
    }

    public static void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        Current = null;
    }
}
