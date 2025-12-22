using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class DiscountEditDialog : ContentDialog
{
    private DiscountItemViewModel? _editingDiscount;
    private bool _isEditing;

    public DiscountEditDialog()
    {
        this.InitializeComponent();
        InitializeDialog();
    }

    public DiscountEditDialog(DiscountItemViewModel discount)
    {
        this.InitializeComponent();
        _editingDiscount = discount;
        _isEditing = true;
        InitializeDialog();
        LoadDiscountData();
    }

    private void InitializeDialog()
    {
        Title = _isEditing ? "Edit Discount" : "Create New Discount";
        
        // Set default values
        StartDatePicker.Date = DateTime.Now;
        EndDatePicker.Date = DateTime.Now.AddDays(30);
        DiscountTypeComboBox.SelectedIndex = 0;
        TargetAudienceComboBox.SelectedIndex = 0;
        PriorityComboBox.SelectedIndex = 1; // Medium priority
        
        // Handle button clicks
        PrimaryButtonClick += DiscountEditDialog_PrimaryButtonClick;
    }

    private void LoadDiscountData()
    {
        if (_editingDiscount == null) return;

        NameTextBox.Text = _editingDiscount.Name;
        DescriptionTextBox.Text = _editingDiscount.Description;
        
        // Parse discount type and value
        if (_editingDiscount.DiscountValue.EndsWith("%"))
        {
            DiscountTypeComboBox.SelectedIndex = 0; // Percentage
            if (decimal.TryParse(_editingDiscount.DiscountValue.TrimEnd('%'), out var percentage))
            {
                PercentageNumberBox.Value = (double)percentage;
            }
        }
        else if (_editingDiscount.DiscountValue.StartsWith("$"))
        {
            DiscountTypeComboBox.SelectedIndex = 1; // Fixed Amount
            if (decimal.TryParse(_editingDiscount.DiscountValue.TrimStart('$'), out var amount))
            {
                FixedAmountNumberBox.Value = (double)amount;
            }
        }

        // Parse end date
        if (DateTime.TryParse(_editingDiscount.EndDate, out var endDate))
        {
            EndDatePicker.Date = endDate;
        }
    }

    private void DiscountTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Hide all panels first
        PercentagePanel.Visibility = Visibility.Collapsed;
        FixedAmountPanel.Visibility = Visibility.Collapsed;
        FreeItemPanel.Visibility = Visibility.Collapsed;
        BuyXGetYPanel.Visibility = Visibility.Collapsed;

        // Show relevant panel based on selection
        if (DiscountTypeComboBox.SelectedItem is ComboBoxItem item)
        {
            switch (item.Tag?.ToString())
            {
                case "Percentage":
                    PercentagePanel.Visibility = Visibility.Visible;
                    break;
                case "FixedAmount":
                    FixedAmountPanel.Visibility = Visibility.Visible;
                    break;
                case "FreeItem":
                    FreeItemPanel.Visibility = Visibility.Visible;
                    break;
                case "BuyXGetY":
                    BuyXGetYPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    private async void DiscountEditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please enter a discount name.");
            return;
        }

        if (DiscountTypeComboBox.SelectedItem == null)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please select a discount type.");
            return;
        }

        // Validate discount value based on type
        var selectedType = ((ComboBoxItem)DiscountTypeComboBox.SelectedItem).Tag?.ToString();
        switch (selectedType)
        {
            case "Percentage":
                if (PercentageNumberBox.Value <= 0 || PercentageNumberBox.Value > 100)
                {
                    args.Cancel = true;
                    await ShowValidationErrorAsync("Please enter a valid percentage (1-100).");
                    return;
                }
                break;
            case "FixedAmount":
                if (FixedAmountNumberBox.Value <= 0)
                {
                    args.Cancel = true;
                    await ShowValidationErrorAsync("Please enter a valid discount amount.");
                    return;
                }
                break;
            case "FreeItem":
                if (string.IsNullOrWhiteSpace(FreeItemTextBox.Text))
                {
                    args.Cancel = true;
                    await ShowValidationErrorAsync("Please enter a free item.");
                    return;
                }
                break;
            case "BuyXGetY":
                if (BuyQuantityNumberBox.Value <= 0 || GetQuantityNumberBox.Value <= 0)
                {
                    args.Cancel = true;
                    await ShowValidationErrorAsync("Please enter valid buy and get quantities.");
                    return;
                }
                break;
        }

        // Validate dates
        if (EndDatePicker.Date <= StartDatePicker.Date)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("End date must be after start date.");
            return;
        }

        // If we get here, validation passed
        // In a real implementation, you would save the discount here
        await ShowSuccessAsync(_isEditing ? "Discount updated successfully!" : "Discount created successfully!");
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

    public DiscountItemViewModel GetDiscountData()
    {
        var discount = new DiscountItemViewModel
        {
            Id = _editingDiscount?.Id ?? Guid.NewGuid().ToString(),
            Name = NameTextBox.Text,
            Description = DescriptionTextBox.Text,
            EndDate = EndDatePicker.Date.DateTime.ToString("MMM dd, yyyy"),
            Type = ((ComboBoxItem)DiscountTypeComboBox.SelectedItem)?.Content?.ToString() ?? ""
        };

        // Set discount value based on type
        var selectedType = ((ComboBoxItem)DiscountTypeComboBox.SelectedItem)?.Tag?.ToString();
        switch (selectedType)
        {
            case "Percentage":
                discount.DiscountValue = $"{PercentageNumberBox.Value}%";
                break;
            case "FixedAmount":
                discount.DiscountValue = $"${FixedAmountNumberBox.Value:F2}";
                break;
            case "FreeItem":
                discount.DiscountValue = "Free Item";
                break;
            case "BuyXGetY":
                discount.DiscountValue = $"Buy {BuyQuantityNumberBox.Value} Get {GetQuantityNumberBox.Value}";
                break;
        }

        return discount;
    }
}
