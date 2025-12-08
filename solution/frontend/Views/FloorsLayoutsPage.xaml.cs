using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Controls;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Views;

public sealed partial class FloorsLayoutsPage : Page
{
    private FloorsLayoutsViewModel ViewModel => PageViewModel ?? (FloorsLayoutsViewModel)DataContext;

    public FloorsLayoutsPage()
    {
        this.InitializeComponent();
        this.Loaded += FloorsLayoutsPage_Loaded;
    }

    private async void FloorsLayoutsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadFloorsAsync();
        await ViewModel.LoadLayoutSettingsAsync();
    }

    private async void AddFloor_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Add New Floor",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var panel = new StackPanel { Spacing = 12 };
        
        var nameBox = new TextBox 
        { 
            Header = "Floor Name",
            PlaceholderText = "Enter floor name"
        };
        panel.Children.Add(nameBox);
        
        var descBox = new TextBox 
        { 
            Header = "Description",
            PlaceholderText = "Optional description",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };
        panel.Children.Add(descBox);
        
        var defaultToggle = new ToggleSwitch 
        { 
            Header = "Set as Default Floor",
            IsOn = false
        };
        panel.Children.Add(defaultToggle);
        
        dialog.Content = panel;
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ViewModel.CreateFloorAsync(
                nameBox.Text,
                string.IsNullOrWhiteSpace(descBox.Text) ? null : descBox.Text,
                defaultToggle.IsOn);
        }
    }

    private async void DeleteFloor_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFloor == null) return;
        
        var dialog = new ContentDialog
        {
            Title = "Delete Floor",
            Content = $"Are you sure you want to delete '{ViewModel.SelectedFloor.FloorName}'? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteFloorAsync(ViewModel.SelectedFloor.FloorId);
        }
    }

    private async void DuplicateFloor_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFloor == null) return;
        
        var dialog = new ContentDialog
        {
            Title = "Duplicate Floor",
            PrimaryButtonText = "Duplicate",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var panel = new StackPanel { Spacing = 12 };
        
        var nameBox = new TextBox 
        { 
            Header = "New Floor Name",
            Text = $"{ViewModel.SelectedFloor.FloorName} Copy",
            PlaceholderText = "Enter new floor name"
        };
        panel.Children.Add(nameBox);
        
        var copyTablesToggle = new ToggleSwitch 
        { 
            Header = "Copy Tables",
            IsOn = true
        };
        panel.Children.Add(copyTablesToggle);
        
        dialog.Content = panel;
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ViewModel.DuplicateFloorAsync(
                ViewModel.SelectedFloor.FloorId,
                nameBox.Text,
                copyTablesToggle.IsOn);
        }
    }

    private void FloorMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FloorDto floor)
        {
            ViewModel.SelectedFloor = floor;
            // MenuFlyout will show automatically via Button.Flyout
        }
    }

    private async void RenameFloor_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFloor == null) return;
        
        var dialog = new ContentDialog
        {
            Title = "Rename Floor",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var nameBox = new TextBox 
        { 
            Header = "Floor Name",
            Text = ViewModel.SelectedFloor.FloorName
        };
        dialog.Content = nameBox;
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            var updateRequest = new UpdateFloorRequest
            {
                FloorName = nameBox.Text
            };
            await ViewModel.UpdateFloorAsync(ViewModel.SelectedFloor.FloorId, updateRequest);
        }
    }

    private async void DuplicateTable_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTable == null) return;
        
        await ViewModel.DuplicateTableAsync(ViewModel.SelectedTable.TableId);
    }

    private async void DeleteTable_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTable == null) return;
        
        var dialog = new ContentDialog
        {
            Title = "Delete Table",
            Content = $"Are you sure you want to delete '{ViewModel.SelectedTable.TableName}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteTableAsync(ViewModel.SelectedTable.TableId);
        }
    }

    private void LayoutDesigner_TableSelected(object? sender, TableLayoutDto table)
    {
        ViewModel.SelectedTable = table;
    }

    private async void LayoutDesigner_TablePositionChanged(object? sender, (Guid TableId, double X, double Y) position)
    {
        await ViewModel.UpdateTablePositionAsync(position.TableId, position.X, position.Y);
    }

    private async void LayoutDesigner_AddTableRequested(object? sender, string tableType)
    {
        if (ViewModel.SelectedFloor == null) return;
        
        var dialog = new ContentDialog
        {
            Title = $"Add {tableType} Table",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var panel = new StackPanel { Spacing = 12 };
        
        var nameBox = new TextBox 
        { 
            Header = "Table Name",
            PlaceholderText = $"e.g., {tableType} Table 1"
        };
        panel.Children.Add(nameBox);
        
        var numberBox = new TextBox 
        { 
            Header = "Table Number",
            PlaceholderText = "Optional"
        };
        panel.Children.Add(numberBox);
        
        dialog.Content = panel;
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ViewModel.CreateTableAsync(
                ViewModel.SelectedFloor!.FloorId,
                nameBox.Text,
                string.IsNullOrWhiteSpace(numberBox.Text) ? null : numberBox.Text,
                tableType);
            await ViewModel.LoadFloorLayoutAsync(ViewModel.SelectedFloor.FloorId);
        }
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Open layout settings dialog
        // This will be implemented as a separate settings page or dialog
    }

    private async void AddBarTable_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFloor == null) return;
        
        var dialog = new ContentDialog
        {
            Title = "Add Bar Table",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var panel = new StackPanel { Spacing = 12 };
        
        var nameBox = new TextBox 
        { 
            Header = "Table Name",
            PlaceholderText = "e.g., Bar Table 1"
        };
        panel.Children.Add(nameBox);
        
        var numberBox = new TextBox 
        { 
            Header = "Table Number",
            PlaceholderText = "Optional"
        };
        panel.Children.Add(numberBox);
        
        dialog.Content = panel;
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ViewModel.CreateTableAsync(
                ViewModel.SelectedFloor!.FloorId,
                nameBox.Text,
                string.IsNullOrWhiteSpace(numberBox.Text) ? null : numberBox.Text,
                "bar");
            await ViewModel.LoadFloorLayoutAsync(ViewModel.SelectedFloor.FloorId);
        }
    }

    private async void AddBilliardTable_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFloor == null) return;
        
        var dialog = new ContentDialog
        {
            Title = "Add Billiard Table",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var panel = new StackPanel { Spacing = 12 };
        
        var nameBox = new TextBox 
        { 
            Header = "Table Name",
            PlaceholderText = "e.g., Table 1"
        };
        panel.Children.Add(nameBox);
        
        var numberBox = new TextBox 
        { 
            Header = "Table Number",
            PlaceholderText = "Optional"
        };
        panel.Children.Add(numberBox);
        
        dialog.Content = panel;
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ViewModel.CreateTableAsync(
                ViewModel.SelectedFloor!.FloorId,
                nameBox.Text,
                string.IsNullOrWhiteSpace(numberBox.Text) ? null : numberBox.Text,
                "billiard");
            await ViewModel.LoadFloorLayoutAsync(ViewModel.SelectedFloor.FloorId);
        }
    }

    // Table drag handlers are now handled by LayoutDesigner control
}

