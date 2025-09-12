# Safe File Operations and Logging Improvements

## Overview

This document describes the comprehensive improvements made to file operations and logging in the MagiDesk application to handle file locking, IO exceptions, and ensure robust logging functionality.

## Problem Statement

The original `ComprehensiveTracingService.FlushToFile()` method was vulnerable to:
- `System.IO.IOException` when log files were in use by other processes
- No retry logic for transient failures
- No exponential backoff for file lock scenarios
- No log rotation to prevent unbounded growth
- Poor error handling and recovery

## Solution Architecture

### 1. SafeFileOperations Utility Class

A new static utility class `SafeFileOperations` provides thread-safe, retry-enabled file operations:

```csharp
public static class SafeFileOperations
{
    // Core methods with retry logic and exponential backoff
    public static async Task SafeWriteAsync(string path, string content, CancellationToken ct = default, int maxRetries = 5)
    public static async Task SafeWriteBytesAsync(string path, byte[] bytes, CancellationToken ct = default, int maxRetries = 5)
    public static async Task<string> SafeReadTextAsync(string path, CancellationToken ct = default, int maxRetries = 5)
    public static async Task<byte[]> SafeReadBytesAsync(string path, CancellationToken ct = default, int maxRetries = 5)
    public static async Task SafeDeleteAsync(string path, CancellationToken ct = default, int maxRetries = 5)
    public static async Task RotateFileIfNeededAsync(string path, long maxSize = 10MB, CancellationToken ct = default)
    public static void EnsureDirectoryExists(string directoryPath)
}
```

### 2. Key Features

#### Retry Logic with Exponential Backoff
- **Max Retries**: 5 attempts by default
- **Base Delay**: 50ms initial delay
- **Exponential Backoff**: Delay doubles with each retry (50ms, 100ms, 200ms, 400ms, 800ms)
- **Smart Retry**: Only retries on file lock scenarios, not on permanent failures

#### File Sharing and Concurrency
- **FileShare.ReadWrite**: Allows concurrent access to log files
- **Async Operations**: All operations are truly asynchronous
- **Buffer Optimization**: 4KB buffer size for optimal performance

#### Error Handling
- **IOException**: Detects file lock scenarios and retries
- **UnauthorizedAccessException**: Fails fast on permission issues
- **DirectoryNotFoundException**: Auto-creates directories and retries once
- **Generic Exceptions**: Retries with exponential backoff

#### Log Rotation
- **Size Limit**: 10MB default maximum file size
- **Automatic Rotation**: Files are rotated when size limit is exceeded
- **Timestamped Names**: Rotated files include timestamp (e.g., `pane-trace-20241201_143022.log`)
- **Safe Rotation**: Uses `File.Move()` for atomic rotation

### 3. ComprehensiveTracingService Improvements

#### Thread Safety
- **SemaphoreSlim**: Ensures only one flush operation at a time
- **ConcurrentQueue**: Thread-safe trace entry collection
- **Atomic Operations**: All state changes are atomic

#### Enhanced Logging
- **Detailed Tracing**: Logs all file operations with context
- **Error Recovery**: Graceful handling of all IO exceptions
- **Performance Monitoring**: Tracks retry attempts and delays

## Implementation Details

### File Lock Detection

The system detects file lock scenarios using multiple indicators:

```csharp
private static bool IsFileLocked(IOException ex)
{
    return ex.HResult == -2147024864 || // ERROR_SHARING_VIOLATION
           ex.HResult == -2147024891 || // ERROR_LOCK_VIOLATION
           ex.Message.Contains("being used by another process") ||
           ex.Message.Contains("sharing violation") ||
           ex.Message.Contains("lock violation");
}
```

### Retry Strategy

```csharp
for (int attempt = 0; attempt < maxRetries; attempt++)
{
    try
    {
        // File operation
        return; // Success
    }
    catch (IOException ex) when (IsFileLocked(ex))
    {
        if (attempt == maxRetries - 1) throw; // Give up after max retries
        
        var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
        await Task.Delay(delay, ct);
    }
    // ... other exception handling
}
```

### Log Rotation Logic

```csharp
public static async Task RotateFileIfNeededAsync(string path, long maxSize = DefaultMaxFileSize, CancellationToken ct = default)
{
    if (!File.Exists(path)) return;

    var fileInfo = new FileInfo(path);
    if (fileInfo.Length <= maxSize) return;

    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var extension = Path.GetExtension(path);
    var nameWithoutExtension = Path.GetFileNameWithoutExtension(path);
    var directory = Path.GetDirectoryName(path)!;
    var rotatedPath = Path.Combine(directory, $"{nameWithoutExtension}-{timestamp}{extension}");
    
    File.Move(path, rotatedPath); // Atomic operation
}
```

## Usage Examples

### Basic File Writing

```csharp
// Safe text writing with automatic retry
await SafeFileOperations.SafeWriteAsync("log.txt", "Log entry");

// Safe binary writing
await SafeFileOperations.SafeWriteBytesAsync("data.bin", bytes);

// Safe reading
var content = await SafeFileOperations.SafeReadTextAsync("config.json");
```

