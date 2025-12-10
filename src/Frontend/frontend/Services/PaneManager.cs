using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;

namespace MagiDesk.Frontend.Services
{
    /// <summary>
    /// Manages non-blocking panes for WinUI 3 Desktop Apps
    /// Ensures only one pane is active at a time with smooth animations
    /// Thread-safe with proper COM interop handling
    /// </summary>
    public sealed class PaneManager
    {
        private readonly ILogger<PaneManager> _logger;
        private readonly Dictionary<string, FrameworkElement> _panes = new();
        private readonly Dictionary<string, Storyboard> _animations = new();
        private FrameworkElement? _currentPane;
        private Grid? _overlayGrid;
        private Window? _mainWindow;
        
        // CRITICAL FIX: Add proper gating to prevent concurrent operations
        private readonly SemaphoreSlim _paneSemaphore = new(1, 1);
        private volatile int _state = (int)PaneState.Closed;
        private readonly ComprehensiveTracingService _tracing;
        private readonly Dictionary<string, TaskCompletionSource<bool>> _pendingOperations = new();

        public enum PaneState
        {
            Closed,
            Opening,
            Open,
            Closing
        }

        public PaneManager(ILogger<PaneManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tracing = ComprehensiveTracingService.Instance;
            _tracing.LogTrace("PaneManager", "Constructor", "PaneManager created");
        }

        /// <summary>
        /// Initialize the PaneManager with the main window
        /// </summary>
        public async Task InitializeAsync(Window mainWindow)
        {
            try
            {
                _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
                
                _logger.LogInformation("PaneManager initialized with main window");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PaneManager");
                throw;
            }
        }

        /// <summary>
        /// Register a pane with the manager
        /// </summary>
        public void RegisterPane(string paneId, FrameworkElement pane)
        {
            try
            {
                if (string.IsNullOrEmpty(paneId))
                    throw new ArgumentException("Pane ID cannot be null or empty", nameof(paneId));
                
                if (pane == null)
                    throw new ArgumentNullException(nameof(pane));

                _panes[paneId] = pane;
                _logger.LogInformation("Pane registered: {PaneId}", paneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register pane: {PaneId}", paneId);
                throw;
            }
        }

        /// <summary>
        /// Show a pane with smooth slide-in animation
        /// Thread-safe with proper gating to prevent concurrent operations
        /// </summary>
        public async Task ShowPaneAsync(string paneId, bool closeOthers = true, CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "ShowPaneAsync_Entry", $"PaneId: {paneId}, CloseOthers: {closeOthers}, CurrentState: {_state}");
            
            try
            {
                // Check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();
                
                // CRITICAL FIX: Check if already opening/open and return existing task
                if (_state == (int)PaneState.Opening || _state == (int)PaneState.Open)
                {
                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_AlreadyOpening", $"State: {_state}, Returning existing operation");
                    
                    if (_pendingOperations.TryGetValue("show", out var existingTcs))
                    {
                        await existingTcs.Task;
                        return;
                    }
                }

                // CRITICAL FIX: Use atomic compare-exchange to prevent race conditions
                var currentState = _state;
                if (Interlocked.CompareExchange(ref _state, (int)PaneState.Opening, currentState) != currentState)
                {
                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_RaceCondition", $"Failed to acquire opening state, current: {_state}");
                    return;
                }

                // Create task completion source for this operation
                var tcs = new TaskCompletionSource<bool>();
                _pendingOperations["show"] = tcs;

                await _paneSemaphore.WaitAsync(cancellationToken);
                try
                {
                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_SemaphoreAcquired", $"PaneId: {paneId}");

                    if (!_panes.TryGetValue(paneId, out var pane))
                    {
                        _tracing.LogTrace("PaneManager", "ShowPaneAsync_PaneNotFound", $"PaneId: {paneId}");
                        throw new InvalidOperationException($"Pane not found: {paneId}");
                    }

                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_PaneFound", $"PaneId: {paneId}, Type: {pane.GetType().Name}");

                    // Close other panes if requested
                    if (closeOthers && _currentPane != null && _currentPane != pane)
                    {
                        _tracing.LogTrace("PaneManager", "ShowPaneAsync_ClosingCurrent", "Closing current pane before showing new one");
                        await HideCurrentPaneInternalAsync(cancellationToken);
                    }

                    // Ensure overlay is set up
                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_EnsuringOverlay", "Setting up overlay");
                    await EnsureOverlayAsync(cancellationToken);

                    // Show the pane with proper UI thread marshaling
                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_ShowingPane", $"Starting animation for pane: {paneId}");
                    await ShowPaneWithAnimationAsync(pane, cancellationToken);
                    
                    _currentPane = pane;
                    _state = (int)PaneState.Open;

                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_Success", $"Pane shown successfully: {paneId}");
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    _tracing.LogTrace("PaneManager", "ShowPaneAsync_Error", ex.Message, ex);
                    _state = (int)PaneState.Closed;
                    tcs.SetException(ex);
                    throw;
                }
                finally
                {
                    _paneSemaphore.Release();
                    _pendingOperations.Remove("show");
                }
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "ShowPaneAsync_FatalError", ex.Message, ex);
                _state = (int)PaneState.Closed;
                throw;
            }
        }

