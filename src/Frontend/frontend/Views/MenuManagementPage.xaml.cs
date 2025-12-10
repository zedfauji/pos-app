using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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

// Enhanced wrapper class for XAML binding with category support
public sealed class MenuItemDisplay
{
    public long MenuItemId { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PictureUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsDiscountable { get; set; }
    public bool IsPartOfCombo { get; set; }
    public bool IsAvailable { get; set; }
    public string Category { get; set; } = "";
    public string? GroupName { get; set; }
    public string? Description { get; set; }
}

// Category group for organizing menu items
public sealed class CategoryGroup
{
    public string CategoryName { get; set; } = "";
    public ObservableCollection<MenuItemDisplay> Items { get; } = new();
    public int ItemCount => Items.Count;
    public bool IsExpanded { get; set; } = true;
    public object? Tag { get; set; }
}

public sealed partial class MenuManagementPage : Page, INotifyPropertyChanged
{
    // Collections
    public ObservableCollection<MenuItemDisplay> AllMenuItems { get; } = new();
    public ObservableCollection<ModifierManagementViewModel> Modifiers { get; } = new();
    public ObservableCollection<CategoryGroup> CategoryGroups { get; } = new();
    
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

    private ModifierManagementViewModel? _selectedModifier;
    public ModifierManagementViewModel? SelectedModifier
    {
        get => _selectedModifier;
        set
        {
            if (_selectedModifier != value)
            {
                _selectedModifier = value;
                OnPropertyChanged(nameof(SelectedModifier));
                ModifierDetailsPanel.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
                if (value != null)
                {
                    LoadModifierDetails(value);
                }
            }
        }
    }

    private readonly MenuApiService? _menuService;

    public MenuManagementPage()
    {
        try
        {
            this.InitializeComponent();
            this.DataContext = this; // CRITICAL: Set DataContext for XAML bindings
            _menuService = App.Menu;
            
            if (_menuService == null)
            {
                ShowErrorDialog("Service Not Available", "Menu service is not available. Please restart the application or contact support.");
                return;
            }
            
            // Load menu items and modifiers asynchronously
            Loaded += async (s, e) => 
            {
                await LoadMenuItemsAsync();
                await LoadModifiersAsync();
                await PopulateCategoryFilterAsync();
                ApplyFiltersAndRefreshUI();
            };
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Initialization Error", $"Failed to initialize Menu Management page: {ex.Message}");
        }
    }

