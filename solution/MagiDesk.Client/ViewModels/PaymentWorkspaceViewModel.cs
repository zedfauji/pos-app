using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Payments;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Shared.Enums;
using MagiDesk.Client.Services.Dtos; // For Void DTO
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Client.ViewModels;

/// <summary>
/// ViewModel for Payment Workspace - Dedicated payment processing screen.
/// Supports full payment (Phase 2) and split/partial payments (Phase 3).
/// </summary>
public partial class PaymentWorkspaceViewModel : ObservableObject
{
    private readonly ITableApi _tableApi;
    private readonly IPaymentApi _paymentApi;
    private readonly INavigationService _navigationService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty]
    private Guid _sessionId;

    [ObservableProperty]
    private Guid _billingId;

    [ObservableProperty]
    private string _tableLabel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ItemLine> _billItems = new();

    [ObservableProperty]
    private ObservableCollection<PaymentDto> _paymentHistory = new();

    [ObservableProperty]
    private BillPreviewDto? _billPreview;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalTotal), nameof(ChangeDue))]
    private double _totalDue;

    [ObservableProperty]
    private double _subtotal;

    [ObservableProperty]
    private double _tax;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeDue))]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeDue))]
    private double _amountTendered;

    [ObservableProperty]
    private double _tipAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalTotal), nameof(ChangeDue))]
    private double _discountAmount;

    [ObservableProperty]
    private string _customerEmail = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Phase 3: Split Payment Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalTotal), nameof(ChangeDue))]
    private bool _isSplitPayment;

    [ObservableProperty]
    private string _splitMode = "ByAmount"; // ByAmount, ByItem, ByPercentage

    [ObservableProperty]
    private double _splitAmount;

    [ObservableProperty]
    private double _splitPercentage = 50;

    [ObservableProperty]
    private ObservableCollection<ItemLine> _selectedItems = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalTotal), nameof(ChangeDue))]
    private double _calculatedSplitAmount;

    // Computed properties for display (backend will validate)
    public double FinalTotal => Math.Max(0, (IsSplitPayment ? CalculatedSplitAmount : TotalDue) - DiscountAmount);
    public double ChangeDue => SelectedPaymentMethod == PaymentMethod.Cash 
        ? Math.Max(0, AmountTendered - FinalTotal) 
        : 0;

    public PaymentWorkspaceViewModel(
        ITableApi tableApi, 
        IPaymentApi paymentApi, 
        INavigationService navigationService,
        IDispatcherService dispatcherService)
    {
        _tableApi = tableApi;
        _paymentApi = paymentApi;
        _navigationService = navigationService;
        _dispatcherService = dispatcherService;
    }

    public async Task InitializeAsync(Guid sessionId, Guid billingId, string tableLabel)
    {
        SessionId = sessionId;
        BillingId = billingId;
        TableLabel = tableLabel;

        await LoadBillAsync();
    }

    [RelayCommand]
    public async Task LoadBillAsync()
    {
        try
        {
            // Fetch bill preview
            BillPreview = await _tableApi.GetBillPreviewAsync(TableLabel);
            
            // Fetch items
            var items = await _tableApi.GetItemsAsync(TableLabel);
            
            _dispatcherService.InvokeOnUIThread(() =>
            {
                BillItems.Clear();
                foreach (var item in items)
                {
                    BillItems.Add(item);
                }

                if (BillPreview != null)
                {
                    TotalDue = Convert.ToDouble(BillPreview.TotalAmount);
                    Subtotal = Convert.ToDouble(BillPreview.Subtotal);
                    Tax = Convert.ToDouble(BillPreview.TaxAmount);
                    
                    // Pre-fill amount tendered for convenience
                    AmountTendered = TotalDue;
                }

                // Property change notifications now handled by [NotifyPropertyChangedFor] attributes
            });

            // Fetch History
            var payments = await _paymentApi.ListPaymentsAsync(BillingId);
            _dispatcherService.InvokeOnUIThread(() =>
            {
                PaymentHistory.Clear();
                foreach (var p in payments) PaymentHistory.Add(p);
            });

            ErrorMessage = string.Empty;

            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load bill: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task VoidPaymentAsync(PaymentDto payment)
    {
        if (payment == null) return;
        if (payment.AmountPaid <= 0) return; // Can't void a void/reversal

        try
        {
            var confirm = new ContentDialog
            {
                Title = "Confirm Void",
                Content = $"Are you sure you want to void this payment of {payment.AmountPaid:C}?",
                PrimaryButtonText = "Void",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = App.Current.MainWindow.Content.XamlRoot
            };

            var res = await confirm.ShowAsync();
            if (res != ContentDialogResult.Primary) return;

            IsProcessing = true;
            // DTO matching client namespace
            var req = new VoidPaymentRequestDto
            {
                BillingId = BillingId,
                SessionId = SessionId,
                AmountToVoid = payment.AmountPaid,
                Reason = "Operator Void",
                ServerId = "System" // Should get current user
            };

            var result = await _paymentApi.VoidPaymentAsync(req);
            if (result.IsSuccessStatusCode)
            {
                 StatusMessage = "Payment Voided Successfully";
                 await LoadBillAsync(); // Reload to see update
            }
            else
            {
                ErrorMessage = "Failed to void payment.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Void error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    public async Task CalculateSplitAsync()
    {
        try
        {
            var request = new CalculateSplitRequest();

            if (SplitMode == "ByItem")
            {
                request.ItemIds = SelectedItems.Select(i => i.itemId).ToList();
            }
            else if (SplitMode == "ByPercentage")
            {
                request.Percentage = (decimal)SplitPercentage;
            }
            else if (SplitMode == "ByAmount")
            {
                request.FixedAmount = (decimal)SplitAmount;
            }

            var result = await _tableApi.CalculateSplitAsync(TableLabel, request);

            _dispatcherService.InvokeOnUIThread(() =>
            {
                CalculatedSplitAmount = (double)result.AmountToPay;
                // Property change notifications now handled by [NotifyPropertyChangedFor] attributes
            });

            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to calculate split: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ProcessPaymentAsync()
    {
        if (IsProcessing) return;

        IsProcessing = true;
        StatusMessage = "Processing payment...";
        ErrorMessage = string.Empty;

        try
        {
            if (IsSplitPayment)
            {
                // Partial payment via PaymentApi
                await ProcessPartialPaymentAsync();
            }
            else
            {
                // Full payment via StopSession
                await ProcessFullPaymentAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Payment error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ProcessFullPaymentAsync()
    {
        // Build settle bill request for unsettled bills
        var request = new Services.Dtos.SettleBillRequest
        {
            PaymentMethod = SelectedPaymentMethod.ToString().ToLower(),
            AmountTendered = SelectedPaymentMethod == PaymentMethod.Cash ? (decimal)AmountTendered : 0,
            TipAmount = SelectedPaymentMethod == PaymentMethod.Card ? (decimal)TipAmount : 0,
            DiscountAmount = (decimal)DiscountAmount
        };

        // Call backend to settle the bill (not stop session - session already ended)
        var result = await _tableApi.SettleBillAsync(BillingId, request);

        if (result.IsSuccessStatusCode)
        {
            await _dispatcherService.InvokeOnUIThreadAsync(async () =>
            {
                StatusMessage = $"Payment successful! Bill settled.";
                
                // Wait a moment for user to see success
                await Task.Delay(1500);
                
                // Navigate back to Payment Hub
                _navigationService.NavigateTo<PaymentHubViewModel>();
            });
        }
        else
        {
            var errorContent = result.Error?.Content ?? "Payment failed";
            ErrorMessage = $"Payment failed: {errorContent}";
        }
    }

    private async Task ProcessPartialPaymentAsync()
    {
        // Build payment registration request
        var paymentLine = new RegisterPaymentLineDto
        {
            AmountPaid = (decimal)FinalTotal,
            PaymentMethod = SelectedPaymentMethod,
            ExternalRef = null,
            Notes = $"Partial payment - {SplitMode}"
        };

        var request = new RegisterPaymentRequestDto(
            SessionId,
            BillingId,
            (decimal?)TotalDue,
            new[] { paymentLine },
            null, // serverId
            SelectedPaymentMethod == PaymentMethod.Cash ? (decimal?)AmountTendered : null
        );

        // Call PaymentApi to register partial payment
        var result = await _paymentApi.RegisterPaymentAsync(request);

        if (result.IsSuccessStatusCode && result.Content != null)
        {
            var transactionResult = result.Content;
            
            await _dispatcherService.InvokeOnUIThreadAsync(async () =>
            {
                StatusMessage = $"Partial payment successful! {transactionResult.Message}";
                
                // Wait a moment for user to see success
                await Task.Delay(1500);
                
                // Navigate back to Payment Hub
                _navigationService.NavigateTo<PaymentHubViewModel>();
            });
        }
        else
        {
            ErrorMessage = "Partial payment failed. Please try again.";
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        _navigationService.NavigateTo<PaymentHubViewModel>();
    }

    partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
    {
        // Reset relevant fields when payment method changes
        if (value == PaymentMethod.Cash)
        {
            TipAmount = 0;
            AmountTendered = FinalTotal;
        }
        else
        {
            AmountTendered = 0;
        }
        
        // Property change notification now handled by [NotifyPropertyChangedFor] attribute
    }

    partial void OnAmountTenderedChanged(double value)
    {
        // Property change notification now handled by [NotifyPropertyChangedFor] attribute
    }

    partial void OnDiscountAmountChanged(double value)
    {
        // Property change notifications now handled by [NotifyPropertyChangedFor] attributes
        
        // Adjust amount tendered if needed
        if (SelectedPaymentMethod == PaymentMethod.Cash && AmountTendered < FinalTotal)
        {
            AmountTendered = FinalTotal;
        }
    }
}
