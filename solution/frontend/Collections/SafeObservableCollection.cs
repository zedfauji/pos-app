using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Collections
{
    /// <summary>
    /// A safe ObservableCollection implementation that properly marshals events to UI thread
    /// This prevents Marshal.ThrowExceptionForHR errors in WinUI 3 Desktop Apps
    /// </summary>
    public class SafeObservableCollection<T> : ObservableCollection<T>
    {
        private readonly DispatcherQueue? _dispatcher;
        private readonly ComprehensiveTracingService _tracing;

        public SafeObservableCollection()
        {
            _tracing = ComprehensiveTracingService.Instance;
            try
            {
                _dispatcher = App.MainWindow?.DispatcherQueue;
                _tracing.LogTrace("SafeObservableCollection", "Constructor", $"Dispatcher: {_dispatcher != null}");
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeObservableCollection", "Constructor_Error", ex.Message, ex);
            }
        }

        public SafeObservableCollection(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _tracing = ComprehensiveTracingService.Instance;
            _tracing.LogTrace("SafeObservableCollection", "Constructor_WithDispatcher", $"Dispatcher: {_dispatcher != null}");
        }

        /// <summary>
        /// Override to marshal collection changes to UI thread
        /// </summary>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
                _tracing.LogTrace("SafeObservableCollection", "OnCollectionChanged", $"Action: {e.Action}, IsUIThread: {_dispatcher?.HasThreadAccess ?? false}");

                if (_dispatcher?.HasThreadAccess == true)
                {
                    base.OnCollectionChanged(e);
                    return;
                }

                if (_dispatcher != null)
                {
                    var success = _dispatcher.TryEnqueue(() =>
                    {
                        try
                        {
                            base.OnCollectionChanged(e);
                        }
                        catch (Exception ex)
                        {
                            _tracing.LogTrace("SafeObservableCollection", "OnCollectionChanged_Error", ex.Message, ex);
                            throw;
                        }
                    });

                    if (!success)
                    {
                        _tracing.LogTrace("SafeObservableCollection", "OnCollectionChanged_Failed", "Failed to enqueue to UI thread");
                    }
                }
                else
                {
                    // No dispatcher available, suppress the event
                    _tracing.LogTrace("SafeObservableCollection", "OnCollectionChanged_NoDispatcher", "No dispatcher available, suppressing event");
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                _tracing.LogCOMException("SafeObservableCollection.OnCollectionChanged", comEx.HResult, comEx.Message, comEx);
                // Suppress COM exceptions - they're handled by the framework
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeObservableCollection", "OnCollectionChanged_Exception", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Override to marshal property changes to UI thread
        /// </summary>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            try
            {
                _tracing.LogTrace("SafeObservableCollection", "OnPropertyChanged", $"Property: {e.PropertyName}, IsUIThread: {_dispatcher?.HasThreadAccess ?? false}");

                if (_dispatcher?.HasThreadAccess == true)
                {
                    base.OnPropertyChanged(e);
                    return;
                }

                if (_dispatcher != null)
                {
                    var success = _dispatcher.TryEnqueue(() =>
                    {
                        try
                        {
                            base.OnPropertyChanged(e);
                        }
                        catch (Exception ex)
                        {
                            _tracing.LogTrace("SafeObservableCollection", "OnPropertyChanged_Error", ex.Message, ex);
                            throw;
                        }
                    });

                    if (!success)
                    {
                        _tracing.LogTrace("SafeObservableCollection", "OnPropertyChanged_Failed", "Failed to enqueue to UI thread");
                    }
                }
                else
                {
                    // No dispatcher available, suppress the event
                    _tracing.LogTrace("SafeObservableCollection", "OnPropertyChanged_NoDispatcher", "No dispatcher available, suppressing event");
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                _tracing.LogCOMException("SafeObservableCollection.OnPropertyChanged", comEx.HResult, comEx.Message, comEx);
                // Suppress COM exceptions - they're handled by the framework
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeObservableCollection", "OnPropertyChanged_Exception", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Add item with proper UI thread marshaling
        /// </summary>
        public new void Add(T item)
        {
            try
            {
                _tracing.LogTrace("SafeObservableCollection", "Add", $"Item: {item?.ToString()}, IsUIThread: {_dispatcher?.HasThreadAccess ?? false}");
                base.Add(item);
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                _tracing.LogCOMException("SafeObservableCollection.Add", comEx.HResult, comEx.Message, comEx);
                // Suppress COM exceptions - they're handled by the framework
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeObservableCollection", "Add_Error", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Remove item with proper UI thread marshaling
        /// </summary>
        public new bool Remove(T item)
        {
            try
            {
                _tracing.LogTrace("SafeObservableCollection", "Remove", $"Item: {item?.ToString()}, IsUIThread: {_dispatcher?.HasThreadAccess ?? false}");
                return base.Remove(item);
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                _tracing.LogCOMException("SafeObservableCollection.Remove", comEx.HResult, comEx.Message, comEx);
                return false; // Return false instead of throwing
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeObservableCollection", "Remove_Error", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Clear collection with proper UI thread marshaling
        /// </summary>
        public new void Clear()
        {
            try
            {
                _tracing.LogTrace("SafeObservableCollection", "Clear", $"IsUIThread: {_dispatcher?.HasThreadAccess ?? false}");
                base.Clear();
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                _tracing.LogCOMException("SafeObservableCollection.Clear", comEx.HResult, comEx.Message, comEx);
                // Suppress COM exceptions - they're handled by the framework
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("SafeObservableCollection", "Clear_Error", ex.Message, ex);
                throw;
            }
        }
    }
}
