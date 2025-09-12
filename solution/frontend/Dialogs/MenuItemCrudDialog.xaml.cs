using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;

namespace MagiDesk.Frontend.Dialogs;

// Editable wrapper for MenuItemVm since the original has init-only properties
public sealed class EditableMenuItemVm
{
    public long MenuItemId { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PictureUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsDiscountable { get; set; }
    public bool IsPartOfCombo { get; set; }
    public bool IsAvailable { get; set; }

    public static EditableMenuItemVm FromMenuItemVm(MenuItemVm item)
    {
        return new EditableMenuItemVm
        {
            MenuItemId = item.MenuItemId,
            Sku = item.Sku,
            Name = item.Name,
            PictureUrl = item.PictureUrl,
            Price = item.Price,
            IsDiscountable = item.IsDiscountable,
            IsPartOfCombo = item.IsPartOfCombo,
            IsAvailable = item.IsAvailable
        };
    }

    public MenuItemVm ToMenuItemVm()
    {
        return new MenuItemVm
        {
            MenuItemId = MenuItemId,
            Sku = Sku,
            Name = Name,
            PictureUrl = PictureUrl,
            Price = Price,
            IsDiscountable = IsDiscountable,
            IsPartOfCombo = IsPartOfCombo,
            IsAvailable = IsAvailable
        };
    }
}

public sealed partial class MenuItemCrudDialog : ContentDialog
{
    public EditableMenuItemVm Item { get; }
    public MenuItemVm OriginalItem { get; }

    public MenuItemCrudDialog(MenuItemVm item)
    {
        this.InitializeComponent();
        OriginalItem = item;
        Item = EditableMenuItemVm.FromMenuItemVm(item);
        this.DataContext = Item;
    }
}
