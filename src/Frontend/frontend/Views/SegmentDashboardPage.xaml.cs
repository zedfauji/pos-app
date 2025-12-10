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
    public sealed partial class SegmentDashboardPage : Page
    {
        private readonly CustomerIntelligenceService _customerService;
        private readonly ObservableCollection<SegmentSummaryDto> _segments;
        private readonly ObservableCollection<SegmentSummaryDto> _filteredSegments;
        private string _searchQuery = string.Empty;
        private string _statusFilter = "All";
        private string _sizeFilter = "All";

        public SegmentDashboardPage()
        {
            this.InitializeComponent();
            _customerService = new CustomerIntelligenceService();
            _segments = new ObservableCollection<SegmentSummaryDto>();
            _filteredSegments = new ObservableCollection<SegmentSummaryDto>();
            SegmentsListView.ItemsSource = _filteredSegments;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadSegmentsAsync();
        }

        private async Task LoadSegmentsAsync()
        {
            try
            {
                ShowLoadingState();
                
                var segments = await _customerService.GetSegmentsAsync();
                
                _segments.Clear();
                
                // Show helpful message if no segments (likely API not available)
                if (segments.Count == 0)
                {
                    ShowEmptyState();
                    return;
                }
                
                foreach (var segment in segments)
                {
                    var segmentSummary = new SegmentSummaryDto
                    {
                        Id = segment.SegmentId.ToString(),
                        Name = segment.Name,
                        Description = segment.Description,
                        IsActive = segment.IsActive,
                        CustomerCount = segment.CustomerCount,
                        LastUpdated = segment.CreatedAt, // Using CreatedAt as LastUpdated
                        CriteriaPreview = GetCriteriaPreview(segment),
                        AverageOrderValue = await GetSegmentAverageOrderValue(segment.SegmentId.ToString()),
                        TotalRevenue = await GetSegmentTotalRevenue(segment.SegmentId.ToString())
                    };
                    _segments.Add(segmentSummary);
                }

                UpdateSummaryCards();
                ApplyFilters();
                ShowContentState();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load segments", ex.Message);
                ShowEmptyState();
            }
        }

        private List<string> GetCriteriaPreview(Services.CustomerSegmentDto segment)
        {
            var preview = new List<string>();
            
            // This would be based on the actual segment criteria
            // For now, adding some sample criteria based on common segmentation
            if (segment.CustomerCount > 0)
            {
                preview.Add("Active Customers");
            }
            
            // Add more criteria based on segment configuration
            preview.Add("Last 30 days");
            
            return preview.Take(3).ToList(); // Show max 3 criteria tags
        }

        private async Task<decimal> GetSegmentAverageOrderValue(string segmentId)
        {
            try
            {
                var analytics = await _customerService.GetSegmentAnalyticsAsync(Guid.Parse(segmentId));
                return analytics?.AverageOrderValue ?? 0;
            }
            catch
            {
                return 0; // Return 0 if analytics not available
            }
        }

        private async Task<decimal> GetSegmentTotalRevenue(string segmentId)
        {
            try
            {
                var analytics = await _customerService.GetSegmentAnalyticsAsync(Guid.Parse(segmentId));
                return analytics?.TotalRevenue ?? 0;
            }
            catch
            {
                return 0; // Return 0 if analytics not available
            }
        }

        private void UpdateSummaryCards()
        {
            TotalSegmentsText.Text = _segments.Count.ToString("N0");
            ActiveSegmentsText.Text = _segments.Count(s => s.IsActive).ToString("N0");
            
            var totalCustomers = _segments.Sum(s => s.CustomerCount);
            TotalCustomersText.Text = totalCustomers.ToString("N0");
            
            var avgSize = _segments.Count > 0 ? totalCustomers / _segments.Count : 0;
            AvgSegmentSizeText.Text = avgSize.ToString("N0");
        }

        private void ApplyFilters()
        {
            _filteredSegments.Clear();

            var filtered = _segments.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                filtered = filtered.Where(s => 
                    s.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Apply status filter
            if (_statusFilter != "All")
            {
                var isActive = _statusFilter == "Active";
                filtered = filtered.Where(s => s.IsActive == isActive);
            }

            // Apply size filter
            if (_sizeFilter != "All")
            {
                filtered = _sizeFilter switch
                {
                    "Small (1-50)" => filtered.Where(s => s.CustomerCount <= 50),
                    "Medium (51-200)" => filtered.Where(s => s.CustomerCount > 50 && s.CustomerCount <= 200),
                    "Large (201+)" => filtered.Where(s => s.CustomerCount > 200),
                    _ => filtered
                };
            }

            foreach (var segment in filtered.OrderByDescending(s => s.LastUpdated))
            {
                _filteredSegments.Add(segment);
            }

            UpdateEmptyState();
        }

        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            SegmentsListView.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowContentState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            SegmentsListView.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            SegmentsListView.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Visible;
        }

        private void UpdateEmptyState()
        {
            if (_filteredSegments.Count == 0)
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
            await LoadSegmentsAsync();
        }

        private async void CreateSegmentButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SegmentEditDialog();
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await LoadSegmentsAsync();
            }
        }

        private async void EditSegmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SegmentSummaryDto segment)
            {
                // Convert to full SegmentDto for editing
                var fullSegment = await _customerService.GetSegmentAsync(Guid.Parse(segment.Id));
                if (fullSegment != null)
                {
                    var segmentDto = new Services.SegmentDto
                    {
                        Id = fullSegment.SegmentId.ToString(),
                        Name = fullSegment.Name,
                        Description = fullSegment.Description,
                        IsActive = fullSegment.IsActive
                    };
                    var dialog = new SegmentEditDialog(segmentDto);
                    dialog.XamlRoot = this.XamlRoot;
                    
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        await LoadSegmentsAsync();
                    }
                }
            }
        }

        private async void RefreshSegmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SegmentSummaryDto segment)
            {
                try
                {
                    button.IsEnabled = false;
                    button.Content = "Refreshing...";

                    await _customerService.RefreshSegmentMembershipAsync(segment.Id);
                    
                    await LoadSegmentsAsync();
                    
                    await ShowSuccessDialog("Segment Refreshed", 
                        $"Segment '{segment.Name}' has been refreshed successfully.");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Refresh Failed", $"Failed to refresh segment: {ex.Message}");
                }
                finally
                {
                    button.IsEnabled = true;
                    button.Content = "Refresh";
                }
            }
        }

        private async void ViewCustomersButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SegmentSummaryDto segment)
            {
                var dialog = new SegmentCustomersDialog(segment.Id, segment.Name);
                dialog.XamlRoot = this.XamlRoot;
                await dialog.ShowAsync();
            }
        }

        private async void CreateCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SegmentSummaryDto segment)
            {
                // Navigate to campaign creation with pre-selected segment
                var dialog = new CampaignEditDialog();
                // Set the target segment in the dialog
                dialog.XamlRoot = this.XamlRoot;
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ShowSuccessDialog("Campaign Created", 
                        $"Campaign has been created for segment '{segment.Name}'.");
                }
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

        private void SizeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SizeFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                _sizeFilter = item.Content.ToString();
                ApplyFilters();
            }
        }

        private void SegmentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

    // DTO for segment summary display
    public class SegmentSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int CustomerCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> CriteriaPreview { get; set; } = new();
        public decimal AverageOrderValue { get; set; }
        public decimal TotalRevenue { get; set; }
        
        // Formatted properties for display
        public string CustomerCountFormatted => CustomerCount.ToString("N0");
        public string AverageOrderValueFormatted => AverageOrderValue.ToString("C");
        public string TotalRevenueFormatted => TotalRevenue.ToString("C");
        public string LastUpdatedFormatted => LastUpdated.ToString("MMM dd");
    }

    // DTO for segment analytics
    public class SegmentAnalyticsDto
    {
        public decimal AverageOrderValue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public double AverageFrequency { get; set; }
        public DateTime LastOrderDate { get; set; }
    }

    // Using SegmentDto from Services namespace
}