    private async Task LoadMenuItemsAsync()
    {
        if (_menuService == null) return;
        
        try
        {
            AllMenuItems.Clear();
            Debug.WriteLine("MenuManagementPage: Loading menu items...");
            var items = await _menuService.ListItemsAsync(new MenuApiService.ItemsQuery(Q: null, Category: null, GroupName: null, AvailableOnly: null));
            Debug.WriteLine($"MenuManagementPage: Retrieved {items.Count()} items from API");
            
            foreach (var item in items)
            {
                AllMenuItems.Add(new MenuItemDisplay
                {
                    MenuItemId = item.Id,
                    Name = item.Name,
                    Price = item.SellingPrice,
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
            
            Debug.WriteLine($"MenuManagementPage: Added {AllMenuItems.Count} items to collection");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MenuManagementPage: Error loading menu items: {ex.Message}");
            ShowErrorDialog("Error", $"Failed to load menu items: {ex.Message}");
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

            // Availability filter
            if (!string.IsNullOrEmpty(_selectedAvailability))
            {
                switch (_selectedAvailability)
                {
                    case "Available":
                        filteredItems = filteredItems.Where(x => x.IsAvailable);
                        break;
                    case "Unavailable":
                        filteredItems = filteredItems.Where(x => !x.IsAvailable);
                        break;
                }
            }

            // Sort
            if (!string.IsNullOrEmpty(_selectedSortOption))
            {
                switch (_selectedSortOption)
                {
                    case "NameAsc":
                        filteredItems = filteredItems.OrderBy(x => x.Name);
                        break;
                    case "NameDesc":
                        filteredItems = filteredItems.OrderByDescending(x => x.Name);
                        break;
                    case "PriceAsc":
                        filteredItems = filteredItems.OrderBy(x => x.Price);
                        break;
                    case "PriceDesc":
                        filteredItems = filteredItems.OrderByDescending(x => x.Price);
                        break;
                    case "Category":
                        filteredItems = filteredItems.OrderBy(x => x.Category).ThenBy(x => x.Name);
                        break;
                }
            }
            else
            {
                // Default sort by category then name
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

                // Create UI for this category
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
                HorizontalAlignment = HorizontalAlignment.Left,
                AllowDrop = true
            };

            headerButton.Click += (s, e) => ToggleCategoryExpansion(categoryGroup, headerButton);
            headerButton.DragOver += CategoryHeader_DragOver;
            headerButton.Drop += CategoryHeader_Drop;

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

            // Items grid (initially visible)
            var itemsGrid = new GridView
            {
                ItemsSource = categoryGroup.Items,
                SelectionMode = ListViewSelectionMode.Single,
                Margin = new Thickness(20, 12, 0, 0)
            };

            // Use a simple template defined in XAML
            var template = (DataTemplate)this.Resources["MenuItemTemplate"];
            if (template != null)
            {
                itemsGrid.ItemTemplate = template;
            }
            
            itemsGrid.SelectionChanged += (s, e) => GridView_SelectionChanged(s, e);

            CategoriesContainer.Children.Add(itemsGrid);

            // Store reference for expansion toggle
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

    // Event handlers for new UI controls
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

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            _selectedCategory = item.Tag?.ToString();
            ApplyFiltersAndRefreshUI();
        }
    }

    private void AvailabilityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            _selectedAvailability = item.Tag?.ToString();
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

    private async void ToggleAvailability_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMenuItem == null || _menuService == null) return;

        try
        {
            var newAvailability = !SelectedMenuItem.IsAvailable;
            var success = await _menuService.SetItemAvailabilityAsync(SelectedMenuItem.MenuItemId, newAvailability);
            
            if (success)
            {
                SelectedMenuItem.IsAvailable = newAvailability;
                await LoadMenuItemsAsync();
                ApplyFiltersAndRefreshUI();
                ShowErrorDialog("Success", $"Item availability updated to {(newAvailability ? "Available" : "Unavailable")}");
            }
            else
            {
                ShowErrorDialog("Error", "Failed to update item availability");
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to toggle availability: {ex.Message}");
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
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to add menu item: {ex.Message}");
        }
    }

    private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMenuItem == null) return;
        
        try
        {
            var dialog = new Dialogs.MenuItemCrudDialog(SelectedMenuItem);
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
            ShowErrorDialog("Error", $"Failed to edit menu item: {ex.Message}");
        }
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMenuItem == null) return;
        
        try
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Menu Item",
                Content = $"Are you sure you want to delete '{SelectedMenuItem.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            
            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (_menuService != null)
                {
                    // Note: DeleteItemAsync is not available in MenuApiService
                    // For now, just show a message that deletion is not supported
                    ShowErrorDialog("Not Supported", "Menu item deletion is not currently supported through the API.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to delete menu item: {ex.Message}");
        }
    }

