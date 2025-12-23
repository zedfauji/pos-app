using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class InventoryItemDialog : ContentDialog
{
    public InventoryItemDto Item { get; private set; }

    public InventoryItemDialog(InventoryItemDto? existingItem = null)
    {
        this.InitializeComponent();
        
        if (existingItem != null)
        {
            Item = existingItem;
            LoadExistingItem();
        }
        else
        {
            Item = new InventoryItemDto
            {
                Id = Guid.NewGuid().ToString(),
                IsActive = true,
                Stock = 0,
                MinStock = 0,
                UnitCost = 0
            };
        }
    }

    private void LoadExistingItem()
    {
        SkuBox.Text = Item.Sku ?? string.Empty;
        NameBox.Text = Item.Name ?? string.Empty;
        
        // Set category
        foreach (ComboBoxItem item in CategoryBox.Items)
        {
            if (item.Tag?.ToString() == Item.Category)
            {
                CategoryBox.SelectedItem = item;
                break;
            }
        }
        
        StockBox.Value = Item.Stock;
        MinStockBox.Value = Item.MinStock;
        UnitCostBox.Value = (double)Item.UnitCost;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(SkuBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
        {
            args.Cancel = true;
            return;
        }

        // Update item with form data
        Item.Sku = SkuBox.Text.Trim();
        Item.Name = NameBox.Text.Trim();
        Item.Category = (CategoryBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Food";
        Item.Stock = (int)StockBox.Value;
        Item.MinStock = (int)MinStockBox.Value;
        Item.UnitCost = (decimal)UnitCostBox.Value;
    }
}
