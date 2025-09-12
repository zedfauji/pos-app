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
        private decimal _discountAmount;
        private string? _discountReason;
        private PaymentApiService? _paymentService;
        private bool _isProcessing = false;
        private ObservableCollection<OrderItemViewModel> _orderItems = new();
        
        // Split payment properties
        private decimal _cashAmount = 0;
        private decimal _cardAmount = 0;
        private decimal _digitalAmount = 0;
        
        // Customer amount and change properties
        private decimal _customerAmount = 0;
        private decimal _changeAmount = 0;
        private decimal _tipPercentage = 0;

        // Events for parent page communication
        public event EventHandler<PaymentCompletedEventArgs>? PaymentCompleted;
        public event EventHandler<PaymentCancelledEventArgs>? PaymentCancelled;

        public EphemeralPaymentPage()
        {
            this.InitializeComponent();
            _paymentService = App.Payments;
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

        private void LoadOrderItems()
        {
            if (_bill?.Items != null)
            {
                _orderItems.Clear();
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
            if (_paymentService == null || _bill == null)
            {
                throw new InvalidOperationException("Payment service or bill information not available");
            }

            try
            {
                // Try to get the active session for this table to get the correct billing ID
                var tableRepository = new TableRepository();
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Looking for active session for table: '{_bill.TableLabel}'");
                var (activeSessionId, activeBillingId) = await tableRepository.GetActiveSessionForTableAsync(_bill.TableLabel ?? "");
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: GetActiveSessionForTableAsync result - SessionId='{activeSessionId}', BillingId='{activeBillingId}'");
                
                // Also get the complete bill information as fallback
                var completeBill = await tableRepository.GetBillByIdAsync(_bill.BillId);
                
                // Since there are no active sessions, use the BillingId from the complete bill
                // The payment API now expects UUID format for both SessionId and BillingId
                var billingId = activeBillingId ?? completeBill?.BillingId ?? _bill.BillingId ?? _bill.BillId;
                var sessionId = activeSessionId ?? 
                    completeBill?.SessionId ?? 
                    _bill.SessionId ?? 
                    Guid.Empty;

                // Debug logging
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Original Bill - BillId={_bill.BillId}, BillingId={_bill.BillingId}, SessionId={_bill.SessionId}");
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Complete Bill - BillId={completeBill?.BillId}, BillingId={completeBill?.BillingId}, SessionId={completeBill?.SessionId}");
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Active Session - SessionId={activeSessionId}, BillingId={activeBillingId}");
                System.Diagnostics.Debug.WriteLine($"EphemeralPaymentPage: Final - BillingId={billingId}, SessionId={sessionId}");

                var paymentLines = new List<PaymentApiService.RegisterPaymentLineDto>();

                if (_selectedPaymentMethod == "Split")
                {
                    // Create multiple payment lines for split payment
                    if (_cashAmount > 0)
                    {
                        paymentLines.Add(new PaymentApiService.RegisterPaymentLineDto(
                            AmountPaid: _cashAmount,
                            PaymentMethod: "Cash",
                            DiscountAmount: 0,
                            DiscountReason: null,
                            TipAmount: 0,
                            ExternalRef: null,
                            Meta: null
                        ));
                    }
                    
                    if (_cardAmount > 0)
                    {
                        paymentLines.Add(new PaymentApiService.RegisterPaymentLineDto(
                            AmountPaid: _cardAmount,
                            PaymentMethod: "Card",
                            DiscountAmount: 0,
                            DiscountReason: null,
                            TipAmount: 0,
                            ExternalRef: null,
                            Meta: null
                        ));
                    }
                    
                    if (_digitalAmount > 0)
                    {
                        paymentLines.Add(new PaymentApiService.RegisterPaymentLineDto(
                            AmountPaid: _digitalAmount,
                            PaymentMethod: "Mobile",
                            DiscountAmount: 0,
                            DiscountReason: null,
                            TipAmount: 0,
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to previous page
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
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