        /// <summary>
        /// Hide the current pane with smooth slide-out animation
        /// </summary>
        public async Task HideCurrentPaneAsync(CancellationToken cancellationToken = default)
        {
            await HideCurrentPaneInternalAsync(cancellationToken);
        }

        /// <summary>
        /// Internal method to hide current pane (used by ShowPaneAsync)
        /// </summary>
        private async Task HideCurrentPaneInternalAsync(CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "HideCurrentPaneInternalAsync_Entry", $"CurrentPane: {_currentPane?.GetType().Name}, State: {_state}");
            
            try
            {
                if (_currentPane == null)
                {
                    _tracing.LogTrace("PaneManager", "HideCurrentPaneInternalAsync_NoCurrentPane", "No current pane to hide");
                    return;
                }

                _state = (int)PaneState.Closing;
                _tracing.LogTrace("PaneManager", "HideCurrentPaneInternalAsync_StartingHide", "Starting hide animation");

                await HidePaneWithAnimationAsync(_currentPane, cancellationToken);
                _currentPane = null;
                _state = (int)PaneState.Closed;

                // Hide overlay if no panes are active
                await HideOverlayAsync(cancellationToken);

                _tracing.LogTrace("PaneManager", "HideCurrentPaneInternalAsync_Success", "Current pane hidden successfully");
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "HideCurrentPaneInternalAsync_Error", ex.Message, ex);
                _state = (int)PaneState.Closed;
                throw;
            }
        }

        /// <summary>
        /// Hide a specific pane
        /// </summary>
        public async Task HidePaneAsync(string paneId)
        {
            try
            {
                if (!_panes.TryGetValue(paneId, out var pane))
                {
                    _logger.LogWarning("Pane not found for hiding: {PaneId}", paneId);
                    return;
                }

                if (_currentPane == pane)
                {
                    await HideCurrentPaneAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide pane: {PaneId}", paneId);
                throw;
            }
        }

        /// <summary>
        /// Check if a pane is currently visible
        /// </summary>
        public bool IsPaneVisible(string paneId)
        {
            return _panes.TryGetValue(paneId, out var pane) && _currentPane == pane;
        }

        /// <summary>
        /// Get a registered pane by ID
        /// </summary>
        public T? GetPane<T>(string paneId) where T : class
        {
            try
            {
                if (_panes.TryGetValue(paneId, out var pane))
                {
                    return pane as T;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pane: {PaneId}", paneId);
                return null;
            }
        }

        /// <summary>
        /// Get the currently visible pane ID
        /// </summary>
        public string? GetCurrentPaneId()
        {
            if (_currentPane == null) return null;

            foreach (var kvp in _panes)
            {
                if (kvp.Value == _currentPane)
                    return kvp.Key;
            }

            return null;
        }

        /// <summary>
        /// Ensure overlay grid is set up for dimming effect
        /// </summary>
        private async Task EnsureOverlayAsync(CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_Entry", $"OverlayGrid: {_overlayGrid != null}");
            
            try
            {
                if (_overlayGrid != null) return;

                // CRITICAL FIX: Use SafeDispatcher for UI operations
                await SafeDispatcher.RunOnUIThreadAsync(async () =>
                {
                    try
                    {
                        if (_overlayGrid != null) return;

                        if (_mainWindow?.Content is not FrameworkElement mainContent)
                        {
                            throw new InvalidOperationException("Main window content is not a FrameworkElement");
                        }

                        _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_CreatingOverlay", "Creating overlay grid");

                        // Create overlay grid
                        _overlayGrid = new Grid
                        {
                            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                Microsoft.UI.Colors.Black),
                            Opacity = 0.3,
                            Visibility = Visibility.Collapsed
                        };

                        // Add overlay to main content using SafeDispatcher
                        if (mainContent.Parent is Panel parentPanel)
                        {
                            _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_AddingToParent", "Adding overlay to parent panel");
                            await SafeDispatcher.SafeAddToCollectionAsync(parentPanel.Children, _overlayGrid, cancellationToken);
                        }
                        else
                        {
                            _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_CreatingWrapper", "Creating wrapper grid");
                            // Wrap main content in a grid
                            var wrapperGrid = new Grid();
                            await SafeDispatcher.SafeAddToCollectionAsync(wrapperGrid.Children, mainContent, cancellationToken);
                            await SafeDispatcher.SafeAddToCollectionAsync(wrapperGrid.Children, _overlayGrid, cancellationToken);
                            
                            if (_mainWindow.Content is Panel windowPanel)
                            {
                                await SafeDispatcher.RunOnUIThreadAsync(() =>
                                {
                                    try
                                    {
                                        windowPanel.Children.Clear();
                                        windowPanel.Children.Add(wrapperGrid);
                                    }
                                    catch (COMException comEx)
                                    {
                                        _tracing.LogCOMException("PaneManager.EnsureOverlayAsync", comEx.HResult, $"COM exception adding wrapper grid: {comEx.Message}", comEx);
                                        throw;
                                    }
                                    catch (Exception ex)
                                    {
                                        _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_Error", $"Error adding wrapper grid: {ex.Message}", ex);
                                        throw;
                                    }
                                }, cancellationToken);
                            }
                        }

                        _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_Success", "Overlay grid created and added");
                    }
                    catch (Exception ex)
                    {
                        _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_Error", ex.Message, ex);
                        throw;
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "EnsureOverlayAsync_FatalError", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Show overlay with fade-in effect using Composition APIs
        /// </summary>
        private async Task ShowOverlayAsync(CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "ShowOverlayAsync_Entry", $"OverlayGrid: {_overlayGrid != null}");
            
            try
            {
                if (_overlayGrid == null) return;

                await SafeDispatcher.RunOnUIThreadAsync(() =>
                {
                    try
                    {
                        _tracing.LogTrace("PaneManager", "ShowOverlayAsync_Showing", "Showing overlay");
                        _overlayGrid.Visibility = Visibility.Visible;
                        _overlayGrid.Opacity = 0.0;
                        
                        // Use simple timer-based animation to avoid COM interop issues
                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                        };
                        
                        var startTime = DateTime.Now;
                        var duration = TimeSpan.FromMilliseconds(200);
                        
                        timer.Tick += (s, e) =>
                        {
                            var elapsed = DateTime.Now - startTime;
                            var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                            
                            // Easing function (ease out)
                            var easedProgress = 1 - Math.Pow(1 - progress, 3);
                            
                            _overlayGrid.Opacity = easedProgress * 0.3;
                            
                            if (progress >= 1.0)
                            {
                                timer.Stop();
                                _overlayGrid.Opacity = 0.3;
                            }
                        };
                        
                        timer.Start();
                        
                        _tracing.LogTrace("PaneManager", "ShowOverlayAsync_Success", "Overlay shown with fade-in animation");
                    }
                    catch (Exception ex)
                    {
                        _tracing.LogTrace("PaneManager", "ShowOverlayAsync_Error", ex.Message, ex);
                        // Fallback: just set opacity directly
                        _overlayGrid.Opacity = 0.3;
                    }
                });

                // Wait for animation to complete
                await Task.Delay(200, cancellationToken);
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "ShowOverlayAsync_FatalError", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Hide overlay with fade-out effect using Composition APIs
        /// </summary>
        private async Task HideOverlayAsync(CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "HideOverlayAsync_Entry", $"OverlayGrid: {_overlayGrid != null}");
            
            try
            {
                if (_overlayGrid == null) return;

                await SafeDispatcher.RunOnUIThreadAsync(() =>
                {
                    try
                    {
                        _tracing.LogTrace("PaneManager", "HideOverlayAsync_Hiding", "Hiding overlay");
                        
                        // Use simple timer-based animation to avoid COM interop issues
                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                        };
                        
                        var startTime = DateTime.Now;
                        var duration = TimeSpan.FromMilliseconds(200);
                        
                        timer.Tick += (s, e) =>
                        {
                            var elapsed = DateTime.Now - startTime;
                            var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                            
                            // Easing function (ease in)
                            var easedProgress = Math.Pow(progress, 3);
                            
                            _overlayGrid.Opacity = 0.3 * (1 - easedProgress);
                            
                            if (progress >= 1.0)
                            {
                                timer.Stop();
                                _overlayGrid.Visibility = Visibility.Collapsed;
                            }
                        };
                        
                        timer.Start();
                        
                        _tracing.LogTrace("PaneManager", "HideOverlayAsync_Success", "Overlay hidden with fade-out animation");
                    }
                    catch (Exception ex)
                    {
                        _tracing.LogTrace("PaneManager", "HideOverlayAsync_Error", ex.Message, ex);
                        // Fallback: just hide directly
                        _overlayGrid.Visibility = Visibility.Collapsed;
                    }
                });

                // Wait for animation to complete
                await Task.Delay(200, cancellationToken);
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "HideOverlayAsync_FatalError", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Show pane with slide-in animation from right with COM exception hardening
        /// Ensures UI thread safety and proper pane initialization
        /// </summary>
        private async Task ShowPaneWithAnimationAsync(FrameworkElement pane, CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_Entry", $"Pane: {pane.GetType().Name}, Parent: {pane.Parent?.GetType().Name}");
            
            try
            {
                // Defensive null checks
                if (pane == null)
                {
                    throw new ArgumentNullException(nameof(pane), "Pane cannot be null");
                }

                if (_overlayGrid == null)
                {
                    throw new InvalidOperationException("Overlay grid not initialized - call EnsureOverlayAsync first");
                }

                // Verify pane is not already in visual tree
                if (pane.Parent != null)
                {
                    _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_AlreadyInTree", $"Pane already has parent: {pane.Parent.GetType().Name}");
                    // Remove from current parent if different
                    if (pane.Parent != _overlayGrid)
                    {
                        await SafeDispatcher.SafeRemoveFromCollectionAsync(((Panel)pane.Parent).Children, pane, cancellationToken);
                    }
                }

                // Show overlay first
                await ShowOverlayAsync(cancellationToken);

                // Ensure pane is fully initialized before adding to visual tree
                await EnsurePaneInitializedAsync(pane, cancellationToken);

                // Add pane to visual tree safely
                var added = await SafeDispatcher.SafeAddToCollectionAsync(_overlayGrid.Children, pane, cancellationToken);
                if (!added)
                {
                    _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_AlreadyAdded", "Pane already in collection, proceeding with animation");
                }

                // Set initial position and visibility safely
                await SafeDispatcher.RunOnUIThreadAsync(() =>
                {
                    try
                    {
                        _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_SettingUp", "Setting up pane for animation");
                        
                        // Set initial position (off-screen to the right)
                        var initialX = pane.ActualWidth > 0 ? pane.ActualWidth : 400;
                        pane.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform
                        {
                            X = initialX
                        };

                        pane.Visibility = Visibility.Visible;
                        _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_SetupComplete", $"Initial position set to X={initialX}");
                    }
                    catch (COMException comEx)
                    {
                        _tracing.LogCOMException("PaneManager.ShowPaneWithAnimationAsync_Setup", comEx.HResult, $"COM exception during setup: {comEx.Message}", comEx);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_SetupError", $"Error during setup: {ex.Message}", ex);
                        throw;
                    }
                }, cancellationToken);

                // Start animation safely
                await StartSlideInAnimationAsync(pane, cancellationToken);
                
                _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_Success", "Pane shown with slide-in animation");
            }
            catch (COMException comEx)
            {
                _tracing.LogCOMException("PaneManager.ShowPaneWithAnimationAsync", comEx.HResult, $"COM exception: {comEx.Message}", comEx);
                throw new InvalidOperationException($"Failed to show pane due to COM exception: {comEx.Message}", comEx);
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "ShowPaneWithAnimationAsync_FatalError", $"Fatal error: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Ensure pane is fully initialized before adding to visual tree
        /// </summary>
        private async Task EnsurePaneInitializedAsync(FrameworkElement pane, CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_Entry", $"Pane: {pane.GetType().Name}");
            
            try
            {
                // Wait for Loaded event if not already loaded
                if (!pane.IsLoaded)
                {
                    _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_WaitingForLoaded", "Waiting for pane Loaded event");
                    
                    var loadedTcs = new TaskCompletionSource();
                    RoutedEventHandler? loadedHandler = null;
                    
                    loadedHandler = (s, e) =>
                    {
                        try
                        {
                            loadedTcs.TrySetResult();
                        }
                        finally
                        {
                            pane.Loaded -= loadedHandler;
                        }
                    };
                    
                    pane.Loaded += loadedHandler;
                    
                    // Wait for loaded event with longer timeout (30 seconds)
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
                    
                    try
                    {
                        await loadedTcs.Task.WaitAsync(timeoutCts.Token);
                        _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_Loaded", "Pane Loaded event received");
                    }
                    catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        pane.Loaded -= loadedHandler;
                        _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_Timeout", "Timeout waiting for Loaded event");
                        throw new TimeoutException("Pane did not load within 30 second timeout period");
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        pane.Loaded -= loadedHandler;
                        _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_Cancelled", "Operation cancelled while waiting for Loaded event");
                        throw;
                    }
                }
                else
                {
                    _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_AlreadyLoaded", "Pane already loaded");
                }

                // Verify pane has valid dimensions
                if (pane.ActualWidth <= 0 || pane.ActualHeight <= 0)
                {
                    _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_InvalidDimensions", $"Invalid dimensions: {pane.ActualWidth}x{pane.ActualHeight}");
                    // Force measure if needed
                    pane.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    pane.Arrange(new Windows.Foundation.Rect(0, 0, pane.DesiredSize.Width, pane.DesiredSize.Height));
                }
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "EnsurePaneInitializedAsync_Error", $"Error ensuring pane initialization: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Start slide-in animation with COM exception handling
        /// </summary>
        private async Task StartSlideInAnimationAsync(FrameworkElement pane, CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "StartSlideInAnimationAsync_Entry", $"Pane: {pane.GetType().Name}");
            
            try
            {
                await SafeDispatcher.RunOnUIThreadAsync(() =>
                {
                    try
                    {
                        var initialX = pane.ActualWidth > 0 ? pane.ActualWidth : 400;
                        
                        // Use simple timer-based animation to avoid COM interop issues
                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                        };
                        
                        var startTime = DateTime.Now;
                        var duration = TimeSpan.FromMilliseconds(300);
                        
                        timer.Tick += (s, e) =>
                        {
                            try
                            {
                                var elapsed = DateTime.Now - startTime;
                                var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                                
                                // Easing function (ease out)
                                var easedProgress = 1 - Math.Pow(1 - progress, 3);
                                
                                var currentX = initialX * (1 - easedProgress);
                                if (pane.RenderTransform is Microsoft.UI.Xaml.Media.TranslateTransform transform)
                                {
                                    transform.X = currentX;
                                }
                                
                                if (progress >= 1.0)
                                {
                                    timer.Stop();
                                    _tracing.LogTrace("PaneManager", "StartSlideInAnimationAsync_Complete", "Slide-in animation completed");
                                }
                            }
                            catch (COMException comEx)
                            {
                                _tracing.LogCOMException("PaneManager.StartSlideInAnimationAsync_Tick", comEx.HResult, $"COM exception in animation tick: {comEx.Message}", comEx);
                                timer.Stop();
                            }
                            catch (Exception ex)
                            {
                                _tracing.LogTrace("PaneManager", "StartSlideInAnimationAsync_TickError", $"Error in animation tick: {ex.Message}", ex);
                                timer.Stop();
                            }
                        };
                        
                        timer.Start();
                        _tracing.LogTrace("PaneManager", "StartSlideInAnimationAsync_Started", "Slide-in animation started");
                    }
                    catch (COMException comEx)
                    {
                        _tracing.LogCOMException("PaneManager.StartSlideInAnimationAsync", comEx.HResult, $"COM exception starting animation: {comEx.Message}", comEx);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _tracing.LogTrace("PaneManager", "StartSlideInAnimationAsync_Error", $"Error starting animation: {ex.Message}", ex);
                        throw;
                    }
                }, cancellationToken);

                // Wait for animation to complete
                await Task.Delay(300, cancellationToken);
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "StartSlideInAnimationAsync_FatalError", $"Fatal error in animation: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Hide pane with slide-out animation to right with COM exception hardening
        /// </summary>
        private async Task HidePaneWithAnimationAsync(FrameworkElement pane, CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "HidePaneWithAnimationAsync_Entry", $"Pane: {pane.GetType().Name}");
            
            try
            {
                // Defensive null checks
                if (pane == null)
                {
                    throw new ArgumentNullException(nameof(pane), "Pane cannot be null");
                }

                await StartSlideOutAnimationAsync(pane, cancellationToken);

                // Remove pane from visual tree safely
                if (pane.Parent is Panel parentPanel)
                {
                    var removed = await SafeDispatcher.SafeRemoveFromCollectionAsync(parentPanel.Children, pane, cancellationToken);
                    if (removed)
                    {
                        _tracing.LogTrace("PaneManager", "HidePaneWithAnimationAsync_Removed", "Pane removed from visual tree");
                    }
                }

                // Hide overlay
                await HideOverlayAsync(cancellationToken);
                
                _tracing.LogTrace("PaneManager", "HidePaneWithAnimationAsync_Success", "Pane hidden with slide-out animation");
            }
            catch (COMException comEx)
            {
                _tracing.LogCOMException("PaneManager.HidePaneWithAnimationAsync", comEx.HResult, $"COM exception: {comEx.Message}", comEx);
                throw new InvalidOperationException($"Failed to hide pane due to COM exception: {comEx.Message}", comEx);
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "HidePaneWithAnimationAsync_FatalError", $"Fatal error: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Start slide-out animation with COM exception handling
        /// </summary>
        private async Task StartSlideOutAnimationAsync(FrameworkElement pane, CancellationToken cancellationToken = default)
        {
            _tracing.LogTrace("PaneManager", "StartSlideOutAnimationAsync_Entry", $"Pane: {pane.GetType().Name}");
            
            try
            {
                await SafeDispatcher.RunOnUIThreadAsync(() =>
                {
                    try
                    {
                        var targetX = pane.ActualWidth > 0 ? pane.ActualWidth : 400;
                        
                        // Use simple timer-based animation to avoid COM interop issues
                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                        };
                        
                        var startTime = DateTime.Now;
                        var duration = TimeSpan.FromMilliseconds(300);
                        
                        timer.Tick += (s, e) =>
                        {
                            try
                            {
                                var elapsed = DateTime.Now - startTime;
                                var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                                
                                // Easing function (ease in)
                                var easedProgress = Math.Pow(progress, 3);
                                
                                var currentX = targetX * easedProgress;
                                if (pane.RenderTransform is Microsoft.UI.Xaml.Media.TranslateTransform transform)
                                {
                                    transform.X = currentX;
                                }
                                
                                if (progress >= 1.0)
                                {
                                    timer.Stop();
                                    pane.Visibility = Visibility.Collapsed;
                                    _tracing.LogTrace("PaneManager", "StartSlideOutAnimationAsync_Complete", "Slide-out animation completed");
                                }
                            }
                            catch (COMException comEx)
                            {
                                _tracing.LogCOMException("PaneManager.StartSlideOutAnimationAsync_Tick", comEx.HResult, $"COM exception in animation tick: {comEx.Message}", comEx);
                                timer.Stop();
                            }
                            catch (Exception ex)
                            {
                                _tracing.LogTrace("PaneManager", "StartSlideOutAnimationAsync_TickError", $"Error in animation tick: {ex.Message}", ex);
                                timer.Stop();
                            }
                        };
                        
                        timer.Start();
                        _tracing.LogTrace("PaneManager", "StartSlideOutAnimationAsync_Started", "Slide-out animation started");
                    }
                    catch (COMException comEx)
                    {
                        _tracing.LogCOMException("PaneManager.StartSlideOutAnimationAsync", comEx.HResult, $"COM exception starting animation: {comEx.Message}", comEx);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _tracing.LogTrace("PaneManager", "StartSlideOutAnimationAsync_Error", $"Error starting animation: {ex.Message}", ex);
                        throw;
                    }
                }, cancellationToken);

                // Wait for animation to complete
                await Task.Delay(300, cancellationToken);
            }
            catch (Exception ex)
            {
                _tracing.LogTrace("PaneManager", "StartSlideOutAnimationAsync_FatalError", $"Fatal error in animation: {ex.Message}", ex);
                throw;
            }
        }
    }
}
