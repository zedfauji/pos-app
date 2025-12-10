using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class PosSettingsPage : Page, ISettingsSubPage
{
    private PosSettings _settings = new();
    private ObservableCollection<TaxRate> _additionalTaxRates = new();

    public PosSettingsPage()
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
        if (settings is PosSettings posSettings)
        {
            _settings = posSettings;
            LoadSettingsToUI();
        }
    }

    public PosSettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void InitializeUI()
    {
        // Initialize additional tax rates list
        AdditionalTaxRatesList.ItemsSource = _additionalTaxRates;
        
        // Set default values
        BaudRateBox.Value = 9600;
        RatePerMinuteBox.Value = 0.50;
        WarnAfterMinutesBox.Value = 30;
        AutoStopAfterMinutesBox.Value = 120;
        DefaultTaxRateBox.Value = 8.5;
    }

    private void LoadSettingsToUI()
    {
        // Cash Drawer Settings
        AutoOpenOnSaleCheck.IsChecked = _settings.CashDrawer.AutoOpenOnSale;
        AutoOpenOnRefundCheck.IsChecked = _settings.CashDrawer.AutoOpenOnRefund;
        ComPortBox.Text = _settings.CashDrawer.ComPort ?? "";
        BaudRateBox.Value = _settings.CashDrawer.BaudRate;
        RequireManagerOverrideCheck.IsChecked = _settings.CashDrawer.RequireManagerOverride;

        // Table Layout Settings
        RatePerMinuteBox.Value = (double)_settings.TableLayout.RatePerMinute;
        WarnAfterMinutesBox.Value = _settings.TableLayout.WarnAfterMinutes;
        AutoStopAfterMinutesBox.Value = _settings.TableLayout.AutoStopAfterMinutes;
        EnableAutoStopCheck.IsChecked = _settings.TableLayout.EnableAutoStop;
        ShowTimerOnTablesCheck.IsChecked = _settings.TableLayout.ShowTimerOnTables;

        // Shift Settings
        ShiftStartTimePicker.Time = _settings.Shifts.ShiftStartTime;
        ShiftEndTimePicker.Time = _settings.Shifts.ShiftEndTime;
        RequireShiftReportsCheck.IsChecked = _settings.Shifts.RequireShiftReports;
        AutoCloseShiftCheck.IsChecked = _settings.Shifts.AutoCloseShift;

        // Tax Settings
        DefaultTaxRateBox.Value = (double)_settings.Tax.DefaultTaxRate;
        TaxDisplayNameBox.Text = _settings.Tax.TaxDisplayName;
        TaxInclusivePricingCheck.IsChecked = _settings.Tax.TaxInclusivePricing;

        // Additional Tax Rates
        _additionalTaxRates.Clear();
        foreach (var taxRate in _settings.Tax.AdditionalTaxRates)
        {
            _additionalTaxRates.Add(new TaxRate
            {
                Name = taxRate.Name,
                Rate = taxRate.Rate,
                IsDefault = taxRate.IsDefault
            });
        }
    }

    private void CollectUIValuesToSettings()
    {
        // Cash Drawer Settings
        _settings.CashDrawer.AutoOpenOnSale = AutoOpenOnSaleCheck.IsChecked ?? false;
        _settings.CashDrawer.AutoOpenOnRefund = AutoOpenOnRefundCheck.IsChecked ?? false;
        _settings.CashDrawer.ComPort = ComPortBox.Text;
        _settings.CashDrawer.BaudRate = (int)BaudRateBox.Value;
        _settings.CashDrawer.RequireManagerOverride = RequireManagerOverrideCheck.IsChecked ?? false;

        // Table Layout Settings
        _settings.TableLayout.RatePerMinute = (decimal)RatePerMinuteBox.Value;
        _settings.TableLayout.WarnAfterMinutes = (int)WarnAfterMinutesBox.Value;
        _settings.TableLayout.AutoStopAfterMinutes = (int)AutoStopAfterMinutesBox.Value;
        _settings.TableLayout.EnableAutoStop = EnableAutoStopCheck.IsChecked ?? false;
        _settings.TableLayout.ShowTimerOnTables = ShowTimerOnTablesCheck.IsChecked ?? false;

        // Shift Settings
        _settings.Shifts.ShiftStartTime = ShiftStartTimePicker.Time;
        _settings.Shifts.ShiftEndTime = ShiftEndTimePicker.Time;
        _settings.Shifts.RequireShiftReports = RequireShiftReportsCheck.IsChecked ?? false;
        _settings.Shifts.AutoCloseShift = AutoCloseShiftCheck.IsChecked ?? false;

        // Tax Settings
        _settings.Tax.DefaultTaxRate = (decimal)DefaultTaxRateBox.Value;
        _settings.Tax.TaxDisplayName = TaxDisplayNameBox.Text;
        _settings.Tax.TaxInclusivePricing = TaxInclusivePricingCheck.IsChecked ?? false;

        // Additional Tax Rates
        _settings.Tax.AdditionalTaxRates.Clear();
        foreach (var taxRate in _additionalTaxRates)
        {
            _settings.Tax.AdditionalTaxRates.Add(new TaxRate
            {
                Name = taxRate.Name,
                Rate = taxRate.Rate,
                IsDefault = taxRate.IsDefault
            });
        }
    }

    private void AddTaxRate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _additionalTaxRates.Add(new TaxRate
        {
            Name = "New Tax",
            Rate = 0,
            IsDefault = false
        });
    }

    private void RemoveTaxRate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (AdditionalTaxRatesList.SelectedItem is TaxRate selectedTaxRate)
        {
            _additionalTaxRates.Remove(selectedTaxRate);
        }
    }
}