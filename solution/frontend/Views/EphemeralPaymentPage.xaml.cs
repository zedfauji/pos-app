using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class EphemeralPaymentPage : Page
    {
        private BillResult? _bill;
        private Window? _parentWindow;
        private string _selectedPaymentMethod = "Cash";
        private decimal _originalAmount;
        private decimal _currentAmount;
        private decimal _tipAmount;
        private decimal _digitalAmount;
        private bool _isProcessing;

        // Customer and Wallet
        private object? _selectedCustomer;
        private object? _customerWallet;
        private List<object> _customerSearchResults = new();
        private bool _isSearching;

        // Services
        private readonly PaymentApiService? _paymentService;
        private readonly PaymentIdResolver? _idResolver;
        private readonly SplitPaymentCalculator? _splitCalculator;
        private readonly BillingService? _billingService;
        private ObservableCollection<OrderItemViewModel> _orderItems = new();
        
        // Split payment properties
        private decimal _cashAmount = 0;
        private decimal _cardAmount = 0;
        
        // Customer amount and change properties
        private decimal _customerAmount = 0;
        private decimal _changeAmount = 0;
        private decimal _tipPercentage = 0;
        
        // Discount properties
        private decimal _discountAmount = 0;
        private string? _discountReason;

        // Events for parent page communication
        public event EventHandler<PaymentCompletedEventArgs>? PaymentCompleted;
        public event EventHandler<PaymentCancelledEventArgs>? PaymentCancelled;

        public EphemeralPaymentPage()
        {
            this.InitializeComponent();
            _paymentService = App.Payments;
            _idResolver = new PaymentIdResolver(new TableRepository());
            _splitCalculator = new SplitPaymentCalculator();
            _billingService = new BillingService();
            Loaded += EphemeralPaymentPage_Loaded;
            
            // Initialize order items list
            OrderItemsList.ItemsSource = _orderItems;
            
            // Register event handlers for NumberBox controls
            CustomerAmountInput.ValueChanged += CustomerAmountInput_ValueChanged;
            TipAmountInput.ValueChanged += TipAmountInput_ValueChanged;
            CashAmountInput.ValueChanged += CashAmountInput_ValueChanged;
            CardAmountInput.ValueChanged += CardAmountInput_ValueChanged;
            DigitalAmountInput.ValueChanged += DigitalAmountInput_ValueChanged;
        }

        private void EphemeralPaymentPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Get bill data from navigation parameter
            if (this.Tag is BillResult bill)
            {
                _ = SetBillInfoAsync(bill, null); // No window needed for native page
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is BillResult bill)
            {
                await SetBillInfoAsync(bill, null);
            }
        }

        public async Task SetBillInfoAsync(BillResult bill, Window? parentWindow)
        {
            _bill = bill;
            _parentWindow = parentWindow;
            _originalAmount = bill.TotalAmount;
            _currentAmount = bill.TotalAmount;
            
            UpdateBillInfo();
            await LoadOrderItemsAsync();
            UpdatePaymentSummary();
        }
        
        // Keep synchronous version for backward compatibility
        public void SetBillInfo(BillResult bill, Window? parentWindow)
        {
            _ = SetBillInfoAsync(bill, parentWindow);
        }

        private void UpdateBillInfo()
        {
            if (_bill != null)
            {
                TransactionIdText.Text = _bill.BillId.ToString();
                TableNumberText.Text = _bill.TableLabel ?? "N/A";
                CustomerNameText.Text = _bill.ServerName ?? "Walk-in Customer";
            }
        }

        private async Task LoadOrderItemsAsync()
        {
            _orderItems.Clear();
            
            // First, try to use items from the bill if available
            if (_bill?.Items != null && _bill.Items.Count > 0)
            {
                foreach (var item in _bill.Items)
                {
                    _orderItems.Add(new OrderItemViewModel
                    {
                        Name = item.name,
                        Quantity = item.quantity,
                        UnitPrice = item.price,
                        Subtotal = item.quantity * item.price
                    });
                }
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Loaded {_orderItems.Count} items from bill");
                return;
            }
            
            // If items are missing or empty, try to fetch them using BillingId
            if (_bill?.BillingId != null && _bill.BillingId != Guid.Empty && _billingService != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Items missing, fetching from BillingId {_bill.BillingId}");
                    var items = await _billingService.GetOrderItemsByBillingIdAsync(_bill.BillingId.Value);
                    
                    if (items != null && items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            _orderItems.Add(new OrderItemViewModel
                            {
                                Name = item.name,
                                Quantity = item.quantity,
                                UnitPrice = item.price,
                                Subtotal = item.quantity * item.price
                            });
                        }
                        System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Fetched {_orderItems.Count} items from BillingService");
                        
                        // Update the bill's Items list for future reference
                        _bill.Items = items;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Error fetching items: {ex.Message}");
                }
            }
            
            // If we still don't have items but have ItemsCost or TotalAmount, create a placeholder item
            if (_orderItems.Count == 0 && _bill != null && (_bill.ItemsCost > 0 || _bill.TotalAmount > 0))
            {
                var itemTotal = _bill.ItemsCost > 0 ? _bill.ItemsCost : _bill.TotalAmount;
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Creating placeholder item with total {itemTotal}");
                
                _orderItems.Add(new OrderItemViewModel
                {
                    Name = "Order Items",
                    Quantity = 1,
                    UnitPrice = itemTotal,
                    Subtotal = itemTotal
                });
            }
            
            if (_orderItems.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: WARNING - No items loaded. BillId: {_bill?.BillId}, BillingId: {_bill?.BillingId}, TotalAmount: {_bill?.TotalAmount}");
            }
        }

        private void UpdatePaymentSummary()
        {
            var itemsTotal = _orderItems.Sum(item => item.Subtotal);
            var tip = _tipAmount;
            var discount = _discountAmount;
            var total = itemsTotal + tip - discount;

            // Update display texts
            ItemsTotalText.Text = itemsTotal.ToString("C");
            SummaryItemsTotalText.Text = itemsTotal.ToString("C");
            SummaryTipText.Text = tip > 0 ? $"+{tip:C}" : "$0.00";
            SummaryDiscountText.Text = discount > 0 ? $"-{discount:C}" : "$0.00";
            TotalAmountText.Text = total.ToString("C");
        }

        private void PaymentMethod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string method)
            {
                _selectedPaymentMethod = method;
                
                // Update button styles
                ResetPaymentMethodButtons();
                button.Background = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                button.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                
                // Show/hide split payment panel
                if (method == "Split")
                {
                    SplitPaymentPanel.Visibility = Visibility.Visible;
                    // Initialize split amounts
                    var total = _orderItems.Sum(item => item.Subtotal) + _tipAmount - _discountAmount;
                    CashAmountInput.Value = (double)(total / 3); // Default split equally
                    CardAmountInput.Value = (double)(total / 3);
                    DigitalAmountInput.Value = (double)(total / 3);
                    UpdateSplitTotal();
                }
                else
                {
                    SplitPaymentPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ResetPaymentMethodButtons()
        {
            var defaultBackground = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            var defaultForeground = new SolidColorBrush(Microsoft.UI.Colors.Black);
            var defaultBorder = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            
            CashButton.Background = defaultBackground;
            CashButton.Foreground = defaultForeground;
            CashButton.BorderBrush = defaultBorder;
            CardButton.Background = defaultBackground;
            CardButton.Foreground = defaultForeground;
            CardButton.BorderBrush = defaultBorder;
            MobileButton.Background = defaultBackground;
            MobileButton.Foreground = defaultForeground;
            MobileButton.BorderBrush = defaultBorder;
            SplitButton.Background = defaultBackground;
            SplitButton.Foreground = defaultForeground;
            SplitButton.BorderBrush = defaultBorder;
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OrderItemViewModel item)
            {
                item.Quantity++;
                item.Subtotal = item.Quantity * item.UnitPrice;
                UpdatePaymentSummary();
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OrderItemViewModel item)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    item.Subtotal = item.Quantity * item.UnitPrice;
                    UpdatePaymentSummary();
                }
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            // Reset all values
            _tipAmount = 0;
            _discountAmount = 0;
            _discountReason = null;
            _cashAmount = 0;
            _cardAmount = 0;
            _digitalAmount = 0;
            _customerAmount = 0;
            _changeAmount = 0;
            _tipPercentage = 0;
            
            // Reset payment method selection
            ResetPaymentMethodButtons();
            _selectedPaymentMethod = "Cash";
            
            // Hide split payment panel
            SplitPaymentPanel.Visibility = Visibility.Collapsed;
            
            // Reset tip buttons
            ResetTipButtons();
            
            // Reset input fields
            CustomerAmountInput.Value = 0;
            TipAmountInput.Value = 0;
            TipPercentageText.Text = "0%";
            ChangeAmountText.Text = "";
            
            // Reset order items to original quantities
            _ = LoadOrderItemsAsync();
            
            UpdatePaymentSummary();
        }

        // Split payment event handlers
        private void CashAmountInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _cashAmount = (decimal)args.NewValue;
            UpdateSplitTotal();
        }

        private void CardAmountInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _cardAmount = (decimal)args.NewValue;
            UpdateSplitTotal();
        }

        private void DigitalAmountInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _digitalAmount = (decimal)args.NewValue;
            UpdateSplitTotal();
        }

        private void UpdateSplitTotal()
        {
            var splitTotal = _cashAmount + _cardAmount + _digitalAmount;
            SplitTotalText.Text = splitTotal.ToString("C");
        }

        // Customer amount and tip calculator methods
        private void CustomerAmountInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _customerAmount = (decimal)args.NewValue;
            CalculateChange();
        }

        private void CalculateChange_Click(object sender, RoutedEventArgs e)
        {
            CalculateChange();
        }

        private void CalculateChange()
        {
            var orderTotal = _orderItems.Sum(item => item.Subtotal) + _tipAmount - _discountAmount;
            _changeAmount = _customerAmount - orderTotal;
            
            if (_changeAmount >= 0)
            {
                ChangeAmountText.Text = $"Change: {_changeAmount:C}";
                ChangeAmountText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
            else
            {
                ChangeAmountText.Text = $"Short: {Math.Abs(_changeAmount):C}";
                ChangeAmountText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }

        private void TipPercentage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                // Reset all tip buttons
                ResetTipButtons();
                
                if (tag == "Custom")
                {
                    // Custom tip - don't set percentage, let user enter amount
                    button.Background = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                    button.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                    TipPercentageText.Text = "Custom";
                    return;
                }
                
                // Set percentage and calculate tip amount
                _tipPercentage = decimal.Parse(tag);
                var orderTotal = _orderItems.Sum(item => item.Subtotal) - _discountAmount;
                _tipAmount = orderTotal * (_tipPercentage / 100);
                
                TipAmountInput.Value = (double)_tipAmount;
                TipPercentageText.Text = $"{_tipPercentage}%";
                
                // Highlight selected button
                button.Background = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                button.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                
                UpdatePaymentSummary();
                CalculateChange();
            }
        }

        private void ResetTipButtons()
        {
            var defaultBackground = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            var defaultForeground = new SolidColorBrush(Microsoft.UI.Colors.Black);
            
            Tip10Button.Background = defaultBackground;
            Tip10Button.Foreground = defaultForeground;
            Tip15Button.Background = defaultBackground;
            Tip15Button.Foreground = defaultForeground;
            Tip20Button.Background = defaultBackground;
            Tip20Button.Foreground = defaultForeground;
            TipCustomButton.Background = defaultBackground;
            TipCustomButton.Foreground = defaultForeground;
        }

        private void TipAmountInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _tipAmount = (decimal)args.NewValue;
            
            // Calculate percentage if not custom
            if (_tipPercentage > 0)
            {
                var orderTotal = _orderItems.Sum(item => item.Subtotal) - _discountAmount;
                if (orderTotal > 0)
                {
                    _tipPercentage = (_tipAmount / orderTotal) * 100;
                    TipPercentageText.Text = $"{_tipPercentage:F1}%";
                }
            }
            else
            {
                TipPercentageText.Text = "Custom";
            }
            
            UpdatePaymentSummary();
            CalculateChange();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentCancelled?.Invoke(this, new PaymentCancelledEventArgs
            {
                BillId = _bill?.BillId ?? Guid.Empty
            });
            
            // Navigate back to previous page
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void ProcessPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;
                ProcessPaymentButton.IsEnabled = false;
                ProcessPaymentButton.Content = "⏳ Processing...";

                // Validate payment
                if (!ValidatePayment())
                {
                    return;
                }

                // Process payment through API
                var success = await ProcessPaymentAsync();
                
                if (success)
                {
                    // Show success dialog
                    var successDialog = new ContentDialog()
                    {
                        Title = "Payment Successful",
                        Content = $"Payment of {_currentAmount:C} has been processed successfully!",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    
                    await successDialog.ShowAsync();
                    
                    // Notify parent and close
                    PaymentCompleted?.Invoke(this, new PaymentCompletedEventArgs
                    {
                        BillId = _bill?.BillId ?? Guid.Empty,
                        AmountPaid = _currentAmount,
                        PaymentMethod = _selectedPaymentMethod,
                        TipAmount = _tipAmount,
                        DiscountAmount = _discountAmount,
                        DiscountReason = _discountReason
                    });
                    
                    // Navigate back to previous page
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                }
                else
                {
                    throw new Exception("Payment processing failed");
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Payment Error",
                    Content = $"Payment failed: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await errorDialog.ShowAsync();
            }
            finally
            {
                _isProcessing = false;
                ProcessPaymentButton.IsEnabled = true;
                ProcessPaymentButton.Content = "✅ Process Payment";
            }
        }

        private bool ValidatePayment()
        {
            if (string.IsNullOrEmpty(_selectedPaymentMethod))
            {
                ShowError("Please select a payment method.");
                return false;
            }

            var itemsTotal = _orderItems.Sum(item => item.Subtotal);
            if (itemsTotal <= 0)
            {
                ShowError("Order total must be greater than zero.");
                return false;
            }

            if (_discountAmount > itemsTotal)
            {
                ShowError("Discount cannot exceed the order total.");
                return false;
            }

            // Validate split payment
            if (_selectedPaymentMethod == "Split")
            {
                var splitTotal = _cashAmount + _cardAmount + _digitalAmount;
                var expectedTotal = itemsTotal + _tipAmount - _discountAmount;
                
                if (splitTotal <= 0)
                {
                    ShowError("Please enter amounts for at least one payment method.");
                    return false;
                }
                
                if (Math.Abs(splitTotal - expectedTotal) > 0.01m) // Allow small rounding differences
                {
                    ShowError($"Split payment total ({splitTotal:C}) must equal the order total ({expectedTotal:C}).");
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> ProcessPaymentAsync()
        {
            if (_paymentService == null || _bill == null || _idResolver == null || _splitCalculator == null)
            {
                throw new InvalidOperationException("Required services not available");
            }

            try
            {
                // Resolve SessionId and BillingId using the new service
                var (sessionId, billingId) = await _idResolver.ResolvePaymentIdsAsync(_bill);
                
                if (!_idResolver.ValidatePaymentIds(sessionId, billingId))
                {
                    throw new InvalidOperationException("Invalid SessionId or BillingId resolved");
                }

                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Resolved IDs - SessionId={sessionId}, BillingId={billingId}");

                var paymentLines = new List<PaymentApiService.RegisterPaymentLineDto>();

                if (_selectedPaymentMethod == "Split")
                {
                    // Use the split payment calculator
                    var splitAmounts = new Dictionary<string, decimal>();
                    if (_cashAmount > 0) splitAmounts["Cash"] = _cashAmount;
                    if (_cardAmount > 0) splitAmounts["Card"] = _cardAmount;
                    if (_digitalAmount > 0) splitAmounts["Mobile"] = _digitalAmount;

                    var splitResult = _splitCalculator.CalculateSplitPayment(
                        _originalAmount, _tipAmount, _discountAmount, splitAmounts);

                    foreach (var split in splitResult.SplitDetails)
                    {
                        paymentLines.Add(new PaymentApiService.RegisterPaymentLineDto(
                            AmountPaid: split.AmountPaid,
                            PaymentMethod: split.PaymentMethod,
                            DiscountAmount: split.DiscountAmount,
                            DiscountReason: _discountReason,
                            TipAmount: split.TipAmount,
                            ExternalRef: null,
                            Meta: null
                        ));
                    }
                }
                else
                {
                    // Single payment method
                    paymentLines.Add(new PaymentApiService.RegisterPaymentLineDto(
                        AmountPaid: _currentAmount,
                        PaymentMethod: _selectedPaymentMethod,
                        DiscountAmount: _discountAmount,
                        DiscountReason: _discountReason,
                        TipAmount: _tipAmount,
                        ExternalRef: null,
                        Meta: null
                    ));
                }

                var request = new PaymentApiService.RegisterPaymentRequestDto(
                    SessionId: sessionId,
                    BillingId: billingId,
                    TotalDue: _originalAmount,
                    Lines: paymentLines,
                    ServerId: null
                );

                // Process payment
                var result = await _paymentService.RegisterPaymentAsync(request);
                
                return result != null && result.Status == "paid";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Payment processing error: {ex.Message}");
                throw;
            }
        }

        private async void ShowError(string message)
        {
            var errorDialog = new ContentDialog()
            {
                Title = "Validation Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await errorDialog.ShowAsync();
        }

        private async void UseWalletButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer == null || _customerWallet == null)
            {
                var dialog1 = new ContentDialog()
                {
                    Title = "Validation Error",
                    Content = "Please select a customer first to use wallet payment.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog1.ShowAsync();
                return;
            }

            var walletBalance = (decimal)(_customerWallet.GetType().GetProperty("Balance")?.GetValue(_customerWallet) ?? 0);
            if (walletBalance <= 0)
            {
                var dialog2 = new ContentDialog()
                {
                    Title = "Validation Error",
                    Content = "Customer has no wallet balance available.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog2.ShowAsync();
                return;
            }

            try
            {
                var itemsTotal = _orderItems.Sum(item => item.Subtotal);
                var totalDue = itemsTotal + _tipAmount - _discountAmount;
                
                // Calculate how much can be paid from wallet
                var walletPaymentAmount = Math.Min(walletBalance, totalDue);
                
                // Show wallet payment confirmation dialog
                var dialog = new ContentDialog()
                {
                    Title = "Use Wallet Balance",
                    Content = CreateWalletPaymentContent(walletPaymentAmount, totalDue, walletBalance),
                    PrimaryButtonText = "Use Wallet",
                    SecondaryButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ProcessWalletPayment(walletPaymentAmount, totalDue);
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = $"Error processing wallet payment: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }


        private async void CustomerSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text;
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    sender.ItemsSource = null;
                    return;
                }

                if (_isSearching) return;

                try
                {
                    _isSearching = true;
                    
                    // Simulate customer search results for now
                    var mockResults = new List<string>
                    {
                        $"John Doe - john.doe@email.com (Gold Member)",
                        $"Jane Smith - jane.smith@email.com (Silver Member)",
                        $"Mike Johnson - mike.j@email.com (Standard Member)"
                    }.Where(r => r.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    sender.ItemsSource = mockResults.Any() ? mockResults : null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Customer search error: {ex.Message}");
                    sender.ItemsSource = null;
                }
                finally
                {
                    _isSearching = false;
                }
            }
        }

        private async void CustomerSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedText)
            {
                await SelectCustomerFromText(selectedText);
            }
        }

        private async void CustomerSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                await SelectCustomerFromText(args.QueryText);
            }
        }

        private async Task SelectCustomerFromText(string customerText)
        {
            try
            {
                // Parse customer info from text (mock implementation)
                var parts = customerText.Split(" - ");
                if (parts.Length >= 2)
                {
                    var name = parts[0];
                    var membershipInfo = parts.Length > 2 ? parts[2] : "Standard Member";
                    
                    // Create mock customer object
                    var mockCustomer = new
                    {
                        FullName = name,
                        Email = parts[1].Split(" ")[0],
                        MembershipLevel = new
                        {
                            Name = membershipInfo.Replace("(", "").Replace(")", "").Replace(" Member", ""),
                            DiscountPercentage = GetDiscountForMembership(membershipInfo)
                        }
                    };

                    var mockWallet = new
                    {
                        Balance = 50.00m // Mock wallet balance
                    };

                    _selectedCustomer = mockCustomer;
                    _customerWallet = mockWallet;
                    
                    // Apply membership discount
                    ApplyMembershipDiscount();
                    
                    // Clear search
                    CustomerSearchBox.Text = "";
                    CustomerSearchBox.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = $"Error selecting customer: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private decimal GetDiscountForMembership(string membershipInfo)
        {
            if (membershipInfo.Contains("Gold")) return 15m;
            if (membershipInfo.Contains("Silver")) return 10m;
            if (membershipInfo.Contains("Bronze")) return 5m;
            return 0m;
        }

        private void ApplyMembershipDiscount()
        {
            if (_selectedCustomer != null)
            {
                var membershipLevel = _selectedCustomer.GetType().GetProperty("MembershipLevel")?.GetValue(_selectedCustomer);
                if (membershipLevel != null)
                {
                    var discountPercentage = (decimal)(membershipLevel.GetType().GetProperty("DiscountPercentage")?.GetValue(membershipLevel) ?? 0);
                    if (discountPercentage > 0)
                    {
                        var itemsTotal = _orderItems.Sum(item => item.Subtotal);
                        var discountAmount = itemsTotal * (discountPercentage / 100m);
                        
                        _discountAmount = discountAmount;
                        _discountReason = $"Membership Discount ({discountPercentage}%)";
                        
                        UpdatePaymentSummary();
                    }
                }
            }
        }

        private void ClearCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedCustomer = null;
            _customerWallet = null;
            _customerSearchResults.Clear();
            
            // Clear any membership discounts
            if (_discountReason?.Contains("Membership") == true)
            {
                _discountAmount = 0;
                _discountReason = null;
                UpdatePaymentSummary();
            }
        }

        private StackPanel CreateWalletPaymentContent(decimal walletAmount, decimal totalDue, decimal walletBalance)
        {
            var panel = new StackPanel { Spacing = 12 };
            
            var customerName = _selectedCustomer?.GetType().GetProperty("FullName")?.GetValue(_selectedCustomer)?.ToString() ?? "Unknown Customer";
            
            panel.Children.Add(new TextBlock 
            { 
                Text = $"Customer: {customerName}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            
            panel.Children.Add(new TextBlock 
            { 
                Text = $"Available Wallet Balance: {walletBalance:C}",
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green)
            });
            
            panel.Children.Add(new TextBlock 
            { 
                Text = $"Total Due: {totalDue:C}"
            });
            
            panel.Children.Add(new TextBlock 
            { 
                Text = $"Wallet Payment: {walletAmount:C}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
            });
            
            if (walletAmount < totalDue)
            {
                panel.Children.Add(new TextBlock 
                { 
                    Text = $"Remaining Balance: {totalDue - walletAmount:C}",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange)
                });
                
                panel.Children.Add(new TextBlock 
                { 
                    Text = "You will need to pay the remaining balance with another payment method.",
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            
            return panel;
        }

        private async Task ProcessWalletPayment(decimal walletAmount, decimal totalDue)
        {
            try
            {
                // If wallet covers full amount, process complete payment
                if (walletAmount >= totalDue)
                {
                    await CompleteWalletPayment(walletAmount);
                }
                else
                {
                    // Partial wallet payment - set up for remaining payment
                    await ProcessPartialWalletPayment(walletAmount, totalDue - walletAmount);
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = $"Failed to process wallet payment: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async Task CompleteWalletPayment(decimal amount)
        {
            // Show success dialog
            var successDialog = new ContentDialog()
            {
                Title = "Payment Successful",
                Content = $"Payment of {amount:C} has been processed successfully using wallet!",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await successDialog.ShowAsync();

            // Notify parent and close
            PaymentCompleted?.Invoke(this, new PaymentCompletedEventArgs
            {
                BillId = _bill?.BillId ?? Guid.Empty,
                AmountPaid = amount,
                PaymentMethod = "Wallet",
                TipAmount = _tipAmount,
                DiscountAmount = _discountAmount,
                DiscountReason = _discountReason
            });

            // Navigate back to previous page
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async Task ProcessPartialWalletPayment(decimal walletAmount, decimal remainingAmount)
        {
            // Update current amount to remaining balance
            _currentAmount = remainingAmount;
            UpdatePaymentSummary();

            // Show success message and prompt for remaining payment
            var dialog = new ContentDialog()
            {
                Title = "Wallet Payment Applied",
                Content = $"${walletAmount:F2} has been deducted from the wallet.\n\nRemaining balance: ${remainingAmount:F2}\n\nPlease select a payment method for the remaining amount.",
                CloseButtonText = "Continue",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            PaymentCancelled?.Invoke(this, new PaymentCancelledEventArgs
            {
                BillId = _bill?.BillId ?? Guid.Empty
            });
            
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentCancelled?.Invoke(this, new PaymentCancelledEventArgs
            {
                BillId = _bill?.BillId ?? Guid.Empty
            });
            
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

    }

    // Order Item ViewModel for data binding
    public class OrderItemViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private int _quantity;
        private decimal _unitPrice;
        private decimal _subtotal;

        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(); } 
        }

        public int Quantity 
        { 
            get => _quantity; 
            set { _quantity = value; OnPropertyChanged(); } 
        }

        public decimal UnitPrice 
        { 
            get => _unitPrice; 
            set { _unitPrice = value; OnPropertyChanged(); } 
        }

        public decimal Subtotal 
        { 
            get => _subtotal; 
            set { _subtotal = value; OnPropertyChanged(); } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Event args for payment completion
    public class PaymentCompletedEventArgs : EventArgs
    {
        public Guid BillId { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = "";
        public decimal TipAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
    }

    // Event args for payment cancellation
    public class PaymentCancelledEventArgs : EventArgs
    {
        public Guid BillId { get; set; }
    }
}