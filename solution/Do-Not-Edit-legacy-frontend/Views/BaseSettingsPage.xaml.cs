using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs.Settings;

namespace MagiDesk.Frontend.Views;

public partial class BaseSettingsPage : Page, ISettingsSubPage
{
    protected string _categoryKey = "";
    protected string _subCategoryKey = "";
    protected object? _settings;

    public BaseSettingsPage()
    {
        this.InitializeComponent();
    }

    public virtual void SetSubCategory(string subCategoryKey)
    {
        _subCategoryKey = subCategoryKey;
        var parts = subCategoryKey.Split('.');
        _categoryKey = parts[0];
        
        // Update UI based on category
        UpdateCategoryDisplay();
    }

    public virtual void SetSettings(object? settings)
    {
        _settings = settings;
        LoadSettingsToUI();
    }

    protected virtual void UpdateCategoryDisplay()
    {
        var categoryInfo = GetCategoryInfo(_categoryKey);
        CategoryTitle.Text = categoryInfo.Title;
        CategoryDescription.Text = categoryInfo.Description;
    }

    protected virtual void LoadSettingsToUI()
    {
        // Override in derived classes
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(new TextBlock 
        { 
            Text = $"Settings for {_categoryKey} category will be displayed here.",
            HorizontalAlignment = HorizontalAlignment.Center,
            Opacity = 0.7
        });
    }

    protected virtual async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Override in derived classes
        var dialog = new ContentDialog
        {
            Title = "Save Settings",
            Content = $"Save {_categoryKey} settings?",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // Implement save logic in derived classes
        }
    }

    protected virtual async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Reset Settings",
            Content = $"Reset {_categoryKey} settings to defaults?",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // Implement reset logic in derived classes
            LoadSettingsToUI();
        }
    }

    private (string Title, string Description) GetCategoryInfo(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "general" => ("General Settings", "Business information, theme, language, and basic configuration"),
            "pos" => ("Point of Sale", "Cash drawer, table layout, shifts, and tax settings"),
            "inventory" => ("Inventory Management", "Stock thresholds, reorder settings, and vendor defaults"),
            "customers" => ("Customer Settings", "Membership tiers, wallet settings, and loyalty programs"),
            "payments" => ("Payment Configuration", "Payment methods, discounts, surcharges, and split payments"),
            "printers" => ("Printer & Device Settings", "Receipt printers, kitchen printers, and device management"),
            "notifications" => ("Notification Settings", "Email, SMS, push notifications, and alert configuration"),
            "security" => ("Security & Roles", "RBAC, login policies, session management, and audit settings"),
            "integrations" => ("Integration Settings", "Payment gateways, webhooks, CRM sync, and API endpoints"),
            "system" => ("System Configuration", "Logging, tracing, background jobs, and performance settings"),
            _ => ("Settings", "Configure system settings")
        };
    }
}
