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
        if (exception != null)
        {
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
