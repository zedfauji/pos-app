using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Services
{
    /// <summary>
    /// Manages non-blocking panes for WinUI 3 Desktop Apps
    /// Ensures only one pane is active at a time with smooth animations
    /// </summary>
    public sealed class PaneManager
    {
        private readonly ILogger<PaneManager> _logger;
        private readonly Dictionary<string, FrameworkElement> _panes = new();
        private readonly Dictionary<string, Storyboard> _animations = new();
        private FrameworkElement? _currentPane;
        private Grid? _overlayGrid;
        private Window? _mainWindow;

        public PaneManager(ILogger<PaneManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initialize the PaneManager with the main window
        /// </summary>
        public void Initialize(Window mainWindow)
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
        /// </summary>
        public async Task ShowPaneAsync(string paneId, bool closeOthers = true)
        {
            try
            {
                _logger.LogInformation("Showing pane: {PaneId}", paneId);

                if (!_panes.TryGetValue(paneId, out var pane))
                {
                    _logger.LogError("Pane not found: {PaneId}", paneId);
                    throw new InvalidOperationException($"Pane not found: {paneId}");
                }

                _logger.LogInformation("Pane found: {PaneId}, type: {PaneType}", paneId, pane.GetType().Name);

                // Close other panes if requested
                if (closeOthers && _currentPane != null && _currentPane != pane)
                {
                    _logger.LogInformation("Closing current pane before showing new one");
                    await HideCurrentPaneAsync();
                }

                // Ensure overlay is set up
                _logger.LogInformation("Ensuring overlay is set up");
                await EnsureOverlayAsync();
                _logger.LogInformation("Overlay setup completed");

                // Show the pane
                _logger.LogInformation("Starting pane animation");
                await ShowPaneWithAnimationAsync(pane);
                _currentPane = pane;

                _logger.LogInformation("Pane shown successfully: {PaneId}", paneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show pane: {PaneId}", paneId);
                throw;
            }
        }

        /// <summary>
        /// Hide the current pane with smooth slide-out animation
        /// </summary>
        public async Task HideCurrentPaneAsync()
        {
            try
            {
                if (_currentPane == null)
                {
                    _logger.LogDebug("No current pane to hide");
                    return;
                }

                _logger.LogInformation("Hiding current pane");

                await HidePaneWithAnimationAsync(_currentPane);
                _currentPane = null;

                // Hide overlay if no panes are active
                await HideOverlayAsync();

                _logger.LogInformation("Current pane hidden successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide current pane");
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
        private async Task EnsureOverlayAsync()
        {
            try
            {
                if (_overlayGrid != null) return;

                // CRITICAL FIX: Ensure UI operations are performed on UI thread
                // This prevents COM exceptions in WinUI 3 Desktop Apps
                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        if (_overlayGrid != null) return;

                        if (_mainWindow?.Content is not FrameworkElement mainContent)
                        {
                            throw new InvalidOperationException("Main window content is not a FrameworkElement");
                        }

                        // Create overlay grid
                        _overlayGrid = new Grid
                        {
                            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                Microsoft.UI.Colors.Black),
                            Opacity = 0.3,
                            Visibility = Visibility.Collapsed
                        };

                        // Add overlay to main content
                        if (mainContent.Parent is Panel parentPanel)
                        {
                            parentPanel.Children.Add(_overlayGrid);
                        }
                        else
                        {
                            // Wrap main content in a grid
                            var wrapperGrid = new Grid();
                            wrapperGrid.Children.Add(mainContent);
                            wrapperGrid.Children.Add(_overlayGrid);
                            
                            if (_mainWindow.Content is Panel windowPanel)
                            {
                                windowPanel.Children.Clear();
                                windowPanel.Children.Add(wrapperGrid);
                            }
                        }

                        _logger.LogDebug("Overlay grid created and added");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create overlay grid on UI thread");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to marshal overlay creation to UI thread");
                throw;
            }
        }

        /// <summary>
        /// Show overlay with fade-in effect (simplified to avoid COM interop issues)
        /// </summary>
        private async Task ShowOverlayAsync()
        {
            try
            {
                if (_overlayGrid == null) return;

                // CRITICAL FIX: Remove Storyboard.SetTarget COM interop calls
                // These cause Marshal.ThrowExceptionForHR errors in WinUI 3 Desktop Apps
                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        _overlayGrid.Visibility = Visibility.Visible;
                        _overlayGrid.Opacity = 0.0;
                        
                        // CRITICAL FIX: Use simple opacity animation without COM interop
                        // DoubleAnimation.Begin() doesn't exist, so we'll use a simple approach
                        _overlayGrid.Opacity = 0.0;
                        
                        // Use a simple timer-based animation to avoid COM interop
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to show overlay animation");
                        // Fallback: just set opacity directly
                        _overlayGrid.Opacity = 0.3;
                    }
                });

                // Wait for animation to complete
                await Task.Delay(200);
                _logger.LogDebug("Overlay shown with fade-in effect");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show overlay");
                throw;
            }
        }

        /// <summary>
        /// Hide overlay with fade-out effect (simplified to avoid COM interop issues)
        /// </summary>
        private async Task HideOverlayAsync()
        {
            try
            {
                if (_overlayGrid == null) return;

                // CRITICAL FIX: Remove Storyboard.SetTarget COM interop calls
                // These cause Marshal.ThrowExceptionForHR errors in WinUI 3 Desktop Apps
                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        // CRITICAL FIX: Use simple opacity animation without COM interop
                        // DoubleAnimation.Begin() doesn't exist, so we'll use a simple approach
                        
                        // Use a simple timer-based animation to avoid COM interop
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to hide overlay animation");
                        // Fallback: just hide directly
                        _overlayGrid.Visibility = Visibility.Collapsed;
                    }
                });

                // Wait for animation to complete
                await Task.Delay(200);
                _logger.LogDebug("Overlay hidden with fade-out effect");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide overlay");
                throw;
            }
        }

        /// <summary>
        /// Show pane with slide-in animation from right
        /// </summary>
        private async Task ShowPaneWithAnimationAsync(FrameworkElement pane)
        {
            try
            {
                // Show overlay first
                await ShowOverlayAsync();

                // CRITICAL FIX: Ensure ALL UI operations including animations are performed on UI thread
                // This prevents COM exceptions in WinUI 3 Desktop Apps
                var animationCompleted = false;
                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        // Ensure pane is in the visual tree
                        if (pane.Parent == null && _overlayGrid != null)
                        {
                            _overlayGrid.Children.Add(pane);
                        }

                        // Set initial position (off-screen to the right)
                        var initialX = pane.ActualWidth > 0 ? pane.ActualWidth : 400;
                        pane.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform
                        {
                            X = initialX
                        };

                        pane.Visibility = Visibility.Visible;

                        // CRITICAL FIX: Use simple slide animation without COM interop
                        // DoubleAnimation.Begin() doesn't exist, so we'll use a simple approach
                        
                        // Use a simple timer-based animation to avoid COM interop
                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                        };
                        
                        var startTime = DateTime.Now;
                        var duration = TimeSpan.FromMilliseconds(300);
                        
                        timer.Tick += (s, e) =>
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
                                animationCompleted = true;
                            }
                        };
                        
                        timer.Start();
                        _logger.LogDebug("Pane shown with slide-in animation");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to setup pane and animation on UI thread");
                        animationCompleted = true; // Mark as completed to avoid hanging
                    }
                });

                // Wait for animation to complete
                while (!animationCompleted)
                {
                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show pane with animation");
                throw;
            }
        }

        /// <summary>
        /// Hide pane with slide-out animation to right
        /// </summary>
        private async Task HidePaneWithAnimationAsync(FrameworkElement pane)
        {
            try
            {
                // CRITICAL FIX: Ensure ALL UI operations including animations are performed on UI thread
                // This prevents COM exceptions in WinUI 3 Desktop Apps
                var animationCompleted = false;
                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        // CRITICAL FIX: Use simple slide animation without COM interop
                        // DoubleAnimation.Begin() doesn't exist, so we'll use a simple approach
                        
                        // Use a simple timer-based animation to avoid COM interop
                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                        };
                        
                        var startTime = DateTime.Now;
                        var duration = TimeSpan.FromMilliseconds(300);
                        var targetX = pane.ActualWidth > 0 ? pane.ActualWidth : 400;
                        
                        timer.Tick += (s, e) =>
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
                                animationCompleted = true;
                            }
                        };
                        
                        timer.Start();
                        _logger.LogDebug("Pane hidden with slide-out animation");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to hide pane animation on UI thread");
                        pane.Visibility = Visibility.Collapsed;
                        animationCompleted = true; // Mark as completed to avoid hanging
                    }
                });

                // Wait for animation to complete
                while (!animationCompleted)
                {
                    await Task.Delay(10);
                }

                // Hide overlay
                await HideOverlayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide pane with animation");
                throw;
            }
        }
    }
}
