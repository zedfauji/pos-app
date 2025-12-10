using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Models;
using System.Collections.Generic;

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
        private readonly ReceiptService _receiptService;
        private readonly ILogger<PaymentViewModel> _logger;
        private readonly IConfiguration _configuration;
        
        public Guid? BillingId { get; private set; }
        public Guid? SessionId { get; private set; }
        
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

        // CRITICAL FIX: Replace ObservableCollection with List to avoid COM interop issues
        // ObservableCollection uses Windows Runtime COM interop internally causing Marshal.ThrowExceptionForHR errors
        private List<string> _methods = new() { "Cash", "Card", "UPI" };
        public IReadOnlyList<string> Methods => _methods.AsReadOnly();
        
        private List<PaymentLineVm> _splitLines = new();
        public IReadOnlyList<PaymentLineVm> SplitLines => _splitLines.AsReadOnly();
        
        private List<OrderItemLineVm> _items = new();
        public IReadOnlyList<OrderItemLineVm> Items => _items.AsReadOnly();

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
        // CRITICAL FIX: Replace ObservableCollection with List to avoid COM interop issues
        private List<string> _discountTypes = new() { "%", "Fixed" };
        public IReadOnlyList<string> DiscountTypes => _discountTypes.AsReadOnly();
        public string? DiscountReason { get; set; }

        public string? Error { get; private set; }
        public bool HasError => !string.IsNullOrEmpty(Error);
        public bool IsPaymentComplete { get; private set; }
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
        _logger = logger ?? NullLoggerFactory.Create<PaymentViewModel>();
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    /// <summary>
    /// Initialize the ReceiptService with printing panel
    /// </summary>
    public async Task InitializePrinting(Microsoft.UI.Xaml.Controls.Panel printingPanel, Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
    {
        _logger.LogInformation("InitializePrinting: Starting initialization");
        System.Diagnostics.Debug.WriteLine("PaymentViewModel.InitializePrinting: Starting initialization");
        
        try
        {
            await _receiptService.InitializeAsync(printingPanel, dispatcherQueue);
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

        public void Initialize(Guid billingId, Guid sessionId, decimal totalDue, IEnumerable<OrderItemLineVm>? items = null)
        {
            BillingId = billingId;
            SessionId = sessionId;
            TotalDue = totalDue;
            _splitLines.Clear();
            _items.Clear();
            if (items != null)
            {
                _items.AddRange(items);
            }
            // Notify that collections have changed
            OnPropertyChanged(nameof(SplitLines));
            OnPropertyChanged(nameof(Items));
        }

        public void AddSplit()
        {
            _splitLines.Add(new PaymentLineVm());
            OnPropertyChanged(nameof(SplitLines));
            UpdateComputed();
        }

        public void RemoveSplit(PaymentLineVm line)
        {
            _splitLines.Remove(line);
            OnPropertyChanged(nameof(SplitLines));
            UpdateComputed();
        }

        private void UpdateComputed()
        {
            var splitSum = _splitLines.Sum(x => x.AmountPaid);
            var amt = (decimal)Amount;
            var tip = (decimal)Tip;
            Paid = amt + splitSum + tip;
            Balance = TotalDue - Paid; // Allow negative values for change
            DebugInfo = $"amt={Amount:0.##}, tip={Tip:0.##}, splits={splitSum:0.##}, paid={Paid:0.##}, due={TotalDue:0.##}";
        }

        public async Task LoadLedgerAsync(CancellationToken ct = default)
        {
            if (BillingId == null || BillingId == Guid.Empty) return;
            var ledger = await _payments.GetLedgerAsync(BillingId.Value, ct);
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
            if (BillingId == null || BillingId == Guid.Empty) { Error = "Missing billing"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            var safeSessionId = SessionId ?? BillingId.Value;
            if (DiscountValue <= 0) { Error = "Discount must be > 0"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
            decimal amount = SelectedDiscountType == "%" ? Math.Round(TotalDue * (DiscountValue / 100m), 2) : DiscountValue;
            var ledger = await _payments.ApplyDiscountAsync(BillingId.Value, safeSessionId, amount, DiscountReason);
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
                if (BillingId == null || BillingId == Guid.Empty)
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
                var safeSessionId = SessionId ?? BillingId.Value;
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
                        BillingId: BillingId.Value,
                        TotalDue: TotalDue,
                        Lines: new[] { paymentLine },
                        ServerId: userId
                    );
                    
                    DebugLogger.LogStep("ConfirmAsync", $"Payment request: BillingId={BillingId}, SessionId={safeSessionId}, TotalDue={TotalDue}, AmountPaid={Paid}, TipAmount={(decimal)Tip}, PaymentMethod={SelectedMethod}");
                    
                    // Register the payment
                    DebugLogger.LogStep("ConfirmAsync", "Calling _payments.RegisterPaymentAsync");
                    var payment = await _payments.RegisterPaymentAsync(paymentRequest);
                    DebugLogger.LogStep("ConfirmAsync", $"RegisterPaymentAsync completed, ledger: {payment?.BillingId.ToString() ?? "null"}");
                    
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
                    
                    // Set payment complete flag
                    IsPaymentComplete = true;
                    OnPropertyChanged(nameof(IsPaymentComplete));
                    
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
                // For PDFSharp-based system, we don't need a preview UI element
                // The receipt is generated as PDF and can be previewed by opening the file
                
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
                
                Error = "Generating PDF receipt...";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                
                _logger.LogInformation("PrintProFormaReceiptAsync: Calling ReceiptService.GeneratePreBillAsync");
                System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Calling ReceiptService.GeneratePreBillAsync");
                
                // Convert to ReceiptService format
                var receiptItems = receiptData.Items.Select(item => new ReceiptService.ReceiptItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList();
                
                var tableNumber = receiptData.TableNumber ?? "Unknown";
                var filePath = await _receiptService.GeneratePreBillAsync(
                    receiptData.BillId ?? "UNKNOWN",
                    tableNumber,
                    receiptItems
                );
                
                _logger.LogInformation("PrintProFormaReceiptAsync: ReceiptService.GeneratePreBillAsync completed with file: {FilePath}", filePath);
                System.Diagnostics.Debug.WriteLine($"PrintProFormaReceiptAsync: ReceiptService.GeneratePreBillAsync completed with file: {filePath}");
                
                // NEW: Direct print for WinUI 3 Desktop Apps (no dialog conflict)
                Error = "Printing receipt...";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                
                await PrintPdfFileAsync(filePath);
                
                Error = "Pro forma receipt printed successfully!";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                _logger.LogInformation("Pro forma receipt printed successfully: {FilePath}", filePath);
                System.Diagnostics.Debug.WriteLine("PrintProFormaReceiptAsync: Pro forma receipt printed successfully");
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

        /// <summary>
        /// Shows print preview dialog - can only be called when no other ContentDialog is open
        /// </summary>
        public async Task ShowPrintPreviewAndPrintAsync(string pdfFilePath, string receiptTitle)
        {
            try
            {
                _logger.LogInformation("ShowPrintPreviewAndPrintAsync: Starting for file: {FilePath}", pdfFilePath);
                
                if (!File.Exists(pdfFilePath))
                {
                    SetError($"PDF file not found: {pdfFilePath}");
                    return;
                }

                // Create print preview dialog
                var printDialog = new ContentDialog
                {
                    Title = $"Print {receiptTitle}",
                    Content = CreatePrintPreviewContent(pdfFilePath),
                    PrimaryButtonText = "Print",
                    SecondaryButtonText = "Open PDF",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = GetXamlRoot()
                };

                var result = await printDialog.ShowAsync();
                
                switch (result)
                {
                    case ContentDialogResult.Primary:
                        // Print the PDF
                        await PrintPdfFileAsync(pdfFilePath);
                        break;
                        
                    case ContentDialogResult.Secondary:
                        // Open PDF in default application
                        await OpenPdfInDefaultAppAsync(pdfFilePath);
                        break;
                        
                    case ContentDialogResult.None:
                    default:
                        // User cancelled
                        _logger.LogInformation("Print cancelled by user");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShowPrintPreviewAndPrintAsync failed");
                throw;
            }
        }

        /// <summary>
        /// Creates print preview content for the dialog
        /// </summary>
        private FrameworkElement CreatePrintPreviewContent(string pdfFilePath)
        {
            var stackPanel = new StackPanel { Spacing = 12, Padding = new Thickness(16) };
            
            // Title
            var titleText = new TextBlock
            {
                Text = "Print Preview",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(titleText);
            
            // File info
            var fileInfo = new TextBlock
            {
                Text = $"File: {Path.GetFileName(pdfFilePath)}\nLocation: {pdfFilePath}",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(fileInfo);
            
            // Instructions
            var instructions = new TextBlock
            {
                Text = "Choose an option:\n• Print: Send to default printer\n• Open PDF: View in default PDF viewer\n• Cancel: Close without printing",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            stackPanel.Children.Add(instructions);
            
            return stackPanel;
        }

        /// <summary>
        /// Prints PDF file using system print command with fallback options
        /// </summary>
        private async Task PrintPdfFileAsync(string pdfFilePath)
        {
            try
            {
                _logger.LogInformation("PrintPdfFileAsync: Starting print for file: {FilePath}", pdfFilePath);
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfFilePath,
                    UseShellExecute = true,
                    Verb = "print"
                };
                
                var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation("PrintPdfFileAsync: Print completed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("PrintPdfFileAsync: Print process exited with code: {ExitCode}", process.ExitCode);
                        // Fallback: Open PDF in default application
                        await OpenPdfInDefaultAppAsync(pdfFilePath);
                    }
                }
                else
                {
                    SetError("Failed to start print process");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PrintPdfFileAsync failed, attempting fallback");
                
                // Fallback: Open PDF in default application
                try
                {
                    await OpenPdfInDefaultAppAsync(pdfFilePath);
                    _logger.LogInformation("PrintPdfFileAsync: Fallback to open PDF succeeded");
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "PrintPdfFileAsync: Fallback also failed");
                    SetError($"Failed to print PDF and fallback failed: {ex.Message}");
                    return;
                }
            }
        }

        /// <summary>
        /// Opens PDF in default application
        /// </summary>
        private async Task OpenPdfInDefaultAppAsync(string pdfFilePath)
        {
            try
            {
                _logger.LogInformation("OpenPdfInDefaultAppAsync: Opening PDF: {FilePath}", pdfFilePath);
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfFilePath,
                    UseShellExecute = true
                };
                
                var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    _logger.LogInformation("OpenPdfInDefaultAppAsync: PDF opened successfully");
                }
                else
                {
                    SetError("Failed to start PDF viewer process");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenPdfInDefaultAppAsync failed");
                SetError($"Failed to open PDF: {ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Gets XamlRoot for dialogs
        /// </summary>
        private Microsoft.UI.Xaml.XamlRoot GetXamlRoot()
        {
            try
            {
                // Try to get XamlRoot from main window first
                if (App.MainWindow?.Content is FrameworkElement mainContent)
                {
                    return mainContent.XamlRoot;
                }
                
                // CRITICAL FIX: Remove Window.Current usage to prevent COM exceptions in WinUI 3 Desktop Apps
                // Window.Current is a Windows Runtime COM interop call that causes Marshal.ThrowExceptionForHR errors
                _logger.LogWarning("Could not get XamlRoot from any window");
                SetError("Unable to get XamlRoot for dialog display");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get XamlRoot");
                SetError($"Failed to get XamlRoot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Prints final receipt after payment completion
        /// </summary>
        public async Task PrintFinalReceiptAsync()
        {
            try
            {
                Error = "Generating final receipt...";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                
                _logger.LogInformation("PrintFinalReceiptAsync: Starting for Bill ID: {BillId}", BillingId);
                
                if (_receiptService == null)
                {
                    Error = "ReceiptService not available";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogError("ReceiptService is null");
                    return;
                }
                
                var receiptData = await CreateReceiptDataAsync(false); // Final receipt (not pro forma)
                
                if (receiptData == null)
                {
                    Error = "Failed to create final receipt data";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    _logger.LogError("CreateReceiptDataAsync returned null for final receipt");
                    return;
                }
                
                // Convert to ReceiptService format
                var receiptItems = receiptData.Items.Select(item => new ReceiptService.ReceiptItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    UnitPrice = item.Price
                }).ToList();
                
                var tableNumber = receiptData.TableNumber ?? "Unknown";
                
                // Create proper ReceiptService.ReceiptData with all items and totals
                var receiptServiceData = new ReceiptService.ReceiptData
                {
                    BillId = receiptData.BillId,
                    StoreName = receiptData.BusinessName,
                    Address = receiptData.BusinessAddress,
                    Phone = receiptData.BusinessPhone,
                    TableNumber = tableNumber,
                    ServerName = receiptData.ServerName ?? "Server",
                    Date = DateTime.Now,
                    IsProForma = false, // Final receipt
                    Items = receiptItems,
                    Subtotal = receiptData.Subtotal,
                    DiscountAmount = receiptData.DiscountAmount,
                    TaxAmount = receiptData.TaxAmount,
                    TotalAmount = receiptData.TotalAmount,
                    PaymentMethod = receiptData.PaymentMethod,
                    Footer = "Thank you for your business!"
                };
                
                // Create PaymentData for GenerateFinalReceiptAsync
                var paymentData = new PaymentData
                {
                    BusinessName = receiptData.BusinessName,
                    Address = receiptData.BusinessAddress,
                    Phone = receiptData.BusinessPhone,
                    TableNumber = tableNumber,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = receiptData.PaymentMethod,
                    Items = receiptItems.Select(item => new PaymentItem
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    }).ToList(),
                    Subtotal = receiptData.Subtotal,
                    DiscountAmount = receiptData.DiscountAmount,
                    TaxAmount = receiptData.TaxAmount,
                    TotalAmount = receiptData.TotalAmount,
                    PrinterName = "Default Printer"
                };
                
                var fileName = $"receipt_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = await _receiptService.GenerateCustomFormattedReceiptAsync(receiptServiceData, fileName);
                
                _logger.LogInformation("PrintFinalReceiptAsync: Final receipt generated: {FilePath}", filePath);
                
                // Direct print for WinUI 3 Desktop Apps (no dialog conflict)
                await PrintPdfFileAsync(filePath);
                
                Error = "Final receipt printed successfully!";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                _logger.LogInformation("Final receipt printed successfully: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                Error = $"Failed to print final receipt: {ex.Message}";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                _logger.LogError(ex, "Failed to print final receipt");
                throw;
            }
        }

        public async Task<string?> ExportReceiptAsPdfAsync()
        {
            try
            {
                _logger.LogInformation("Exporting receipt as PDF for Bill ID: {BillId}", BillingId);
                
                var receiptData = await CreateReceiptDataAsync(false); // Final receipt
                if (receiptData == null)
                {
                    _logger.LogError("CreateReceiptDataAsync returned null for final receipt");
                    return null;
                }
                
                // Convert to ReceiptService format with all items and totals
                var receiptItems = receiptData.Items.Select(item => new ReceiptService.ReceiptItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    UnitPrice = item.Price
                }).ToList();
                
                var receiptServiceData = new ReceiptService.ReceiptData
                {
                    BillId = receiptData.BillId,
                    StoreName = receiptData.BusinessName,
                    Address = receiptData.BusinessAddress,
                    Phone = receiptData.BusinessPhone,
                    TableNumber = receiptData.TableNumber ?? "Unknown",
                    ServerName = receiptData.ServerName ?? "Server",
                    Date = DateTime.Now,
                    IsProForma = true, // Pre-bill receipt
                    Items = receiptItems,
                    Subtotal = receiptData.Subtotal,
                    DiscountAmount = receiptData.DiscountAmount,
                    TaxAmount = receiptData.TaxAmount,
                    TotalAmount = receiptData.TotalAmount,
                    PaymentMethod = receiptData.PaymentMethod,
                    Footer = "Thank you for your business!"
                };
                
                var fileName = $"prebill_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = await _receiptService.GenerateCustomFormattedReceiptAsync(receiptServiceData, fileName);
                
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

        private async Task<ReceiptService.ReceiptData?> CreateReceiptDataAsync(bool isProForma)
        {
            try
            {
                // Load business settings
                var businessName = _configuration?["ReceiptSettings:BusinessName"] ?? "MagiDesk Billiard Club";
                var businessAddress = _configuration?["ReceiptSettings:BusinessAddress"] ?? "123 Main Street, City";
                var businessPhone = _configuration?["ReceiptSettings:BusinessPhone"] ?? "(555) 123-4567";

                if (BillingId == null || BillingId == Guid.Empty)
                {
                    _logger.LogError("No billing ID available for receipt generation");
                    Error = "No billing ID available for receipt generation";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    return null;
                }

                _logger.LogInformation("Creating receipt data for billing ID {BillingId}", BillingId);
                
                // Use new BillingService for accurate data retrieval
                var receiptItems = new List<ReceiptService.ReceiptItem>();
                decimal calculatedSubtotal = 0m;
                
                try
                {
                    // Query database directly using the new BillingService
                    var billingService = new Services.BillingService();
                    var dbItems = await billingService.GetOrderItemsByBillingIdAsync(BillingId.Value);
                    
                    if (dbItems != null && dbItems.Any())
                    {
                        _logger.LogInformation("Found {ItemCount} items from billing service", dbItems.Count);
                        
                        foreach (var item in dbItems)
                        {
                            if (item == null) continue;
                            
                            _logger.LogInformation("Adding DB item: {ItemName} x{Quantity} @ ${UnitPrice:F2}", 
                                item.name, item.quantity, item.price);
                            
                            var receiptItem = new ReceiptService.ReceiptItem
                            {
                                Name = item.name ?? "Unknown Item",
                                Quantity = item.quantity,
                                Price = item.price,
                                UnitPrice = item.price,
                                Subtotal = item.price * item.quantity
                            };
                            receiptItems.Add(receiptItem);
                            calculatedSubtotal += receiptItem.Subtotal;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No items found from billing service, falling back to UI items");
                        
                        // Fallback to UI collection if database query fails
                        if (_items != null && _items.Count > 0)
                        {
                            _logger.LogInformation("Using fallback UI collection with {ItemCount} items", _items.Count);
                            
                            foreach (var item in _items)
                            {
                                if (item == null) continue;
                                
                                _logger.LogInformation("Adding UI item: {ItemName} x{Quantity} @ ${UnitPrice:F2}", 
                                    item.Name, item.Quantity, item.UnitPrice);
                                
                                var receiptItem = new ReceiptService.ReceiptItem
                                {
                                    Name = item.Name ?? "Unknown Item",
                                    Quantity = item.Quantity,
                                    Price = item.UnitPrice,
                                    UnitPrice = item.UnitPrice,
                                    Subtotal = item.UnitPrice * item.Quantity
                                };
                                receiptItems.Add(receiptItem);
                                calculatedSubtotal += receiptItem.Subtotal;
                            }
                        }
                        else
                        {
                            _logger.LogError("No items found in either billing service or UI collection");
                        }
                    }
                }
                catch (Exception billingEx)
                {
                    _logger.LogError(billingEx, "Failed to query billing service for order items, falling back to UI items");
                    
                    // Final fallback to UI items if billing service fails
                    if (_items != null && _items.Count > 0)
                    {
                        _logger.LogInformation("Using UI collection fallback with {ItemCount} items", _items.Count);
                        
                        foreach (var item in _items)
                        {
                            if (item == null) continue;
                            
                            _logger.LogInformation("Adding fallback UI item: {ItemName} x{Quantity} @ ${UnitPrice:F2}", 
                                item.Name, item.Quantity, item.UnitPrice);
                            
                            var receiptItem = new ReceiptService.ReceiptItem
                            {
                                Name = item.Name ?? "Unknown Item",
                                Quantity = item.Quantity,
                                Price = item.UnitPrice,
                                UnitPrice = item.UnitPrice,
                                Subtotal = item.UnitPrice * item.Quantity
                            };
                            receiptItems.Add(receiptItem);
                            calculatedSubtotal += receiptItem.Subtotal;
                        }
                    }
                }

                if (receiptItems.Count == 0)
                {
                    _logger.LogError("No items found for receipt generation");
                    Error = "No items found for receipt generation";
                    OnPropertyChanged(nameof(Error));
                    OnPropertyChanged(nameof(HasError));
                    return null;
                }

                _logger.LogInformation("Total receipt items: {ItemCount}, Calculated subtotal: ${Subtotal:F2}", 
                    receiptItems.Count, calculatedSubtotal);

                // Use calculated subtotal or TotalDue if available
                var subtotal = TotalDue > 0 ? TotalDue : calculatedSubtotal;
                var taxAmount = subtotal * 0.08m; // 8% tax rate
                var totalAmount = subtotal + taxAmount - LedgerDiscount;

                var receiptData = new ReceiptService.ReceiptData
                {
                    BusinessName = businessName,
                    BusinessAddress = businessAddress,
                    BusinessPhone = businessPhone,
                    TableNumber = "Table", // This should come from the session data
                    ServerName = "Server", // This should come from the session data
                    StartTime = DateTime.Now.AddHours(-2), // This should come from the session data
                    EndTime = DateTime.Now,
                    Items = receiptItems,
                    Subtotal = subtotal,
                    DiscountAmount = LedgerDiscount,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    BillId = BillingId.Value.ToString(),
                    PaymentMethod = SelectedMethod ?? "Cash",
                    AmountPaid = isProForma ? 0 : Paid,
                    Change = isProForma ? 0 : Math.Max(0, Paid - totalAmount),
                    IsProForma = isProForma
                };
                
                _logger.LogInformation("Receipt data created - Subtotal: ${Subtotal:F2}, Total: ${Total:F2}", 
                    receiptData.Subtotal, receiptData.TotalAmount);
                
                return receiptData;
            }
            catch (Exception ex)
            {
                Error = $"Failed to create receipt data: {ex.Message}";
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(HasError));
                _logger.LogError(ex, "Failed to create receipt data");
                return null;
            }
        }

        /// <summary>
        /// Get the parent window for printing
        /// </summary>
        private Window? GetParentWindow()
        {
            try
            {
                // Try to get the main window first (more reliable)
                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    return mainWindow;
                }
                
                // Try to get the current window as fallback
                // CRITICAL FIX: Remove Window.Current usage to prevent COM exceptions in WinUI 3 Desktop Apps
                // Window.Current is a Windows Runtime COM interop call that causes Marshal.ThrowExceptionForHR errors
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

        private void SetError(string message)
        {
            Error = message;
            OnPropertyChanged(nameof(Error));
            OnPropertyChanged(nameof(HasError));
            _logger.LogError("PaymentViewModel Error: {Message}", message);
        }

        private void ClearError()
        {
            Error = null;
            OnPropertyChanged(nameof(Error));
            OnPropertyChanged(nameof(HasError));
        }
    }
}