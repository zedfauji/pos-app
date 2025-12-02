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
        private int _numberOfPeople = 2;
        private string _splitMode = "Amount"; // "Amount", "Percentage", "People", "Items"
        private decimal _cashPercentage = 0;
        private decimal _cardPercentage = 0;
        private decimal _digitalPercentage = 0;
        
        // Customer amount and change properties
        private decimal _customerAmount = 0;
        private decimal _changeAmount = 0;
        private decimal _tipPercentage = 0;
        
        // Discount properties
        private decimal _discountAmount = 0;
        private string? _discountReason;
        
        // Tip distribution
        private string _tipDistribution = "SplitEqually"; // "SplitEqually", "OnCardOnly", "OnCashOnly", "Custom"

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
                SetBillInfo(bill, null); // No window needed for native page
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is BillResult bill)
            {
                SetBillInfo(bill, null);
            }
        }

        public void SetBillInfo(BillResult bill, Window? parentWindow)
        {
            _bill = bill;
            _parentWindow = parentWindow;
            _originalAmount = bill.TotalAmount;
            _currentAmount = bill.TotalAmount;
            
            UpdateBillInfo();
            LoadOrderItems();
            UpdatePaymentSummary();
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

        // Synchronous wrapper for backward compatibility
        private void LoadOrderItems()
        {
            _ = LoadOrderItemsAsync();
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
                        Subtotal = item.quantity * item.price,
                        AssignedPaymentMethod = "",
                        IsIncluded = true // Default to included
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
                                Subtotal = item.quantity * item.price,
                                AssignedPaymentMethod = "",
                                IsIncluded = true // Default to included
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
                    Subtotal = itemTotal,
                    AssignedPaymentMethod = "",
                    IsIncluded = true // Default to included
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
                    // Initialize split mode to "Amount" (default)
                    _splitMode = "Amount";
                    
                    // Set the correct radio button as checked (only Amount should be checked)
                    if (SplitAmountRadio != null)
                        SplitAmountRadio.IsChecked = true;
                    if (SplitPercentageRadio != null)
                        SplitPercentageRadio.IsChecked = false;
                    if (SplitPeopleRadio != null)
                        SplitPeopleRadio.IsChecked = false;
                    if (SplitItemsRadio != null)
                        SplitItemsRadio.IsChecked = false;
                    
                    // Ensure ItemsModePanel is hidden when Amount mode is selected
                    if (ItemsModePanel != null)
                        ItemsModePanel.Visibility = Visibility.Collapsed;
                    
                    // Show/hide appropriate panels
                    if (AmountModePanel != null)
                        AmountModePanel.Visibility = Visibility.Visible;
                    if (PeopleModePanel != null)
                        PeopleModePanel.Visibility = Visibility.Collapsed;
                    if (ItemsModePanel != null)
                        ItemsModePanel.Visibility = Visibility.Collapsed;
                    
                    // Hide percentage text blocks for Amount mode
                    if (CashPercentageText != null)
                        CashPercentageText.Visibility = Visibility.Collapsed;
                    if (CardPercentageText != null)
                        CardPercentageText.Visibility = Visibility.Collapsed;
                    if (DigitalPercentageText != null)
                        DigitalPercentageText.Visibility = Visibility.Collapsed;
                    
                    // Initialize items split list if needed
                    if (ItemsSplitList != null && ItemsSplitList.ItemsSource == null)
                    {
                        ItemsSplitList.ItemsSource = _orderItems;
                    }
                    
                    // Initialize split amounts with equal split
                    SplitEvenly();
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
            if (ChangeAmountBorder != null) ChangeAmountBorder.Visibility = Visibility.Collapsed;
            if (ChangeAmountText != null) ChangeAmountText.Text = "";
            
            // Reset order items to original quantities
            LoadOrderItems();
            
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
            _cardAmount = Math.Round((decimal)args.NewValue, 2);
            UpdateSplitTotal();
        }
        
        private void DigitalAmountInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _digitalAmount = Math.Round((decimal)args.NewValue, 2);
            UpdateSplitTotal();
        }

        private void UpdateSplitTotal()
        {
            if (SplitTotalText == null) return;
            
            var splitTotal = _cashAmount + _cardAmount + _digitalAmount;
            SplitTotalText.Text = splitTotal.ToString("C");
            
            // Update balance indicator
            var itemsTotal = _orderItems.Sum(item => item.Subtotal);
            var expectedTotal = itemsTotal + _tipAmount - _discountAmount;
            var difference = splitTotal - expectedTotal;
            
            UpdateBalanceIndicator(difference, expectedTotal);
            
            // Update percentage displays if in percentage mode
            if (_splitMode == "Percentage" && expectedTotal > 0)
            {
                _cashPercentage = _cashAmount > 0 ? (_cashAmount / expectedTotal) * 100 : 0;
                _cardPercentage = _cardAmount > 0 ? (_cardAmount / expectedTotal) * 100 : 0;
                _digitalPercentage = _digitalAmount > 0 ? (_digitalAmount / expectedTotal) * 100 : 0;
                
                if (CashPercentageText != null)
                {
                    CashPercentageText.Text = _cashPercentage > 0 ? $"{_cashPercentage:F1}%" : "";
                    CashPercentageText.Visibility = _cashPercentage > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                if (CardPercentageText != null)
                {
                    CardPercentageText.Text = _cardPercentage > 0 ? $"{_cardPercentage:F1}%" : "";
                    CardPercentageText.Visibility = _cardPercentage > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                if (DigitalPercentageText != null)
                {
                    DigitalPercentageText.Text = _digitalPercentage > 0 ? $"{_digitalPercentage:F1}%" : "";
                    DigitalPercentageText.Visibility = _digitalPercentage > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else
            {
                if (CashPercentageText != null)
                    CashPercentageText.Visibility = Visibility.Collapsed;
                if (CardPercentageText != null)
                    CardPercentageText.Visibility = Visibility.Collapsed;
                if (DigitalPercentageText != null)
                    DigitalPercentageText.Visibility = Visibility.Collapsed;
            }
        }
        
        private void UpdateBalanceIndicator(decimal difference, decimal expectedTotal)
        {
            // Check if UI elements are initialized
            if (BalanceStatusText == null || BalanceIcon == null || BalanceIndicator == null)
                return;
                
            var absDifference = Math.Abs(difference);
            
            if (absDifference <= 0.01m) // Balanced (within 1 cent)
            {
                BalanceStatusText.Text = $"Balance: {expectedTotal:C} ✅";
                BalanceStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 16)); // Green
                BalanceIcon.Text = "✅";
                BalanceIndicator.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 255, 240)); // Light green
            }
            else if (absDifference <= 0.10m) // Close to balanced (within 10 cents)
            {
                BalanceStatusText.Text = $"Balance: {expectedTotal:C} (Difference: {difference:C})";
                BalanceStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 185, 0)); // Yellow
                BalanceIcon.Text = "⚠️";
                BalanceIndicator.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 250, 240)); // Light yellow
            }
            else // Not balanced
            {
                if (difference > 0)
                {
                    BalanceStatusText.Text = $"Over by: {difference:C}";
                }
                else
                {
                    BalanceStatusText.Text = $"Short by: {Math.Abs(difference):C}";
                }
                BalanceStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 17, 35)); // Red
                BalanceIcon.Text = "❌";
                BalanceIndicator.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 240, 240)); // Light red
            }
        }
        
        private void SplitMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio && radio.Tag is string mode)
            {
                _splitMode = mode;
                
                // Check if UI elements are initialized (split panel might not be visible yet)
                if (AmountModePanel == null || PeopleModePanel == null || ItemsModePanel == null)
                    return;
                
                // Uncheck all other radio buttons to ensure only one is selected
                if (SplitAmountRadio != null && mode != "Amount")
                    SplitAmountRadio.IsChecked = false;
                if (SplitPercentageRadio != null && mode != "Percentage")
                    SplitPercentageRadio.IsChecked = false;
                if (SplitPeopleRadio != null && mode != "People")
                    SplitPeopleRadio.IsChecked = false;
                if (SplitItemsRadio != null && mode != "Items")
                    SplitItemsRadio.IsChecked = false;
                
                // Show/hide appropriate panels
                AmountModePanel.Visibility = (mode == "People" || mode == "Items") ? Visibility.Collapsed : Visibility.Visible;
                PeopleModePanel.Visibility = mode == "People" ? Visibility.Visible : Visibility.Collapsed;
                ItemsModePanel.Visibility = mode == "Items" ? Visibility.Visible : Visibility.Collapsed;
                
                if (mode == "Percentage")
                {
                    // Show percentage text blocks
                    if (CashPercentageText != null)
                        CashPercentageText.Visibility = Visibility.Visible;
                    if (CardPercentageText != null)
                        CardPercentageText.Visibility = Visibility.Visible;
                    if (DigitalPercentageText != null)
                        DigitalPercentageText.Visibility = Visibility.Visible;
                }
                else
                {
                    if (CashPercentageText != null)
                        CashPercentageText.Visibility = Visibility.Collapsed;
                    if (CardPercentageText != null)
                        CardPercentageText.Visibility = Visibility.Collapsed;
                    if (DigitalPercentageText != null)
                        DigitalPercentageText.Visibility = Visibility.Collapsed;
                }
                
                // Recalculate based on new mode
                if (mode == "People")
                {
                    CalculateSplitByPeople();
                }
                else if (mode == "Percentage")
                {
                    CalculateSplitByPercentage();
                }
                else if (mode == "Items")
                {
                    InitializeItemsSplit();
                }
                else if (mode == "Amount")
                {
                    // For Amount mode, just update the split total
                    UpdateSplitTotal();
                }
            }
        }
        
        private void SplitEvenly_Click(object sender, RoutedEventArgs e)
        {
            SplitEvenly();
        }
        
        private void SplitEvenly()
        {
            var itemsTotal = _orderItems.Sum(item => item.Subtotal);
            var expectedTotal = itemsTotal + _tipAmount - _discountAmount;
            
            if (_splitMode == "People")
            {
                CalculateSplitByPeople();
            }
            else
            {
                // Split equally among 3 payment methods
                var amountPerMethod = expectedTotal / 3;
                if (CashAmountInput != null)
                    CashAmountInput.Value = (double)Math.Round(amountPerMethod, 2);
                if (CardAmountInput != null)
                    CardAmountInput.Value = (double)Math.Round(amountPerMethod, 2);
                if (DigitalAmountInput != null)
                    DigitalAmountInput.Value = (double)Math.Round(amountPerMethod, 2);
                
                // Adjust for rounding - put remainder in cash
                var splitTotal = _cashAmount + _cardAmount + _digitalAmount;
                var remainder = expectedTotal - splitTotal;
                if (Math.Abs(remainder) > 0.01m && CashAmountInput != null)
                {
                    CashAmountInput.Value = (double)(_cashAmount + remainder);
                }
            }
        }
        
        private void AutoFillRemaining_Click(object sender, RoutedEventArgs e)
        {
            var itemsTotal = _orderItems.Sum(item => item.Subtotal);
            var expectedTotal = itemsTotal + _tipAmount - _discountAmount;
            var currentTotal = _cashAmount + _cardAmount + _digitalAmount;
            var remaining = expectedTotal - currentTotal;
            
            if (remaining > 0)
            {
                // Fill remaining in the method with the smallest amount
                if (_cashAmount <= _cardAmount && _cashAmount <= _digitalAmount && CashAmountInput != null)
                {
                    CashAmountInput.Value = (double)(_cashAmount + remaining);
                }
                else if (_cardAmount <= _digitalAmount && CardAmountInput != null)
                {
                    CardAmountInput.Value = (double)(_cardAmount + remaining);
                }
                else if (DigitalAmountInput != null)
                {
                    DigitalAmountInput.Value = (double)(_digitalAmount + remaining);
                }
            }
        }
        
        private void NumberOfPeopleInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _numberOfPeople = (int)args.NewValue;
            CalculateSplitByPeople();
        }
        
        private void CalculateSplitByPeople()
        {
            if (_numberOfPeople < 2) return;
            
            var itemsTotal = _orderItems.Sum(item => item.Subtotal);
            var expectedTotal = itemsTotal + _tipAmount - _discountAmount;
            var perPerson = expectedTotal / _numberOfPeople;
            
            if (PerPersonAmountText != null)
                PerPersonAmountText.Text = $"Per Person: {perPerson:C}";
            
            // Reset amounts
            _cashAmount = 0;
            _cardAmount = 0;
            _digitalAmount = 0;
            
            // Distribute per person amounts across payment methods
            var remaining = expectedTotal;
            var methodIndex = 0;
            
            for (int i = 0; i < _numberOfPeople && remaining > 0.01m; i++)
            {
                var amountToAdd = Math.Min(perPerson, remaining);
                
                // Distribute to methods in round-robin fashion
                if (methodIndex % 3 == 0)
                {
                    _cashAmount += amountToAdd;
                }
                else if (methodIndex % 3 == 1)
                {
                    _cardAmount += amountToAdd;
                }
                else
                {
                    _digitalAmount += amountToAdd;
                }
                
                remaining -= amountToAdd;
                methodIndex++;
            }
            
            // Adjust for rounding - put remainder in cash
            if (remaining > 0.01m)
            {
                _cashAmount += remaining;
            }
            
            // Update UI - check if controls are initialized
            if (CashAmountInput != null)
                CashAmountInput.Value = (double)_cashAmount;
            if (CardAmountInput != null)
                CardAmountInput.Value = (double)_cardAmount;
            if (DigitalAmountInput != null)
                DigitalAmountInput.Value = (double)_digitalAmount;
        }
        
        private void CalculateSplitByPercentage()
        {
            var itemsTotal = _orderItems.Sum(item => item.Subtotal);
            var expectedTotal = itemsTotal + _tipAmount - _discountAmount;
            
            if (_cashPercentage + _cardPercentage + _digitalPercentage > 100.01m)
            {
                // Percentages exceed 100%, normalize them
                var totalPercent = _cashPercentage + _cardPercentage + _digitalPercentage;
                _cashPercentage = (_cashPercentage / totalPercent) * 100;
                _cardPercentage = (_cardPercentage / totalPercent) * 100;
                _digitalPercentage = (_digitalPercentage / totalPercent) * 100;
            }
            
            _cashAmount = Math.Round(expectedTotal * (_cashPercentage / 100), 2);
            _cardAmount = Math.Round(expectedTotal * (_cardPercentage / 100), 2);
            _digitalAmount = Math.Round(expectedTotal * (_digitalPercentage / 100), 2);
            
            // Adjust for rounding
            var splitTotal = _cashAmount + _cardAmount + _digitalAmount;
            var remainder = expectedTotal - splitTotal;
            if (Math.Abs(remainder) > 0.01m)
            {
                _cashAmount += remainder;
            }
            
            // Update UI - check if controls are initialized
            if (CashAmountInput != null)
                CashAmountInput.Value = (double)_cashAmount;
            if (CardAmountInput != null)
                CardAmountInput.Value = (double)_cardAmount;
            if (DigitalAmountInput != null)
                DigitalAmountInput.Value = (double)_digitalAmount;
        }
        
        private void TipDistribution_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio && radio.Tag is string distribution)
            {
                _tipDistribution = distribution;
            }
        }
        
        private void InitializeItemsSplit()
        {
            // Initialize items split mode - assign items to payment methods
            if (ItemsSplitList == null) return;
            
            // If no items are assigned yet, auto-assign them
            bool hasAssignments = _orderItems.Any(item => !string.IsNullOrEmpty(item.AssignedPaymentMethod));
            if (!hasAssignments)
            {
                AutoAssignItems();
            }
            else
            {
                // Recalculate split amounts based on current assignments
                CalculateSplitByItems();
            }
        }
        
        private void ItemPaymentMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // Get the item from the Tag
                if (comboBox.Tag is OrderItemViewModel item)
                {
                    if (comboBox.SelectedItem is string paymentMethod)
                    {
                        item.AssignedPaymentMethod = paymentMethod;
                        CalculateSplitByItems();
                    }
                }
                else if (comboBox.DataContext is OrderItemViewModel itemFromContext)
                {
                    if (comboBox.SelectedItem is string paymentMethod)
                    {
                        itemFromContext.AssignedPaymentMethod = paymentMethod;
                        CalculateSplitByItems();
                    }
                }
            }
        }
        
        private void ItemInclude_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is OrderItemViewModel item)
            {
                item.IsIncluded = true;
                CalculateSplitByItems();
            }
        }
        
        private void ItemInclude_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is OrderItemViewModel item)
            {
                item.IsIncluded = false;
                item.AssignedPaymentMethod = ""; // Clear payment method when excluded
                CalculateSplitByItems();
            }
        }
        
        private void AutoAssignItems_Click(object sender, RoutedEventArgs e)
        {
            AutoAssignItems();
        }
        
        private void IncludeAllItems_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems == null) return;
            
            foreach (var item in _orderItems)
            {
                item.IsIncluded = true;
            }
            
            // Auto-assign included items
            AutoAssignItems();
        }
        
        private void ExcludeAllItems_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems == null) return;
            
            foreach (var item in _orderItems)
            {
                item.IsIncluded = false;
                item.AssignedPaymentMethod = "";
            }
            
            // Reset amounts
            _cashAmount = 0;
            _cardAmount = 0;
            _digitalAmount = 0;
            
            // Update UI
            if (CashAmountInput != null)
                CashAmountInput.Value = 0;
            if (CardAmountInput != null)
                CardAmountInput.Value = 0;
            if (DigitalAmountInput != null)
                DigitalAmountInput.Value = 0;
            
            UpdateSplitTotal();
        }
        
        private void AutoAssignItems()
        {
            if (_orderItems == null || _orderItems.Count == 0) return;
            
            // Distribute items evenly across payment methods (only for included items)
            var methods = new[] { "Cash", "Card", "Digital" };
            int methodIndex = 0;
            
            foreach (var item in _orderItems)
            {
                // Only assign payment method to included items
                if (item.IsIncluded)
                {
                    item.AssignedPaymentMethod = methods[methodIndex % methods.Length];
                    methodIndex++;
                }
                else
                {
                    // Clear payment method for excluded items
                    item.AssignedPaymentMethod = "";
                }
            }
            
            CalculateSplitByItems();
        }
        
        private void CalculateSplitByItems()
        {
            if (_orderItems == null || _orderItems.Count == 0) return;
            
            // Reset amounts
            _cashAmount = 0;
            _cardAmount = 0;
            _digitalAmount = 0;
            
            // Calculate amounts based on item assignments (only for included items)
            foreach (var item in _orderItems)
            {
                // Skip excluded items
                if (!item.IsIncluded)
                    continue;
                    
                if (string.IsNullOrEmpty(item.AssignedPaymentMethod))
                    continue;
                    
                switch (item.AssignedPaymentMethod)
                {
                    case "Cash":
                        _cashAmount += item.Subtotal;
                        break;
                    case "Card":
                        _cardAmount += item.Subtotal;
                        break;
                    case "Digital":
                        _digitalAmount += item.Subtotal;
                        break;
                }
            }
            
            // Update UI - check if controls are initialized
            if (CashAmountInput != null)
                CashAmountInput.Value = (double)_cashAmount;
            if (CardAmountInput != null)
                CardAmountInput.Value = (double)_cardAmount;
            if (DigitalAmountInput != null)
                DigitalAmountInput.Value = (double)_digitalAmount;
            
            // Update split total and balance
            UpdateSplitTotal();
        }

        // Customer amount and tip calculator methods
        private void CustomerAmountInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _customerAmount = (decimal)args.NewValue;
            CalculateChange();
        }

        private void CalculateChange()
        {
            if (ChangeAmountBorder == null || ChangeAmountText == null) return;
            
            var orderTotal = _orderItems.Sum(item => item.Subtotal) + _tipAmount - _discountAmount;
            _changeAmount = _customerAmount - orderTotal;
            
            // Only show change if customer amount is greater than 0
            if (_customerAmount > 0)
            {
                ChangeAmountBorder.Visibility = Visibility.Visible;
                
                if (_changeAmount >= 0)
                {
                    ChangeAmountText.Text = $"Change: {_changeAmount:C}";
                    // Green background with white text for high contrast
                    ChangeAmountText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                    ChangeAmountBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Green);
                    ChangeAmountBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DarkGreen);
                }
                else
                {
                    ChangeAmountText.Text = $"Short: {Math.Abs(_changeAmount):C}";
                    // Red background with white text for high contrast
                    ChangeAmountText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                    ChangeAmountBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    ChangeAmountBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                }
            }
            else
            {
                ChangeAmountBorder.Visibility = Visibility.Collapsed;
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
                    
                    // Add a small delay to ensure payment is fully processed on backend
                    await Task.Delay(500);
                    
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

                    // Calculate tip distribution based on selected option
                    var tipAmounts = DistributeTip(_tipAmount, splitAmounts);

                    var splitResult = _splitCalculator.CalculateSplitPayment(
                        _originalAmount, _tipAmount, _discountAmount, splitAmounts);

                    // Override tip amounts based on distribution preference
                    foreach (var split in splitResult.SplitDetails)
                    {
                        if (tipAmounts.ContainsKey(split.PaymentMethod))
                        {
                            split.TipAmount = tipAmounts[split.PaymentMethod];
                        }
                        
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

        private Dictionary<string, decimal> DistributeTip(decimal totalTip, Dictionary<string, decimal> splitAmounts)
        {
            var tipAmounts = new Dictionary<string, decimal>();
            
            if (_tipDistribution == "NoTip" || totalTip <= 0)
            {
                foreach (var method in splitAmounts.Keys)
                {
                    tipAmounts[method] = 0;
                }
            }
            else if (_tipDistribution == "OnCardOnly")
            {
                foreach (var method in splitAmounts.Keys)
                {
                    tipAmounts[method] = method == "Card" ? totalTip : 0;
                }
            }
            else if (_tipDistribution == "OnCashOnly")
            {
                foreach (var method in splitAmounts.Keys)
                {
                    tipAmounts[method] = method == "Cash" ? totalTip : 0;
                }
            }
            else // SplitEqually or default
            {
                var netTotal = splitAmounts.Values.Sum();
                if (netTotal > 0)
                {
                    foreach (var kvp in splitAmounts)
                    {
                        var proportion = kvp.Value / netTotal;
                        tipAmounts[kvp.Key] = Math.Round(totalTip * proportion, 2);
                    }
                    
                    // Adjust for rounding
                    var actualTip = tipAmounts.Values.Sum();
                    var difference = totalTip - actualTip;
                    if (Math.Abs(difference) > 0.01m && tipAmounts.Count > 0)
                    {
                        var firstMethod = tipAmounts.Keys.First();
                        tipAmounts[firstMethod] += difference;
                    }
                }
            }
            
            return tipAmounts;
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
        private string _assignedPaymentMethod = ""; // For split by items
        private bool _isIncluded = true; // Whether this item is included in payment

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
        
        public string AssignedPaymentMethod
        {
            get => _assignedPaymentMethod;
            set 
            { 
                _assignedPaymentMethod = value; 
                OnPropertyChanged(); 
            }
        }
        
        public bool IsIncluded
        {
            get => _isIncluded;
            set 
            { 
                _isIncluded = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExcluded));
                // If excluded, clear payment method assignment
                if (!value)
                {
                    AssignedPaymentMethod = "";
                }
            }
        }
        
        public bool IsExcluded => !_isIncluded;

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