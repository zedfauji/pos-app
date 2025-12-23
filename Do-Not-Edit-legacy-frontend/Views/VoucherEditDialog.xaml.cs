using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class VoucherEditDialog : ContentDialog
{
    private VoucherItemViewModel? _editingVoucher;
    private bool _isEditing;

    public VoucherEditDialog()
    {
        this.InitializeComponent();
        InitializeDialog();
    }

    public VoucherEditDialog(VoucherItemViewModel voucher)
    {
        this.InitializeComponent();
        _editingVoucher = voucher;
        _isEditing = true;
        InitializeDialog();
        LoadVoucherData();
    }

    private void InitializeDialog()
    {
        Title = _isEditing ? "Edit Voucher" : "Create New Voucher";
        
        // Set default values
        ExpiryDatePicker.Date = DateTime.Now.AddDays(30);
        DiscountTypeComboBox.SelectedIndex = 0; // Percentage
        
        // Handle events
        PrimaryButtonClick += VoucherEditDialog_PrimaryButtonClick;
        NameTextBox.TextChanged += UpdatePreview;
        CodeTextBox.TextChanged += UpdatePreview;
        PercentageNumberBox.ValueChanged += UpdatePreview;
        FixedAmountNumberBox.ValueChanged += UpdatePreview;
    }

    private void LoadVoucherData()
    {
        if (_editingVoucher == null) return;

        NameTextBox.Text = _editingVoucher.Name;
        CodeTextBox.Text = _editingVoucher.Code;
        
        // Parse discount value
        if (_editingVoucher.Value.EndsWith("%"))
        {
            DiscountTypeComboBox.SelectedIndex = 0; // Percentage
            if (decimal.TryParse(_editingVoucher.Value.TrimEnd('%'), out var percentage))
            {
                PercentageNumberBox.Value = (double)percentage;
            }
        }
        else if (_editingVoucher.Value.StartsWith("$"))
        {
            DiscountTypeComboBox.SelectedIndex = 1; // Fixed Amount
            if (decimal.TryParse(_editingVoucher.Value.TrimStart('$'), out var amount))
            {
                FixedAmountNumberBox.Value = (double)amount;
            }
        }

        // Parse expiry date
        if (DateTime.TryParse(_editingVoucher.ExpiryDate, out var expiryDate))
        {
            ExpiryDatePicker.Date = expiryDate;
        }

        UpdatePreview(null, null);
    }

    private void DiscountTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiscountTypeComboBox.SelectedItem is ComboBoxItem item)
        {
            switch (item.Tag?.ToString())
            {
                case "Percentage":
                    PercentageNumberBox.Visibility = Visibility.Visible;
                    FixedAmountNumberBox.Visibility = Visibility.Collapsed;
                    break;
                case "FixedAmount":
                    PercentageNumberBox.Visibility = Visibility.Collapsed;
                    FixedAmountNumberBox.Visibility = Visibility.Visible;
                    break;
            }
        }
        UpdatePreview(null, null);
    }

    private void UpdatePreview(object sender, object e)
    {
        if (PreviewText == null) return;

        var name = NameTextBox?.Text ?? "";
        var code = CodeTextBox?.Text ?? "";
        var discountText = "";

        if (DiscountTypeComboBox?.SelectedItem is ComboBoxItem item)
        {
            switch (item.Tag?.ToString())
            {
                case "Percentage":
                    discountText = $"{PercentageNumberBox?.Value ?? 0}% off";
                    break;
                case "FixedAmount":
                    discountText = $"${FixedAmountNumberBox?.Value ?? 0:F2} off";
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            PreviewText.Text = "Enter voucher details to see preview";
        }
        else
        {
            PreviewText.Text = $"Code: {code}\n{name} - {discountText}";
        }
    }

    private async void VoucherEditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please enter a voucher name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please enter a voucher code.");
            return;
        }

        if (DiscountTypeComboBox.SelectedItem == null)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Please select a discount type.");
            return;
        }

        // Validate discount value
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
        }

        // Validate expiry date
        if (ExpiryDatePicker.Date <= DateTime.Now)
        {
            args.Cancel = true;
            await ShowValidationErrorAsync("Expiry date must be in the future.");
            return;
        }

        // If we get here, validation passed
        await ShowSuccessAsync(_isEditing ? "Voucher updated successfully!" : "Voucher created successfully!");
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

    public VoucherItemViewModel GetVoucherData()
    {
        var voucher = new VoucherItemViewModel
        {
            Id = _editingVoucher?.Id ?? Guid.NewGuid().ToString(),
            Name = NameTextBox.Text,
            Code = CodeTextBox.Text.ToUpper(),
            ExpiryDate = ExpiryDatePicker.Date.DateTime.ToString("MMM dd, yyyy")
        };

        // Set discount value based on type
        var selectedType = ((ComboBoxItem)DiscountTypeComboBox.SelectedItem)?.Tag?.ToString();
        switch (selectedType)
        {
            case "Percentage":
                voucher.Value = $"{PercentageNumberBox.Value}%";
                break;
            case "FixedAmount":
                voucher.Value = $"${FixedAmountNumberBox.Value:F2}";
                break;
        }

        return voucher;
    }
}
