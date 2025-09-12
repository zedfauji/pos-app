using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class InventoryManagementPage : Page, IToolbarConsumer
{
    private readonly InventoryManagementViewModel _vm;

    public InventoryManagementPage()
    {
        this.InitializeComponent();
        
        // CRITICAL FIX: Ensure Api is initialized before creating ViewModel
        if (App.Api == null)
        {
            throw new InvalidOperationException("Api not initialized. Ensure App.InitializeApiAsync() has completed successfully.");
        }
        _vm = new InventoryManagementViewModel(App.Api);
        
        this.DataContext = _vm;
        Loaded += InventoryManagementPage_Loaded;
    }

    private async void InventoryManagementPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private async void AddItem_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement add item dialog
        var dialog = new ContentDialog
        {
            Title = "Add Inventory Item",
            Content = new TextBlock { Text = "Add item functionality will be implemented here" },
            PrimaryButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    private async void EditItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is InventoryItemDto item)
        {
            // TODO: Implement edit item dialog
            var dialog = new ContentDialog
            {
                Title = "Edit Inventory Item",
                Content = new TextBlock { Text = $"Edit {item.Name} functionality will be implemented here" },
                PrimaryButtonText = "OK"
            };
            await dialog.ShowAsync();
        }
    }

    private async void AdjustQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is InventoryItemDto item)
        {
            // TODO: Implement quantity adjustment dialog
            var dialog = new ContentDialog
            {
                Title = "Adjust Quantity",
                Content = new TextBlock { Text = $"Adjust quantity for {item.Name} functionality will be implemented here" },
                PrimaryButtonText = "OK"
            };
            await dialog.ShowAsync();
        }
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is InventoryItemDto item)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Item",
                Content = new TextBlock { Text = $"Are you sure you want to delete {item.Name}?" },
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };
            
            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // TODO: Implement delete functionality
                await _vm.LoadAsync(); // Refresh after deletion
            }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _vm.SearchText = textBox.Text;
            _vm.FilterItems();
        }
    }

    // IToolbarConsumer implementation
    public void OnAdd()
    {
        AddItem_Click(this, new RoutedEventArgs());
    }

    public void OnEdit()
    {
        // TODO: Implement edit functionality
    }

    public void OnDelete()
    {
        // TODO: Implement delete functionality
    }

    public void OnRefresh()
    {
        Refresh_Click(this, new RoutedEventArgs());
    }
}
