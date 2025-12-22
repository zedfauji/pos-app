using System.Collections.Generic;
using System.Linq;
using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class GeneralSettingsPage : Page, ISettingsSubPage
{
    private GeneralSettings _settings = new();

    public GeneralSettingsPage()
    {
        this.InitializeComponent();
        PopulateComboBoxes();
    }

    public void SetSubCategory(string subCategoryKey)
    {
        // Handle subcategory-specific logic if needed
    }

    public void SetSettings(object settings)
    {
        if (settings is GeneralSettings generalSettings)
        {
            _settings = generalSettings;
            LoadSettingsToUI();
        }
    }

    public GeneralSettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void PopulateComboBoxes()
    {
        ThemeCombo.ItemsSource = new List<string> { "System", "Light", "Dark" };
        LanguageCombo.ItemsSource = new List<string> { "en-US", "es-ES", "fr-FR" }; // Example languages
        TimeZoneCombo.ItemsSource = TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id).ToList();
        CurrencyCombo.ItemsSource = new List<string> { "USD", "CAD", "EUR", "GBP" };
    }

    private void LoadSettingsToUI()
    {
        BusinessNameBox.Text = _settings.BusinessName;
        PhoneBox.Text = _settings.BusinessPhone;
        AddressBox.Text = _settings.BusinessAddress;
        EmailBox.Text = _settings.BusinessEmail ?? "";
        WebsiteBox.Text = _settings.BusinessWebsite ?? "";

        ThemeCombo.SelectedItem = _settings.Theme;
        LanguageCombo.SelectedItem = _settings.Language;
        TimeZoneCombo.SelectedItem = _settings.Timezone;
        CurrencyCombo.SelectedItem = _settings.Currency;
    }

    private void CollectUIValuesToSettings()
    {
        _settings.BusinessName = BusinessNameBox.Text;
        _settings.BusinessPhone = PhoneBox.Text;
        _settings.BusinessAddress = AddressBox.Text;
        _settings.BusinessEmail = EmailBox.Text;
        _settings.BusinessWebsite = WebsiteBox.Text;

        _settings.Theme = ThemeCombo.SelectedItem as string ?? "System";
        _settings.Language = LanguageCombo.SelectedItem as string ?? "en-US";
        _settings.Timezone = TimeZoneCombo.SelectedItem as string ?? "America/New_York";
        _settings.Currency = CurrencyCombo.SelectedItem as string ?? "USD";
    }
}