    private async void RefreshMenuItems_Click(object sender, RoutedEventArgs e)
    {
        await LoadMenuItemsAsync();
        await PopulateCategoryFilterAsync();
        ApplyFiltersAndRefreshUI();
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is GridView gridView && gridView.SelectedItem is MenuItemDisplay display)
        {
            SelectedMenuItem = new MenuItemVm
            {
                MenuItemId = display.MenuItemId,
                Name = display.Name,
                Price = display.Price,
                PictureUrl = display.PictureUrl,
                Sku = display.Sku,
                IsAvailable = display.IsAvailable,
                IsDiscountable = display.IsDiscountable,
                IsPartOfCombo = display.IsPartOfCombo,
                Category = display.Category,
                GroupName = display.GroupName,
                Description = display.Description
            };
            UpdateButtonStates();
        }
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = SelectedMenuItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
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
            // Fallback: Log error if dialog fails
            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
        }
    }

    // Modifier-related methods
    private async Task LoadModifiersAsync()
    {
        if (_menuService == null) return;
        
        try
        {
            Modifiers.Clear();
            Debug.WriteLine("MenuManagementPage: Loading modifiers...");
            var modifiers = await _menuService.ListModifiersAsync(new MenuApiService.ModifierQuery(Q: null));
            Debug.WriteLine($"MenuManagementPage: Retrieved {modifiers.Count()} modifiers from API");
            
            foreach (var modifier in modifiers)
            {
                Modifiers.Add(new ModifierManagementViewModel
                {
                    Id = modifier.Id,
                    Name = modifier.Name,
                    Description = "", // ModifierDto doesn't have Description property
                    IsRequired = modifier.IsRequired,
                    AllowMultiple = modifier.AllowMultiple,
                    MaxSelections = modifier.MaxSelections,
                    Options = new ObservableCollection<ModifierOptionManagementViewModel>(
                        modifier.Options.Select(o => new ModifierOptionManagementViewModel
                        {
                            Id = o.Id,
                            Name = o.Name,
                            PriceDelta = o.PriceDelta
                        })
                    )
                });
            }
            
            Debug.WriteLine($"MenuManagementPage: Added {Modifiers.Count} modifiers to collection");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MenuManagementPage: Error loading modifiers: {ex.Message}");
            ShowErrorDialog("Error", $"Failed to load modifiers: {ex.Message}");
        }
    }

    private void LoadModifierDetails(ModifierManagementViewModel modifier)
    {
        ModifierNameTextBox.Text = modifier.Name;
        ModifierDescriptionTextBox.Text = modifier.Description;
        IsRequiredCheckBox.IsChecked = modifier.IsRequired;
        AllowMultipleCheckBox.IsChecked = modifier.AllowMultiple;
        MaxSelectionsNumberBox.Value = modifier.MaxSelections ?? 1;
        
        // Show/hide max selections panel based on allow multiple
        MaxSelectionsPanel.Visibility = modifier.AllowMultiple ? Visibility.Visible : Visibility.Collapsed;
        
        // Load options
        OptionsListView.ItemsSource = modifier.Options;
    }

    private async void AddModifier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Dialogs.ModifierCrudDialog();
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadModifiersAsync(); // Refresh the list
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to add modifier: {ex.Message}");
        }
    }

    private async void EditModifier_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedModifier == null) return;
        
        try
        {
            var dialog = new Dialogs.ModifierCrudDialog(SelectedModifier);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadModifiersAsync(); // Refresh the list
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to edit modifier: {ex.Message}");
        }
    }

    private async void DeleteModifier_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedModifier == null) return;
        
        try
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Modifier",
                Content = $"Are you sure you want to delete '{SelectedModifier.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            
            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (_menuService != null)
                {
                    await _menuService.DeleteModifierAsync(SelectedModifier.Id);
                    await LoadModifiersAsync(); // Refresh the list
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to delete modifier: {ex.Message}");
        }
    }

    private void ModifiersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is ModifierManagementViewModel modifier)
        {
            SelectedModifier = modifier;
        }
    }

    private async void AddOption_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedModifier == null) return;
        
        try
        {
            var dialog = new Dialogs.ModifierOptionCrudDialog();
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                // Refresh the modifier details
                LoadModifierDetails(SelectedModifier);
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to add option: {ex.Message}");
        }
    }

    private async void EditOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierOptionManagementViewModel option)
        {
            try
            {
                var dialog = new Dialogs.ModifierOptionCrudDialog(option);
                dialog.XamlRoot = this.XamlRoot;
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    // Refresh the modifier details
                    if (SelectedModifier != null)
                    {
                        LoadModifierDetails(SelectedModifier);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to edit option: {ex.Message}");
            }
        }
    }

    private async void DeleteOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierOptionManagementViewModel option)
        {
            try
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Delete Option",
                    Content = $"Are you sure you want to delete '{option.Name}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };
                
                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                if (_menuService != null)
                {
                    // Note: DeleteModifierOptionAsync is not available in MenuApiService
                    // For now, just show a message that deletion is not supported
                    ShowErrorDialog("Not Supported", "Modifier option deletion is not currently supported through the API.");
                    return;
                }
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to delete option: {ex.Message}");
            }
        }
    }

    private async void SaveModifier_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedModifier == null) return;
        
        try
        {
            if (_menuService == null) return;
            
            // Update the modifier with current form values
            var updateDto = new MenuApiService.UpdateModifierDto(
                Name: ModifierNameTextBox.Text,
                Description: ModifierDescriptionTextBox.Text,
                IsRequired: IsRequiredCheckBox.IsChecked ?? false,
                AllowMultiple: AllowMultipleCheckBox.IsChecked ?? false,
                MaxSelections: (int)MaxSelectionsNumberBox.Value,
                MinSelections: 0,
                Options: new List<MenuApiService.UpdateModifierOptionDto>()
            );
            
            await _menuService.UpdateModifierAsync(SelectedModifier.Id, updateDto);
            await LoadModifiersAsync(); // Refresh the list
            
            ShowErrorDialog("Success", "Modifier updated successfully!");
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to save modifier: {ex.Message}");
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        // Reset form to original values
        if (SelectedModifier != null)
        {
            LoadModifierDetails(SelectedModifier);
        }
    }

    // Drag and Drop Event Handlers
    private void MenuItem_DragStarting(object sender, DragStartingEventArgs e)
    {
        try
        {
            if (sender is Border border && border.DataContext is MenuItemDisplay item)
            {
                e.Data.SetText($"MenuItem:{item.MenuItemId}");
                e.Data.Properties.Add("MenuItem", item);
                e.DragUI.SetContentFromDataPackage();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in MenuItem_DragStarting: {ex.Message}");
        }
    }

    private void CategoryHeader_DragOver(object sender, DragEventArgs e)
    {
        try
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.DragUIOverride.Caption = "Move to this category";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in CategoryHeader_DragOver: {ex.Message}");
        }
    }

    private async void CategoryHeader_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (sender is Button button && e.Data.Properties.TryGetValue("MenuItem", out var itemObj) && itemObj is MenuItemDisplay item)
            {
                // Extract category name from button content
                string targetCategory = ExtractCategoryNameFromButton(button);
                
                if (!string.IsNullOrEmpty(targetCategory) && targetCategory != item.Category)
                {
                    // Update the item's category
                    await UpdateMenuItemCategoryAsync(item, targetCategory);
                    
                    // Refresh the UI
                    await LoadMenuItemsAsync();
                    await PopulateCategoryFilterAsync();
                    ApplyFiltersAndRefreshUI();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in CategoryHeader_Drop: {ex.Message}");
            ShowErrorDialog("Error", $"Failed to move item to category: {ex.Message}");
        }
    }

    private string ExtractCategoryNameFromButton(Button button)
    {
        try
        {
            if (button.Content is StackPanel stackPanel && stackPanel.Children.Count >= 2)
            {
                if (stackPanel.Children[1] is TextBlock textBlock)
                {
                    string text = textBlock.Text;
                    // Extract category name from "CategoryName (count)" format
                    int parenIndex = text.IndexOf('(');
                    if (parenIndex > 0)
                    {
                        return text.Substring(0, parenIndex).Trim();
                    }
                    return text.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting category name: {ex.Message}");
        }
        return string.Empty;
    }

    private async Task UpdateMenuItemCategoryAsync(MenuItemDisplay item, string newCategory)
    {
        try
        {
            // Get the MenuApiService from DI container
            var menuApiService = App.Services.GetService(typeof(MenuApiService)) as MenuApiService;
            if (menuApiService != null)
            {
                // Create update DTO with only the category field changed
                var updateDto = new MenuApiService.UpdateMenuItemDto(
                    Name: null,
                    Description: null,
                    Category: newCategory,
                    GroupName: null,
                    VendorPrice: null,
                    SellingPrice: null,
                    Price: null,
                    PictureUrl: null,
                    IsDiscountable: null,
                    IsPartOfCombo: null,
                    IsAvailable: null
                );

                await menuApiService.UpdateMenuItemAsync(item.MenuItemId, updateDto);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating menu item category: {ex.Message}");
            throw;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Converter for availability display
public sealed class AvailabilityConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isAvailable)
        {
            return isAvailable ? "✓ Available" : "✗ Unavailable";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
