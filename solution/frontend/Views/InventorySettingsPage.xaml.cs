using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class InventorySettingsPage : Page, ISettingsSubPage
{
    private InventorySettings _settings = new();

    public InventorySettingsPage()
    {
        this.InitializeComponent();
        InitializeUI();
    }

    public void SetSubCategory(string subCategoryKey)
    {
        // Handle subcategory-specific logic if needed
        // Could show/hide sections based on subcategory
    }

    public void SetSettings(object settings)
    {
        if (settings is InventorySettings inventorySettings)
        {
            _settings = inventorySettings;
            LoadSettingsToUI();
        }
    }

    public InventorySettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void InitializeUI()
    {
        // Set default values
        LowStockThresholdBox.Value = 10;
        CriticalStockThresholdBox.Value = 5;
        ReorderLeadTimeDaysBox.Value = 7;
        SafetyStockMultiplierBox.Value = 1.5;
        DefaultPaymentTermsBox.Value = 30;

        // Populate currency combo
        DefaultCurrencyCombo.ItemsSource = new List<string> { "USD", "CAD", "EUR", "GBP" };
    }

    private void LoadSettingsToUI()
    {
        // Stock Settings
        LowStockThresholdBox.Value = _settings.Stock.LowStockThreshold;
        CriticalStockThresholdBox.Value = _settings.Stock.CriticalStockThreshold;
        EnableStockAlertsCheck.IsChecked = _settings.Stock.EnableStockAlerts;
        AutoDeductStockCheck.IsChecked = _settings.Stock.AutoDeductStock;
        AllowNegativeStockCheck.IsChecked = _settings.Stock.AllowNegativeStock;

        // Reorder Settings
        EnableAutoReorderCheck.IsChecked = _settings.Reorder.EnableAutoReorder;
        ReorderLeadTimeDaysBox.Value = _settings.Reorder.ReorderLeadTimeDays;
        SafetyStockMultiplierBox.Value = (double)_settings.Reorder.SafetyStockMultiplier;

        // Vendor Settings
        DefaultPaymentTermsBox.Value = _settings.Vendors.DefaultPaymentTerms;
        DefaultCurrencyCombo.SelectedItem = _settings.Vendors.DefaultCurrency;
        RequireApprovalForOrdersCheck.IsChecked = _settings.Vendors.RequireApprovalForOrders;
    }

    private void CollectUIValuesToSettings()
    {
        // Stock Settings
        _settings.Stock.LowStockThreshold = (int)LowStockThresholdBox.Value;
        _settings.Stock.CriticalStockThreshold = (int)CriticalStockThresholdBox.Value;
        _settings.Stock.EnableStockAlerts = EnableStockAlertsCheck.IsChecked ?? false;
        _settings.Stock.AutoDeductStock = AutoDeductStockCheck.IsChecked ?? false;
        _settings.Stock.AllowNegativeStock = AllowNegativeStockCheck.IsChecked ?? false;

        // Reorder Settings
        _settings.Reorder.EnableAutoReorder = EnableAutoReorderCheck.IsChecked ?? false;
        _settings.Reorder.ReorderLeadTimeDays = (int)ReorderLeadTimeDaysBox.Value;
        _settings.Reorder.SafetyStockMultiplier = (decimal)SafetyStockMultiplierBox.Value;

        // Vendor Settings
        _settings.Vendors.DefaultPaymentTerms = (int)DefaultPaymentTermsBox.Value;
        _settings.Vendors.DefaultCurrency = DefaultCurrencyCombo.SelectedItem as string ?? "USD";
        _settings.Vendors.RequireApprovalForOrders = RequireApprovalForOrdersCheck.IsChecked ?? false;
    }
}