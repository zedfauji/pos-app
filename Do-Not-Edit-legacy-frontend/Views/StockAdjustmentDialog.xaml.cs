using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class StockAdjustmentDialog : ContentDialog
{
    private readonly string _itemId;
    private readonly IInventoryService _inventoryService;

    public decimal Adjustment { get; private set; }
    public string? Notes { get; private set; }

    public StockAdjustmentDialog(string itemId)
    {
        this.InitializeComponent();
        _itemId = itemId;
        _inventoryService = new InventoryService(new HttpClient(), new NullLogger<InventoryService>());
        
        Loaded += StockAdjustmentDialog_Loaded;
    }

    private async void StockAdjustmentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var item = await _inventoryService.GetItemAsync(_itemId);
            if (item != null)
            {
                ItemInfoText.Text = $"{item.Name} (Current Stock: {item.Stock})";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading item: {ex.Message}");
            ItemInfoText.Text = "Item not found";
        }
    }

    private void AdjustmentTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Update placeholder text based on selection
        if (AdjustmentTypeBox.SelectedItem is ComboBoxItem item)
        {
            switch (item.Tag?.ToString())
            {
                case "Add":
                    AmountBox.PlaceholderText = "Amount to add";
                    break;
                case "Remove":
                    AmountBox.PlaceholderText = "Amount to remove";
                    break;
                case "Set":
                    AmountBox.PlaceholderText = "New stock level";
                    break;
            }
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate input
        if (AmountBox.Value <= 0)
        {
            args.Cancel = true;
            return;
        }

        // Calculate adjustment based on type
        var amount = (decimal)AmountBox.Value;
        var adjustmentType = (AdjustmentTypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

        Adjustment = adjustmentType switch
        {
            "Add" => amount,
            "Remove" => -amount,
            "Set" => amount, // This would need current stock to calculate difference
            _ => amount
        };

        Notes = NotesBox.Text?.Trim();
    }
}
