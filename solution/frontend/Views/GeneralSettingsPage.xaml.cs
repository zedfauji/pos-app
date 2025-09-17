using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class GeneralSettingsPage : Page, ISettingsSubPage
{
    public GeneralSettingsPage()
    {
        this.InitializeComponent();
    }

    public void SetSubCategory(string subCategoryKey)
    {
        // Handle subcategory-specific logic if needed
    }

    public void SetSettings(object settings)
    {
        // Load settings into UI controls
        if (settings is MagiDesk.Shared.DTOs.Settings.GeneralSettings generalSettings)
        {
            BusinessNameBox.Text = generalSettings.BusinessName ?? "";
            PhoneBox.Text = generalSettings.BusinessPhone ?? "";
            AddressBox.Text = generalSettings.BusinessAddress ?? "";
            // Set other fields as needed
        }
    }
}
