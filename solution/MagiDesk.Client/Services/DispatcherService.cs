using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services;

/// <summary>
/// Implementation of IDispatcherService that wraps WinUI 3 DispatcherQueue.
/// This service eliminates ViewModel dependencies on Microsoft.UI.Xaml types.
/// </summary>
public class DispatcherService : IDispatcherService
{
    private readonly Lazy<DispatcherQueue> _dispatcherQueue;

    public DispatcherService()
    {
        // Lazy initialization to avoid accessing Window before it's created
        // This allows DispatcherService to be registered in DI before OnLaunched
        _dispatcherQueue = new Lazy<DispatcherQueue>(() =>
        {
            // Try MainWindow first (preferred)
            var app = Microsoft.UI.Xaml.Application.Current as App;
            if (app?.MainWindow?.DispatcherQueue != null)
            {
                return app.MainWindow.DispatcherQueue;
            }

            // Fallback to Window.Current (works if there's an active window)
            var currentWindow = Microsoft.UI.Xaml.Window.Current;
            if (currentWindow?.DispatcherQueue != null)
            {
                return currentWindow.DispatcherQueue;
            }

            // Last resort: try to get from Application
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
            {
                return dispatcherQueue;
            }

            throw new InvalidOperationException(
                "DispatcherService could not obtain a DispatcherQueue. " +
                "Ensure the service is only used after a Window has been created and activated.");
        });
    }

    public bool IsOnUIThread
    {
        get
        {
            try
            {
                return _dispatcherQueue.Value.HasThreadAccess;
            }
            catch (InvalidOperationException)
            {
                // DispatcherQueue not available yet, assume we're on UI thread if we can get it from current thread
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                return dispatcherQueue?.HasThreadAccess ?? false;
            }
        }
    }

    public void InvokeOnUIThread(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (IsOnUIThread)
        {
            // Already on UI thread, execute directly
            action();
        }
        else
        {
            // Schedule on UI thread
            var enqueueResult = _dispatcherQueue.Value.TryEnqueue(
                DispatcherQueuePriority.Normal,
                () => action());

            if (!enqueueResult)
            {
                throw new InvalidOperationException(
                    "Failed to enqueue action on UI thread. " +
                    "The dispatcher queue may be shut down.");
            }
        }
    }

    public async Task InvokeOnUIThreadAsync(Func<Task> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        if (IsOnUIThread)
        {
            // Already on UI thread, execute directly
            await func();
        }
        else
        {
            // Schedule on UI thread and await completion
            var taskCompletionSource = new TaskCompletionSource<bool>();

            var enqueueResult = _dispatcherQueue.Value.TryEnqueue(
                DispatcherQueuePriority.Normal,
                async () =>
                {
                    try
                    {
                        await func();
                        taskCompletionSource.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }
                });

            if (!enqueueResult)
            {
                throw new InvalidOperationException(
                    "Failed to enqueue async function on UI thread. " +
                    "The dispatcher queue may be shut down.");
            }

            await taskCompletionSource.Task;
        }
    }
}

