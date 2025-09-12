using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace MagiDesk.Frontend.Services
{
    /// <summary>
    /// Comprehensive tracing service for debugging COM interop issues and pane operations
    /// </summary>
    public sealed class ComprehensiveTracingService
    {
        private static readonly Lazy<ComprehensiveTracingService> _instance = new(() => new ComprehensiveTracingService());
        public static ComprehensiveTracingService Instance => _instance.Value;

        private readonly ConcurrentQueue<TraceEntry> _traceEntries = new();
        private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
        private readonly object _lock = new object();
        private bool _isEnabled = true;
        private string _traceFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "pane-trace.log");
        private const long MaxLogFileSize = 10 * 1024 * 1024; // 10MB
        private const int MaxRetryAttempts = 5;
        private const int BaseRetryDelayMs = 50;

        private ComprehensiveTracingService()
        {
            // Ensure directory exists
            SafeFileOperations.EnsureDirectoryExists(Path.GetDirectoryName(_traceFilePath)!);
        }

        public void EnableTracing(bool enabled = true)
        {
            _isEnabled = enabled;
        }

        public void LogTrace(string method, string operation, object? data = null, Exception? exception = null)
        {
            if (!_isEnabled) return;

            var entry = new TraceEntry
            {
                Timestamp = DateTime.Now,
                ThreadId = Environment.CurrentManagedThreadId,
                Method = method,
                Operation = operation,
                Data = data?.ToString(),
                Exception = exception?.ToString(),
                StackTrace = Environment.StackTrace
            };

            _traceEntries.Enqueue(entry);

            // Flush to file periodically
            if (_traceEntries.Count > 100)
            {
                _ = Task.Run(FlushToFile);
            }
        }

        public void LogCOMException(string method, int hResult, string message, Exception? innerException = null)
        {
            LogTrace(method, "COM_EXCEPTION", $"HRESULT: 0x{hResult:X8}, Message: {message}", innerException);
        }

        public void LogUIThreadOperation(string method, string operation, bool isUIThread)
        {
            LogTrace(method, "UI_THREAD_OP", $"Operation: {operation}, IsUIThread: {isUIThread}");
        }

        public void LogPaneStateChange(string method, string paneId, string fromState, string toState)
        {
            LogTrace(method, "PANE_STATE_CHANGE", $"PaneId: {paneId}, From: {fromState}, To: {toState}");
        }

        /// <summary>
        /// Flushes trace entries to file with retry logic and exponential backoff
        /// </summary>
        public async Task FlushToFile()
        {
            if (_traceEntries.IsEmpty) return;

            // Ensure only one flush operation at a time
            await _flushSemaphore.WaitAsync();
            try
            {
                var entries = new List<TraceEntry>();
                while (_traceEntries.TryDequeue(out var entry))
                {
                    entries.Add(entry);
                }

                if (entries.Count == 0) return;

                var sb = new StringBuilder();
                foreach (var entry in entries)
                {
                    sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.ThreadId:D3}] {entry.Method}.{entry.Operation}");
                    if (!string.IsNullOrEmpty(entry.Data))
                    {
                        sb.AppendLine($"  Data: {entry.Data}");
                    }
                    if (!string.IsNullOrEmpty(entry.Exception))
                    {
                        sb.AppendLine($"  Exception: {entry.Exception}");
                    }
                    sb.AppendLine();
                }

                // Check if log rotation is needed
                await RotateLogIfNeeded(_traceFilePath);

                await SafeFileOperations.SafeWriteAsync(_traceFilePath, sb.ToString());
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }

        /// <summary>
        /// Rotates log file if it exceeds maximum size
        /// </summary>
        private async Task RotateLogIfNeeded(string path)
        {
            try
            {
                await SafeFileOperations.RotateFileIfNeededAsync(path, MaxLogFileSize);
            }
            catch (Exception ex)
            {
                LogTrace("RotateLogIfNeeded", "ROTATION_FAILED", $"Path: {path}", ex);
            }
        }

        public string GetTraceFilePath() => _traceFilePath;

        private class TraceEntry
        {
            public DateTime Timestamp { get; set; }
            public int ThreadId { get; set; }
            public string Method { get; set; } = string.Empty;
            public string Operation { get; set; } = string.Empty;
            public string? Data { get; set; }
            public string? Exception { get; set; }
            public string StackTrace { get; set; } = string.Empty;
        }
    }
}
