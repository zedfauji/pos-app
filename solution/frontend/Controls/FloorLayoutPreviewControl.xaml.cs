using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using MagiDesk.Shared.DTOs.Tables;
using System.Diagnostics;

namespace MagiDesk.Frontend.Controls;

public sealed partial class FloorLayoutPreviewControl : UserControl
{
    private FloorDto? _floor;
    private ObservableCollection<TableLayoutDto>? _tables;
    private double _zoomLevel = 1.0;
    private bool _showGrid = false;
    private bool _showLabels = true;
    private bool _showAvailabilityColor = true;
    private bool _showShapesOnly = false;

    public event EventHandler<TableLayoutDto>? TableClicked;

    public FloorDto? Floor
    {
        get => _floor;
        set
        {
            _floor = value;
        }
    }

    public ObservableCollection<TableLayoutDto>? Tables
    {
        get => _tables;
        set
        {
            var oldCount = _tables?.Count ?? 0;
            var newCount = value?.Count ?? 0;
            var isSameReference = _tables == value;
            
            Debug.WriteLine($"[FloorLayoutPreview] Tables property setter: OldCount={oldCount}, NewCount={newCount}, IsSameReference={isSameReference}");
            
            if (_tables != value)
            {
                if (_tables != null)
                {
                    _tables.CollectionChanged -= Tables_CollectionChanged;
                }
                
                _tables = value;
                
                if (_tables != null)
                {
                    _tables.CollectionChanged += Tables_CollectionChanged;
                }
                
                // Only update if this is actually a different collection
                // If it's the same reference, the collection change handler will handle updates
                if (!isSameReference)
                {
                    Debug.WriteLine("[FloorLayoutPreview] Tables property setter: Different reference, calling UpdateTables");
                    UpdateTables();
                }
                else
                {
                    Debug.WriteLine("[FloorLayoutPreview] Tables property setter: Same reference, skipping UpdateTables to prevent reset");
                }
            }
            else
            {
                Debug.WriteLine("[FloorLayoutPreview] Tables property setter: Same value, no update needed");
            }
        }
    }

    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            _zoomLevel = value;
            UpdateZoom();
        }
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            _showGrid = value;
            UpdateGridOverlay();
        }
    }

    public bool ShowLabels
    {
        get => _showLabels;
        set
        {
            _showLabels = value;
            UpdateLabelVisibility();
        }
    }

    public bool ShowAvailabilityColor
    {
        get => _showAvailabilityColor;
        set
        {
            _showAvailabilityColor = value;
            UpdateLabelVisibility();
        }
    }

    public bool ShowShapesOnly
    {
        get => _showShapesOnly;
        set
        {
            _showShapesOnly = value;
            UpdateLabelVisibility();
        }
    }

    public FloorLayoutPreviewControl()
    {
        this.InitializeComponent();
        this.Loaded += FloorLayoutPreviewControl_Loaded;
    }

    private void FloorLayoutPreviewControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTables();
        UpdateGridOverlay();
        UpdateZoom();
        
        // Center layout after control is loaded
        this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            CenterLayout();
        });
    }

    private void Tables_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateTables();
    }

    private void UpdateTables()
    {
        if (TablesContainer == null)
        {
            Debug.WriteLine("[FloorLayoutPreview] UpdateTables: TablesContainer is null");
            return;
        }
        
        try
        {
            var tableCount = _tables?.Count ?? 0;
            Debug.WriteLine($"[FloorLayoutPreview] UpdateTables: Setting ItemsSource with {tableCount} tables");
            
            // Check if we're replacing the same collection (same count and references)
            var currentSource = TablesContainer.ItemsSource;
            bool isSameCollection = currentSource == _tables;
            bool isSameCount = currentSource is ObservableCollection<TableLayoutDto> currentObs && 
                              _tables != null && 
                              currentObs.Count == _tables.Count;
            
            if (isSameCollection)
            {
                Debug.WriteLine("[FloorLayoutPreview] UpdateTables: Same collection reference, skipping ItemsSource update to prevent reset");
                // Just refresh positions and labels without changing ItemsSource
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    RefreshTablePositions();
                    UpdateLabelVisibility();
                });
                return;
            }
            
            TablesContainer.ItemsSource = _tables;
            
            // Update positions and labels after items are set
            this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                Debug.WriteLine($"[FloorLayoutPreview] UpdateTables: Refreshing positions and centering layout");
                RefreshTablePositions();
                UpdateLabelVisibility();
                // Auto-fit if this is a new collection or count changed
                if (!isSameCount)
                {
                    Debug.WriteLine("[FloorLayoutPreview] UpdateTables: Count changed, auto-fitting layout");
                    // Delay auto-fit to ensure layout is complete
                    this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        AutoFitTables();
                    });
                }
                else
                {
                    Debug.WriteLine("[FloorLayoutPreview] UpdateTables: Same count, skipping auto-fit to preserve view");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FloorLayoutPreview] UpdateTables error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void RefreshTablePositions()
    {
        if (TablesContainer?.ItemsPanelRoot is not Canvas canvas || _tables == null) return;
        
        var containers = canvas.Children;
        for (int i = 0; i < containers.Count && i < _tables.Count; i++)
        {
            var container = containers[i];
            var table = _tables[i];
            
            if (container is ContentPresenter presenter)
            {
                Canvas.SetLeft(presenter, table.XPosition);
                Canvas.SetTop(presenter, table.YPosition);
            }
        }
    }

    private void UpdateZoom()
    {
        if (MainScrollViewer != null)
        {
            try
            {
                var currentX = MainScrollViewer.HorizontalOffset;
                var currentY = MainScrollViewer.VerticalOffset;
                MainScrollViewer.ChangeView(currentX, currentY, (float)_zoomLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FloorLayoutPreview] UpdateZoom error: {ex.Message}");
            }
        }
    }
    
    public void AutoFitTables()
    {
        if (_tables == null || _tables.Count == 0 || MainScrollViewer == null || DesignCanvas == null)
        {
            Debug.WriteLine("[FloorLayoutPreview] AutoFitTables: No tables or controls not ready");
            return;
        }
        
        try
        {
            // Wait for layout to be ready
            this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                if (MainScrollViewer.ActualWidth == 0 || MainScrollViewer.ActualHeight == 0)
                {
                    // Wait for next layout update
                    MainScrollViewer.LayoutUpdated += (s, e) =>
                    {
                        MainScrollViewer.LayoutUpdated -= (s2, e2) => { };
                        PerformAutoFit();
                    };
                    return;
                }
                
                PerformAutoFit();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FloorLayoutPreview] AutoFitTables error: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private void PerformAutoFit()
    {
        if (_tables == null || _tables.Count == 0 || MainScrollViewer == null) return;
        
        try
        {
            // Calculate bounding box of all tables
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            
            foreach (var table in _tables)
            {
                // Table size is approximately 100x100 (from MinWidth/MinHeight in XAML)
                const double tableSize = 100;
                double left = table.XPosition;
                double top = table.YPosition;
                double right = left + tableSize;
                double bottom = top + tableSize;
                
                minX = Math.Min(minX, left);
                minY = Math.Min(minY, top);
                maxX = Math.Max(maxX, right);
                maxY = Math.Max(maxY, bottom);
            }
            
            if (minX == double.MaxValue) return; // No valid tables
            
            // Add padding
            const double padding = 40;
            double contentWidth = maxX - minX + (padding * 2);
            double contentHeight = maxY - minY + (padding * 2);
            
            // Get available viewport size (accounting for padding)
            double viewportWidth = MainScrollViewer.ActualWidth - 16; // Account for border padding
            double viewportHeight = MainScrollViewer.ActualHeight - 16;
            
            if (viewportWidth <= 0 || viewportHeight <= 0) return;
            
            // Calculate zoom to fit
            double zoomX = viewportWidth / contentWidth;
            double zoomY = viewportHeight / contentHeight;
            double zoom = Math.Min(zoomX, zoomY);
            
            // Clamp zoom to reasonable bounds
            zoom = Math.Max(0.1, Math.Min(2.0, zoom));
            
            Debug.WriteLine($"[FloorLayoutPreview] AutoFit: Content=({contentWidth}x{contentHeight}), Viewport=({viewportWidth}x{viewportHeight}), Zoom={zoom}");
            
            // Set zoom level
            _zoomLevel = zoom;
            UpdateZoom();
            
            // Center the content
            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;
            
            // Calculate scroll position to center
            double scrollX = (centerX * zoom) - (viewportWidth / 2);
            double scrollY = (centerY * zoom) - (viewportHeight / 2);
            
            MainScrollViewer.ChangeView(scrollX, scrollY, (float)zoom);
            
            Debug.WriteLine($"[FloorLayoutPreview] AutoFit: Centered at ({scrollX}, {scrollY})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FloorLayoutPreview] PerformAutoFit error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void UpdateGridOverlay()
    {
        if (GridOverlay == null) return;
        
        GridOverlay.Children.Clear();
        
        if (!_showGrid) return;

        const int canvasWidth = 2000;
        const int canvasHeight = 2000;
        var gridColor = new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.3 };

        // Vertical lines
        for (int x = 0; x <= canvasWidth; x += 20)
        {
            var line = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = canvasHeight,
                Stroke = gridColor,
                StrokeThickness = 0.5
            };
            GridOverlay.Children.Add(line);
        }

        // Horizontal lines
        for (int y = 0; y <= canvasHeight; y += 20)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = canvasWidth,
                Y2 = y,
                Stroke = gridColor,
                StrokeThickness = 0.5
            };
            GridOverlay.Children.Add(line);
        }
    }

    private void UpdateLabelVisibility()
    {
        if (TablesContainer?.ItemsPanelRoot is Canvas canvas)
        {
            foreach (var child in canvas.Children)
            {
                if (child is ContentPresenter presenter)
                {
                    var overlay = FindVisualChild<Grid>(presenter, "TableInfoOverlay");
                    var statusBadge = FindVisualChild<Border>(presenter, "StatusBadge");
                    var statusTextBlock = FindVisualChild<TextBlock>(presenter, "StatusTextBlock");
                    
                    if (overlay != null)
                    {
                        overlay.Visibility = _showLabels ? Visibility.Visible : Visibility.Collapsed;
                    }
                    
                    if (statusBadge != null)
                    {
                        statusBadge.Visibility = (_showLabels && _showAvailabilityColor) ? Visibility.Visible : Visibility.Collapsed;
                    }
                    
                    // Update status text to uppercase
                    if (statusTextBlock != null && presenter.Content is TableLayoutDto table)
                    {
                        statusTextBlock.Text = table.Status?.ToUpperInvariant() ?? "UNKNOWN";
                    }
                }
            }
        }
    }

    private void CenterLayout()
    {
        if (_tables == null || _tables.Count == 0 || MainScrollViewer == null) return;

        try
        {
            // Calculate bounding box
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var table in _tables)
            {
                var tableWidth = 120;
                var tableHeight = 140;
                
                minX = Math.Min(minX, table.XPosition);
                minY = Math.Min(minY, table.YPosition);
                maxX = Math.Max(maxX, table.XPosition + tableWidth);
                maxY = Math.Max(maxY, table.YPosition + tableHeight);
            }

            if (minX == double.MaxValue) return;

            var padding = 50.0;
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;

            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;
            
            var svWidth = MainScrollViewer.ActualWidth;
            var svHeight = MainScrollViewer.ActualHeight;
            
            if (svWidth > 0 && svHeight > 0)
            {
                double targetX = centerX - svWidth / 2 / _zoomLevel;
                double targetY = centerY - svHeight / 2 / _zoomLevel;
                
                MainScrollViewer.ChangeView(targetX, targetY, (float)_zoomLevel);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FloorLayoutPreview] CenterLayout error: {ex.Message}");
        }
    }

    private void Table_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Only handle left-click, ignore right-click (which should show context menu)
        var pointer = e.GetCurrentPoint(sender as UIElement);
        if (pointer.Properties.IsRightButtonPressed)
        {
            return; // Let RightTapped handle it
        }
        
        if (sender is FrameworkElement element && element.DataContext is TableLayoutDto table)
        {
            TableClicked?.Invoke(this, table);
        }
    }
    
    private void Table_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Update menu visibility when right-clicked
        if (sender is FrameworkElement element && element.DataContext is TableLayoutDto table)
        {
            // Update menu visibility based on table type and status
            if (element.ContextFlyout is MenuFlyout menu)
            {
                // Set DataContext on menu items so they can access the table
                foreach (var item in menu.Items.OfType<MenuFlyoutItem>())
                {
                    item.DataContext = table;
                }
                
                // Update visibility - this will be called again in Opening, but doing it here ensures it's ready
                UpdateContextMenuVisibility(element, table);
            }
        }
        // Mark as handled to prevent other handlers from processing this event
        e.Handled = true;
    }
    
    private void TableContextMenu_Opening(object sender, object e)
    {
        // Find the element that owns this ContextFlyout
        if (sender is MenuFlyout menu)
        {
            // In WinUI 3, the Target property should give us the element that owns the ContextFlyout
            var target = menu.Target;
            if (target is FrameworkElement element)
            {
                // Try to get the DataContext from the target element
                if (element.DataContext is TableLayoutDto table)
                {
                    // Set DataContext on menu items so they can access the table
                    foreach (var item in menu.Items.OfType<MenuFlyoutItem>())
                    {
                        item.DataContext = table;
                    }
                    
                    // Update menu visibility based on table type and status
                    UpdateContextMenuVisibility(element, table);
                }
                else
                {
                    // If DataContext is not on the target, try to find it in the visual tree
                    var parent = element.Parent;
                    while (parent != null)
                    {
                        if (parent is FrameworkElement parentElement && parentElement.DataContext is TableLayoutDto parentTable)
                        {
                            foreach (var item in menu.Items.OfType<MenuFlyoutItem>())
                            {
                                item.DataContext = parentTable;
                            }
                            UpdateContextMenuVisibility(parentElement, parentTable);
                            break;
                        }
                        parent = (parent as FrameworkElement)?.Parent;
                    }
                }
            }
        }
    }
    
    private void UpdateContextMenuVisibility(FrameworkElement element, TableLayoutDto table)
    {
        if (element.ContextFlyout is not MenuFlyout menu) return;
        
        var isBilliard = table.TableType?.ToLower() == "billiard";
        var isBar = table.TableType?.ToLower() == "bar";
        var isOccupied = table.Status?.ToLower() == "occupied";
        var isAvailable = !isOccupied;
        
        // Billiard table specific items
        var startSessionItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "StartSessionMenuItem");
        var stopSessionItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "StopSessionMenuItem");
        
        // Bar table specific items
        var openTableItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "OpenTableMenuItem");
        var closeTableItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "CloseTableMenuItem");
        var checkSummaryItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "CheckSummaryMenuItem");
        var assignToBilliardItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "AssignToBilliardMenuItem");
        
        // Threshold items (billiard only)
        var setThresholdItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "SetThresholdMenuItem");
        var clearThresholdItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Name == "ClearThresholdMenuItem");
        
        // Common items
        var addItemsItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Add Items");
        var moveTableItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Move Table");
        var tableStatusItem = menu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Table Status");
        
        // Update visibility
        if (startSessionItem != null) startSessionItem.Visibility = (isBilliard && isAvailable) ? Visibility.Visible : Visibility.Collapsed;
        if (stopSessionItem != null) stopSessionItem.Visibility = (isBilliard && isOccupied) ? Visibility.Visible : Visibility.Collapsed;
        
        if (openTableItem != null) openTableItem.Visibility = (isBar && isAvailable) ? Visibility.Visible : Visibility.Collapsed;
        if (closeTableItem != null) closeTableItem.Visibility = (isBar && isOccupied) ? Visibility.Visible : Visibility.Collapsed;
        if (checkSummaryItem != null) checkSummaryItem.Visibility = (isBar && isOccupied) ? Visibility.Visible : Visibility.Collapsed;
        if (assignToBilliardItem != null) assignToBilliardItem.Visibility = (isBar && isOccupied) ? Visibility.Visible : Visibility.Collapsed;
        
        if (setThresholdItem != null) setThresholdItem.Visibility = isBilliard ? Visibility.Visible : Visibility.Collapsed;
        if (clearThresholdItem != null) clearThresholdItem.Visibility = isBilliard ? Visibility.Visible : Visibility.Collapsed;
        
        if (addItemsItem != null) addItemsItem.Visibility = isOccupied ? Visibility.Visible : Visibility.Collapsed;
        if (moveTableItem != null) moveTableItem.Visibility = isOccupied ? Visibility.Visible : Visibility.Collapsed;
        if (tableStatusItem != null) tableStatusItem.Visibility = isOccupied ? Visibility.Visible : Visibility.Collapsed;
    }
    
    // Event handlers that will be forwarded to parent
    public event EventHandler<TableLayoutDto>? StartSessionRequested;
    public event EventHandler<TableLayoutDto>? StopSessionRequested;
    public event EventHandler<TableLayoutDto>? OpenTableRequested;
    public event EventHandler<TableLayoutDto>? CloseTableRequested;
    public event EventHandler<TableLayoutDto>? AddItemsRequested;
    public event EventHandler<TableLayoutDto>? CheckSummaryRequested;
    public event EventHandler<TableLayoutDto>? MoveTableRequested;
    public event EventHandler<TableLayoutDto>? AssignToBilliardRequested;
    public event EventHandler<TableLayoutDto>? TableStatusRequested;
    public event EventHandler<TableLayoutDto>? SetThresholdRequested;
    public event EventHandler<TableLayoutDto>? ClearThresholdRequested;
    public event EventHandler<TableLayoutDto>? DiagnosticsRequested;
    
    private void StartSessionMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            StartSessionRequested?.Invoke(this, table);
        }
    }
    
    private void StopSessionMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            StopSessionRequested?.Invoke(this, table);
        }
    }
    
    private void OpenTableMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            OpenTableRequested?.Invoke(this, table);
        }
    }
    
    private void CloseTableMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            CloseTableRequested?.Invoke(this, table);
        }
    }
    
    private void AddItemsMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            AddItemsRequested?.Invoke(this, table);
        }
    }
    
    private void CheckSummaryMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            CheckSummaryRequested?.Invoke(this, table);
        }
    }
    
    private void MoveTableMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            MoveTableRequested?.Invoke(this, table);
        }
    }
    
    private void AssignToBilliardMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            AssignToBilliardRequested?.Invoke(this, table);
        }
    }
    
    private void TableStatusMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            TableStatusRequested?.Invoke(this, table);
        }
    }
    
    private void SetThresholdMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            SetThresholdRequested?.Invoke(this, table);
        }
    }
    
    private void ClearThresholdMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            ClearThresholdRequested?.Invoke(this, table);
        }
    }
    
    private void DiagnosticsMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TableLayoutDto table)
        {
            DiagnosticsRequested?.Invoke(this, table);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
    {
        for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t && (child as FrameworkElement)?.Name == name)
                return t;
            
            var result = FindVisualChild<T>(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
