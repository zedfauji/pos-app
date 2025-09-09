using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class PaymentLineVm : INotifyPropertyChanged
    {
        public string PaymentMethod { get; set; } = "Cash";
        private decimal _amountPaid; public decimal AmountPaid { get => _amountPaid; set { _amountPaid = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }

    public sealed class PaymentViewModel : INotifyPropertyChanged
    {
        private readonly PaymentApiService _payments;
        public string? BillingId { get; private set; }
        public string? SessionId { get; private set; }

        public ObservableCollection<string> Methods { get; } = new() { "Cash", "Card", "UPI" };
        public ObservableCollection<PaymentLineVm> SplitLines { get; } = new();
        public ObservableCollection<OrderItemLineVm> Items { get; } = new();

        private string _selectedMethod = "Cash"; public string SelectedMethod { get => _selectedMethod; set { _selectedMethod = value; OnPropertyChanged(); } }
        private decimal _amount; public decimal Amount { get => _amount; set { _amount = value; OnPropertyChanged(); UpdateComputed(); } }
        private decimal _tip; public decimal Tip { get => _tip; set { _tip = value; OnPropertyChanged(); UpdateComputed(); } }

        private decimal _totalDue; public decimal TotalDue { get => _totalDue; set { _totalDue = value; OnPropertyChanged(); UpdateComputed(); } }
        private decimal _paid; public decimal Paid { get => _paid; private set { _paid = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaidText)); OnPropertyChanged(nameof(BalanceText)); } }
        private decimal _balance; public decimal Balance { get => _balance; private set { _balance = value; OnPropertyChanged(); OnPropertyChanged(nameof(BalanceText)); } }

        public string PaidText => $"Paid: {Paid:C}";
        public string BalanceText => $"Balance: {Balance:C}";

        private decimal _ledgerPaid; public decimal LedgerPaid { get => _ledgerPaid; private set { _ledgerPaid = value; OnPropertyChanged(); OnPropertyChanged(nameof(LedgerPaidText)); } }
        private decimal _ledgerTip; public decimal LedgerTip { get => _ledgerTip; private set { _ledgerTip = value; OnPropertyChanged(); OnPropertyChanged(nameof(LedgerTipText)); } }
        private decimal _ledgerDiscount; public decimal LedgerDiscount { get => _ledgerDiscount; private set { _ledgerDiscount = value; OnPropertyChanged(); OnPropertyChanged(nameof(LedgerDiscountText)); } }
        public string LedgerPaidText => $"Already Paid: {LedgerPaid:C}";
        public string LedgerTipText => $"Tip: {LedgerTip:C}";
        public string LedgerDiscountText => $"Discounts: -{LedgerDiscount:C}";

        private string? _discountType; public string? SelectedDiscountType { get => _discountType; set { _discountType = value; OnPropertyChanged(); } }
        private decimal _discountValue; public decimal DiscountValue { get => _discountValue; set { _discountValue = value; OnPropertyChanged(); } }
        public ObservableCollection<string> DiscountTypes { get; } = new() { "%", "Fixed" };
        public string? DiscountReason { get; set; }

        public string? Error { get; private set; }
        public bool HasError => !string.IsNullOrEmpty(Error);

        public PaymentViewModel()
        {
            _payments = App.Payments ?? throw new InvalidOperationException("Payments API not initialized");
        }

        public void Initialize(string billingId, string sessionId, decimal totalDue, IEnumerable<OrderItemLineVm>? items = null)
        {
            BillingId = billingId;
            SessionId = sessionId;
            TotalDue = totalDue;
            SplitLines.Clear();
            Items.Clear();
            if (items != null)
            {
                foreach (var it in items) Items.Add(it);
            }
        }

        public void AddSplit()
        {
            SplitLines.Add(new PaymentLineVm());
            UpdateComputed();
        }

        public void RemoveSplit(PaymentLineVm line)
        {
            SplitLines.Remove(line);
            UpdateComputed();
        }

        private void UpdateComputed()
        {
            var splitSum = SplitLines.Sum(x => x.AmountPaid);
            Paid = Amount + splitSum + Tip;
            Balance = Math.Max(0, TotalDue - Paid);
        }

        public async Task LoadLedgerAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(BillingId)) return;
            var ledger = await _payments.GetLedgerAsync(BillingId!, ct);
            if (ledger != null)
            {
                TotalDue = ledger.TotalDue;
                LedgerDiscount = ledger.TotalDiscount;
                LedgerPaid = ledger.TotalPaid;
                LedgerTip = ledger.TotalTip;
            }
        }

        public async Task<bool> ApplyDiscountAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(BillingId) || string.IsNullOrWhiteSpace(SessionId)) return false;
            if (DiscountValue <= 0) { Error = "Discount must be > 0"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            decimal amount = SelectedDiscountType == "%" ? Math.Round(TotalDue * (DiscountValue / 100m), 2) : DiscountValue;
            var ledger = await _payments.ApplyDiscountAsync(BillingId!, SessionId!, amount, DiscountReason);
            if (ledger is null) { Error = "Failed to apply discount"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            TotalDue = ledger.TotalDue;
            LedgerDiscount = ledger.TotalDiscount;
            LedgerPaid = ledger.TotalPaid;
            LedgerTip = ledger.TotalTip;
            UpdateComputed();
            return true;
        }

        public async Task<bool> ConfirmAsync(CancellationToken ct = default)
        {
            Error = null; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError));
            if (string.IsNullOrWhiteSpace(BillingId) || string.IsNullOrWhiteSpace(SessionId)) { Error = "Missing billing/session"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            if (Paid <= 0) { Error = "Payment amount must be > 0"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            if (Paid > TotalDue) { Error = "Overpayment not allowed"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }

            var lines = new List<PaymentApiService.RegisterPaymentLineDto>();
            if (Amount > 0) lines.Add(new PaymentApiService.RegisterPaymentLineDto(Amount, SelectedMethod, 0, null, Tip, null, null));
            foreach (var s in SplitLines)
                if (s.AmountPaid > 0)
                    lines.Add(new PaymentApiService.RegisterPaymentLineDto(s.AmountPaid, s.PaymentMethod, 0, null, 0, null, null));

            var req = new PaymentApiService.RegisterPaymentRequestDto(SessionId!, BillingId!, TotalDue, lines, Services.SessionService.Current?.UserId);
            var ledger = await _payments.RegisterPaymentAsync(req, ct);
            if (ledger is null) { Error = "Payment failed"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            // Update ledger fields after payment
            TotalDue = ledger.TotalDue;
            LedgerDiscount = ledger.TotalDiscount;
            LedgerPaid = ledger.TotalPaid;
            LedgerTip = ledger.TotalTip;
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }
}
