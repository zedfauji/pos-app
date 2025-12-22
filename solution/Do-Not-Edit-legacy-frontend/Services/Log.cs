using System.Diagnostics;
using System.Text;

namespace MagiDesk.Frontend.Services;

public static class Log
{
    private static readonly object _lock = new();
    private static readonly string _logPath;

    static Log()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "frontend.log");
        try { File.AppendAllText(_logPath, $"\n==== Session {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====\n"); } catch { }
    }

    public static void Info(string message)
    {
        var line = $"[INFO] {DateTime.Now:HH:mm:ss} {message}";
        Debug.WriteLine(line);
        SafeAppend(line);
    }

    public static void Warning(string message)
    {
        var line = $"[WARNING] {DateTime.Now:HH:mm:ss} {message}";
        Debug.WriteLine(line);
        SafeAppend(line);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var line = $"[ERROR] {DateTime.Now:HH:mm:ss} {message} {ex}";
        Debug.WriteLine(line);
        SafeAppend(line);
    }

    private static void SafeAppend(string line)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch { }
    }
}
