using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MagiDesk.Frontend.Dialogs;

// Editable wrapper for MenuItemVm since the original has init-only properties
public sealed class EditableMenuItemVm
{
    public long MenuItemId { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PictureUrl { get; set; }
    public double Price { get; set; }
    public bool IsDiscountable { get; set; }
    public bool IsPartOfCombo { get; set; }
    public bool IsAvailable { get; set; }
    public string Category { get; set; } = "";
    public string? GroupName { get; set; }
    public string? Description { get; set; }

    public static EditableMenuItemVm FromMenuItemVm(MenuItemVm item)
    {
        return new EditableMenuItemVm
        {
            MenuItemId = item.MenuItemId,
            Sku = item.Sku,
            Name = item.Name,
            PictureUrl = item.PictureUrl,
            Price = (double)item.Price,
            IsDiscountable = item.IsDiscountable,
            IsPartOfCombo = item.IsPartOfCombo,
            IsAvailable = item.IsAvailable,
            Category = item.Category,
            GroupName = item.GroupName,
            Description = item.Description
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
            Price = (decimal)Price,
            IsDiscountable = IsDiscountable,
            IsPartOfCombo = IsPartOfCombo,
            IsAvailable = IsAvailable,
            Category = Category,
            GroupName = GroupName,
            Description = Description
        };
    }
}

public sealed partial class MenuItemCrudDialog : ContentDialog
{
    public EditableMenuItemVm Item { get; }
    public MenuItemVm OriginalItem { get; }
    private readonly MenuApiService _menuApiService;

    public MenuItemCrudDialog(MenuItemVm item)
    {
        this.InitializeComponent();
        OriginalItem = item;
        Item = EditableMenuItemVm.FromMenuItemVm(item);
        this.DataContext = Item;
        
        // Clear any error message
        ErrorMessageText.Visibility = Visibility.Collapsed;
        
        // Get the MenuApiService from DI
        _menuApiService = App.Services?.GetRequiredService<MenuApiService>() 
            ?? throw new InvalidOperationException("MenuApiService not available");
        
        // Handle the save button click
        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Prevent the dialog from closing until we're done saving
        var deferral = args.GetDeferral();
        
        try
        {
            // Create the update DTO
            // Convert dollars to cents for database storage
            var priceInCents = (decimal)(Item.Price * 100);
            var updateDto = new MenuApiService.UpdateMenuItemDto(
                Name: Item.Name,
                Description: Item.Description,
                Category: Item.Category,
                GroupName: Item.GroupName,
                VendorPrice: null,
                SellingPrice: priceInCents,
                Price: priceInCents,
                PictureUrl: Item.PictureUrl,
                IsDiscountable: Item.IsDiscountable,
                IsPartOfCombo: Item.IsPartOfCombo,
                IsAvailable: Item.IsAvailable
            );

            // Update the menu item
            var result = await _menuApiService.UpdateMenuItemAsync(Item.MenuItemId, updateDto);
            
            if (result == null)
            {
                // Show error and prevent dialog from closing
                ShowErrorDialog("Error", "Failed to update menu item. Please try again.");
                args.Cancel = true;
                return;
            }
            
            // Success - dialog will close
        }
        catch (Exception ex)
        {
            // Show detailed error and prevent dialog from closing
            ShowErrorDialog("Error", $"Failed to update menu item: {ex.Message}");
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void ShowErrorDialog(string title, string message)
    {
        // Show error message in the existing dialog
        ErrorMessageText.Text = $"{title}: {message}";
        ErrorMessageText.Visibility = Visibility.Visible;
        
        // Reset the title to the original
        this.Title = "Menu Item";
    }
}