### With Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await SafeFileOperations.SafeWriteAsync("log.txt", content, cts.Token);
```

### Custom Retry Count

```csharp
// Use only 3 retries instead of default 5
await SafeFileOperations.SafeWriteAsync("log.txt", content, maxRetries: 3);
```

### Log Rotation

```csharp
// Rotate file if it exceeds 5MB
await SafeFileOperations.RotateFileIfNeededAsync("log.txt", 5 * 1024 * 1024);
```

## Testing

### Unit Tests

Comprehensive unit tests cover:
- **Basic Operations**: Write, read, delete functionality
- **Directory Creation**: Auto-creation of missing directories
- **File Rotation**: Size-based rotation logic
- **Error Handling**: Various exception scenarios
- **Cancellation**: CancellationToken support
- **Concurrency**: Thread-safe operations

### Test Scenarios

1. **Normal Operations**: Successful file operations
2. **Directory Creation**: Missing directory handling
3. **File Rotation**: Size-based rotation
4. **Cancellation**: Operation cancellation
5. **Error Recovery**: Exception handling and retry logic

## Performance Considerations

### Memory Usage
- **Streaming Operations**: Uses `FileStream` with 4KB buffers
- **Async Operations**: Non-blocking I/O operations
- **Resource Disposal**: Proper `using` statements for all streams

### Disk I/O
- **Buffer Size**: 4KB buffers for optimal performance
- **File Sharing**: Allows concurrent access without blocking
- **Atomic Operations**: File rotation uses `File.Move()` for atomicity

### Network Considerations
- **Local Operations**: All operations are local file system
- **No Network I/O**: No network dependencies
- **Retry Logic**: Optimized for local file system characteristics

## Error Scenarios and Handling

### File Lock Scenarios
- **Multiple Processes**: Log files accessed by multiple processes
- **Antivirus Scanning**: Files locked during antivirus scans
- **Backup Operations**: Files locked during backup operations
- **Solution**: Retry with exponential backoff

### Permission Issues
- **Access Denied**: Insufficient permissions
- **Read-Only Files**: Attempting to write to read-only files
- **Solution**: Fail fast with clear error messages

### Disk Space Issues
- **Disk Full**: Insufficient disk space
- **Quota Exceeded**: User quota exceeded
- **Solution**: Retry with exponential backoff, fail gracefully

### Directory Issues
- **Missing Directories**: Parent directories don't exist
- **Invalid Paths**: Malformed file paths
- **Solution**: Auto-create directories, validate paths

## Monitoring and Debugging

### Logging
- **Operation Tracing**: All file operations are logged
- **Error Details**: Full exception information with HRESULT
- **Performance Metrics**: Retry counts and delays
- **File Rotation**: Rotation events and file sizes

### Debugging Tools
- **Trace Files**: Comprehensive trace logs
- **Error Codes**: HRESULT values for COM exceptions
- **Stack Traces**: Full stack trace information
- **Context Information**: Thread IDs, timestamps, operation context

## Migration Guide

### From Old File Operations

**Before:**
```csharp
File.WriteAllText(path, content);
File.AppendAllText(path, content);
File.ReadAllText(path);
```

**After:**
```csharp
await SafeFileOperations.SafeWriteAsync(path, content);
await SafeFileOperations.SafeWriteAsync(path, content); // Append mode
var content = await SafeFileOperations.SafeReadTextAsync(path);
```

### From Old Logging

**Before:**
```csharp
await File.AppendAllTextAsync(_traceFilePath, sb.ToString());
```

**After:**
```csharp
await SafeFileOperations.SafeWriteAsync(_traceFilePath, sb.ToString());
```

## Best Practices

### File Operations
1. **Always Use SafeFileOperations**: For all file I/O operations
2. **Handle Cancellation**: Use CancellationToken for long-running operations
3. **Monitor Log Sizes**: Implement log rotation for long-running applications
4. **Error Handling**: Always handle exceptions appropriately

### Logging
1. **Structured Logging**: Use consistent log formats
2. **Log Levels**: Implement appropriate log levels
3. **Performance Monitoring**: Monitor log file sizes and rotation
4. **Error Recovery**: Implement graceful error recovery

### Testing
1. **Unit Tests**: Test all file operations
2. **Integration Tests**: Test with actual file system
3. **Error Scenarios**: Test error handling and recovery
4. **Performance Tests**: Test with large files and high concurrency

## Future Improvements

### Planned Enhancements
1. **Compression**: Add log file compression for rotated files
2. **Encryption**: Add encryption for sensitive log data
3. **Remote Logging**: Add remote logging capabilities
4. **Metrics**: Add performance metrics and monitoring

### Performance Optimizations
1. **Batching**: Batch multiple log entries for better performance
2. **Async Flushing**: Implement background log flushing
3. **Memory Mapping**: Use memory-mapped files for large operations
4. **Caching**: Implement file operation caching

## Conclusion

The SafeFileOperations utility and ComprehensiveTracingService improvements provide:

- **Robust Error Handling**: Comprehensive exception handling with retry logic
- **Thread Safety**: Safe concurrent access to log files
- **Performance**: Optimized I/O operations with proper buffering
- **Maintainability**: Clean, well-documented code with comprehensive tests
- **Reliability**: Handles all common file system scenarios gracefully

This implementation ensures that the MagiDesk application can handle file operations reliably in production environments with multiple processes, antivirus software, and various error conditions.
