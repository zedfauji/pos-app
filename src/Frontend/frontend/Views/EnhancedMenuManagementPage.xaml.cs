using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using Windows.Foundation.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace MagiDesk.Frontend.Views;

public sealed partial class EnhancedMenuManagementPage : Page, INotifyPropertyChanged
{
    // Collections
    public ObservableCollection<MenuItemDisplay> AllMenuItems { get; } = new();
    public ObservableCollection<ModifierManagementViewModel> Modifiers { get; } = new();
    public ObservableCollection<CategoryGroup> CategoryGroups { get; } = new();
    
    // Services
    private readonly MenuApiService? _menuService;
    private readonly MenuAnalyticsService? _analyticsService;
    private readonly MenuBulkOperationService? _bulkService;
    
    // Filtering and search state
    private string _currentSearchQuery = "";
    private string? _selectedCategory = null;
    private string? _selectedAvailability = null;
    private string? _selectedSortOption = null;
    
    private MenuItemVm? _selectedMenuItem;
    public MenuItemVm? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (_selectedMenuItem != value)
            {
                _selectedMenuItem = value;
                OnPropertyChanged(nameof(SelectedMenuItem));
                MenuItemDetailsPanel.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
                UpdateButtonStates();
            }
        }
    }

    // Analytics properties
    public decimal TotalRevenue { get; set; }
    public string TopSeller { get; set; } = "N/A";
    public decimal AverageRating { get; set; }
    
    // Analytics collections
    public ObservableCollection<object> AnalyticsItems { get; } = new();
    public ObservableCollection<object> Insights { get; } = new();
    public ObservableCollection<object> Trends { get; } = new();

    public EnhancedMenuManagementPage()
    {
        try
        {
            this.InitializeComponent();
            this.DataContext = this;
            
            _menuService = App.Menu;
            _analyticsService = App.Services?.GetService(typeof(MenuAnalyticsService)) as MenuAnalyticsService;
            _bulkService = App.Services?.GetService(typeof(MenuBulkOperationService)) as MenuBulkOperationService;
            
            if (_menuService == null)
            {
                ShowErrorDialog("Service Not Available", "Menu service is not available. Please restart the application or contact support.");
                return;
            }
            
            // Load data asynchronously
            Loaded += async (s, e) => 
            {
                await LoadMenuItemsAsync();
                await LoadModifiersAsync();
                await PopulateCategoryFilterAsync();
                await LoadAnalyticsAsync();
                ApplyFiltersAndRefreshUI();
            };
            
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Initialization Error", $"Failed to initialize Enhanced Menu Management page: {ex.Message}");
        }
    }

    private async Task LoadMenuItemsAsync()
    {
        if (_menuService == null) return;
        
        try
        {
            AllMenuItems.Clear();
            Debug.WriteLine("EnhancedMenuManagementPage: Loading menu items...");
            var items = await _menuService.ListItemsAsync(new MenuApiService.ItemsQuery(Q: null, Category: null, GroupName: null, AvailableOnly: null));
            Debug.WriteLine($"EnhancedMenuManagementPage: Retrieved {items.Count()} items from API");
            
            foreach (var item in items)
            {
                AllMenuItems.Add(new MenuItemDisplay
                {
                    MenuItemId = item.Id,
                    Name = item.Name,
                    Price = item.SellingPrice / 100, // Convert cents to dollars
                    PictureUrl = item.PictureUrl,
                    Sku = item.Sku,
                    IsAvailable = item.IsAvailable,
                    IsDiscountable = item.IsDiscountable,
                    IsPartOfCombo = item.IsPartOfCombo,
                    Category = item.Category,
                    GroupName = item.GroupName,
                    Description = item.Description
                });
            }
            
            Debug.WriteLine($"EnhancedMenuManagementPage: Added {AllMenuItems.Count} items to collection");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EnhancedMenuManagementPage: Error loading menu items: {ex.Message}");
            ShowErrorDialog("Error", $"Failed to load menu items: {ex.Message}");
        }
    }

    private async Task LoadAnalyticsAsync()
    {
        if (_analyticsService == null) return;

        try
        {
            var dashboard = await _analyticsService.GetDashboardDataAsync(
                FromDatePicker.Date.DateTime, 
                ToDatePicker.Date.DateTime);
            
            if (dashboard.TryGetValue("totalRevenue", out var revenueObj) && revenueObj is decimal revenueValue)
            {
                TotalRevenue = revenueValue;
                OnPropertyChanged(nameof(TotalRevenue));
            }
            
            if (dashboard.TryGetValue("topSeller", out var topSellerObj) && topSellerObj is string topSellerValue)
            {
                TopSeller = topSellerValue;
                OnPropertyChanged(nameof(TopSeller));
            }
            
            if (dashboard.TryGetValue("averageRating", out var ratingObj) && ratingObj is decimal ratingValue)
            {
                AverageRating = ratingValue;
                OnPropertyChanged(nameof(AverageRating));
            }
            
            // Populate analytics items with sample data
            AnalyticsItems.Clear();
            AnalyticsItems.Add(new { Name = "Margherita Pizza", Revenue = 1250.50m, Orders = 45, Rating = 4.5 });
            AnalyticsItems.Add(new { Name = "Caesar Salad", Revenue = 875.25m, Orders = 32, Rating = 4.2 });
            AnalyticsItems.Add(new { Name = "Chocolate Cake", Revenue = 450.75m, Orders = 18, Rating = 4.8 });
            AnalyticsItems.Add(new { Name = "Coca Cola", Revenue = 320.00m, Orders = 80, Rating = 4.0 });
            AnalyticsItems.Add(new { Name = "French Fries", Revenue = 680.50m, Orders = 55, Rating = 4.3 });
            
            // Populate insights with sample data
            Insights.Clear();
            Insights.Add(new { Type = "Revenue", Message = "Pizza category shows 15% growth this month", Priority = "High" });
            Insights.Add(new { Type = "Inventory", Message = "Consider restocking popular beverages", Priority = "Medium" });
            Insights.Add(new { Type = "Customer", Message = "High satisfaction scores for dessert items", Priority = "Low" });
            Insights.Add(new { Type = "Trend", Message = "Weekend orders increased by 25%", Priority = "High" });
            Insights.Add(new { Type = "Recommendation", Message = "Promote seasonal items during peak hours", Priority = "Medium" });

            // Add sample trends data
            Trends.Clear();
            Trends.Add(new { TrendIcon = "📈", Category = "Pizza", ChangePercentageText = "+15%", TrendColor = "Green", Recommendation = "Consider increasing pizza inventory", RevenueChangeText = "+$2,500", OrderChangeText = "+45 orders" });
            Trends.Add(new { TrendIcon = "📉", Category = "Beverages", ChangePercentageText = "-8%", TrendColor = "Red", Recommendation = "Review beverage pricing strategy", RevenueChangeText = "-$800", OrderChangeText = "-12 orders" });
            Trends.Add(new { TrendIcon = "📊", Category = "Desserts", ChangePercentageText = "+22%", TrendColor = "Green", Recommendation = "Expand dessert menu options", RevenueChangeText = "+$1,200", OrderChangeText = "+28 orders" });
            
            OnPropertyChanged(nameof(AnalyticsItems));
            OnPropertyChanged(nameof(Insights));
            OnPropertyChanged(nameof(Trends));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EnhancedMenuManagementPage: Error loading analytics: {ex.Message}");
            
            // Populate with fallback data if service fails
            AnalyticsItems.Clear();
            AnalyticsItems.Add(new { Name = "Sample Item 1", Revenue = 100.00m, Orders = 10, Rating = 4.0 });
            AnalyticsItems.Add(new { Name = "Sample Item 2", Revenue = 200.00m, Orders = 20, Rating = 4.5 });
            
            Insights.Clear();
            Insights.Add(new { Type = "Info", Message = "Analytics service unavailable", Priority = "Low" });
            
            // Add offline trends data
            Trends.Clear();
            Trends.Add(new { TrendIcon = "📊", Category = "Sample", ChangePercentageText = "N/A", TrendColor = "Gray", Recommendation = "Connect to analytics service", RevenueChangeText = "N/A", OrderChangeText = "N/A" });
            
            OnPropertyChanged(nameof(AnalyticsItems));
            OnPropertyChanged(nameof(Insights));
            OnPropertyChanged(nameof(Trends));
        }
    }

    private async Task PopulateCategoryFilterAsync()
    {
        try
        {
            CategoryFilter.Items.Clear();
            CategoryFilter.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = null });
            
            var categories = AllMenuItems.Select(x => x.Category).Distinct().OrderBy(x => x).ToList();
            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category))
                {
                    CategoryFilter.Items.Add(new ComboBoxItem { Content = category, Tag = category });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error populating category filter: {ex.Message}");
        }
    }

    private void ApplyFiltersAndRefreshUI()
    {
        try
        {
            // Apply filters
            var filteredItems = AllMenuItems.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(_currentSearchQuery))
            {
                filteredItems = filteredItems.Where(x => 
                    x.Name.Contains(_currentSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    x.Sku.Contains(_currentSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    (x.Description?.Contains(_currentSearchQuery, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Category filter
            if (!string.IsNullOrEmpty(_selectedCategory))
            {
                filteredItems = filteredItems.Where(x => x.Category == _selectedCategory);
            }

            // Sort
            if (!string.IsNullOrEmpty(_selectedSortOption))
            {
                filteredItems = _selectedSortOption switch
                {
                    "Revenue" => filteredItems.OrderByDescending(x => x.Price),
                    "Orders" => filteredItems.OrderByDescending(x => x.Name), // Placeholder
                    "Rating" => filteredItems.OrderByDescending(x => x.Name), // Placeholder
                    "NameAsc" => filteredItems.OrderBy(x => x.Name),
                    "NameDesc" => filteredItems.OrderByDescending(x => x.Name),
                    _ => filteredItems.OrderBy(x => x.Category).ThenBy(x => x.Name)
                };
            }
            else
            {
                filteredItems = filteredItems.OrderBy(x => x.Category).ThenBy(x => x.Name);
            }

            // Group by category
            RefreshCategoryGroups(filteredItems.ToList());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error applying filters: {ex.Message}");
            ShowErrorDialog("Error", $"Failed to apply filters: {ex.Message}");
        }
    }

    private void RefreshCategoryGroups(List<MenuItemDisplay> items)
    {
        try
        {
            CategoryGroups.Clear();
            CategoriesContainer.Children.Clear();

            var groupedItems = items.GroupBy(x => x.Category ?? "Uncategorized")
                                  .OrderBy(g => g.Key);

            foreach (var group in groupedItems)
            {
                var categoryGroup = new CategoryGroup
                {
                    CategoryName = group.Key,
                    IsExpanded = true
                };

                foreach (var item in group.OrderBy(x => x.Name))
                {
                    categoryGroup.Items.Add(item);
                }

                CategoryGroups.Add(categoryGroup);
                CreateCategoryUI(categoryGroup);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing category groups: {ex.Message}");
            ShowErrorDialog("Error", $"Failed to refresh category groups: {ex.Message}");
        }
    }

    private void CreateCategoryUI(CategoryGroup categoryGroup)
    {
        try
        {
            // Category header
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = categoryGroup.IsExpanded ? "▼" : "▶",
                            FontSize = 12,
                            Margin = new Thickness(0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = $"{categoryGroup.CategoryName} ({categoryGroup.ItemCount})",
                            FontSize = 18,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                },
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            headerButton.Click += (s, e) => ToggleCategoryExpansion(categoryGroup, headerButton);

            Grid.SetColumn(headerButton, 0);
            headerGrid.Children.Add(headerButton);

            // Add item button
            var addButton = new Button
            {
                Content = "Add Item",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            addButton.Click += (s, e) => AddMenuItemToCategory(categoryGroup.CategoryName);

            Grid.SetColumn(addButton, 1);
            headerGrid.Children.Add(addButton);

            CategoriesContainer.Children.Add(headerGrid);

            // Items grid
            var itemsGrid = new GridView
            {
                ItemsSource = categoryGroup.Items,
                SelectionMode = ListViewSelectionMode.Single,
                Margin = new Thickness(20, 12, 0, 0)
            };

            var template = (DataTemplate)this.Resources["MenuItemTemplate"];
            if (template != null)
            {
                itemsGrid.ItemTemplate = template;
            }
            
            itemsGrid.SelectionChanged += (s, e) => GridView_SelectionChanged(s, e);
            
            // Add context menu to GridView
            var contextMenu = new MenuFlyout();
            
            var editItem = new MenuFlyoutItem
            {
                Text = "✏️ Edit Item",
                Tag = itemsGrid
            };
            editItem.Click += (s, e) => EditMenuItem_Click(s, e);
            contextMenu.Items.Add(editItem);
            
            var toggleItem = new MenuFlyoutItem
            {
                Text = "🔄 Toggle Availability",
                Tag = itemsGrid
            };
            toggleItem.Click += (s, e) => ToggleAvailability_Click(s, e);
            contextMenu.Items.Add(toggleItem);
            
            var deleteItem = new MenuFlyoutItem
            {
                Text = "🗑️ Delete Item",
                Tag = itemsGrid
            };
            deleteItem.Click += (s, e) => DeleteMenuItem_Click(s, e);
            contextMenu.Items.Add(deleteItem);
            
            itemsGrid.ContextFlyout = contextMenu;
            
            CategoriesContainer.Children.Add(itemsGrid);
            categoryGroup.Tag = itemsGrid;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating category UI: {ex.Message}");
        }
    }

    private void ToggleCategoryExpansion(CategoryGroup categoryGroup, Button headerButton)
    {
        try
        {
            categoryGroup.IsExpanded = !categoryGroup.IsExpanded;
            
            if (headerButton.Content is StackPanel headerPanel && headerPanel.Children[0] is TextBlock arrow)
            {
                arrow.Text = categoryGroup.IsExpanded ? "▼" : "▶";
            }

            if (categoryGroup.Tag is GridView itemsGrid)
            {
                itemsGrid.Visibility = categoryGroup.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error toggling category expansion: {ex.Message}");
        }
    }

    private async void AddMenuItemToCategory(string categoryName)
    {
        try
        {
            var newItem = new MenuItemVm
            {
                MenuItemId = 0,
                Sku = "",
                Name = "",
                PictureUrl = null,
                Price = 0,
                IsDiscountable = false,
                IsPartOfCombo = false,
                IsAvailable = true,
                Category = categoryName
            };
            
            var dialog = new Dialogs.MenuItemCrudDialog(newItem);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadMenuItemsAsync();
                await PopulateCategoryFilterAsync();
                ApplyFiltersAndRefreshUI();
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to add menu item: {ex.Message}");
        }
    }

    // Event handlers
    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _currentSearchQuery = args.QueryText;
        ApplyFiltersAndRefreshUI();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _currentSearchQuery = sender.Text;
        ApplyFiltersAndRefreshUI();
    }

    private void FromDatePicker_DateChanged(object? sender, DatePickerValueChangedEventArgs e)
    {
        _ = LoadAnalyticsAsync();
    }

    private void ToDatePicker_DateChanged(object? sender, DatePickerValueChangedEventArgs e)
    {
        _ = LoadAnalyticsAsync();
    }

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            _selectedCategory = item.Tag?.ToString();
            ApplyFiltersAndRefreshUI();
        }
    }

    private void SortOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            _selectedSortOption = item.Tag?.ToString();
            ApplyFiltersAndRefreshUI();
        }
    }

    private async void AddMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var newItem = new MenuItemVm
            {
                MenuItemId = 0,
                Sku = "",
                Name = "",
                PictureUrl = null,
                Price = 0,
                IsDiscountable = false,
                IsPartOfCombo = false,
                IsAvailable = true,
                Category = "Appetizers" // Default category
            };
            
            var dialog = new Dialogs.MenuItemCrudDialog(newItem);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadMenuItemsAsync();
                await PopulateCategoryFilterAsync();
                ApplyFiltersAndRefreshUI();
                ShowSuccessDialog("Success", "Menu item added successfully!");
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to add menu item: {ex.Message}");
        }
    }

    private async void BulkActions_Click(object sender, RoutedEventArgs e)
    {
        MainTabView.SelectedIndex = 2; // Switch to Bulk Operations tab
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadMenuItemsAsync();
        await LoadAnalyticsAsync();
        await PopulateCategoryFilterAsync();
        ApplyFiltersAndRefreshUI();
    }

    // Bulk Operations Event Handlers
    private async void UpdatePrices_Click(object sender, RoutedEventArgs e)
    {
        if (_bulkService == null) return;

        try
        {
            var selectedItems = GetAllSelectedItems();
            if (!selectedItems.Any())
            {
                ShowErrorDialog("No Selection", "Please select items to update prices.");
                return;
            }

            if (!decimal.TryParse(PriceChangeTextBox.Text, out var priceChange))
            {
                ShowErrorDialog("Invalid Input", "Please enter a valid price change amount.");
                return;
            }

            var changeType = (PriceOperationComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Add";
            var result = await _bulkService.UpdatePricesAsync(selectedItems, priceChange, changeType);
            
            ShowBulkOperationResult(result);
            await LoadMenuItemsAsync();
            ApplyFiltersAndRefreshUI();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to update prices: {ex.Message}");
        }
    }

    private async void ChangeCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_bulkService == null) return;

        try
        {
            var selectedItems = GetAllSelectedItems();
            if (!selectedItems.Any())
            {
                ShowErrorDialog("No Selection", "Please select items to change category.");
                return;
            }

            var newCategory = (NewCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(newCategory))
            {
                ShowErrorDialog("Invalid Input", "Please select a new category.");
                return;
            }

            var result = await _bulkService.ChangeCategoryAsync(selectedItems, newCategory);
            
            ShowBulkOperationResult(result);
            await LoadMenuItemsAsync();
            ApplyFiltersAndRefreshUI();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to change category: {ex.Message}");
        }
    }

    private async void UpdateAvailability_Click(object sender, RoutedEventArgs e)
    {
        if (_bulkService == null) return;

        try
        {
            var selectedItems = GetAllSelectedItems();
            if (!selectedItems.Any())
            {
                ShowErrorDialog("No Selection", "Please select items to update availability.");
                return;
            }

            var isAvailable = bool.Parse((AvailabilityComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "true");
            var result = await _bulkService.ToggleAvailabilityAsync(selectedItems, isAvailable);
            
            ShowBulkOperationResult(result);
            await LoadMenuItemsAsync();
            ApplyFiltersAndRefreshUI();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to update availability: {ex.Message}");
        }
    }

    private async void UpdateImages_Click(object sender, RoutedEventArgs e)
    {
        if (_bulkService == null) return;

        try
        {
            var selectedItems = GetAllSelectedItems();
            if (!selectedItems.Any())
            {
                ShowErrorDialog("No Selection", "Please select items to update images.");
                return;
            }

            var imageUrl = ImageUrlTextBox.Text;
            if (string.IsNullOrEmpty(imageUrl))
            {
                ShowErrorDialog("Invalid Input", "Please enter an image URL.");
                return;
            }

            var result = await _bulkService.UpdateImagesAsync(selectedItems, imageUrl);
            
            ShowBulkOperationResult(result);
            await LoadMenuItemsAsync();
            ApplyFiltersAndRefreshUI();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to update images: {ex.Message}");
        }
    }

    private List<long> GetAllSelectedItems()
    {
        // This would typically get selected items from checkboxes or multi-select
        // For now, return all items as a placeholder
        return AllMenuItems.Select(x => x.MenuItemId).ToList();
    }

    private async void ShowBulkOperationResult(MenuBulkOperationService.BulkOperationResultDto result)
    {
        var message = $"Operation: {result.Operation}\n" +
                     $"Total Items: {result.TotalItems}\n" +
                     $"Success: {result.SuccessCount}\n" +
                     $"Failures: {result.FailureCount}\n" +
                     $"Duration: {result.Duration.TotalSeconds:F1}s";

        if (result.Errors.Any())
        {
            message += $"\n\nErrors:\n{string.Join("\n", result.Errors)}";
        }

        ShowErrorDialog("Bulk Operation Result", message);
    }

    // CRUD Event Handlers
    private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MenuItemDisplay? item = null;
        
        // Try to get the item from the MenuFlyoutItem's Tag (set in XAML)
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MenuItemDisplay menuItemDisplay)
        {
            item = menuItemDisplay;
        }
        // Fallback to SelectedMenuItem if available
        else if (SelectedMenuItem != null)
        {
            // Convert SelectedMenuItem back to MenuItemDisplay for consistency
            item = new MenuItemDisplay
            {
                MenuItemId = SelectedMenuItem.MenuItemId,
                Sku = SelectedMenuItem.Sku,
                Name = SelectedMenuItem.Name,
                PictureUrl = SelectedMenuItem.PictureUrl,
                Price = SelectedMenuItem.Price,
                IsDiscountable = SelectedMenuItem.IsDiscountable,
                IsPartOfCombo = SelectedMenuItem.IsPartOfCombo,
                IsAvailable = SelectedMenuItem.IsAvailable,
                Category = SelectedMenuItem.Category,
                GroupName = SelectedMenuItem.GroupName,
                Description = SelectedMenuItem.Description
            };
        }

        if (item == null) 
        {
            ShowErrorDialog("No Selection", "Please select a menu item to edit.");
            return;
        }

        // Convert MenuItemDisplay to MenuItemVm for the dialog
        var menuItemVm = new MenuItemVm
        {
            MenuItemId = item.MenuItemId,
            Sku = item.Sku,
            Name = item.Name,
            PictureUrl = item.PictureUrl,
            Price = item.Price,
            IsDiscountable = item.IsDiscountable,
            IsPartOfCombo = item.IsPartOfCombo,
            IsAvailable = item.IsAvailable,
            Category = item.Category,
            GroupName = item.GroupName,
            Description = item.Description
        };
        
        try
        {
            var dialog = new Dialogs.MenuItemCrudDialog(menuItemVm);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadMenuItemsAsync();
                await PopulateCategoryFilterAsync();
                ApplyFiltersAndRefreshUI();
                ShowSuccessDialog("Success", "Menu item updated successfully!");
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to edit menu item: {ex.Message}");
        }
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MenuItemDisplay? item = null;
        
        // Try to get the item from the MenuFlyoutItem's Tag (set in XAML)
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MenuItemDisplay menuItemDisplay)
        {
            item = menuItemDisplay;
        }
        // Fallback to SelectedMenuItem if available
        else if (SelectedMenuItem != null)
        {
            // Convert SelectedMenuItem back to MenuItemDisplay for consistency
            item = new MenuItemDisplay
            {
                MenuItemId = SelectedMenuItem.MenuItemId,
                Sku = SelectedMenuItem.Sku,
                Name = SelectedMenuItem.Name,
                PictureUrl = SelectedMenuItem.PictureUrl,
                Price = SelectedMenuItem.Price,
                IsDiscountable = SelectedMenuItem.IsDiscountable,
                IsPartOfCombo = SelectedMenuItem.IsPartOfCombo,
                IsAvailable = SelectedMenuItem.IsAvailable,
                Category = SelectedMenuItem.Category,
                GroupName = SelectedMenuItem.GroupName,
                Description = SelectedMenuItem.Description
            };
        }

        if (item == null) 
        {
            ShowErrorDialog("No Selection", "Please select a menu item to delete.");
            return;
        }
        
        try
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Menu Item",
                Content = $"Are you sure you want to delete '{item.Name}'?\n\nThis action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };
            
            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (_menuService != null)
                {
                    // Note: DeleteItemAsync is not available in MenuApiService
                    // For now, just show a message that deletion is not supported
                    ShowErrorDialog("Not Supported", "Menu item deletion is not currently supported through the API. Please contact your administrator.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to delete menu item: {ex.Message}");
        }
    }

    private async void ToggleAvailability_Click(object sender, RoutedEventArgs e)
    {
        MenuItemDisplay? item = null;
        
        // Try to get the item from the MenuFlyoutItem's Tag (set in XAML)
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MenuItemDisplay menuItemDisplay)
        {
            item = menuItemDisplay;
        }
        // Fallback to SelectedMenuItem if available
        else if (SelectedMenuItem != null)
        {
            // Convert SelectedMenuItem back to MenuItemDisplay for consistency
            item = new MenuItemDisplay
            {
                MenuItemId = SelectedMenuItem.MenuItemId,
                Sku = SelectedMenuItem.Sku,
                Name = SelectedMenuItem.Name,
                PictureUrl = SelectedMenuItem.PictureUrl,
                Price = SelectedMenuItem.Price,
                IsDiscountable = SelectedMenuItem.IsDiscountable,
                IsPartOfCombo = SelectedMenuItem.IsPartOfCombo,
                IsAvailable = SelectedMenuItem.IsAvailable,
                Category = SelectedMenuItem.Category,
                GroupName = SelectedMenuItem.GroupName,
                Description = SelectedMenuItem.Description
            };
        }

        if (item == null) 
        {
            ShowErrorDialog("No Selection", "Please select a menu item to toggle availability.");
            return;
        }
        
        try
        {
            if (_menuService != null)
            {
                var newAvailability = !item.IsAvailable;
                var updateDto = new MenuApiService.UpdateMenuItemDto(
                    Name: null,
                    Description: null,
                    Category: null,
                    GroupName: null,
                    VendorPrice: null,
                    SellingPrice: null,
                    Price: null,
                    PictureUrl: null,
                    IsDiscountable: null,
                    IsPartOfCombo: null,
                    IsAvailable: newAvailability
                );
                
                var updatedItem = await _menuService.UpdateMenuItemAsync(item.MenuItemId, updateDto);
                
                if (updatedItem != null)
                {
                    await LoadMenuItemsAsync();
                    ApplyFiltersAndRefreshUI();
                    
                    var status = newAvailability ? "available" : "unavailable";
                    ShowSuccessDialog("Success", $"Menu item '{item.Name}' is now {status}.");
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to toggle availability: {ex.Message}");
        }
    }

    private async void CopyMenuItemDetails_Click(object sender, RoutedEventArgs e)
    {
        MenuItemDisplay? item = null;
        
        // Try to get the item from the MenuFlyoutItem's Tag (set in XAML)
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is MenuItemDisplay menuItemDisplay)
        {
            item = menuItemDisplay;
        }
        // Fallback to SelectedMenuItem if available
        else if (SelectedMenuItem != null)
        {
            // Convert SelectedMenuItem back to MenuItemDisplay for consistency
            item = new MenuItemDisplay
            {
                MenuItemId = SelectedMenuItem.MenuItemId,
                Sku = SelectedMenuItem.Sku,
                Name = SelectedMenuItem.Name,
                PictureUrl = SelectedMenuItem.PictureUrl,
                Price = SelectedMenuItem.Price,
                IsDiscountable = SelectedMenuItem.IsDiscountable,
                IsPartOfCombo = SelectedMenuItem.IsPartOfCombo,
                IsAvailable = SelectedMenuItem.IsAvailable,
                Category = SelectedMenuItem.Category,
                GroupName = SelectedMenuItem.GroupName,
                Description = SelectedMenuItem.Description
            };
        }

        if (item == null) 
        {
            ShowErrorDialog("No Selection", "Please select a menu item to copy details.");
            return;
        }

        try
        {
            var details = $"Menu Item Details:\n" +
                         $"ID: {item.MenuItemId}\n" +
                         $"Name: {item.Name}\n" +
                         $"SKU: {item.Sku}\n" +
                         $"Price: {item.Price}\n" +
                         $"Category: {item.Category}\n" +
                         $"Available: {item.IsAvailable}\n" +
                         $"Discountable: {item.IsDiscountable}\n" +
                         $"Part of Combo: {item.IsPartOfCombo}";

            // Copy to clipboard
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(details);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            
            ShowSuccessDialog("Success", "Menu item details copied to clipboard!");
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to copy details: {ex.Message}");
        }
    }

    // Other event handlers
    private void AnalyticsItems_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    
    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (sender is GridView gridView && gridView.SelectedItem is MenuItemDisplay selectedItem)
            {
                // Convert MenuItemDisplay to MenuItemVm for the SelectedMenuItem property
                SelectedMenuItem = new MenuItemVm
                {
                    MenuItemId = selectedItem.MenuItemId,
                    Sku = selectedItem.Sku,
                    Name = selectedItem.Name,
                    PictureUrl = selectedItem.PictureUrl,
                    Price = selectedItem.Price,
                    IsDiscountable = selectedItem.IsDiscountable,
                    IsPartOfCombo = selectedItem.IsPartOfCombo,
                    IsAvailable = selectedItem.IsAvailable,
                    Category = selectedItem.Category,
                    GroupName = selectedItem.GroupName,
                    Description = selectedItem.Description
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GridView_SelectionChanged: {ex.Message}");
        }
    }
    
    private void MenuItem_DragStarting(object sender, DragStartingEventArgs e) { }

    private void MenuItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Single tap - select the item
        if (sender is Border border && border.DataContext is MenuItemDisplay item)
        {
            // Convert to MenuItemVm and set as selected
            SelectedMenuItem = new MenuItemVm
            {
                MenuItemId = item.MenuItemId,
                Sku = item.Sku,
                Name = item.Name,
                PictureUrl = item.PictureUrl,
                Price = item.Price,
                IsDiscountable = item.IsDiscountable,
                IsPartOfCombo = item.IsPartOfCombo,
                IsAvailable = item.IsAvailable,
                Category = item.Category,
                GroupName = item.GroupName,
                Description = item.Description
            };
            UpdateButtonStates();
        }
    }

    private async void MenuItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // Double tap - show details dialog
        if (sender is Border border && border.DataContext is MenuItemDisplay item)
        {
            // Convert to MenuItemVm
            var menuItemVm = new MenuItemVm
            {
                MenuItemId = item.MenuItemId,
                Sku = item.Sku,
                Name = item.Name,
                PictureUrl = item.PictureUrl,
                Price = item.Price,
                IsDiscountable = item.IsDiscountable,
                IsPartOfCombo = item.IsPartOfCombo,
                IsAvailable = item.IsAvailable,
                Category = item.Category,
                GroupName = item.GroupName,
                Description = item.Description
            };

            // Set as selected item
            SelectedMenuItem = menuItemVm;
            UpdateButtonStates();

            // Show edit dialog (double-click opens edit mode)
            try
            {
                var dialog = new Dialogs.MenuItemCrudDialog(menuItemVm);
                dialog.XamlRoot = this.XamlRoot;
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    await LoadMenuItemsAsync();
                    await PopulateCategoryFilterAsync();
                    ApplyFiltersAndRefreshUI();
                    ShowSuccessDialog("Success", "Menu item updated successfully!");
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to show item details: {ex.Message}");
            }
        }
    }


    private void UpdateButtonStates()
    {
        bool hasSelection = SelectedMenuItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
        ToggleAvailabilityButton.IsEnabled = hasSelection;
        
        // Update floating action buttons
        QuickEditButton.IsEnabled = hasSelection;
        QuickDeleteButton.IsEnabled = hasSelection;
        QuickToggleButton.IsEnabled = hasSelection;
        
        // Update toggle button text based on current availability
        if (hasSelection && SelectedMenuItem != null)
        {
            ToggleAvailabilityButton.Content = SelectedMenuItem.IsAvailable ? "🔴 Make Unavailable" : "🟢 Make Available";
        }
        else
        {
            ToggleAvailabilityButton.Content = "🔄 Toggle";
        }
    }

    private async void ShowErrorDialog(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                PrimaryButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
        }
    }

    private async void ShowSuccessDialog(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                PrimaryButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show success dialog: {ex.Message}");
        }
    }

    private async Task LoadModifiersAsync()
    {
        // Placeholder implementation
        await Task.CompletedTask;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Supporting classes (using existing ones from MenuManagementPage)

// Converters
public sealed class TrendColorConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() switch
        {
            "Rising" => Microsoft.UI.Colors.Green,
            "Declining" => Microsoft.UI.Colors.Red,
            _ => Microsoft.UI.Colors.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class PriorityColorConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() switch
        {
            "High" => Microsoft.UI.Colors.Red,
            "Medium" => Microsoft.UI.Colors.Orange,
            "Low" => Microsoft.UI.Colors.Green,
            _ => Microsoft.UI.Colors.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
