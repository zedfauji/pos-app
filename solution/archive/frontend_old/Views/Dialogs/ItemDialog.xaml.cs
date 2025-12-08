using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.Views;

public sealed class ItemDialog : ContentDialog
{
    private readonly ItemDto _dto;
    private readonly TextBox _skuText = new() { Header = "SKU" };
    private readonly TextBox _nameText = new() { Header = "Name" };
    private readonly NumberBox _priceNumber = new() { Header = "Price", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, Minimum = 0 };
    private readonly NumberBox _stockNumber = new() { Header = "Stock", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, Minimum = 0 };

    public ItemDialog(ItemDto dto)
    {
        _dto = dto;

        Title = "Item";
        PrimaryButtonText = "Save";
        CloseButtonText = "Cancel";

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(_skuText);
        panel.Children.Add(_nameText);
        panel.Children.Add(_priceNumber);
        panel.Children.Add(_stockNumber);
        Content = panel;

        Loaded += OnLoaded;
        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _skuText.Text = _dto.Sku;
        _nameText.Text = _dto.Name;
        _priceNumber.Value = (double)_dto.Price;
        _stockNumber.Value = _dto.Stock;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _dto.Sku = _skuText.Text?.Trim() ?? string.Empty;
        _dto.Name = _nameText.Text?.Trim() ?? string.Empty;
        _dto.Price = (decimal)_priceNumber.Value;
        _dto.Stock = (int)_stockNumber.Value;
    }
}
