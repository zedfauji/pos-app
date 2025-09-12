using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MagiDesk.Frontend.Views;

public sealed partial class InventorySettingsPage : Page
{
    private InventorySettingsViewModel? _viewModel;

    public InventorySettingsPage()
    {
        this.InitializeComponent();
        Loaded += InventorySettingsPage_Loaded;
    }

    private async void InventorySettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Initialize ViewModel
            var settingsService = App.InventorySettingsService;
            if (settingsService == null)
            {
                ShowErrorDialog("Service Error", "Inventory settings service is not available.");
                return;
            }

            _viewModel = new InventorySettingsViewModel(settingsService);
            this.DataContext = _viewModel;

            // Load settings
            await _viewModel.LoadSettingsAsync();
            PopulateControls();
            
            StatusText.Text = "Settings loaded successfully";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading inventory settings: {ex.Message}");
            ShowErrorDialog("Loading Error", $"Failed to load inventory settings: {ex.Message}");
        }
    }

    private void PopulateControls()
    {
        if (_viewModel?.Settings == null) return;

        var settings = _viewModel.Settings;

        // General Settings
        SetComboBoxSelection(DefaultUnitComboBox, settings.DefaultUnit);
        DefaultTaxRateNumberBox.Value = (double)(settings.DefaultTaxRate ?? 0);
        AutoSyncMenuItemsToggle.IsOn = settings.AutoSyncMenuItems ?? false;
        RequireApprovalToggle.IsOn = settings.RequireApproval ?? false;
        SetComboBoxSelection(DefaultVendorComboBox, settings.DefaultVendorId);
        SetComboBoxSelection(TrackingMethodComboBox, settings.TrackingMethod);

        // Stock Management Settings
        LowStockThresholdNumberBox.Value = (double)(settings.LowStockThreshold ?? 10);
        CriticalStockThresholdNumberBox.Value = (double)(settings.CriticalStockThreshold ?? 5);
        AutoReorderToggle.IsOn = settings.AutoReorder ?? false;
        ReorderMultiplierNumberBox.Value = (double)(settings.ReorderMultiplier ?? 2);
        StockAlertEmailTextBox.Text = settings.StockAlertEmail ?? "";
        SetComboBoxSelection(AlertFrequencyComboBox, settings.AlertFrequency);

        // Vendor Management Settings
        SetComboBoxSelection(PaymentTermsComboBox, settings.PaymentTerms);
        SetComboBoxSelection(DefaultCurrencyComboBox, settings.DefaultCurrency);
        AutoGeneratePOToggle.IsOn = settings.AutoGeneratePO ?? false;
        RequireVendorApprovalToggle.IsOn = settings.RequireVendorApproval ?? false;
        SetComboBoxSelection(ContactMethodComboBox, settings.ContactMethod);

        // Reporting Settings
        SetComboBoxSelection(ReportFormatComboBox, settings.ReportFormat);
        IncludeImagesToggle.IsOn = settings.IncludeImages ?? true;
        AutoGenerateReportsToggle.IsOn = settings.AutoGenerateReports ?? false;
        ReportRetentionNumberBox.Value = (double)(settings.ReportRetentionDays ?? 90);
        ReportEmailTextBox.Text = settings.ReportEmail ?? "";

        // Integration Settings
        InventoryApiUrlTextBox.Text = settings.InventoryApiUrl ?? "";
        SetComboBoxSelection(SyncFrequencyComboBox, settings.SyncFrequency);
        ApiLoggingToggle.IsOn = settings.ApiLogging ?? false;
        SetComboBoxSelection(BackupSettingsComboBox, settings.BackupSettings);
    }

    private void SetComboBoxSelection(ComboBox comboBox, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        foreach (ComboBoxItem item in comboBox.Items)
        {
            if (item.Tag?.ToString() == value)
            {
                comboBox.SelectedItem = item;
                break;
            }
        }
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.Settings == null) return;

        try
        {
            StatusText.Text = "Saving settings...";
            SaveButton.IsEnabled = false;

            // Update settings from controls
            UpdateSettingsFromControls();

            // Save settings
            var success = await _viewModel.SaveSettingsAsync();
            
            if (success)
            {
                StatusText.Text = "Settings saved successfully";
                LastSavedText.Text = $"Last saved: {DateTime.Now:HH:mm:ss}";
            }
            else
            {
                StatusText.Text = "Failed to save settings";
                ShowErrorDialog("Save Error", "Failed to save inventory settings. Please try again.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving inventory settings: {ex.Message}");
            StatusText.Text = "Error saving settings";
            ShowErrorDialog("Save Error", $"Failed to save inventory settings: {ex.Message}");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void UpdateSettingsFromControls()
    {
        if (_viewModel?.Settings == null) return;

        var settings = _viewModel.Settings;

        // General Settings
        settings.DefaultUnit = GetComboBoxSelection(DefaultUnitComboBox);
        settings.DefaultTaxRate = (decimal)DefaultTaxRateNumberBox.Value;
        settings.AutoSyncMenuItems = AutoSyncMenuItemsToggle.IsOn;
        settings.RequireApproval = RequireApprovalToggle.IsOn;
        settings.DefaultVendorId = GetComboBoxSelection(DefaultVendorComboBox);
        settings.TrackingMethod = GetComboBoxSelection(TrackingMethodComboBox);

        // Stock Management Settings
        settings.LowStockThreshold = (int)LowStockThresholdNumberBox.Value;
        settings.CriticalStockThreshold = (int)CriticalStockThresholdNumberBox.Value;
        settings.AutoReorder = AutoReorderToggle.IsOn;
        settings.ReorderMultiplier = (int)ReorderMultiplierNumberBox.Value;
        settings.StockAlertEmail = StockAlertEmailTextBox.Text;
        settings.AlertFrequency = GetComboBoxSelection(AlertFrequencyComboBox);

        // Vendor Management Settings
        settings.PaymentTerms = GetComboBoxSelection(PaymentTermsComboBox);
        settings.DefaultCurrency = GetComboBoxSelection(DefaultCurrencyComboBox);
        settings.AutoGeneratePO = AutoGeneratePOToggle.IsOn;
        settings.RequireVendorApproval = RequireVendorApprovalToggle.IsOn;
        settings.ContactMethod = GetComboBoxSelection(ContactMethodComboBox);

        // Reporting Settings
        settings.ReportFormat = GetComboBoxSelection(ReportFormatComboBox);
        settings.IncludeImages = IncludeImagesToggle.IsOn;
        settings.AutoGenerateReports = AutoGenerateReportsToggle.IsOn;
        settings.ReportRetentionDays = (int)ReportRetentionNumberBox.Value;
        settings.ReportEmail = ReportEmailTextBox.Text;

        // Integration Settings
        settings.InventoryApiUrl = InventoryApiUrlTextBox.Text;
        settings.SyncFrequency = GetComboBoxSelection(SyncFrequencyComboBox);
        settings.ApiLogging = ApiLoggingToggle.IsOn;
        settings.BackupSettings = GetComboBoxSelection(BackupSettingsComboBox);
    }

    private string? GetComboBoxSelection(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem item)
        {
            return item.Tag?.ToString();
        }
        return null;
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
            Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Refresh settings when navigating to this page
        if (_viewModel != null)
        {
            _ = Task.Run(async () => await _viewModel.LoadSettingsAsync());
        }
    }
}
