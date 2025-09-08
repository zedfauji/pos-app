using MagiDesk.Shared.DTOs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Dialogs;

public class ItemDialog : ContentDialog
{
    private readonly TextBox _sku = new() { PlaceholderText = "SKU (auto)", IsReadOnly = true };
    private readonly TextBox _name = new() { PlaceholderText = "Name" };
    private readonly NumberBox _price = new() { PlaceholderText = "Price", SmallChange = 1, LargeChange = 10 };
    private readonly NumberBox _stock = new() { PlaceholderText = "Stock", SmallChange = 1, LargeChange = 5 };

    public ItemDto Dto { get; }

    public ItemDialog(ItemDto dto)
    {
        Dto = dto;
        Title = string.IsNullOrWhiteSpace(dto.Id) ? "Add Item" : "Edit Item";
        PrimaryButtonText = "Save";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;

        if (string.IsNullOrWhiteSpace(dto.Sku))
        {
            dto.Sku = Guid.NewGuid().ToString("N");
        }
        _sku.Text = dto.Sku ?? string.Empty;
        _name.Text = dto.Name ?? string.Empty;
        _price.Value = (double)dto.Price;
        _stock.Value = dto.Stock;

        Content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock{ Text = "SKU"}, _sku,
                new TextBlock{ Text = "Name"}, _name,
                new TextBlock{ Text = "Price"}, _price,
                new TextBlock{ Text = "Stock"}, _stock,
            }
        };

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Dto.Sku = _sku.Text;
        Dto.Name = _name.Text;
        Dto.Price = (decimal)_price.Value;
        Dto.Stock = (int)_stock.Value;
    }
}
