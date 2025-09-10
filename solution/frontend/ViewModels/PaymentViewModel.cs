using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MagiDesk.Frontend.ViewModels
{
    /// <summary>
    /// Null logger implementation for when DI is not available
    /// </summary>
    public class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

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
        private readonly ReceiptService _receiptService;
        private readonly ILogger<PaymentViewModel> _logger;
        private readonly IConfiguration _configuration;
        
        public string? BillingId { get; private set; }
        public string? SessionId { get; private set; }
        
        // Receipt-related properties
        private UIElement? _receiptPreview;
        public UIElement? ReceiptPreview 
        { 
            get => _receiptPreview; 
            private set 
            { 
                _receiptPreview = value; 
                OnPropertyChanged(); 
            } 
        }

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
        private string? _debugInfo;
        public string? DebugInfo 
        { 
            get => _debugInfo; 
            private set 
            { 
                _debugInfo = value; 
                OnPropertyChanged(); 
            } 
        }

    public PaymentViewModel(PaymentApiService paymentService, ReceiptService receiptService, ILogger<PaymentViewModel>? logger, IConfiguration? configuration)
    {
        _payments = paymentService;
        _receiptService = receiptService;
        _logger = logger ?? new NullLogger<PaymentViewModel>();
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    /// <summary>
    /// Initialize the ReceiptService with printing panel
    /// </summary>
    public void InitializePrinting(Microsoft.UI.Xaml.Controls.Panel printingPanel, Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
    {
        _logger.LogInformation("InitializePrinting: Starting initialization");
        System.Diagnostics.Debug.WriteLine("PaymentViewModel.InitializePrinting: Starting initialization");
        
        try
        {
            _receiptService.Initialize(printingPanel, dispatcherQueue);
            _logger.LogInformation("InitializePrinting: ReceiptService initialized successfully");
            System.Diagnostics.Debug.WriteLine("PaymentViewModel.InitializePrinting: ReceiptService initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InitializePrinting: Failed to initialize ReceiptService");
            System.Diagnostics.Debug.WriteLine($"PaymentViewModel.InitializePrinting: Exception - {ex.Message}");
            throw;
        }
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

        public async Task RefreshReceiptPreviewAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing receipt preview for Bill ID: {BillId}", BillingId);
                
                var receiptData = await CreateReceiptDataAsync(true); // Pro forma
                var preview = await _receiptService.GenerateReceiptPreviewAsync(receiptData);
                ReceiptPreview = preview;
                
                _logger.LogInformation("Receipt preview refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh receipt preview");
                Error = $"Failed to refresh receipt preview: {ex.Message}";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
            }
        }

        public async Task PrintProFormaReceiptAsync()
        {
            try
            {
                // Set error message to show we're starting
                Error = "Starting print process...";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                
                _logger.LogInformation("PrintProFormaReceiptAsync: Starting for Bill ID: {BillId}", BillingId);
                System.Diagnostics.Debug.WriteLine($"PrintProFormaReceiptAsync: Starting for Bill ID: {BillingId}");
                
                // Check if ReceiptService is available
                if (_receiptService == null)
                {
                    Error = "ReceiptService not available";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogError("ReceiptService is null");
                    System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: ReceiptService is null");
                    return;
                }
                
                _logger.LogInformation("PrintProFormaReceiptAsync: ReceiptService is available");
                System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: ReceiptService is available");
                
                Error = "Creating receipt data...";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                
                _logger.LogInformation("PrintProFormaReceiptAsync: Calling CreateReceiptDataAsync");
                System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Calling CreateReceiptDataAsync");
                
                var receiptData = await CreateReceiptDataAsync(true); // Pro forma
                
                _logger.LogInformation("PrintProFormaReceiptAsync: CreateReceiptDataAsync completed, receiptData is {ReceiptDataStatus}", 
                    receiptData == null ? "NULL" : "NOT NULL");
                System.Diagnostics.Debug.WriteLine($"PrintProFormaReceiptAsync: CreateReceiptDataAsync completed, receiptData is {(receiptData == null ? "NULL" : "NOT NULL")}");
                
                if (receiptData == null)
                {
                    Error = "Failed to create receipt data";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogError("CreateReceiptDataAsync returned null");
                    System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: CreateReceiptDataAsync returned null");
                    return;
                }
                
                _logger.LogInformation("PrintProFormaReceiptAsync: Receipt data created successfully");
                System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Receipt data created successfully");
                
                Error = "Calling ReceiptService...";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                
                _logger.LogInformation("PrintProFormaReceiptAsync: Calling ReceiptService.PrintReceiptAsync");
                System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Calling ReceiptService.PrintReceiptAsync");
                
                var parentWindow = GetParentWindow();
                if (parentWindow == null)
                {
                    Error = "Could not find parent window for printing";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogError("Parent window is null");
                    System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Parent window is null");
                    return;
                }
                
                var success = await _receiptService.PrintReceiptAsync(receiptData, parentWindow, showPreview: true);
                
                _logger.LogInformation("PrintProFormaReceiptAsync: ReceiptService.PrintReceiptAsync completed with result: {Success}", success);
                System.Diagnostics.Debug.WriteLine($"PrintProFormaReceiptAsync: ReceiptService.PrintReceiptAsync completed with result: {success}");
                
                if (success)
                {
                    Error = "Pro forma receipt printed successfully!";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogInformation("Pro forma receipt printed successfully");
                    System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Pro forma receipt printed successfully");
                }
                else
                {
                    Error = "Failed to print pro forma receipt";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogWarning("Pro forma receipt printing failed or was cancelled");
                    System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Pro forma receipt printing failed or was cancelled");
                }
            }
            catch (Exception ex)
            {
                Error = $"Failed to print pro forma receipt: {ex.Message}";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                _logger.LogError(ex, "Failed to print pro forma receipt");
                System.Diagnostics.Debug.WriteLine($"PrintProFormaReceiptAsync: Exception - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PrintProFormaReceiptAsync: Stack trace - {ex.StackTrace}");
                throw; // Re-throw to be caught by caller
            }
        }

        public async Task<string?> ExportReceiptAsPdfAsync()
        {
            try
            {
                _logger.LogInformation("Exporting receipt as PDF for Bill ID: {BillId}", BillingId);
                
                var receiptData = await CreateReceiptDataAsync(false); // Final receipt
                var filePath = await _receiptService.ExportReceiptAsPdfAsync(receiptData);
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    _logger.LogInformation("Receipt exported as PDF: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogInformation("PDF export cancelled by user");
                }
                
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export receipt as PDF");
                Error = $"Failed to export receipt as PDF: {ex.Message}";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                return null;
            }
        }

        private async Task<ReceiptService.ReceiptData> CreateReceiptDataAsync(bool isProForma)
        {
            try
            {
                // Add UI feedback to show we're creating receipt data
                Error = "Creating receipt data...";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                
                _logger.LogInformation("Starting CreateReceiptDataAsync for isProForma: {IsProForma}", isProForma);
                
                // Get business settings from configuration
                var businessName = _configuration?["ReceiptSettings:BusinessName"] ?? "MagiDesk Billiard Club";
                var businessAddress = _configuration?["ReceiptSettings:BusinessAddress"] ?? "123 Main Street, City, State 12345";
                var businessPhone = _configuration?["ReceiptSettings:BusinessPhone"] ?? "(555) 123-4567";
                
                _logger.LogInformation("Business settings loaded - Name: {BusinessName}", businessName);

                // Check if Items collection is available
                if (Items == null)
                {
                    Error = "Items collection is null";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogError("Items collection is null");
                    return null;
                }

                // Convert items to receipt items
                _logger.LogInformation("Converting {ItemCount} items to receipt items", Items.Count);
                
                // Debug each item individually
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    _logger.LogInformation("Item {Index}: Name='{Name}', Quantity={Quantity}, UnitPrice={UnitPrice}, LineTotal={LineTotal}", 
                        i, item?.Name ?? "NULL", item?.Quantity ?? -1, item?.UnitPrice ?? -1, item?.LineTotal ?? -1);
                }
                
                var receiptItems = new List<ReceiptService.ReceiptItem>();
                for (int i = 0; i < Items.Count; i++)
                {
                    try
                    {
                        var item = Items[i];
                        _logger.LogInformation("Creating ReceiptItem {Index} from OrderItemLineVm", i);
                        
                        var receiptItem = new ReceiptService.ReceiptItem
                        {
                            Name = item?.Name ?? "Unknown",
                            Quantity = item?.Quantity ?? 0,
                            UnitPrice = item?.UnitPrice ?? 0,
                            Subtotal = item?.LineTotal ?? 0
                        };
                        
                        _logger.LogInformation("ReceiptItem {Index} created: Name='{Name}', Quantity={Quantity}, UnitPrice={UnitPrice}, Subtotal={Subtotal}", 
                            i, receiptItem.Name, receiptItem.Quantity, receiptItem.UnitPrice, receiptItem.Subtotal);
                        
                        receiptItems.Add(receiptItem);
                    }
                    catch (Exception itemEx)
                    {
                        _logger.LogError(itemEx, "Failed to create ReceiptItem {Index}", i);
                        Error = $"Failed to create receipt item {i}: {itemEx.Message}";
                        OnPropertyChanged(nameof(Error));
                        OnPropertyChanged(nameof(HasError));
                        return null;
                    }
                }
                
                _logger.LogInformation("Receipt items created successfully - Count: {Count}", receiptItems.Count);

                // Calculate totals
                var subtotal = receiptItems.Sum(item => item.Subtotal);
                var taxAmount = subtotal * 0.08m; // 8% tax rate
                var totalAmount = subtotal + taxAmount - LedgerDiscount;
                
                _logger.LogInformation("Totals calculated - Subtotal: {Subtotal}, Tax: {Tax}, Total: {Total}", subtotal, taxAmount, totalAmount);

                _logger.LogInformation("Creating ReceiptData object...");
                _logger.LogInformation("ReceiptData properties: BusinessName='{BusinessName}', BillingId='{BillingId}', SelectedMethod='{SelectedMethod}', LedgerDiscount={LedgerDiscount}, Paid={Paid}", 
                    businessName, BillingId ?? "NULL", SelectedMethod ?? "NULL", LedgerDiscount, Paid);

                try
                {
                    var receiptData = new ReceiptService.ReceiptData
                    {
                        BusinessName = businessName,
                        BusinessAddress = businessAddress,
                        BusinessPhone = businessPhone,
                        TableNumber = "Table 1", // This should come from the session data
                        ServerName = "Server", // This should come from the session data
                        StartTime = DateTime.Now.AddHours(-2), // This should come from the session data
                        EndTime = DateTime.Now,
                        Items = receiptItems,
                        Subtotal = subtotal,
                        DiscountAmount = LedgerDiscount,
                        TaxAmount = taxAmount,
                        TotalAmount = totalAmount,
                        BillId = BillingId ?? "Unknown",
                        PaymentMethod = SelectedMethod,
                        AmountPaid = isProForma ? 0 : Paid,
                        Change = isProForma ? 0 : Math.Max(0, Paid - totalAmount),
                        IsProForma = isProForma
                    };
                    
                    _logger.LogInformation("ReceiptData object created successfully");
                    _logger.LogInformation("ReceiptData final values: BillId='{BillId}', ItemsCount={ItemsCount}, TotalAmount={TotalAmount}, IsProForma={IsProForma}", 
                        receiptData.BillId, receiptData.Items?.Count ?? -1, receiptData.TotalAmount, receiptData.IsProForma);
                    
                    return receiptData;
                }
                catch (Exception receiptDataEx)
                {
                    _logger.LogError(receiptDataEx, "Failed to create ReceiptData object");
                    Error = $"Failed to create ReceiptData: {receiptDataEx.Message}";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    return null;
                }
            }
            catch (Exception ex)
            {
                Error = $"Failed to create receipt data: {ex.Message}";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                _logger.LogError(ex, "Failed to create receipt data");
                return null; // Return null instead of throwing
            }
        }

        /// <summary>
        /// Get the parent window for printing
        /// </summary>
        private Window? GetParentWindow()
        {
            try
            {
                // Try to get the current window
                var currentWindow = Window.Current;
                if (currentWindow != null)
                {
                    return currentWindow;
                }
                
                // If Window.Current is null, try to get the main window
                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    return mainWindow;
                }
                
                _logger.LogWarning("Could not find parent window for printing");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parent window");
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }
}
