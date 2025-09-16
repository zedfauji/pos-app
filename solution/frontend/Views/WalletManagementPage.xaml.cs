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
using WalletDto = MagiDesk.Frontend.Services.WalletDto;
using WalletTransactionDto = MagiDesk.Frontend.Services.WalletTransactionDto;
using AddWalletFundsRequest = MagiDesk.Frontend.Services.AddWalletFundsRequest;
using DeductWalletFundsRequest = MagiDesk.Frontend.Services.DeductWalletFundsRequest;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class WalletManagementPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ObservableCollection<WalletTransactionItemViewModel> _transactions;
        
        private Guid _customerId;
        private CustomerDto? _customer;
        private WalletDto? _wallet;
        
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;
        private string _transactionTypeFilter = "";
        private string _dateRangeFilter = "all";

        public WalletManagementPage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _transactions = new ObservableCollection<WalletTransactionItemViewModel>();
            
            TransactionsListView.ItemsSource = _transactions;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is Guid customerId)
            {
                _customerId = customerId;
                await LoadWalletDataAsync();
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

        private async Task LoadWalletDataAsync()
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
                UpdateWalletUI();
                
                // Load transactions
                await LoadTransactionsAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to load wallet data: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private void UpdateWalletUI()
        {
            if (_customer == null) return;

            _dispatcherQueue.TryEnqueue(() =>
            {
                // Header information
                CustomerNameText.Text = _customer.FullName;
                CustomerInitialsText.Text = $"{_customer.FirstName.FirstOrDefault()}{_customer.LastName.FirstOrDefault()}".ToUpper();
                
                // Wallet statistics
                WalletBalanceText.Text = _wallet?.Balance.ToString("C") ?? "$0.00";
                TotalLoadedText.Text = _wallet?.TotalLoaded.ToString("C") ?? "$0.00";
                TotalSpentText.Text = _wallet?.TotalSpent.ToString("C") ?? "$0.00";
            });
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                if (App.CustomerApi == null) return;

                var response = await App.CustomerApi.GetWalletTransactionsAsync(_customerId, _currentPage, _pageSize);
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _transactions.Clear();
                    
                    if (response?.Transactions?.Any() == true)
                    {
                        var filteredTransactions = response.Transactions.AsEnumerable();
                        
                        // Apply transaction type filter
                        if (!string.IsNullOrEmpty(_transactionTypeFilter))
                        {
                            filteredTransactions = filteredTransactions.Where(t => 
                                t.TransactionType.ToLower() == _transactionTypeFilter.ToLower());
                        }
                        
                        // Apply date range filter
                        if (_dateRangeFilter != "all" && int.TryParse(_dateRangeFilter, out var days))
                        {
                            var cutoffDate = DateTime.Now.AddDays(-days);
                            filteredTransactions = filteredTransactions.Where(t => t.CreatedAt >= cutoffDate);
                        }
                        
                        foreach (var transaction in filteredTransactions)
                        {
                            _transactions.Add(new WalletTransactionItemViewModel(transaction));
                        }
                        
                        NoTransactionsText.Visibility = _transactions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                        
                        // Update pagination
                        _totalPages = response.TotalPages;
                        UpdatePaginationUI();
                    }
                    else
                    {
                        NoTransactionsText.Visibility = Visibility.Visible;
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to load transactions: {ex.Message}");
            }
        }

        private void UpdatePaginationUI()
        {
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            PageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
        }

        private bool ValidateAmount(string amountText, out decimal amount)
        {
            amount = 0;
            
            if (string.IsNullOrWhiteSpace(amountText))
            {
                return false;
            }
            
            if (!decimal.TryParse(amountText, out amount))
            {
                return false;
            }
            
            return amount > 0;
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

        private async Task ShowSuccessAsync(string title, string message)
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

        private async void AddFundsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAmount(AddFundsAmountTextBox.Text, out decimal amount))
            {
                await ShowErrorAsync("Invalid Amount", "Please enter a valid amount greater than 0");
                return;
            }

            try
            {
                LoadingRing.IsActive = true;
                AddFundsButton.IsEnabled = false;

                if (App.CustomerApi == null)
                {
                    await ShowErrorAsync("Error", "Customer API is not available");
                    return;
                }

                var request = new AddWalletFundsRequest
                {
                    Amount = amount,
                    Description = string.IsNullOrWhiteSpace(AddFundsDescriptionTextBox.Text) 
                        ? "Manual fund addition" 
                        : AddFundsDescriptionTextBox.Text.Trim()
                };

                var success = await App.CustomerApi.AddFundsAsync(_customerId, request);

                if (success)
                {
                    await ShowSuccessAsync("Success", $"Successfully added {amount:C} to wallet");
                    
                    // Clear form
                    AddFundsAmountTextBox.Text = "";
                    AddFundsDescriptionTextBox.Text = "";
                    
                    // Reload data
                    await LoadWalletDataAsync();
                }
                else
                {
                    await ShowErrorAsync("Error", "Failed to add funds. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to add funds: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                AddFundsButton.IsEnabled = true;
            }
        }

        private async void DeductFundsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAmount(DeductFundsAmountTextBox.Text, out decimal amount))
            {
                await ShowErrorAsync("Invalid Amount", "Please enter a valid amount greater than 0");
                return;
            }

            // Check if sufficient balance
            if (_wallet != null && amount > _wallet.Balance)
            {
                await ShowErrorAsync("Insufficient Balance", $"Cannot deduct {amount:C}. Current balance is {_wallet.Balance:C}");
                return;
            }

            try
            {
                LoadingRing.IsActive = true;
                DeductFundsButton.IsEnabled = false;

                if (App.CustomerApi == null)
                {
                    await ShowErrorAsync("Error", "Customer API is not available");
                    return;
                }

                var request = new DeductWalletFundsRequest
                {
                    Amount = amount,
                    Description = string.IsNullOrWhiteSpace(DeductFundsDescriptionTextBox.Text) 
                        ? "Manual fund deduction" 
                        : DeductFundsDescriptionTextBox.Text.Trim()
                };

                var success = await App.CustomerApi.DeductFundsAsync(_customerId, request);

                if (success)
                {
                    await ShowSuccessAsync("Success", $"Successfully deducted {amount:C} from wallet");
                    
                    // Clear form
                    DeductFundsAmountTextBox.Text = "";
                    DeductFundsDescriptionTextBox.Text = "";
                    
                    // Reload data
                    await LoadWalletDataAsync();
                }
                else
                {
                    await ShowErrorAsync("Error", "Failed to deduct funds. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to deduct funds: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                DeductFundsButton.IsEnabled = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadWalletDataAsync();
        }

        private async void TransactionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionTypeComboBox.SelectedItem is ComboBoxItem item)
            {
                _transactionTypeFilter = item.Tag?.ToString() ?? "";
                _currentPage = 1;
                await LoadTransactionsAsync();
            }
        }

        private async void DateRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DateRangeComboBox.SelectedItem is ComboBoxItem item)
            {
                _dateRangeFilter = item.Tag?.ToString() ?? "all";
                _currentPage = 1;
                await LoadTransactionsAsync();
            }
        }

        private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadTransactionsAsync();
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadTransactionsAsync();
            }
        }

        private async void ViewTransactionDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Guid transactionId)
            {
                // TODO: Show transaction details dialog
                await ShowErrorAsync("Not Implemented", "Transaction details view will be implemented next.");
            }
        }
    }

}
