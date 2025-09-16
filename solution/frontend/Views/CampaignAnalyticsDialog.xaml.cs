using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class CampaignAnalyticsDialog : ContentDialog
    {
        private readonly CustomerIntelligenceService _customerService;
        private readonly string _campaignId;
        private readonly ObservableCollection<Services.CampaignActivityDto> _recentActivities = new();

        public CampaignAnalyticsDialog(string campaignId)
        {
            this.InitializeComponent();
            _customerService = new CustomerIntelligenceService();
            _campaignId = campaignId;
            // _recentActivities already initialized above
            RecentActivityListView.ItemsSource = _recentActivities;

            this.Loaded += CampaignAnalyticsDialog_Loaded;
        }

        private async void CampaignAnalyticsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAnalyticsAsync();
        }

        private async Task LoadAnalyticsAsync()
        {
            try
        {
                ShowLoadingState();

                // Load campaign details
                var campaign = await _customerService.GetCampaignAsync(Guid.Parse(_campaignId));
                if (campaign != null)
                {
                    LoadCampaignOverview(campaign);
                }

                // Load analytics data
                var analytics = await _customerService.GetCampaignAnalyticsAsync(_campaignId);
                if (analytics != null)
                {
                    LoadMetrics(analytics);
                }

                // Load recent activity
                var activities = await _customerService.GetCampaignActivitiesAsync(_campaignId);
                LoadRecentActivities(activities);

                ShowContentState();
            }
            catch (Exception ex)
            {
                ShowErrorState($"Failed to load campaign analytics: {ex.Message}");
            }
        }

        private void LoadCampaignOverview(Services.CampaignDto campaign)
        {
            CampaignNameText.Text = campaign.Name;
            CampaignDescriptionText.Text = campaign.Description;
            CampaignStatusText.Text = campaign.Status;
            CampaignTypeText.Text = campaign.CampaignType;
            CampaignChannelText.Text = campaign.Channel;

            // Calculate duration
            var duration = campaign.EndDate.HasValue 
                ? (campaign.EndDate.Value - campaign.StartDate).Days 
                : (DateTime.Now - campaign.StartDate).Days;
            CampaignDurationText.Text = $"{duration} days";
        }

        private void LoadMetrics(Services.CampaignAnalyticsDto analytics)
        {
            // Messages Sent
            MessagesSentText.Text = analytics.MessagesSent.ToString("N0");
            MessagesSentChangeText.Text = FormatChange(analytics.MessagesSentChange);
            MessagesSentChangeText.Foreground = GetChangeColor(analytics.MessagesSentChange);

            // Redemptions
            RedemptionsText.Text = analytics.Redemptions.ToString("N0");
            RedemptionsChangeText.Text = FormatChange(analytics.RedemptionsChange);
            RedemptionsChangeText.Foreground = GetChangeColor(analytics.RedemptionsChange);

            // Redemption Rate
            RedemptionRateText.Text = analytics.RedemptionRate.ToString("P1");
            RedemptionRateChangeText.Text = FormatPercentageChange(analytics.RedemptionRateChange);
            RedemptionRateChangeText.Foreground = GetChangeColor(analytics.RedemptionRateChange);

            // Revenue Impact
            RevenueImpactText.Text = analytics.RevenueImpact.ToString("C");
            RevenueImpactChangeText.Text = FormatCurrencyChange(analytics.RevenueImpactChange);
            RevenueImpactChangeText.Foreground = GetChangeColor(analytics.RevenueImpactChange);

            // Unique Customers
            UniqueCustomersText.Text = analytics.UniqueCustomers.ToString("N0");
            UniqueCustomersChangeText.Text = FormatChange(analytics.UniqueCustomersChange);
            UniqueCustomersChangeText.Foreground = GetChangeColor(analytics.UniqueCustomersChange);

            // Average Order Value
            AvgOrderValueText.Text = analytics.AverageOrderValue.ToString("C");
            AvgOrderValueChangeText.Text = FormatCurrencyChange(analytics.AverageOrderValueChange);
            AvgOrderValueChangeText.Foreground = GetChangeColor(analytics.AverageOrderValueChange);

            // Cost Per Acquisition
            CostPerAcquisitionText.Text = analytics.CostPerAcquisition.ToString("C");
            CostPerAcquisitionChangeText.Text = FormatCurrencyChange(analytics.CostPerAcquisitionChange);
            CostPerAcquisitionChangeText.Foreground = GetChangeColor(analytics.CostPerAcquisitionChange);

            // ROI
            ROIText.Text = analytics.ROI.ToString("P1");
            ROIChangeText.Text = FormatPercentageChange(analytics.ROIChange);
            ROIChangeText.Foreground = GetChangeColor(analytics.ROIChange);
        }

        private void LoadRecentActivities(IEnumerable<Services.CampaignActivityDto> activities)
        {
            _recentActivities.Clear();
            foreach (var activity in activities.OrderByDescending(a => a.Timestamp).Take(20))
            {
                _recentActivities.Add(activity);
            }
        }

        private string FormatChange(double change)
        {
            if (change == 0) return "No change";
            var sign = change > 0 ? "+" : "";
            return $"{sign}{change:N0} from last period";
        }

        private string FormatPercentageChange(double change)
        {
            if (change == 0) return "No change";
            var sign = change > 0 ? "+" : "";
            return $"{sign}{change:P1} from last period";
        }

        private string FormatCurrencyChange(decimal change)
        {
            if (change == 0) return "No change";
            var sign = change > 0 ? "+" : "";
            return $"{sign}{change:C} from last period";
        }

        private Microsoft.UI.Xaml.Media.Brush GetChangeColor(double change)
        {
            if (change > 0)
                return (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
            else if (change < 0)
                return (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
            else
                return (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        }

        private Microsoft.UI.Xaml.Media.Brush GetChangeColor(decimal change)
        {
            return GetChangeColor((double)change);
        }

        private void TimeRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // In a real implementation, this would reload the chart data
            // based on the selected time range
        }

        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            CampaignOverviewPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.IsOpen = false;
        }

        private void ShowContentState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CampaignOverviewPanel.Visibility = Visibility.Visible;
            ErrorInfoBar.IsOpen = false;
        }

        private void ShowErrorState(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CampaignOverviewPanel.Visibility = Visibility.Collapsed;
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }
    }

    // Additional DTOs for analytics
    public class CampaignAnalyticsDto
    {
        public int MessagesSent { get; set; }
        public double MessagesSentChange { get; set; }
        public int Redemptions { get; set; }
        public double RedemptionsChange { get; set; }
        public double RedemptionRate { get; set; }
        public double RedemptionRateChange { get; set; }
        public decimal RevenueImpact { get; set; }
        public decimal RevenueImpactChange { get; set; }
        public int UniqueCustomers { get; set; }
        public double UniqueCustomersChange { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal AverageOrderValueChange { get; set; }
        public decimal CostPerAcquisition { get; set; }
        public decimal CostPerAcquisitionChange { get; set; }
        public double ROI { get; set; }
        public double ROIChange { get; set; }
    }

    public class CampaignActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
