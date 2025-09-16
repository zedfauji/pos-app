using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class CampaignManagementPage : Page
    {
        private readonly CustomerIntelligenceService _customerService;
        private readonly ObservableCollection<CampaignDto> _campaigns;
        private readonly ObservableCollection<CampaignDto> _filteredCampaigns;
        private string _searchQuery = string.Empty;
        private string _statusFilter = "All";
        private string _typeFilter = "All";
        private string _channelFilter = "All";

        public CampaignManagementPage()
        {
            this.InitializeComponent();
            _customerService = new CustomerIntelligenceService();
            _campaigns = new ObservableCollection<CampaignDto>();
            _filteredCampaigns = new ObservableCollection<CampaignDto>();
            CampaignsListView.ItemsSource = _filteredCampaigns;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadCampaignsAsync();
        }

        private async Task LoadCampaignsAsync()
        {
            try
            {
                ShowLoadingState();
                
                var campaigns = await _customerService.GetCampaignsAsync();
                
                _campaigns.Clear();
                
                // Show helpful message if no campaigns (likely API not available)
                if (campaigns.Count == 0)
                {
                    ShowEmptyState();
                    return;
                }
                
                foreach (var campaign in campaigns)
                {
                    var campaignDto = new CampaignDto
                    {
                        Id = campaign.CampaignId.ToString(),
                        Name = campaign.Name,
                        Description = campaign.Description,
                        CampaignType = campaign.CampaignType,
                        Status = campaign.Status,
                        Channel = campaign.Channel,
                        TargetSegmentId = campaign.TargetSegmentId?.ToString(),
                        TargetSegmentName = campaign.TargetSegmentName,
                        StartDate = campaign.StartDate,
                        EndDate = campaign.EndDate,
                        CreatedAt = campaign.CreatedAt,
                        RedemptionRate = campaign.RedemptionRate
                    };
                    _campaigns.Add(campaignDto);
                }

                ApplyFilters();
                ShowContentState();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load campaigns", ex.Message);
                ShowEmptyState();
            }
        }

        private void ApplyFilters()
        {
            _filteredCampaigns.Clear();

            var filtered = _campaigns.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                filtered = filtered.Where(c => 
                    c.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Apply status filter
            if (_statusFilter != "All")
            {
                filtered = filtered.Where(c => c.Status.Equals(_statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply type filter
            if (_typeFilter != "All")
            {
                filtered = filtered.Where(c => c.CampaignType.Equals(_typeFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply channel filter
            if (_channelFilter != "All")
            {
                filtered = filtered.Where(c => c.Channel.Equals(_channelFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var campaign in filtered.OrderByDescending(c => c.CreatedAt))
            {
                _filteredCampaigns.Add(campaign);
            }

            UpdateEmptyState();
        }

        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            CampaignsListView.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowContentState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CampaignsListView.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CampaignsListView.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Visible;
        }

        private void UpdateEmptyState()
        {
            if (_filteredCampaigns.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                ShowContentState();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadCampaignsAsync();
        }

        private async void CreateCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CampaignEditDialog();
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await LoadCampaignsAsync();
            }
        }

        private async void EditCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CampaignDto campaign)
            {
                var dialog = new CampaignEditDialog(campaign);
                dialog.XamlRoot = this.XamlRoot;
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await LoadCampaignsAsync();
                }
            }
        }

        private async void ExecuteCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CampaignDto campaign)
            {
                try
                {
                    var confirmDialog = new ContentDialog
                    {
                        Title = "Execute Campaign",
                        Content = $"Are you sure you want to execute the campaign '{campaign.Name}'? This will send messages to all eligible customers in the target segment.",
                        PrimaryButtonText = "Execute",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await confirmDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        button.IsEnabled = false;
                        button.Content = "Executing...";

                        var executionResult = await _customerService.ExecuteCampaignAsync(campaign.Id);
                        
                        await ShowSuccessDialog("Campaign Executed", 
                            $"Campaign '{campaign.Name}' has been executed successfully. " +
                            $"Messages sent: {executionResult.MessagesSent}, " +
                            $"Errors: {executionResult.Errors.Count}");

                        await LoadCampaignsAsync();
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Execution Failed", $"Failed to execute campaign: {ex.Message}");
                    button.IsEnabled = true;
                    button.Content = "Execute";
                }
            }
        }

        private async void ViewAnalyticsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CampaignDto campaign)
            {
                var dialog = new CampaignAnalyticsDialog(campaign.Id);
                dialog.XamlRoot = this.XamlRoot;
                await dialog.ShowAsync();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text;
            ApplyFilters();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                _statusFilter = item.Content.ToString();
                ApplyFilters();
            }
        }

        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                _typeFilter = item.Content.ToString();
                ApplyFilters();
            }
        }

        private void ChannelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                _channelFilter = item.Content.ToString();
                ApplyFilters();
            }
        }

        private void CampaignsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection if needed for future features
        }

        private async void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    // DTO classes for data binding (these should match the service DTOs)
    public class CampaignDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CampaignType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string? TargetSegmentId { get; set; }
        public string? TargetSegmentName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public double RedemptionRate { get; set; }
        public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        
        // Formatted properties for display
        public string RedemptionRateFormatted => RedemptionRate.ToString("P1");
        public string StartDateFormatted => StartDate.ToString("MMM dd, yyyy");
        public string EndDateFormatted => EndDate?.ToString("MMM dd, yyyy") ?? "No end date";
    }
}
