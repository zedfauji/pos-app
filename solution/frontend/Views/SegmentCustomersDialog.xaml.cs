using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class SegmentCustomersDialog : ContentDialog
    {
        private readonly CustomerIntelligenceService _customerService;
        private readonly string _segmentId;
        private readonly string _segmentName;
        private readonly ObservableCollection<SegmentCustomerDto> _customers;
        private readonly ObservableCollection<SegmentCustomerDto> _filteredCustomers;
        private string _searchQuery = string.Empty;
        private string _sortBy = "Name";

        public SegmentCustomersDialog(string segmentId, string segmentName)
        {
            this.InitializeComponent();
            _customerService = new CustomerIntelligenceService();
            _segmentId = segmentId;
            _segmentName = segmentName;
            _customers = new ObservableCollection<SegmentCustomerDto>();
            _filteredCustomers = new ObservableCollection<SegmentCustomerDto>();
            CustomersListView.ItemsSource = _filteredCustomers;

            SegmentNameText.Text = _segmentName;
            
            this.Loaded += SegmentCustomersDialog_Loaded;
        }

        private async void SegmentCustomersDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCustomersAsync();
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                ShowLoadingState();
                
                var customers = await _customerService.GetSegmentCustomersAsync(_segmentId);
                
                // Convert CustomerDto to SegmentCustomerDto
                var segmentCustomers = customers.Select(c => new SegmentCustomerDto
                {
                    Id = c.CustomerId.ToString(),
                    Name = c.FullName,
                    Email = c.Email ?? string.Empty,
                    Phone = c.Phone ?? string.Empty,
                    OrderCount = c.TotalVisits,
                    TotalSpent = c.TotalSpent,
                    LastOrderDate = c.CreatedAt, // Using CreatedAt as placeholder
                    JoinDate = c.CreatedAt,
                    MembershipLevel = c.MembershipLevel?.Name ?? "Bronze",
                    LoyaltyPoints = c.LoyaltyPoints
                }).ToList();
                
                _customers.Clear();
                foreach (var customer in segmentCustomers)
                {
                    _customers.Add(customer);
                }

                CustomerCountText.Text = $"{_customers.Count:N0} customers in this segment";
                ApplyFiltersAndSort();
                ShowContentState();
            }
            catch (Exception ex)
            {
                ShowErrorState($"Failed to load customers: {ex.Message}");
            }
        }

        private void ApplyFiltersAndSort()
        {
            _filteredCustomers.Clear();

            var filtered = _customers.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                filtered = filtered.Where(c => 
                    c.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Apply sorting
            filtered = _sortBy switch
            {
                "Name" => filtered.OrderBy(c => c.Name),
                "Total Spent" => filtered.OrderByDescending(c => c.TotalSpent),
                "Order Count" => filtered.OrderByDescending(c => c.OrderCount),
                "Last Order" => filtered.OrderByDescending(c => c.LastOrderDate),
                "Join Date" => filtered.OrderByDescending(c => c.JoinDate),
                _ => filtered.OrderBy(c => c.Name)
            };

            foreach (var customer in filtered)
            {
                _filteredCustomers.Add(customer);
            }

            UpdateEmptyState();
        }

        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            CustomersListView.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowContentState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CustomersListView.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CustomersListView.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Visible;
        }

        private void ShowErrorState(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            CustomersListView.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Visible;
            
            // Update empty state to show error
            var errorIcon = EmptyStatePanel.Children[0] as FontIcon;
            if (errorIcon != null)
            {
                errorIcon.Glyph = "\uE783"; // Error icon
            }
            
            var errorTitle = EmptyStatePanel.Children[1] as TextBlock;
            if (errorTitle != null)
            {
                errorTitle.Text = "Error Loading Customers";
            }
            
            var errorMessage = EmptyStatePanel.Children[2] as TextBlock;
            if (errorMessage != null)
            {
                errorMessage.Text = message;
            }
        }

        private void UpdateEmptyState()
        {
            if (_filteredCustomers.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                ShowContentState();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text;
            ApplyFiltersAndSort();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem item)
            {
                _sortBy = item.Content.ToString();
                ApplyFiltersAndSort();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportButton.IsEnabled = false;
                ExportButton.Content = "Exporting...";

                // Create file picker
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", new List<string>() { ".csv" });
                savePicker.SuggestedFileName = $"{_segmentName}_customers_{DateTime.Now:yyyyMMdd}";

                // Get the current window handle
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await ExportCustomersToCSV(file);
                    
                    var successDialog = new ContentDialog
                    {
                        Title = "Export Complete",
                        Content = $"Customer data has been exported to {file.Name}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Export Failed",
                    Content = $"Failed to export customer data: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            finally
            {
                ExportButton.IsEnabled = true;
                ExportButton.Content = "Export";
            }
        }

        private async Task ExportCustomersToCSV(StorageFile file)
        {
            var csvContent = new List<string>
            {
                "Name,Email,Phone,Order Count,Total Spent,Last Order Date,Join Date,Membership Level,Loyalty Points"
            };

            foreach (var customer in _filteredCustomers)
            {
                var line = $"\"{customer.Name}\",\"{customer.Email}\",\"{customer.Phone}\"," +
                          $"{customer.OrderCount},{customer.TotalSpent:F2}," +
                          $"\"{customer.LastOrderDate:yyyy-MM-dd}\",\"{customer.JoinDate:yyyy-MM-dd}\"," +
                          $"\"{customer.MembershipLevel}\",{customer.LoyaltyPoints}";
                csvContent.Add(line);
            }

            await FileIO.WriteLinesAsync(file, csvContent);
        }
    }

    // DTO for segment customers
    public class SegmentCustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastOrderDate { get; set; }
        public DateTime JoinDate { get; set; }
        public string MembershipLevel { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        
        // Formatted properties for display
        public string OrderCountFormatted => $"{OrderCount} orders";
        public string TotalSpentFormatted => TotalSpent.ToString("C");
        public string LastOrderDateFormatted => $"Last: {LastOrderDate:MMM dd}";
        public string LoyaltyPointsFormatted => $"{LoyaltyPoints:N0} pts";
    }
}
