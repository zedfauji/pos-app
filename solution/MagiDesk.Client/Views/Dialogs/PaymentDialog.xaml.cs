using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MagiDesk.Client.Views.Dialogs
{
    public sealed partial class PaymentDialog : ContentDialog, INotifyPropertyChanged
    {
        private double _totalAmount;
        private double _discountAmount;
        private double _amountTendered;
        private double _tipAmount;
        private string _customerEmail = string.Empty;
        private PaymentMethod _selectedMethod = PaymentMethod.Cash;

        public event PropertyChangedEventHandler? PropertyChanged;

        public double TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmountString)); OnPropertyChanged(nameof(FinalTotalString)); OnPropertyChanged(nameof(ChangeDueString)); }
        }

        public double DiscountAmount
        {
            get => _discountAmount;
            set { _discountAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(FinalTotalString)); OnPropertyChanged(nameof(ChangeDueString)); }
        }

        public double AmountTendered
        {
            get => _amountTendered;
            set { _amountTendered = value; OnPropertyChanged(); OnPropertyChanged(nameof(ChangeDueString)); }
        }

        public double TipAmount
        {
            get => _tipAmount;
            set { _tipAmount = value; OnPropertyChanged(); }
        }

        public string CustomerEmail
        {
            get => _customerEmail;
            set { _customerEmail = value; OnPropertyChanged(); }
        }

        public string TotalAmountString => TotalAmount.ToString("C");
        public string FinalTotalString => (TotalAmount - DiscountAmount).ToString("C");

        public string ChangeDueString 
        {
            get 
            {
                var change = AmountTendered - (TotalAmount - DiscountAmount);
                return (change > 0 ? change : 0).ToString("C");
            }
        }

        public PaymentDialog(double total)
        {
            this.InitializeComponent();
            TotalAmount = total;
            AmountTendered = total; // Pre-fill exact amount for convenience
            
            // Set default selection manually to ensure UI is fully loaded before event fires
            // This prevents NRE on CashPanel/CardPanel access during XAML parsing
            CashOption.IsChecked = true;
        }

        private void OnMethodChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                if (CashPanel == null || CardPanel == null) return;

                if (rb.Tag?.ToString() == "Cash")
                {
                    _selectedMethod = PaymentMethod.Cash;
                    CashPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    CardPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    _selectedMethod = PaymentMethod.Card;
                    CashPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    CardPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                }
            }
        }

        private void OnValuesChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // Trigger updates if bindings don't catch it immediately (NumberBox TwoWay can be tricky)
            // But PropertyChanged handlers above should cover it.
        }

        public StopSessionRequest GetRequest()
        {
            return new StopSessionRequest
            {
                PaymentMethod = _selectedMethod,
                AmountTendered = _selectedMethod == PaymentMethod.Cash ? (decimal)AmountTendered : 0,
                TipAmount = _selectedMethod == PaymentMethod.Card ? (decimal)TipAmount : 0,
                DiscountAmount = (decimal)DiscountAmount,
                CustomerEmail = CustomerEmail
            };
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
