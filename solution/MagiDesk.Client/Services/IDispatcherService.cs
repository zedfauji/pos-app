using System;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services;

/// <summary>
/// Service abstraction for UI thread dispatching operations.
/// Eliminates ViewModel dependencies on Microsoft.UI.Xaml types.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Invokes an action on the UI thread synchronously.
    /// </summary>
    void InvokeOnUIThread(Action action);

    /// <summary>
    /// Invokes an async function on the UI thread.
    /// </summary>
    Task InvokeOnUIThreadAsync(Func<Task> func);

    /// <summary>
    /// Checks if the current thread is the UI thread.
    /// </summary>
    bool IsOnUIThread { get; }
}

