using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace MagiDesk.Frontend.Views;

public sealed partial class ComboEditDialog : ContentDialog
{
    private ComboItemViewModel? _editingCombo;
    private bool _isEditing;
    private ObservableCollection<ComboItemDetails> _comboItems = new();

    public ComboEditDialog()
    {
        this.InitializeComponent();
        InitializeDialog();
    }

    public ComboEditDialog(ComboItemViewModel combo)
    {
        this.InitializeComponent();
        _editingCombo = combo;
        _isEditing = true;
        InitializeDialog();
        LoadComboData();
    }

    private void InitializeDialog()
    {
        Title = _isEditing ? "Edit Combo Deal" : "Create New Combo Deal";
        
        // Set default values
        StartDatePicker.Date = DateTime.Now;
        EndDatePicker.Date = DateTime.Now.AddDays(30);
        
        ComboItemsList.ItemsSource = _comboItems;
        
        // Handle events
        PrimaryButtonClick += ComboEditDialog_PrimaryButtonClick;
        NameTextBox.TextChanged += UpdatePreview;
    }

    private void LoadComboData()
    {
        if (_editingCombo == null) return;

        NameTextBox.Text = _editingCombo.Name;
        DescriptionTextBox.Text = _editingCombo.Description;
        
        // Parse prices
        if (decimal.TryParse(_editingCombo.OriginalPrice.TrimStart('$'), out var originalPrice))
        {
            OriginalPriceNumberBox.Value = (double)originalPrice;
        }
        
        if (decimal.TryParse(_editingCombo.ComboPrice.TrimStart('$'), out var comboPrice))
        {
            ComboPriceNumberBox.Value = (double)comboPrice;
        }

        // Load mock items for demonstration
        _comboItems.Add(new ComboItemDetails
        {
            ItemName = "Burger Deluxe",
            Quantity = 1,
            Price = 12.00m,
            IsRequired = true
        });
        
        _comboItems.Add(new ComboItemDetails
        {
            ItemName = "French Fries",
            Quantity = 1,
            Price = 6.00m,
            IsRequired = true
        });
        
        _comboItems.Add(new ComboItemDetails
        {
            ItemName = "Soft Drink",
            Quantity = 1,
            Price = 3.50m,
            IsRequired = false
        });

        UpdatePreview(null, null);
    }

    private void PriceNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        UpdateSavings();
        UpdatePreview(null, null);
    }

    private void UpdateSavings()
    {
        var originalPrice = (decimal)OriginalPriceNumberBox.Value;
        var comboPrice = (decimal)ComboPriceNumberBox.Value;
        var savings = originalPrice - comboPrice;
        
        SavingsText.Text = savings > 0 ? $"Savings: ${savings:F2}" : "No savings";
        SavingsText.Foreground = savings > 0 ? 
            (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"] :
            (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
    }

    private void UpdatePreview(object sender, object e)
    {
        if (PreviewText == null) return;

        var name = NameTextBox?.Text ?? "";
        var itemCount = _comboItems.Count;
        var originalPrice = (decimal)(OriginalPriceNumberBox?.Value ?? 0);
        var comboPrice = (decimal)(ComboPriceNumberBox?.Value ?? 0);
        var savings = originalPrice - comboPrice;

        if (string.IsNullOrWhiteSpace(name) || itemCount == 0)
        {
            PreviewText.Text = "Add items and pricing to see preview";
        }
        else
        {
            PreviewText.Text = $"{name}\n{itemCount} items • ${comboPrice:F2} (Save ${savings:F2})";
        }
    }

    private async void AddItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ComboItemEditDialog();
        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            var newItem = dialog.GetItemData();
            _comboItems.Add(newItem);
            UpdatePreview(null, null);
        }
    }

    private async void EditItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ComboItemDetails item)
        {
            var dialog = new ComboItemEditDialog(item);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                var updatedItem = dialog.GetItemData();
                var index = _comboItems.IndexOf(item);
                _comboItems[index] = updatedItem;
                UpdatePreview(null, null);
            }
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ComboItemDetails item)
        {
            _comboItems.Remove(item);
            UpdatePreview(null, null);
        }
    }

    private async void ComboEditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please enter a combo name.");
            return;
        }

        if (_comboItems.Count == 0)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please add at least one item to the combo.");
            return;
        }

        if (OriginalPriceNumberBox.Value <= 0)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please enter a valid original price.");
            return;
        }

        if (ComboPriceNumberBox.Value <= 0)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please enter a valid combo price.");
            return;
        }

        if (ComboPriceNumberBox.Value >= OriginalPriceNumberBox.Value)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Combo price should be less than original price.");
            return;
        }

        // Validate dates
        if (EndDatePicker.Date <= StartDatePicker.Date)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("End date must be after start date.");
            return;
        }

        // If we get here, validation passed
        await ShowSuccessAsync(_isEditing ? "Combo deal updated successfully!" : "Combo deal created successfully!");
    }

    private async Task ShowValidationErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Validation Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        
        await dialog.ShowAsync();
    }

    private async Task ShowSuccessAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Success",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        
        await dialog.ShowAsync();
    }
}

// Simple dialog for adding/editing combo items
public sealed partial class ComboItemEditDialog : ContentDialog
{
    private ComboItemDetails? _editingItem;

    public ComboItemEditDialog()
    {
        InitializeComponent();
    }

    public ComboItemEditDialog(ComboItemDetails item)
    {
        _editingItem = item;
        InitializeComponent();
        LoadItemData();
    }

    private void InitializeComponent()
    {
        Title = _editingItem == null ? "Add Item" : "Edit Item";
        PrimaryButtonText = "Save";
        SecondaryButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;

        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(4) };
        
        var nameBox = new TextBox 
        { 
            Header = "Item Name*", 
            PlaceholderText = "e.g., Burger Deluxe" 
        };
        nameBox.Name = "ItemNameTextBox";
        panel.Children.Add(nameBox);
        
        var quantityBox = new NumberBox 
        { 
            Header = "Quantity*", 
            Minimum = 1, 
            Value = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
        };
        quantityBox.Name = "QuantityNumberBox";
        panel.Children.Add(quantityBox);
        
        var priceBox = new NumberBox 
        { 
            Header = "Price ($)*", 
            Minimum = 0.01,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
        };
        priceBox.Name = "PriceNumberBox";
        panel.Children.Add(priceBox);
        
        var requiredBox = new CheckBox 
        { 
            Content = "Required item", 
            IsChecked = true 
        };
        requiredBox.Name = "IsRequiredCheckBox";
        panel.Children.Add(requiredBox);

        Content = panel;
    }

    private void LoadItemData()
    {
        if (_editingItem == null) return;
        
        // This would need proper name resolution in a real implementation
        // For now, just set some default values
    }

    public ComboItemDetails GetItemData()
    {
        return new ComboItemDetails
        {
            ItemName = "Sample Item", // Would get from TextBox
            Quantity = 1,
            Price = 10.00m,
            IsRequired = true
        };
    }
}

public class ComboItemDetails
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public bool IsRequired { get; set; }
}
