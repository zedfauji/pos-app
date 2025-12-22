using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs.Menu;

namespace MagiDesk.Client.Views.Dialogs;

public sealed partial class MenuItemDialog : ContentDialog
{
    public string Sku => SkuTextBox.Text;
    public string ItemName => NameTextBox.Text;
    public string Description => DescriptionTextBox.Text;
    public string Category => CategoryTextBox.Text;
    public string? GroupName => string.IsNullOrWhiteSpace(GroupTextBox.Text) ? null : GroupTextBox.Text;
    public decimal SellingPrice => (decimal)PriceBox.Value;
    public decimal? VendorPrice => double.IsNaN(CostBox.Value) ? null : (decimal)CostBox.Value;
    public string? PictureUrl => string.IsNullOrWhiteSpace(PictureUrlTextBox.Text) ? null : PictureUrlTextBox.Text;
    public bool IsAvailable => IsAvailableCheckBox.IsChecked ?? false;
    public bool IsDiscountable => IsDiscountableCheckBox.IsChecked ?? false;
    public bool IsPartOfCombo => IsComboCheckBox.IsChecked ?? false;

    public MenuItemDialog(MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto? item = null, bool isEdit = false)
    {
        this.InitializeComponent();

        if (item != null)
        {
            Title = isEdit ? "Edit Menu Item" : "New Menu Item";
            SkuTextBox.Text = item.Sku;
            SkuTextBox.IsEnabled = !isEdit; // SKU cannot be changed on edit
            NameTextBox.Text = item.Name;
            DescriptionTextBox.Text = item.Description ?? "";
            CategoryTextBox.Text = item.Category;
            GroupTextBox.Text = item.GroupName ?? "";
            PriceBox.Value = (double)item.SellingPrice;
            CostBox.Value = (double)(item.Price ?? 0);
            PictureUrlTextBox.Text = item.PictureUrl ?? "";
            IsAvailableCheckBox.IsChecked = item.IsAvailable;
            IsDiscountableCheckBox.IsChecked = item.IsDiscountable;
            IsComboCheckBox.IsChecked = item.IsPartOfCombo;
        }
    }

    public MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto GetResult()
    {
        return new MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto(
            SkuTextBox.Text,
            NameTextBox.Text,
            DescriptionTextBox.Text,
            CategoryTextBox.Text,
            string.IsNullOrWhiteSpace(GroupTextBox.Text) ? null : GroupTextBox.Text,
            VendorPrice ?? 0,
            SellingPrice,
            null, // Price (MSRP) is not exposed in UI yet
            PictureUrl,
            IsDiscountable,
            IsPartOfCombo,
            IsAvailable
        );
    }
}
