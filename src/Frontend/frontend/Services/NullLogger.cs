using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Null logger implementation for when DI is not available
/// </summary>
public class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    
    public bool IsEnabled(LogLevel logLevel) => true;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        System.Diagnostics.Debug.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        if (exception != null)
        {
            System.Diagnostics.Debug.WriteLine($"Exception: {exception}");
        }
    }
}

/// <summary>
/// Static factory for creating null loggers
/// </summary>
public static class NullLoggerFactory
{
    public static ILogger<T> Create<T>() => new NullLogger<T>();
}
