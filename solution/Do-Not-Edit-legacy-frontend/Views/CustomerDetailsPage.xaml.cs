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
using WalletDto = MagiDesk.Frontend.Services.WalletDto;
using WalletTransactionDto = MagiDesk.Frontend.Services.WalletTransactionDto;
using LoyaltyTransactionDto = MagiDesk.Frontend.Services.LoyaltyTransactionDto;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class CustomerDetailsPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ObservableCollection<WalletTransactionItemViewModel> _walletTransactions;
        private readonly ObservableCollection<LoyaltyTransactionItemViewModel> _loyaltyTransactions;
        
        private Guid _customerId;
        private CustomerDto? _customer;
        private WalletDto? _wallet;

        public CustomerDetailsPage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _walletTransactions = new ObservableCollection<WalletTransactionItemViewModel>();
            _loyaltyTransactions = new ObservableCollection<LoyaltyTransactionItemViewModel>();
            
            WalletTransactionsListView.ItemsSource = _walletTransactions;
            LoyaltyTransactionsListView.ItemsSource = _loyaltyTransactions;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is Guid customerId)
            {
                _customerId = customerId;
                await LoadCustomerDetailsAsync();
            }
            else
            {
                await ShowErrorAsync("Error", "Invalid customer ID provided");
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
        }

        private async Task LoadCustomerDetailsAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                
                if (App.CustomerApi == null)
                {
                    await ShowErrorAsync("Error", "Customer API is not available");
                    return;
                }

                // Load customer details
                _customer = await App.CustomerApi.GetCustomerByIdAsync(_customerId);
                if (_customer == null)
                {
                    await ShowErrorAsync("Error", "Customer not found");
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                    return;
                }

                // Load wallet information
                _wallet = await App.CustomerApi.GetWalletByCustomerIdAsync(_customerId);

                // Update UI
                UpdateCustomerUI();
                
                // Load transactions
                await LoadWalletTransactionsAsync();
                await LoadLoyaltyTransactionsAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to load customer details: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private void UpdateCustomerUI()
        {
            if (_customer == null) return;

            _dispatcherQueue.TryEnqueue(() =>
            {
                // Header information
                CustomerNameText.Text = _customer.FullName;
                CustomerInitialsText.Text = $"{_customer.FirstName.FirstOrDefault()}{_customer.LastName.FirstOrDefault()}".ToUpper();
                
                // Status
                CustomerStatusText.Text = _customer.IsActive ? "Active" : "Inactive";
                CustomerStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    _customer.IsActive ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);

                // Membership badge
                if (_customer.MembershipLevel != null)
                {
                    MembershipLevelText.Text = _customer.MembershipLevel.Name;
                    MembershipBadge.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        GetMembershipColor(_customer.MembershipLevel.Name));
                }

                // Statistics
                TotalSpentText.Text = _customer.TotalSpent.ToString("C");
                LoyaltyPointsText.Text = $"{_customer.LoyaltyPoints} pts";
                WalletBalanceText.Text = _wallet?.Balance.ToString("C") ?? "$0.00";
                TotalVisitsText.Text = _customer.TotalVisits.ToString();

                // Personal information
                FullNameText.Text = _customer.FullName;
                EmailText.Text = !string.IsNullOrEmpty(_customer.Email) ? _customer.Email : "Not provided";
                PhoneText.Text = !string.IsNullOrEmpty(_customer.Phone) ? _customer.Phone : "Not provided";
                DateOfBirthText.Text = _customer.DateOfBirth?.ToString("MMM dd, yyyy") ?? "Not provided";
                MemberSinceText.Text = _customer.CreatedAt.ToString("MMM dd, yyyy");

                // Membership details
                if (_customer.MembershipLevel != null)
                {
                    MembershipDetailsText.Text = _customer.MembershipLevel.Name;
                    MembershipExpiryText.Text = _customer.MembershipExpiryDate?.ToString("MMM dd, yyyy") ?? "Never";
                    DiscountText.Text = $"{_customer.MembershipLevel.DiscountPercentage:F1}%";
                    LoyaltyMultiplierText.Text = $"{_customer.MembershipLevel.LoyaltyMultiplier:F1}x";
                }

                // Notes
                NotesText.Text = !string.IsNullOrEmpty(_customer.Notes) ? _customer.Notes : "No notes available";
            });
        }

        private async Task LoadWalletTransactionsAsync()
        {
            try
            {
                if (App.CustomerApi == null) return;

                var response = await App.CustomerApi.GetWalletTransactionsAsync(_customerId, 1, 10);
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _walletTransactions.Clear();
                    
                    if (response?.Transactions?.Any() == true)
                    {
                        foreach (var transaction in response.Transactions)
                        {
                            _walletTransactions.Add(new WalletTransactionItemViewModel(transaction));
                        }
                        NoTransactionsText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NoTransactionsText.Visibility = Visibility.Visible;
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to load wallet transactions: {ex.Message}");
            }
        }

        private async Task LoadLoyaltyTransactionsAsync()
        {
            try
            {
                if (App.CustomerApi == null) return;

                var response = await App.CustomerApi.GetLoyaltyTransactionsAsync(_customerId, 1, 10);
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _loyaltyTransactions.Clear();
                    
                    if (response?.Transactions?.Any() == true)
                    {
                        foreach (var transaction in response.Transactions)
                        {
                            _loyaltyTransactions.Add(new LoyaltyTransactionItemViewModel(transaction));
                        }
                        NoLoyaltyText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NoLoyaltyText.Visibility = Visibility.Visible;
                    }
                });
            }
            catch (Exception ex)
            {
                {
                    await ShowErrorAsync("Error", $"Failed to load loyalty transactions: {ex.Message}");
                }
            }
        }

        private Windows.UI.Color GetMembershipColor(string membershipLevel)
        {
            return membershipLevel.ToLower() switch
            {
                "bronze" => Microsoft.UI.Colors.SaddleBrown,
                "silver" => Microsoft.UI.Colors.Silver,
                "gold" => Microsoft.UI.Colors.Gold,
                "platinum" => Microsoft.UI.Colors.LightGray,
                "diamond" => Microsoft.UI.Colors.LightBlue,
                _ => Microsoft.UI.Colors.Gray
            };
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
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void EditCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to customer edit page
            await ShowErrorAsync("Not Implemented", "Customer edit functionality will be implemented next.");
        }

        private async void ManageWallet_Click(object sender, RoutedEventArgs e)
        {
            if (_customerId != Guid.Empty)
            {
                Frame.Navigate(typeof(WalletManagementPage), _customerId);
            }
        }

        private async void ManageWalletButton_Click(object sender, RoutedEventArgs e)
        {
            if (_customerId != Guid.Empty)
            {
                Frame.Navigate(typeof(WalletManagementPage), _customerId);
            }
        }

        private async void ViewAllTransactionsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to full wallet transactions page
            await ShowErrorAsync("Not Implemented", "Full wallet transactions view will be implemented next.");
        }

        private async void ViewAllLoyaltyButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to full loyalty transactions page
            await ShowErrorAsync("Not Implemented", "Full loyalty transactions view will be implemented next.");
        }
    }

    public class WalletTransactionItemViewModel
    {
        public string Description { get; }
        public string DateText { get; }
        public string AmountText { get; }
        public string AmountColor { get; }
        public string TypeIcon { get; }
        public string TypeColor { get; }

        public WalletTransactionItemViewModel(WalletTransactionDto transaction)
        {
            Description = transaction.Description;
            DateText = transaction.CreatedAt.ToString("MMM dd, yyyy");
            
            var isCredit = transaction.TransactionType.ToLower() == "credit";
            AmountText = (isCredit ? "+" : "-") + transaction.Amount.ToString("C");
            AmountColor = isCredit ? "#10B981" : "#EF4444";
            TypeIcon = isCredit ? "&#xE710;" : "&#xE711;";
            TypeColor = isCredit ? "#10B981" : "#EF4444";
        }
    }

    public class LoyaltyTransactionItemViewModel
    {
        public string Description { get; }
        public string DateText { get; }
        public string PointsText { get; }
        public string PointsColor { get; }
        public string TypeIcon { get; }
        public string TypeColor { get; }

        public LoyaltyTransactionItemViewModel(LoyaltyTransactionDto transaction)
        {
            Description = transaction.Description;
            DateText = transaction.CreatedAt.ToString("MMM dd, yyyy");
            
            var isEarned = transaction.TransactionType.ToLower() == "earned";
            PointsText = (isEarned ? "+" : "-") + transaction.Points.ToString() + " pts";
            PointsColor = isEarned ? "#10B981" : "#EF4444";
            TypeIcon = isEarned ? "&#xE710;" : "&#xE711;";
            TypeColor = isEarned ? "#10B981" : "#EF4444";
        }
    }
}
