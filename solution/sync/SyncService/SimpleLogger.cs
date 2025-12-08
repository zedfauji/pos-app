using System.Text;

namespace MagiDesk.SyncService;

public class SimpleLogger
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public SimpleLogger(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public void Info(string message) => Write("INFO", message);
    public void Error(string message, Exception? ex = null) => Write("ERROR", message + (ex is null ? string.Empty : $" :: {ex}"));

    private void Write(string level, string message)
    {
        var line = $"{DateTime.UtcNow:O} [{level}] {message}{Environment.NewLine}";
        lock (_lock)
        {
            File.AppendAllText(_filePath, line, Encoding.UTF8);
        }
    }
}
