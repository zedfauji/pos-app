using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Services
{
    /// <summary>
    /// Safe file operations utility with retry logic, exponential backoff, and proper error handling
    /// </summary>
    public static class SafeFileOperations
    {
        private const int MaxRetryAttempts = 5;
        private const int BaseRetryDelayMs = 50;
        private const long DefaultMaxFileSize = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Safely writes content to file with retry logic, exponential backoff, and proper error handling
        /// </summary>
        /// <param name="path">File path to write to</param>
        /// <param name="content">Content to write</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        public static async Task SafeWriteAsync(string path, string content, CancellationToken ct = default, int maxRetries = MaxRetryAttempts)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Ensure directory exists
                    EnsureDirectoryExists(Path.GetDirectoryName(path)!);

                    // Use FileShare.ReadWrite to allow concurrent access
                    using var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                    using var writer = new StreamWriter(fileStream, Encoding.UTF8);
                    
                    await writer.WriteAsync(content);
                    await writer.FlushAsync();
                    await fileStream.FlushAsync(ct);
                    
                    return; // Success
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    // File is locked by another process
                    if (attempt == maxRetries - 1)
                    {
                        throw new InvalidOperationException($"File write failed after {maxRetries} attempts due to file lock: {path}", ex);
                    }
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
                    await Task.Delay(delay, ct);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied to file: {path}", ex);
                }
                catch (DirectoryNotFoundException ex)
                {
                    try
                    {
                        EnsureDirectoryExists(Path.GetDirectoryName(path)!);
                        // Retry once after creating directory
                        if (attempt == 0) continue;
                    }
                    catch (Exception createEx)
                    {
                        throw new InvalidOperationException($"Failed to create directory for file: {path}", createEx);
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries - 1) throw; // Give up after max retries
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
            }
        }

        /// <summary>
        /// Safely writes bytes to file with retry logic
        /// </summary>
        /// <param name="path">File path to write to</param>
        /// <param name="bytes">Bytes to write</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        public static async Task SafeWriteBytesAsync(string path, byte[] bytes, CancellationToken ct = default, int maxRetries = MaxRetryAttempts)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Ensure directory exists
                    EnsureDirectoryExists(Path.GetDirectoryName(path)!);

                    // Use FileShare.ReadWrite to allow concurrent access
                    using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                    await fileStream.WriteAsync(bytes, 0, bytes.Length, ct);
                    await fileStream.FlushAsync(ct);
                    
                    return; // Success
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    if (attempt == maxRetries - 1)
                    {
                        throw new InvalidOperationException($"File write failed after {maxRetries} attempts due to file lock: {path}", ex);
                    }
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied to file: {path}", ex);
                }
                catch (DirectoryNotFoundException ex)
                {
                    try
                    {
                        EnsureDirectoryExists(Path.GetDirectoryName(path)!);
                        if (attempt == 0) continue;
                    }
                    catch (Exception createEx)
                    {
                        throw new InvalidOperationException($"Failed to create directory for file: {path}", createEx);
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries - 1) throw;
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
            }
        }

        /// <summary>
        /// Safely reads text from file with retry logic
        /// </summary>
        /// <param name="path">File path to read from</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        public static async Task<string> SafeReadTextAsync(string path, CancellationToken ct = default, int maxRetries = MaxRetryAttempts)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                    using var reader = new StreamReader(fileStream, Encoding.UTF8);
                    
                    return await reader.ReadToEndAsync();
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    if (attempt == maxRetries - 1)
                    {
                        throw new InvalidOperationException($"File read failed after {maxRetries} attempts due to file lock: {path}", ex);
                    }
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
                catch (FileNotFoundException)
                {
                    throw; // Don't retry if file doesn't exist
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied to file: {path}", ex);
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries - 1) throw;
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
            }

            throw new InvalidOperationException($"File read failed after {maxRetries} attempts: {path}");
        }

        /// <summary>
        /// Safely reads bytes from file with retry logic
        /// </summary>
        /// <param name="path">File path to read from</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        public static async Task<byte[]> SafeReadBytesAsync(string path, CancellationToken ct = default, int maxRetries = MaxRetryAttempts)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                    using var memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream, ct);
                    return memoryStream.ToArray();
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    if (attempt == maxRetries - 1)
                    {
                        throw new InvalidOperationException($"File read failed after {maxRetries} attempts due to file lock: {path}", ex);
                    }
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
                catch (FileNotFoundException)
                {
                    throw; // Don't retry if file doesn't exist
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied to file: {path}", ex);
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries - 1) throw;
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
            }

            throw new InvalidOperationException($"File read failed after {maxRetries} attempts: {path}");
        }

        /// <summary>
        /// Safely deletes file with retry logic
        /// </summary>
        /// <param name="path">File path to delete</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        public static async Task SafeDeleteAsync(string path, CancellationToken ct = default, int maxRetries = MaxRetryAttempts)
        {
            if (!File.Exists(path)) return;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    File.Delete(path);
                    return; // Success
                }
                catch (IOException ex) when (IsFileLocked(ex))
                {
                    if (attempt == maxRetries - 1)
                    {
                        throw new InvalidOperationException($"File delete failed after {maxRetries} attempts due to file lock: {path}", ex);
                    }
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied to delete file: {path}", ex);
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries - 1) throw;
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, ct);
                }
            }
        }

        /// <summary>
        /// Rotates file if it exceeds maximum size
        /// </summary>
        /// <param name="path">File path to rotate</param>
        /// <param name="maxSize">Maximum file size in bytes</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task RotateFileIfNeededAsync(string path, long maxSize = DefaultMaxFileSize, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(path)) return;

                var fileInfo = new FileInfo(path);
                if (fileInfo.Length <= maxSize) return;

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var extension = Path.GetExtension(path);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                var directory = Path.GetDirectoryName(path)!;
                var rotatedPath = Path.Combine(directory, $"{nameWithoutExtension}-{timestamp}{extension}");
                
                // Move current file to rotated name
                File.Move(path, rotatedPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"File rotation failed: {path}", ex);
            }
        }

        /// <summary>
        /// Ensures directory exists, creating it if necessary
        /// </summary>
        /// <param name="directoryPath">Directory path to ensure exists</param>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Checks if IOException is due to file being locked
        /// </summary>
        /// <param name="ex">IOException to check</param>
        /// <returns>True if the exception is due to file lock</returns>
        private static bool IsFileLocked(IOException ex)
        {
            // Common HRESULTs for file locked scenarios
            return ex.HResult == -2147024864 || // ERROR_SHARING_VIOLATION
                   ex.HResult == -2147024891 || // ERROR_LOCK_VIOLATION
                   ex.Message.Contains("being used by another process") ||
                   ex.Message.Contains("sharing violation") ||
                   ex.Message.Contains("lock violation");
        }
    }
}
