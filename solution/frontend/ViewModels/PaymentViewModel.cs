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
        // Use double to align with NumberBox.Value type; convert to decimal internally
        private double _amount; public double Amount { get => _amount; set { _amount = value; OnPropertyChanged(); UpdateComputed(); } }
        private double _tip; public double Tip { get => _tip; set { _tip = value; OnPropertyChanged(); UpdateComputed(); } }

        private decimal _totalDue; public decimal TotalDue { get => _totalDue; set { _totalDue = value; OnPropertyChanged(); UpdateComputed(); } }
        private decimal _paid; public decimal Paid { get => _paid; private set { _paid = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaidText)); OnPropertyChanged(nameof(BalanceText)); } }
        private decimal _balance; public decimal Balance { get => _balance; private set { _balance = value; OnPropertyChanged(); OnPropertyChanged(nameof(BalanceText)); } }

        public string PaidText => $"Paid: {Paid:C}";
        public string BalanceText => Balance < 0 ? $"Change: {Math.Abs(Balance):C}" : $"Balance: {Balance:C}";

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
        public string? DebugInfo { get; private set; }

        public PaymentViewModel(PaymentApiService paymentService)
        {
            _payments = paymentService;
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
            var amt = (decimal)Amount;
            var tip = (decimal)Tip;
            Paid = amt + splitSum + tip;
            Balance = TotalDue - Paid; // Allow negative values for change
            DebugInfo = $"amt={Amount:0.##}, tip={Tip:0.##}, splits={splitSum:0.##}, paid={Paid:0.##}, due={TotalDue:0.##}";
            OnPropertyChanged(nameof(DebugInfo));
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
            if (string.IsNullOrWhiteSpace(BillingId)) { Error = "Missing billing"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            var safeSessionId = string.IsNullOrWhiteSpace(SessionId) ? BillingId! : SessionId!;
            if (DiscountValue <= 0) { Error = "Discount must be > 0"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            decimal amount = SelectedDiscountType == "%" ? Math.Round(TotalDue * (DiscountValue / 100m), 2) : DiscountValue;
            var ledger = await _payments.ApplyDiscountAsync(BillingId!, safeSessionId, amount, DiscountReason);
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
            DebugLogger.LogMethodEntry("PaymentViewModel.ConfirmAsync");
            try
            {
                DebugLogger.LogStep("ConfirmAsync", "Setting Error to null");
                Error = null; 
                OnPropertyChanged(nameof(Error)); 
                OnPropertyChanged(nameof(HasError));
                DebugLogger.LogStep("ConfirmAsync", "Error properties updated");
                
                // Validate required fields
                if (string.IsNullOrWhiteSpace(BillingId))
                {
                    DebugLogger.LogStep("ConfirmAsync", "ERROR: BillingId is null or empty");
                    Error = "Missing billing ID";
                    OnPropertyChanged(nameof(Error)); 
                    OnPropertyChanged(nameof(HasError));
                    DebugLogger.LogMethodExit("PaymentViewModel.ConfirmAsync", "Failed - Missing BillingId");
                    return false;
                }
                
                DebugLogger.LogStep("ConfirmAsync", $"BillingId: {BillingId}");
                
                // Get session ID (fallback to billing ID if session is null)
                var safeSessionId = string.IsNullOrWhiteSpace(SessionId) ? BillingId! : SessionId!;
                DebugLogger.LogStep("ConfirmAsync", $"SessionId: {safeSessionId}");
                
                // Validate payment amount
                if (Paid <= 0)
                {
                    DebugLogger.LogStep("ConfirmAsync", "ERROR: Paid amount is <= 0");
                    Error = "Payment amount must be greater than 0";
                    OnPropertyChanged(nameof(Error)); 
                    OnPropertyChanged(nameof(HasError));
                    DebugLogger.LogMethodExit("PaymentViewModel.ConfirmAsync", "Failed - Invalid payment amount");
                    return false;
                }
                
                DebugLogger.LogStep("ConfirmAsync", $"Paid amount: {Paid}");
                
                // Get user ID (fallback to system if null)
                var userId = Services.SessionService.Current?.UserId ?? "system";
                DebugLogger.LogStep("ConfirmAsync", $"UserId: {userId}");
                
                try
                {
                    // Create payment line
                    var paymentLine = new PaymentApiService.RegisterPaymentLineDto(
                        AmountPaid: Paid,
                        PaymentMethod: SelectedMethod,
                        DiscountAmount: 0, // No discount applied in this flow
                        DiscountReason: null,
                        TipAmount: (decimal)Tip,
                        ExternalRef: null,
                        Meta: null
                    );
                    
                    // Create payment request
                    var paymentRequest = new PaymentApiService.RegisterPaymentRequestDto(
                        SessionId: safeSessionId,
                        BillingId: BillingId!,
                        TotalDue: TotalDue,
                        Lines: new[] { paymentLine },
                        ServerId: userId
                    );
                    
                    DebugLogger.LogStep("ConfirmAsync", $"Payment request: BillingId={BillingId}, SessionId={safeSessionId}, TotalDue={TotalDue}, AmountPaid={Paid}, TipAmount={(decimal)Tip}, PaymentMethod={SelectedMethod}");
                    
                    // Register the payment
                    DebugLogger.LogStep("ConfirmAsync", "Calling _payments.RegisterPaymentAsync");
                    var payment = await _payments.RegisterPaymentAsync(paymentRequest);
                    DebugLogger.LogStep("ConfirmAsync", $"RegisterPaymentAsync completed, ledger: {payment?.BillingId ?? "null"}");
                    
                    if (payment == null)
                    {
                        DebugLogger.LogStep("ConfirmAsync", "ERROR: RegisterPaymentAsync returned null");
                        Error = "Failed to register payment";
                        OnPropertyChanged(nameof(Error)); 
                        OnPropertyChanged(nameof(HasError));
                        DebugLogger.LogMethodExit("PaymentViewModel.ConfirmAsync", "Failed - Payment registration returned null");
                        return false;
                    }
                    
                    DebugLogger.LogStep("ConfirmAsync", $"Payment registered successfully: BillingId={payment.BillingId}, Status={payment.Status}");
                    DebugLogger.LogMethodExit("PaymentViewModel.ConfirmAsync", "Success");
                    return true;
                }
                catch (Exception paymentEx)
                {
                    DebugLogger.LogException("ConfirmAsync.PaymentRegistration", paymentEx);
                    Error = $"Payment registration failed: {paymentEx.Message}";
                    OnPropertyChanged(nameof(Error)); 
                    OnPropertyChanged(nameof(HasError));
                    DebugLogger.LogMethodExit("PaymentViewModel.ConfirmAsync", "Exception during payment registration");
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PaymentViewModel.ConfirmAsync", ex);
                Error = $"Payment failed: {ex.Message}";
                OnPropertyChanged(nameof(Error)); 
                OnPropertyChanged(nameof(HasError)); 
                DebugLogger.LogMethodExit("PaymentViewModel.ConfirmAsync", "Critical Exception");
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }
}
