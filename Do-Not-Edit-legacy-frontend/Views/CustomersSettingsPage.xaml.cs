using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MagiDesk.Frontend.Views;

public sealed partial class CustomersSettingsPage : Page, ISettingsSubPage
{
    private CustomerSettings _settings = new();
    private ObservableCollection<MembershipTier> _membershipTiers = new();

    public CustomersSettingsPage()
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
        if (settings is CustomerSettings customerSettings)
        {
            _settings = customerSettings;
            LoadSettingsToUI();
        }
    }

    public CustomerSettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void InitializeUI()
    {
        // Initialize membership tiers list
        MembershipTiersList.ItemsSource = _membershipTiers;
        
        // Set default values
        DefaultExpiryDaysBox.Value = 365;
        MaxWalletBalanceBox.Value = 1000;
        MinTopUpAmountBox.Value = 10;
        PointsPerDollarBox.Value = 1.0;
        PointValueBox.Value = 0.01;
        MinPointsForRedemptionBox.Value = 100;
    }

    private void LoadSettingsToUI()
    {
        // Membership Settings
        DefaultExpiryDaysBox.Value = _settings.Membership.DefaultExpiryDays;
        AutoRenewMembershipsCheck.IsChecked = _settings.Membership.AutoRenewMemberships;
        RequireEmailForMembershipCheck.IsChecked = _settings.Membership.RequireEmailForMembership;

        // Membership Tiers
        _membershipTiers.Clear();
        foreach (var tier in _settings.Membership.Tiers)
        {
            _membershipTiers.Add(new MembershipTier
            {
                Name = tier.Name,
                DiscountPercentage = tier.DiscountPercentage,
                MinimumSpend = tier.MinimumSpend,
                Color = tier.Color
            });
        }

        // Wallet Settings
        EnableWalletSystemCheck.IsChecked = _settings.Wallet.EnableWalletSystem;
        MaxWalletBalanceBox.Value = (double)_settings.Wallet.MaxWalletBalance;
        MinTopUpAmountBox.Value = (double)_settings.Wallet.MinTopUpAmount;
        AllowNegativeBalanceCheck.IsChecked = _settings.Wallet.AllowNegativeBalance;
        RequireIdForWalletUseCheck.IsChecked = _settings.Wallet.RequireIdForWalletUse;

        // Loyalty Settings
        EnableLoyaltyProgramCheck.IsChecked = _settings.Loyalty.EnableLoyaltyProgram;
        PointsPerDollarBox.Value = (double)_settings.Loyalty.PointsPerDollar;
        PointValueBox.Value = (double)_settings.Loyalty.PointValue;
        MinPointsForRedemptionBox.Value = _settings.Loyalty.MinPointsForRedemption;
    }

    private void CollectUIValuesToSettings()
    {
        // Membership Settings
        _settings.Membership.DefaultExpiryDays = (int)DefaultExpiryDaysBox.Value;
        _settings.Membership.AutoRenewMemberships = AutoRenewMembershipsCheck.IsChecked ?? false;
        _settings.Membership.RequireEmailForMembership = RequireEmailForMembershipCheck.IsChecked ?? false;

        // Membership Tiers
        _settings.Membership.Tiers.Clear();
        foreach (var tier in _membershipTiers)
        {
            _settings.Membership.Tiers.Add(new MembershipTier
            {
                Name = tier.Name,
                DiscountPercentage = tier.DiscountPercentage,
                MinimumSpend = tier.MinimumSpend,
                Color = tier.Color
            });
        }

        // Wallet Settings
        _settings.Wallet.EnableWalletSystem = EnableWalletSystemCheck.IsChecked ?? false;
        _settings.Wallet.MaxWalletBalance = (decimal)MaxWalletBalanceBox.Value;
        _settings.Wallet.MinTopUpAmount = (decimal)MinTopUpAmountBox.Value;
        _settings.Wallet.AllowNegativeBalance = AllowNegativeBalanceCheck.IsChecked ?? false;
        _settings.Wallet.RequireIdForWalletUse = RequireIdForWalletUseCheck.IsChecked ?? false;

        // Loyalty Settings
        _settings.Loyalty.EnableLoyaltyProgram = EnableLoyaltyProgramCheck.IsChecked ?? false;
        _settings.Loyalty.PointsPerDollar = (decimal)PointsPerDollarBox.Value;
        _settings.Loyalty.PointValue = (decimal)PointValueBox.Value;
        _settings.Loyalty.MinPointsForRedemption = (int)MinPointsForRedemptionBox.Value;
    }

    private void AddMembershipTier_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _membershipTiers.Add(new MembershipTier
        {
            Name = "New Tier",
            DiscountPercentage = 0,
            MinimumSpend = 0,
            Color = "#007ACC"
        });
    }

    private void RemoveMembershipTier_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (MembershipTiersList.SelectedItem is MembershipTier selectedTier)
        {
            _membershipTiers.Remove(selectedTier);
        }
    }
}

