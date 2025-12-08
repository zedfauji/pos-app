using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Controls;

public sealed partial class LayoutDesigner : UserControl
{
    public static readonly DependencyProperty TablesProperty =
        DependencyProperty.Register(nameof(Tables), typeof(System.Collections.ObjectModel.ObservableCollection<TableLayoutDto>),
            typeof(LayoutDesigner), new PropertyMetadata(null, OnTablesChanged));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel), typeof(double),
            typeof(LayoutDesigner), new PropertyMetadata(1.0, OnZoomLevelChanged));

    public static readonly DependencyProperty GridSizeProperty =
        DependencyProperty.Register(nameof(GridSize), typeof(int),
            typeof(LayoutDesigner), new PropertyMetadata(20, OnGridSizeChanged));

    public static readonly DependencyProperty ShowGridProperty =
        DependencyProperty.Register(nameof(ShowGrid), typeof(bool),
            typeof(LayoutDesigner), new PropertyMetadata(true, OnShowGridChanged));

    public static readonly DependencyProperty SnapToGridProperty =
        DependencyProperty.Register(nameof(SnapToGrid), typeof(bool),
            typeof(LayoutDesigner), new PropertyMetadata(true));

    private System.Collections.ObjectModel.ObservableCollection<TableLayoutDto>? _tables;
    private System.Collections.Specialized.NotifyCollectionChangedEventHandler? _collectionChangedHandler;

    public ObservableCollection<TableLayoutDto> Tables
    {
        get => (ObservableCollection<TableLayoutDto>)GetValue(TablesProperty);
        set
        {
            // Unsubscribe from old collection
            if (_tables != null && _collectionChangedHandler != null)
            {
                _tables.CollectionChanged -= _collectionChangedHandler;
            }
            
            SetValue(TablesProperty, value);
            _tables = value;
            
            // Subscribe to new collection
            if (_tables != null)
            {
                _collectionChangedHandler = (s, e) => UpdateTablesSource();
                _tables.CollectionChanged += _collectionChangedHandler;
                UpdateTablesSource();
            }
        }
    }
    
    private void UpdateTablesSource()
    {
        if (TablesContainer != null && Tables != null)
        {
            TablesContainer.ItemsSource = Tables;
            
            // Force update layout to ensure containers are created
            TablesContainer.UpdateLayout();
            
            // Refresh positions after layout update
            RefreshTablePositions();
        }
    }
    
    private void RefreshTablePositions()
    {
        if (TablesContainer == null || Tables == null) return;
        
        // Update positions for all existing containers
        // ItemsControl uses ContentPresenter as containers
        var containers = TablesContainer.ItemsPanelRoot?.Children;
        if (containers != null)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                var container = containers[i];
                if (container is ContentPresenter contentPresenter)
                {
                    // Get the table from the ContentPresenter's Content
                    if (contentPresenter.Content is TableLayoutDto table)
                    {
                        Canvas.SetLeft(contentPresenter, table.XPosition);
                        Canvas.SetTop(contentPresenter, table.YPosition);
                    }
                }
            }
        }
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public int GridSize
    {
        get => (int)GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public bool SnapToGrid
    {
        get => (bool)GetValue(SnapToGridProperty);
        set => SetValue(SnapToGridProperty, value);
    }

    private TableLayoutDto? _draggedTable;
    private FrameworkElement? _draggedElement;
    private Point _dragStartPoint;
    private Point _tableStartPosition;
    private bool _isDragging;
    
    private void Table_Loaded(object sender, RoutedEventArgs e)
    {
        // Set Canvas position when table border loads
        if (sender is FrameworkElement element && element.DataContext is TableLayoutDto table)
        {
            var contentPresenter = FindParent<ContentPresenter>(element);
            if (contentPresenter != null)
            {
                Canvas.SetLeft(contentPresenter, table.XPosition);
                Canvas.SetTop(contentPresenter, table.YPosition);
            }
        }
    }

    public event EventHandler<TableLayoutDto>? TableSelected;
    public event EventHandler<(Guid TableId, double X, double Y)>? TablePositionChanged;
    public event EventHandler<string>? AddTableRequested;

    public LayoutDesigner()
    {
        this.InitializeComponent();
        this.Loaded += LayoutDesigner_Loaded;
        this.DataContextChanged += LayoutDesigner_DataContextChanged;
    }
    
    private void LayoutDesigner_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // Update zoom text when data context changes
        UpdateZoom();
    }

    private void LayoutDesigner_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateGridOverlay();
        UpdateZoom();
        UpdateTablesSource();
    }

    private static void OnTablesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutDesigner designer)
        {
            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<TableLayoutDto> oldCollection && designer._collectionChangedHandler != null)
            {
                oldCollection.CollectionChanged -= designer._collectionChangedHandler;
            }
            
            // Update internal reference
            designer._tables = e.NewValue as ObservableCollection<TableLayoutDto>;
            
            // Subscribe to new collection
            if (designer._tables != null)
            {
                if (designer._collectionChangedHandler == null)
                {
                    designer._collectionChangedHandler = (s, args) => 
                    {
                        designer.UpdateTablesSource();
                        // Refresh positions after collection changes
                        designer.RefreshTablePositions();
                    };
                }
                designer._tables.CollectionChanged += designer._collectionChangedHandler;
            }
            
            // Update ItemsSource
            designer.UpdateTablesSource();
            
            // Refresh positions after a delay to ensure containers are created
            if (designer.TablesContainer != null)
            {
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (dispatcherQueue != null)
                {
                    _ = dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        designer.TablesContainer.UpdateLayout();
                        designer.RefreshTablePositions();
                    });
                }
            }
        }
    }

    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutDesigner designer)
        {
            designer.UpdateZoom();
            designer.UpdateZoomText();
        }
    }

    private static void OnGridSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutDesigner designer)
        {
            designer.UpdateGridOverlay();
        }
    }

    private static void OnShowGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutDesigner designer)
        {
            designer.GridOverlay.Visibility = designer.ShowGrid ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateZoom()
    {
        if (MainScrollViewer != null)
        {
            MainScrollViewer.ChangeView(null, null, (float)ZoomLevel);
        }
        
        if (ZoomLevelText != null)
        {
            ZoomLevelText.Text = $"{ZoomLevel:P0}";
        }
    }

    private void UpdateGridOverlay()
    {
        GridOverlay.Children.Clear();
        
        if (!ShowGrid) return;

        const int canvasWidth = 2000;
        const int canvasHeight = 2000;
        var gridColor = new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.3 };

        // Vertical lines
        for (int x = 0; x <= canvasWidth; x += GridSize)
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
        for (int y = 0; y <= canvasHeight; y += GridSize)
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

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomLevel = Math.Min(ZoomLevel + 0.25, 4.0);
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomLevel = Math.Max(ZoomLevel - 0.25, 0.25);
    }
    
    private void Table_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TableLayoutDto table)
        {
            // Check if table is locked - prevent dragging if locked
            if (table.IsLocked)
            {
                // Still allow selection but not dragging
                TableSelected?.Invoke(this, table);
                return;
            }
            
            _draggedTable = table;
            _draggedElement = element;
            _isDragging = false;
            _dragStartPoint = e.GetCurrentPoint(DesignCanvas).Position;
            _tableStartPosition = new Point(table.XPosition, table.YPosition);
            
            TableSelected?.Invoke(this, table);
            element.CapturePointer(e.Pointer);
            
            // Visual feedback
            if (element is Border border)
            {
                border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue);
                border.BorderThickness = new Thickness(3);
            }
        }
    }

    private void Table_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedTable == null || _draggedElement == null || sender is not FrameworkElement element)
            return;
            
        // Prevent dragging if table is locked
        if (_draggedTable.IsLocked)
            return;
            
        var currentPoint = e.GetCurrentPoint(DesignCanvas).Position;
        var deltaX = currentPoint.X - _dragStartPoint.X;
        var deltaY = currentPoint.Y - _dragStartPoint.Y;

        // Check if movement is significant enough to start dragging
        if (!_isDragging && (Math.Abs(deltaX) > 5 || Math.Abs(deltaY) > 5))
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            var newX = _tableStartPosition.X + deltaX;
            var newY = _tableStartPosition.Y + deltaY;

            // Snap to grid if enabled
            if (SnapToGrid)
            {
                newX = Math.Round(newX / GridSize) * GridSize;
                newY = Math.Round(newY / GridSize) * GridSize;
            }

            // Check for collisions before updating position
            if (WouldOverlap(_draggedTable, newX, newY))
            {
                // Don't allow movement if it would cause overlap
                return;
            }

            // Update table DTO position
            _draggedTable.XPosition = newX;
            _draggedTable.YPosition = newY;
            
            // Update Canvas position on ContentPresenter
            var contentPresenter = FindParent<ContentPresenter>(element);
            if (contentPresenter != null)
            {
                Canvas.SetLeft(contentPresenter, newX);
                Canvas.SetTop(contentPresenter, newY);
            }
        }
    }
    
    /// <summary>
    /// Checks if moving a table to the new position would cause it to overlap with any other table
    /// </summary>
    private bool WouldOverlap(TableLayoutDto table, double newX, double newY)
    {
        if (Tables == null) return false;
        
        var (tableWidth, tableHeight) = GetTableDimensions(table);
        
        // Check collision with all other tables
        foreach (var otherTable in Tables)
        {
            // Skip self
            if (otherTable.TableId == table.TableId)
                continue;
                
            // Skip locked tables (they can't be moved, so they act as obstacles)
            // But we still check collision with them
            
            var (otherWidth, otherHeight) = GetTableDimensions(otherTable);
            
            // Simple axis-aligned bounding box collision detection
            // (We're not handling rotation for collision detection - using bounding box)
            var tableLeft = newX;
            var tableRight = newX + tableWidth;
            var tableTop = newY;
            var tableBottom = newY + tableHeight;
            
            var otherLeft = otherTable.XPosition;
            var otherRight = otherTable.XPosition + otherWidth;
            var otherTop = otherTable.YPosition;
            var otherBottom = otherTable.YPosition + otherHeight;
            
            // Check for overlap (with small padding to prevent touching)
            const double padding = 5.0; // 5px minimum gap between tables
            if (tableLeft < otherRight + padding &&
                tableRight + padding > otherLeft &&
                tableTop < otherBottom + padding &&
                tableBottom + padding > otherTop)
            {
                return true; // Overlap detected
            }
        }
        
        return false; // No overlap
    }
    
    /// <summary>
    /// Gets the width and height of a table based on its Size property or explicit Width/Height
    /// </summary>
    private (double width, double height) GetTableDimensions(TableLayoutDto table)
    {
        // Use explicit dimensions if set
        if (table.Width.HasValue && table.Height.HasValue)
        {
            return (table.Width.Value, table.Height.Value);
        }
        
        // Otherwise use defaults based on Size property
        return table.Size switch
        {
            "S" => (80.0, 80.0),   // Small
            "L" => (120.0, 120.0), // Large
            _ => (100.0, 100.0)    // Medium (default, matches MinWidth/MinHeight in XAML)
        };
    }
    
    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T t)
                return t;
            parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    private void Table_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedTable != null && sender is FrameworkElement element)
        {
            element.ReleasePointerCapture(e.Pointer);

            // Reset visual feedback
            if (element is Border border)
            {
                border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                border.BorderThickness = new Thickness(2);
            }

            if (_isDragging)
            {
                // Notify position change
                TablePositionChanged?.Invoke(this, (_draggedTable.TableId, _draggedTable.XPosition, _draggedTable.YPosition));
            }

            _draggedTable = null;
            _draggedElement = null;
            _isDragging = false;
        }
    }

    private void Table_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && !_isDragging)
        {
            border.Opacity = 0.9;
            var transform = border.RenderTransform as CompositeTransform;
            if (transform != null)
            {
                transform.ScaleX = 1.05;
                transform.ScaleY = 1.05;
            }
        }
    }

    private void Table_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && !_isDragging)
        {
            border.Opacity = 1.0;
            var transform = border.RenderTransform as CompositeTransform;
            if (transform != null)
            {
                transform.ScaleX = 1.0;
                transform.ScaleY = 1.0;
            }
        }
    }

    private void AddBarTable_Click(object sender, RoutedEventArgs e)
    {
        AddTableRequested?.Invoke(this, "bar");
    }

    private void AddBilliardTable_Click(object sender, RoutedEventArgs e)
    {
        AddTableRequested?.Invoke(this, "billiard");
    }
    
    private void ZoomLevelText_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            UpdateZoomText(textBlock);
        }
    }
    
    private void UpdateZoomText(TextBlock? textBlock = null)
    {
        var target = textBlock ?? (this.FindName("ZoomLevelText") as TextBlock);
        if (target != null)
        {
            target.Text = $"{ZoomLevel:P0}";
        }
    }
}

