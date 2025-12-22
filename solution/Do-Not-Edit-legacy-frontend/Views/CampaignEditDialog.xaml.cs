using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class CampaignEditDialog : ContentDialog, INotifyPropertyChanged
    {
        private readonly CustomerIntelligenceService _customerService;
        private readonly CampaignDto? _existingCampaign;
        private readonly ObservableCollection<Services.SegmentDto> _segments = new();
        private readonly ObservableCollection<MenuItemDto> _menuItems;

        // Form fields
        private string _campaignName = string.Empty;
        private string _campaignDescription = string.Empty;
        private string _messageTemplate = string.Empty;
        private bool _isActive = true;
        private int _selectedCampaignTypeIndex = 0;
        private int _selectedChannelIndex = 0;
        private int _selectedSegmentIndex = -1;
        private double _maxRedemptions = 1;
        private double _minOrderValue = 0;
        private double _discountValue = 0;
        private double _loyaltyPoints = 0;
        private double _walletCredit = 0;
        private bool _isFormValid = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CampaignEditDialog() : this(null) { }

        public CampaignEditDialog(CampaignDto? existingCampaign = null)
        {
            this.InitializeComponent();
            _customerService = new CustomerIntelligenceService();
            _existingCampaign = existingCampaign;
            _segments = new ObservableCollection<Services.SegmentDto>();
            _menuItems = new ObservableCollection<MenuItemDto>();

            // UI elements will be bound in XAML
            // SegmentComboBox.ItemsSource = _segments;
            // FreeItemComboBox.ItemsSource = _menuItems;

            if (_existingCampaign != null)
            {
                Title = "Edit Campaign";
                LoadExistingCampaign();
            }
            else
            {
                Title = "Create Campaign";
                SetDefaultValues();
            }

            this.Loaded += CampaignEditDialog_Loaded;
            this.PrimaryButtonClick += CampaignEditDialog_PrimaryButtonClick;
            CampaignTypeComboBox.SelectionChanged += CampaignTypeComboBox_SelectionChanged;
        }

        #region Properties

        public string CampaignName
        {
            get => _campaignName;
            set => SetProperty(ref _campaignName, value);
        }

        public string CampaignDescription
        {
            get => _campaignDescription;
            set => SetProperty(ref _campaignDescription, value);
        }

        public string MessageTemplate
        {
            get => _messageTemplate;
            set => SetProperty(ref _messageTemplate, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public int SelectedCampaignTypeIndex
        {
            get => _selectedCampaignTypeIndex;
            set => SetProperty(ref _selectedCampaignTypeIndex, value);
        }

        public int SelectedChannelIndex
        {
            get => _selectedChannelIndex;
            set => SetProperty(ref _selectedChannelIndex, value);
        }

        public int SelectedSegmentIndex
        {
            get => _selectedSegmentIndex;
            set => SetProperty(ref _selectedSegmentIndex, value);
        }

        public double MaxRedemptions
        {
            get => _maxRedemptions;
            set => SetProperty(ref _maxRedemptions, value);
        }

        public double MinOrderValue
        {
            get => _minOrderValue;
            set => SetProperty(ref _minOrderValue, value);
        }

        public double DiscountValue
        {
            get => _discountValue;
            set => SetProperty(ref _discountValue, value);
        }

        public double LoyaltyPoints
        {
            get => _loyaltyPoints;
            set => SetProperty(ref _loyaltyPoints, value);
        }

        public double WalletCredit
        {
            get => _walletCredit;
            set => SetProperty(ref _walletCredit, value);
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set => SetProperty(ref _isFormValid, value);
        }

        #endregion

        private async void CampaignEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            ValidateForm();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load segments
                var segments = await _customerService.GetSegmentsAsync();
                _segments.Clear();
                _segments.Add(new Services.SegmentDto { Id = "", Name = "All Customers", IsActive = true });
                foreach (var segment in segments)
                {
                    // Convert Services.CustomerSegmentDto to Services.SegmentDto
                    var viewSegment = new Services.SegmentDto
                    {
                        Id = segment.SegmentId.ToString(),
                        Name = segment.Name,
                        Description = segment.Description,
                        IsActive = segment.IsActive
                    };
                    _segments.Add(viewSegment);
                }

                // Load menu items for free item campaigns
                // Note: This would need to be implemented to call MenuApi
                // For now, adding placeholder items
                _menuItems.Clear();
                _menuItems.Add(new MenuItemDto { Id = "1", Name = "Coffee" });
                _menuItems.Add(new MenuItemDto { Id = "2", Name = "Pastry" });
                _menuItems.Add(new MenuItemDto { Id = "3", Name = "Sandwich" });
            }
            catch (Exception ex)
            {
                // Handle error loading data
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private void SetDefaultValues()
        {
            StartDatePicker.Date = DateTime.Today;
            EndDatePicker.Date = DateTime.Today.AddDays(30);
            MessageTemplate = "Hi {CustomerName}! We have a special offer just for you. Don't miss out!";
        }

        private void LoadExistingCampaign()
        {
            if (_existingCampaign == null) return;

            CampaignName = _existingCampaign.Name;
            CampaignDescription = _existingCampaign.Description;
            IsActive = _existingCampaign.IsActive;
            StartDatePicker.Date = _existingCampaign.StartDate;
            if (_existingCampaign.EndDate.HasValue)
            {
                EndDatePicker.Date = _existingCampaign.EndDate.Value;
            }

            // Set campaign type
            SelectedCampaignTypeIndex = _existingCampaign.CampaignType switch
            {
                "Discount" => 0,
                "Loyalty" => 1,
                "Wallet" => 2,
                "FreeItem" => 3,
                "Upgrade" => 4,
                _ => 0
            };

            // Set channel
            SelectedChannelIndex = _existingCampaign.Channel switch
            {
                "Email" => 0,
                "SMS" => 1,
                "WhatsApp" => 2,
                "InApp" => 3,
                _ => 0
            };
        }

        private async void CampaignEditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true; // Cancel default close behavior

            if (!ValidateForm())
            {
                ValidationInfoBar.IsOpen = true;
                return;
            }

            try
            {
                IsPrimaryButtonEnabled = false;
                PrimaryButtonText = "Saving...";

                var campaignData = CreateCampaignData();

                if (_existingCampaign != null)
                {
                    var updateRequest = new Services.UIUpdateCampaignRequest
                    {
                        Name = campaignData.Name,
                        Description = campaignData.Description,
                        CampaignType = campaignData.CampaignType,
                        Channel = campaignData.Channel,
                        TargetSegmentId = campaignData.TargetSegmentId,
                        StartDate = campaignData.StartDate,
                        EndDate = campaignData.EndDate,
                        IsActive = campaignData.IsActive,
                        MaxRedemptionsPerCustomer = campaignData.MaxRedemptionsPerCustomer,
                        MinimumOrderValue = campaignData.MinimumOrderValue,
                        MessageTemplate = campaignData.MessageTemplate,
                        OfferValue = campaignData.OfferValue,
                        OfferConfiguration = campaignData.OfferConfiguration
                    };
                    await _customerService.UpdateCampaignAsync(Guid.Parse(_existingCampaign.Id), updateRequest);
                }
                else
                {
                    var createRequest = new Services.UICreateCampaignRequest
                    {
                        Name = campaignData.Name,
                        Description = campaignData.Description,
                        CampaignType = campaignData.CampaignType,
                        Channel = campaignData.Channel,
                        TargetSegmentId = campaignData.TargetSegmentId,
                        StartDate = campaignData.StartDate,
                        EndDate = campaignData.EndDate,
                        IsActive = campaignData.IsActive,
                        MaxRedemptionsPerCustomer = campaignData.MaxRedemptionsPerCustomer,
                        MinimumOrderValue = campaignData.MinimumOrderValue,
                        MessageTemplate = campaignData.MessageTemplate,
                        OfferValue = campaignData.OfferValue,
                        OfferConfiguration = campaignData.OfferConfiguration
                    };
                    await _customerService.CreateCampaignAsync(createRequest);
                }

                Hide();
            }
            catch (Exception ex)
            {
                ValidationInfoBar.Message = $"Failed to save campaign: {ex.Message}";
                ValidationInfoBar.Severity = InfoBarSeverity.Error;
                ValidationInfoBar.IsOpen = true;
            }
            finally
            {
                IsPrimaryButtonEnabled = true;
                PrimaryButtonText = "Save";
            }
        }

        private Services.UICreateCampaignRequest CreateCampaignData()
        {
            var campaignType = CampaignTypeComboBox.SelectedItem?.ToString() ?? "Discount";
            var channel = ChannelComboBox.SelectedItem?.ToString() ?? "Email";
            var targetSegmentId = SelectedSegmentIndex > 0 ? _segments[SelectedSegmentIndex].Id : null;

            return new Services.UICreateCampaignRequest
            {
                Name = CampaignName,
                Description = CampaignDescription,
                CampaignType = campaignType,
                Channel = channel,
                TargetSegmentId = targetSegmentId,
                StartDate = StartDatePicker.Date.DateTime,
                EndDate = EndDatePicker.Date != DateTimeOffset.MinValue ? EndDatePicker.Date.DateTime : (DateTime?)null,
                IsActive = IsActive,
                MaxRedemptionsPerCustomer = (int)MaxRedemptions,
                MinimumOrderValue = (decimal)MinOrderValue,
                MessageTemplate = MessageTemplate,
                OfferValue = GetOfferValue(),
                OfferConfiguration = GetOfferConfiguration()
            };
        }

        private decimal GetOfferValue()
        {
            return SelectedCampaignTypeIndex switch
            {
                0 => (decimal)DiscountValue, // Discount
                1 => (decimal)LoyaltyPoints, // Loyalty
                2 => (decimal)WalletCredit,  // Wallet
                3 => 1,                      // FreeItem
                4 => 1,                      // Upgrade
                _ => 0
            };
        }

        private Dictionary<string, object> GetOfferConfiguration()
        {
            var config = new Dictionary<string, object>();

            switch (SelectedCampaignTypeIndex)
            {
                case 0: // Discount
                    config["DiscountType"] = DiscountTypeComboBox.SelectedIndex == 0 ? "Percentage" : "FixedAmount";
                    config["DiscountValue"] = DiscountValue;
                    break;
                case 1: // Loyalty
                    config["LoyaltyPoints"] = LoyaltyPoints;
                    break;
                case 2: // Wallet
                    config["WalletCredit"] = WalletCredit;
                    break;
                case 3: // FreeItem
                    if (FreeItemComboBox.SelectedItem is MenuItemDto selectedItem)
                    {
                        config["FreeItemId"] = selectedItem.Id;
                        config["FreeItemName"] = selectedItem.Name;
                    }
                    break;
                case 4: // Upgrade
                    config["UpgradeType"] = "Size";
                    break;
            }

            return config;
        }

        private void CampaignTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateOfferConfigurationVisibility();
            ValidateForm();
        }

        private void UpdateOfferConfigurationVisibility()
        {
            // Hide all configuration panels
            DiscountConfigPanel.Visibility = Visibility.Collapsed;
            LoyaltyConfigPanel.Visibility = Visibility.Collapsed;
            WalletConfigPanel.Visibility = Visibility.Collapsed;
            FreeItemConfigPanel.Visibility = Visibility.Collapsed;

            // Show relevant configuration panel
            switch (SelectedCampaignTypeIndex)
            {
                case 0: // Discount
                    DiscountConfigPanel.Visibility = Visibility.Visible;
                    break;
                case 1: // Loyalty
                    LoyaltyConfigPanel.Visibility = Visibility.Visible;
                    break;
                case 2: // Wallet
                    WalletConfigPanel.Visibility = Visibility.Visible;
                    break;
                case 3: // FreeItem
                    FreeItemConfigPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void OnFormFieldChanged(object sender, object e)
        {
            ValidateForm();
        }

        private bool ValidateForm()
        {
            var isValid = true;
            var errors = new List<string>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(CampaignName))
            {
                errors.Add("Campaign name is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(CampaignDescription))
            {
                errors.Add("Campaign description is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(MessageTemplate))
            {
                errors.Add("Message template is required");
                isValid = false;
            }

            // Validate dates
            if (StartDatePicker.Date != DateTimeOffset.MinValue && EndDatePicker.Date != DateTimeOffset.MinValue)
            {
                if (EndDatePicker.Date <= StartDatePicker.Date)
                {
                    errors.Add("End date must be after start date");
                    isValid = false;
                }
            }

            // Validate offer configuration based on campaign type
            switch (SelectedCampaignTypeIndex)
            {
                case 0: // Discount
                    if (DiscountValue <= 0)
                    {
                        errors.Add("Discount value must be greater than 0");
                        isValid = false;
                    }
                    break;
                case 1: // Loyalty
                    if (LoyaltyPoints <= 0)
                    {
                        errors.Add("Loyalty points must be greater than 0");
                        isValid = false;
                    }
                    break;
                case 2: // Wallet
                    if (WalletCredit <= 0)
                    {
                        errors.Add("Wallet credit must be greater than 0");
                        isValid = false;
                    }
                    break;
                case 3: // FreeItem
                    if (FreeItemComboBox.SelectedItem == null)
                    {
                        errors.Add("Please select a free item");
                        isValid = false;
                    }
                    break;
            }

            IsFormValid = isValid;

            if (!isValid)
            {
                if (ValidationInfoBar != null)
                {
                    ValidationInfoBar.Message = string.Join(", ", errors);
                    ValidationInfoBar.Severity = InfoBarSeverity.Error;
                    ValidationInfoBar.IsOpen = true;
                }
            }
            else
            {
                if (ValidationInfoBar != null)
                {
                    ValidationInfoBar.IsOpen = false;
                }
            }

            return isValid;
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    // Additional DTOs for the dialog
    public class SegmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
    }

    public class MenuItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    // Using DTOs from Services namespace
}
