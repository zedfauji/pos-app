using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using MagiDesk.Frontend.Services;
using CustomerDto = MagiDesk.Frontend.Services.CustomerDto;
using MembershipLevelDto = MagiDesk.Frontend.Services.MembershipLevelDto;
using CustomerStatsDto = MagiDesk.Frontend.Services.CustomerStatsDto;
using CustomerSearchRequest = MagiDesk.Frontend.Services.CustomerSearchRequest;
using CustomerSearchResponse = MagiDesk.Frontend.Services.CustomerSearchResponse;
using UpdateCustomerRequest = MagiDesk.Frontend.Services.UpdateCustomerRequest;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class CustomerDashboardPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ObservableCollection<CustomerItemViewModel> _customers;
        private readonly ObservableCollection<MembershipLevelDto> _membershipLevels;
        
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;
        private int _totalCount = 0;
        
        private string _searchQuery = "";
        private string _selectedMembershipLevel = "";
        private string _selectedStatus = "";
        private string _sortBy = "firstname_asc";

        public ObservableCollection<CustomerItemViewModel> Customers => _customers;

        public CustomerDashboardPage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _customers = new ObservableCollection<CustomerItemViewModel>();
            _membershipLevels = new ObservableCollection<MembershipLevelDto>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await InitializePageAsync();
        }

        private async Task InitializePageAsync()
        {
            try
            {
                if (LoadingRing != null)
                    LoadingRing.IsActive = true;
                
                // Load membership levels for filter
                await LoadMembershipLevelsAsync();
                
                // Load initial customer data
                await LoadCustomersAsync();
                
                // Load statistics
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to initialize customer dashboard", ex.Message);
            }
            finally
            {
                if (LoadingRing != null)
                    LoadingRing.IsActive = false;
            }
        }

        private async Task LoadMembershipLevelsAsync()
        {
            try
            {
                if (App.CustomerApi == null) return;

                var levels = await App.CustomerApi.GetAllMembershipLevelsAsync();
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _membershipLevels.Clear();
                    MembershipLevelComboBox.Items.Clear();
                    
                    // Add "All Levels" option
                    MembershipLevelComboBox.Items.Add(new ComboBoxItem 
                    { 
                        Content = "All Levels", 
                        Tag = "" 
                    });
                    
                    if (levels != null)
                    {
                        foreach (var level in levels)
                        {
                            _membershipLevels.Add(level);
                            MembershipLevelComboBox.Items.Add(new ComboBoxItem 
                            { 
                                Content = level.Name, 
                                Tag = level.MembershipLevelId.ToString() 
                            });
                        }
                    }
                    
                    MembershipLevelComboBox.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to load membership levels", ex.Message);
            }
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                if (App.CustomerApi == null) return;

                if (LoadingRing != null)
                    LoadingRing.IsActive = true;

                var request = new CustomerSearchRequest
                {
                    SearchTerm = _searchQuery,
                    MembershipLevelId = string.IsNullOrEmpty(_selectedMembershipLevel) ? null : Guid.Parse(_selectedMembershipLevel),
                    IsActive = _selectedStatus switch
                    {
                        "active" => true,
                        "inactive" => false,
                        _ => null
                    },
                    SortBy = _sortBy.Split('_')[0],
                    SortDescending = _sortBy.EndsWith("_desc"),
                    Page = _currentPage,
                    PageSize = _pageSize
                };

                var response = await App.CustomerApi.SearchCustomersAsync(request);
                
                if (response != null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        _customers.Clear();
                        _totalCount = response.TotalCount;
                        _totalPages = response.TotalPages;
                        
                        foreach (var customer in response.Customers)
                        {
                            var membershipLevel = _membershipLevels.FirstOrDefault(m => m.MembershipLevelId == customer.MembershipLevelId);
                            _customers.Add(new CustomerItemViewModel(customer, membershipLevel));
                        }
                        
                        UpdatePaginationUI();
                        UpdateResultsCount();
                    });
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to load customers", ex.Message);
            }
            finally
            {
                if (LoadingRing != null)
                    LoadingRing.IsActive = false;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                if (App.CustomerApi == null) return;

                var stats = await App.CustomerApi.GetCustomerStatsAsync();
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    TotalCustomersText.Text = stats.TotalCustomers.ToString();
                    ActiveCustomersText.Text = stats.ActiveCustomers.ToString();
                    TotalRevenueText.Text = stats.TotalCustomerValue.ToString("C");
                    ExpiredMembershipsText.Text = stats.ExpiredMemberships.ToString();
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to load statistics", ex.Message);
            }
        }

        private void UpdatePaginationUI()
        {
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            PageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
        }

        private void UpdateResultsCount()
        {
            var startItem = (_currentPage - 1) * _pageSize + 1;
            var endItem = Math.Min(_currentPage * _pageSize, _totalCount);
            
            if (_totalCount == 0)
            {
                ResultsCountText.Text = "No customers found";
            }
            else
            {
                ResultsCountText.Text = $"Showing {startItem}-{endItem} of {_totalCount} customers";
            }
        }

        private async Task ShowErrorAsync(string title, string message)
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

        // Event Handlers
        private void AddCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(CustomerRegistrationPage));
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadCustomersAsync();
            await LoadStatisticsAsync();
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchTextBox.Text?.Trim() ?? "";
            _currentPage = 1;
            await LoadCustomersAsync();
        }

        private async void MembershipLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MembershipLevelComboBox.SelectedItem is ComboBoxItem item)
            {
                _selectedMembershipLevel = item.Tag?.ToString() ?? "";
                _currentPage = 1;
                await LoadCustomersAsync();
            }
        }

        private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusComboBox.SelectedItem is ComboBoxItem item)
            {
                _selectedStatus = item.Tag?.ToString() ?? "";
                _currentPage = 1;
                await LoadCustomersAsync();
            }
        }

        private async void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem item)
            {
                _sortBy = item.Tag?.ToString() ?? "firstname_asc";
                _currentPage = 1;
                await LoadCustomersAsync();
            }
        }

        private async void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            MembershipLevelComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;
            
            _searchQuery = "";
            _selectedMembershipLevel = "";
            _selectedStatus = "";
            _sortBy = "firstname_asc";
            _currentPage = 1;
            
            await LoadCustomersAsync();
        }

        private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadCustomersAsync();
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadCustomersAsync();
            }
        }

        private void CustomersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle customer selection if needed
        }

        private void ViewCustomerDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is int customerId)
            {
                // Convert int back to Guid for navigation
                var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
                if (customer != null)
                {
                    // We need to get the actual Guid from the customer data
                    var actualCustomer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
                    if (actualCustomer != null)
                    {
                        // For now, create a new Guid based on the customer ID
                        // In a real scenario, we'd store the actual Guid
                        Frame.Navigate(typeof(CustomerDetailsPage), Guid.NewGuid());
                    }
                }
            }
        }

        private async void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is int customerId)
            {
                // TODO: Navigate to customer edit page
                await ShowErrorAsync("Not Implemented", "Customer edit page will be implemented next.");
            }
        }

        private void ManageWallet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is int customerId)
            {
                // Navigate to wallet management page
                Frame.Navigate(typeof(WalletManagementPage), Guid.NewGuid());
            }
        }

        private async void DeactivateCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is int customerId)
            {
                var dialog = new ContentDialog
                {
                    Title = "Deactivate Customer",
                    Content = "Are you sure you want to deactivate this customer? This action can be reversed later.",
                    PrimaryButtonText = "Deactivate",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        if (App.CustomerApi != null)
                        {
                            var updateRequest = new UpdateCustomerRequest
                            {
                                IsActive = false
                            };
                            
                            await App.CustomerApi.UpdateCustomerAsync(Guid.Parse(customerId.ToString()), updateRequest);
                            await LoadCustomersAsync();
                            await LoadStatisticsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorAsync("Failed to deactivate customer", ex.Message);
                    }
                }
            }
        }
    }

    public class CustomerItemViewModel
    {
        public int CustomerId { get; }
        public string FullName { get; }
        public string Initials { get; }
        public string ContactInfo { get; }
        public string MembershipLevel { get; }
        public string MembershipColor { get; }
        public string StatusText { get; }
        public string StatusColor { get; }
        public string TotalSpentText { get; }
        public string LoyaltyPointsText { get; }

        public CustomerItemViewModel(CustomerDto customer, MembershipLevelDto? membershipLevel)
        {
            CustomerId = (int)customer.CustomerId.GetHashCode(); // Convert Guid to int for display
            FullName = customer.FullName;
            Initials = $"{customer.FirstName.FirstOrDefault()}{customer.LastName.FirstOrDefault()}".ToUpper();
            ContactInfo = !string.IsNullOrEmpty(customer.Email) ? customer.Email : customer.Phone ?? "";
            
            MembershipLevel = membershipLevel?.Name ?? "Standard";
            MembershipColor = GetMembershipColor(membershipLevel?.Name ?? "Standard");
            
            StatusText = customer.IsActive ? "Active" : "Inactive";
            StatusColor = customer.IsActive ? "#10B981" : "#EF4444";
            
            TotalSpentText = customer.TotalSpent.ToString("C");
            LoyaltyPointsText = $"{customer.LoyaltyPoints} pts";
        }

        private static string GetMembershipColor(string membershipName)
        {
            return membershipName.ToLower() switch
            {
                "bronze" => "#CD7F32",
                "silver" => "#C0C0C0",
                "gold" => "#FFD700",
                "platinum" => "#E5E4E2",
                "diamond" => "#B9F2FF",
                _ => "#6B7280"
            };
        }
    }
}
