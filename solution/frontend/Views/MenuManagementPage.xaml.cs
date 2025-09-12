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

namespace MagiDesk.Frontend.Views;

// Simple wrapper class for XAML binding without init-only properties
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
}

public sealed partial class MenuManagementPage : Page, INotifyPropertyChanged
{
    public ObservableCollection<MenuItemDisplay> MenuItems { get; } = new();
    public ObservableCollection<ModifierManagementViewModel> Modifiers { get; } = new();
    
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
            MenuItems.Clear();
            System.Diagnostics.Debug.WriteLine("MenuManagementPage: Loading menu items...");
            var items = await _menuService.ListItemsAsync(new MenuApiService.ItemsQuery(Q: null, Category: null, GroupName: null, AvailableOnly: null));
            System.Diagnostics.Debug.WriteLine($"MenuManagementPage: Retrieved {items.Count()} items from API");
            
            foreach (var item in items)
            {
                MenuItems.Add(new MenuItemDisplay
                {
                    MenuItemId = item.Id,
                    Name = item.Name,
                    Price = item.SellingPrice,
                    PictureUrl = item.PictureUrl,
                    Sku = item.Sku,
                    IsAvailable = item.IsAvailable,
                    IsDiscountable = item.IsDiscountable,
                    IsPartOfCombo = item.IsPartOfCombo
                });
            }
            
            System.Diagnostics.Debug.WriteLine($"MenuManagementPage: Added {MenuItems.Count} items to collection");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MenuManagementPage: Error loading menu items: {ex.Message}");
            ShowErrorDialog("Error", $"Failed to load menu items: {ex.Message}");
        }
    }

    private async void LoadMenuItems()
    {
        await LoadMenuItemsAsync();
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
                IsAvailable = true
            };
            var dialog = new Dialogs.MenuItemCrudDialog(newItem);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                LoadMenuItems(); // Refresh the list
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
                    await LoadMenuItemsAsync(); // Refresh the list
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
        LoadMenuItems();
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
                IsPartOfCombo = display.IsPartOfCombo
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
