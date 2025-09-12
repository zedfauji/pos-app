using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace MagiDesk.Frontend.Services
{
    /// <summary>
    /// Safe dispatcher helper for marshaling operations to UI thread
    /// Prevents COM interop exceptions in WinUI 3 Desktop Apps
    /// Includes retry logic and comprehensive error handling
    /// </summary>
    public static class SafeDispatcher
    {
        private const int MaxRetryAttempts = 5; // Increased for better reliability
        private const int BaseRetryDelayMs = 100; // Increased base delay
        private static readonly ComprehensiveTracingService _tracing = ComprehensiveTracingService.Instance;

        /// <summary>
        /// Execute action on UI thread safely with COM exception hardening
        /// </summary>
        public static async Task RunOnUIThreadAsync(Action action, CancellationToken cancellationToken = default)
        {
            await RunOnUIThreadWithRetryAsync(async () =>
            {
                action();
                return Task.CompletedTask;
            }, cancellationToken);
        }

        /// <summary>
        /// Execute function on UI thread safely with COM exception hardening
        /// </summary>
        public static async Task<T> RunOnUIThreadAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            return await RunOnUIThreadWithRetryAsync(async () =>
            {
                return func();
            }, cancellationToken);
        }

        /// <summary>
        /// Execute async action on UI thread safely with COM exception hardening
        /// </summary>
        public static async Task RunOnUIThreadAsync(Func<Task> asyncAction, CancellationToken cancellationToken = default)
        {
            await RunOnUIThreadWithRetryAsync<object>(async () =>
            {
                await asyncAction();
                return Task.FromResult<object>(null!);
            }, cancellationToken);
        }

        /// <summary>
        /// Execute async function on UI thread safely with COM exception hardening
        /// </summary>
        public static async Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default)
        {
            return await RunOnUIThreadWithRetryAsync(asyncFunc, cancellationToken);
        }

        /// <summary>
        /// Core method with retry logic and COM exception handling
        /// </summary>
        private static async Task<T> RunOnUIThreadWithRetryAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default)
        {
            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    return await ExecuteOnUIThreadAsync(asyncFunc, cancellationToken);
                }
                catch (COMException comEx) when (IsRecoverableCOMException(comEx))
                {
                    _tracing.LogCOMException("SafeDispatcher", comEx.HResult, $"COM exception on attempt {attempt + 1}: {comEx.Message}", comEx);
                    
                    if (attempt == MaxRetryAttempts - 1)
                    {
                        _tracing.LogTrace("SafeDispatcher", "MaxRetriesExceeded", $"Failed after {MaxRetryAttempts} attempts");
                        throw new InvalidOperationException($"UI thread operation failed after {MaxRetryAttempts} attempts due to COM exceptions", comEx);
                    }
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    _tracing.LogTrace("SafeDispatcher", "Retrying", $"Retrying in {delay}ms (attempt {attempt + 1})");
                    await Task.Delay(delay, cancellationToken);
                }
                catch (UnauthorizedAccessException authEx)
                {
                    _tracing.LogTrace("SafeDispatcher", "UnauthorizedAccess", $"Access denied: {authEx.Message}", authEx);
                    throw new InvalidOperationException("UI thread operation failed due to access restrictions", authEx);
                }
                catch (Exception ex)
                {
                    _tracing.LogTrace("SafeDispatcher", "UnexpectedError", $"Unexpected error on attempt {attempt + 1}: {ex.Message}", ex);
                    if (attempt == MaxRetryAttempts - 1) throw;
                    
                    var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new InvalidOperationException($"UI thread operation failed after {MaxRetryAttempts} attempts");
        }

        /// <summary>
        /// Execute the actual UI thread operation with proper cancellation handling
        /// </summary>
        private static async Task<T> ExecuteOnUIThreadAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default)
        {
            var dispatcher = GetDispatcher();
            if (dispatcher == null)
            {
                throw new InvalidOperationException("No dispatcher available - main window may not be initialized");
            }

            if (dispatcher.HasThreadAccess)
            {
                _tracing.LogTrace("SafeDispatcher", "ExecuteOnUIThread", "Already on UI thread");
                return await asyncFunc();
            }

            var tcs = new TaskCompletionSource<T>();
            
            // Create a cancellation token registration to handle cancellation
            using var ctsRegistration = cancellationToken.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
            });

            var success = dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    // Check if operation was cancelled before execution
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    _tracing.LogTrace("SafeDispatcher", "ExecuteOnUIThread", "Executing on UI thread via TryEnqueue");
                    var result = await asyncFunc();
                    
                    // Check if operation was cancelled during execution
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _tracing.LogTrace("SafeDispatcher", "ExecuteOnUIThread_Cancelled", "Operation cancelled");
                    tcs.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    _tracing.LogTrace("SafeDispatcher", "ExecuteOnUIThread_Error", $"Error in UI thread operation: {ex.Message}", ex);
                    tcs.TrySetException(ex);
                }
            });

            if (!success)
            {
                throw new InvalidOperationException("Failed to enqueue operation to UI thread - dispatcher may be shutting down");
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Check if current thread is UI thread
        /// </summary>
        public static bool IsUIThread()
        {
            try
            {
                var dispatcher = GetDispatcher();
                return dispatcher?.HasThreadAccess ?? false;
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeDispatcher", "IsUIThread_Error", $"Error checking UI thread: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Safely add element to UI collection with COM exception handling
        /// </summary>
        public static async Task<bool> SafeAddToCollectionAsync<T>(IList<T> collection, T element, CancellationToken cancellationToken = default) where T : class
        {
            return await RunOnUIThreadAsync(() =>
            {
                try
                {
                    if (collection.Contains(element))
                    {
                        _tracing.LogTrace("SafeDispatcher", "SafeAddToCollection", "Element already in collection, skipping add");
                        return false;
                    }
                    
                    collection.Add(element);
                    _tracing.LogTrace("SafeDispatcher", "SafeAddToCollection", $"Successfully added {typeof(T).Name} to collection");
                    return true;
                }
                catch (COMException comEx)
                {
                    _tracing.LogCOMException("SafeDispatcher.SafeAddToCollection", comEx.HResult, $"COM exception adding to collection: {comEx.Message}", comEx);
                    return false; // Return false instead of throwing
                }
                catch (Exception ex)
                {
                    _tracing.LogTrace("SafeDispatcher", "SafeAddToCollection_Error", $"Error adding to collection: {ex.Message}", ex);
                    return false; // Return false instead of throwing
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Safely remove element from UI collection with COM exception handling
        /// </summary>
        public static async Task<bool> SafeRemoveFromCollectionAsync<T>(IList<T> collection, T element, CancellationToken cancellationToken = default) where T : class
        {
            return await RunOnUIThreadAsync(() =>
            {
                try
                {
                    if (!collection.Contains(element))
                    {
                        _tracing.LogTrace("SafeDispatcher", "SafeRemoveFromCollection", "Element not in collection, skipping remove");
                        return false;
                    }
                    
                    collection.Remove(element);
                    _tracing.LogTrace("SafeDispatcher", "SafeRemoveFromCollection", $"Successfully removed {typeof(T).Name} from collection");
                    return true;
                }
                catch (COMException comEx)
                {
                    _tracing.LogCOMException("SafeDispatcher.SafeRemoveFromCollection", comEx.HResult, $"COM exception removing from collection: {comEx.Message}", comEx);
                    return false; // Return false instead of throwing
                }
                catch (Exception ex)
                {
                    _tracing.LogTrace("SafeDispatcher", "SafeRemoveFromCollection_Error", $"Error removing from collection: {ex.Message}", ex);
                    return false; // Return false instead of throwing
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Safely set element visibility with COM exception handling
        /// </summary>
        public static async Task SafeSetVisibilityAsync(FrameworkElement element, Visibility visibility, CancellationToken cancellationToken = default)
        {
            await RunOnUIThreadAsync(() =>
            {
                try
                {
                    if (element.Visibility == visibility)
                    {
                        _tracing.LogTrace("SafeDispatcher", "SafeSetVisibility", $"Element already has visibility {visibility}, skipping");
                        return;
                    }
                    
                    element.Visibility = visibility;
                    _tracing.LogTrace("SafeDispatcher", "SafeSetVisibility", $"Successfully set visibility to {visibility}");
                }
                catch (COMException comEx)
                {
                    _tracing.LogCOMException("SafeDispatcher.SafeSetVisibility", comEx.HResult, $"COM exception setting visibility: {comEx.Message}", comEx);
                    // Don't throw, just log the error
                }
                catch (Exception ex)
                {
                    _tracing.LogTrace("SafeDispatcher", "SafeSetVisibility_Error", $"Error setting visibility: {ex.Message}", ex);
                    // Don't throw, just log the error
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Safely set DataContext with COM exception handling
        /// </summary>
        public static async Task SafeSetDataContextAsync(FrameworkElement element, object? dataContext, CancellationToken cancellationToken = default)
        {
            await RunOnUIThreadAsync(() =>
            {
                try
                {
                    element.DataContext = dataContext;
                    _tracing.LogTrace("SafeDispatcher", "SafeSetDataContext", $"Successfully set DataContext for {element.GetType().Name}");
                }
                catch (COMException comEx)
                {
                    _tracing.LogCOMException("SafeDispatcher.SafeSetDataContext", comEx.HResult, $"COM exception setting DataContext: {comEx.Message}", comEx);
                    // Don't throw, just log the error
                }
                catch (Exception ex)
                {
                    _tracing.LogTrace("SafeDispatcher", "SafeSetDataContext_Error", $"Error setting DataContext: {ex.Message}", ex);
                    // Don't throw, just log the error
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Check if COM exception is recoverable (can be retried)
        /// </summary>
        private static bool IsRecoverableCOMException(COMException ex)
        {
            // Common recoverable COM exceptions
            return ex.HResult == unchecked((int)0x800F1000) || // No installed components were detected
                   ex.HResult == unchecked((int)0x80070005) || // Access denied
                   ex.HResult == unchecked((int)0x8007000E) || // Not enough memory
                   ex.HResult == unchecked((int)0x80070015) || // The device is not ready
                   ex.Message.Contains("No installed components were detected") ||
                   ex.Message.Contains("Access denied") ||
                   ex.Message.Contains("Not enough memory");
        }

        /// <summary>
        /// Get the current dispatcher with error handling
        /// </summary>
        private static DispatcherQueue? GetDispatcher()
        {
            try
            {
                return App.MainWindow?.DispatcherQueue;
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeDispatcher", "GetDispatcher_Error", $"Error getting dispatcher: {ex.Message}", ex);
                return null;
            }
        }
    }
}
