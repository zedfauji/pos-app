using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class PaymentsSettingsPage : Page, ISettingsSubPage
{
    private PaymentSettings _settings = new();

    public PaymentsSettingsPage()
    {
        this.InitializeComponent();
    }

    public void SetSubCategory(string subCategoryKey)
    {
        // Sub-category logic can be added here if needed
    }

    public void SetSettings(object settings)
    {
        if (settings is PaymentSettings paymentSettings)
        {
            _settings = paymentSettings;
            LoadSettingsToUI();
        }
    }

    public PaymentSettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void LoadSettingsToUI()
    {
        // Discount Settings
        RequireManagerApprovalSwitch.IsOn = _settings.Discounts.RequireManagerApproval;
        AllowStackingDiscountsSwitch.IsOn = _settings.Discounts.AllowStackingDiscounts;
        MaxDiscountPercentageBox.Text = _settings.Discounts.MaxDiscountPercentage.ToString();

        // Surcharge Settings
        EnableSurchargesSwitch.IsOn = _settings.Surcharges.EnableSurcharges;
        ShowSurchargeOnReceiptSwitch.IsOn = _settings.Surcharges.ShowSurchargeOnReceipt;
        CardSurchargePercentageBox.Text = _settings.Surcharges.CardSurchargePercentage.ToString();

        // Split Payment Settings
        EnableSplitPaymentsSwitch.IsOn = _settings.SplitPayments.EnableSplitPayments;
        AllowUnevenSplitsSwitch.IsOn = _settings.SplitPayments.AllowUnevenSplits;
        MaxSplitCountBox.Value = _settings.SplitPayments.MaxSplitCount;
    }

    private void CollectUIValuesToSettings()
    {
        // Discount Settings
        _settings.Discounts.RequireManagerApproval = RequireManagerApprovalSwitch.IsOn;
        _settings.Discounts.AllowStackingDiscounts = AllowStackingDiscountsSwitch.IsOn;
        if (decimal.TryParse(MaxDiscountPercentageBox.Text, out var maxDiscount))
        {
            _settings.Discounts.MaxDiscountPercentage = maxDiscount;
        }

        // Surcharge Settings
        _settings.Surcharges.EnableSurcharges = EnableSurchargesSwitch.IsOn;
        _settings.Surcharges.ShowSurchargeOnReceipt = ShowSurchargeOnReceiptSwitch.IsOn;
        if (decimal.TryParse(CardSurchargePercentageBox.Text, out var surcharge))
        {
            _settings.Surcharges.CardSurchargePercentage = surcharge;
        }

        // Split Payment Settings
        _settings.SplitPayments.EnableSplitPayments = EnableSplitPaymentsSwitch.IsOn;
        _settings.SplitPayments.AllowUnevenSplits = AllowUnevenSplitsSwitch.IsOn;
        _settings.SplitPayments.MaxSplitCount = (int)MaxSplitCountBox.Value;
    }
}
