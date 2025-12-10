using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class ModifierOptionCrudDialog : ContentDialog
{
    public ModifierOptionManagementViewModel Option { get; }

    public ModifierOptionCrudDialog(ModifierOptionManagementViewModel? option = null)
    {
        this.InitializeComponent();
        
        Option = option ?? new ModifierOptionManagementViewModel
        {
            Id = 0, // Use long for ID to match the database schema
            Name = "",
            PriceDelta = 0
        };
        
        this.DataContext = Option;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(Option.Name))
        {
            args.Cancel = true;
            ShowErrorDialog("Validation Error", "Option name is required.");
            return;
        }
    }

    private async void ShowErrorDialog(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                PrimaryButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
        }
    }
}
