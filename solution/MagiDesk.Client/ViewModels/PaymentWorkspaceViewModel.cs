using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Payments;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Shared.Enums;
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
    private readonly ShellViewModel _shellViewModel;

    [ObservableProperty]
    private Guid _sessionId;

    [ObservableProperty]
    private Guid _billingId;

    [ObservableProperty]
    private string _tableLabel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ItemLine> _billItems = new();

    [ObservableProperty]
    private BillPreviewDto? _billPreview;

    [ObservableProperty]
    private double _totalDue;

    [ObservableProperty]
    private double _subtotal;

    [ObservableProperty]
    private double _tax;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private double _amountTendered;

    [ObservableProperty]
    private double _tipAmount;

    [ObservableProperty]
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
    private double _calculatedSplitAmount;

    // Computed properties for display (backend will validate)
    public double FinalTotal => Math.Max(0, (IsSplitPayment ? CalculatedSplitAmount : TotalDue) - DiscountAmount);
    public double ChangeDue => SelectedPaymentMethod == PaymentMethod.Cash 
        ? Math.Max(0, AmountTendered - FinalTotal) 
        : 0;

    public PaymentWorkspaceViewModel(ITableApi tableApi, IPaymentApi paymentApi, ShellViewModel shellViewModel)
    {
        _tableApi = tableApi;
        _paymentApi = paymentApi;
        _shellViewModel = shellViewModel;
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
            
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            app.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                BillItems.Clear();
                foreach (var item in items)
                {
                    BillItems.Add(item);
                }

                if (BillPreview != null)
                {
                    TotalDue = (double)BillPreview.TotalAmount;
                    Subtotal =  (double)BillPreview.Subtotal;
                    Tax = (double)BillPreview.TaxAmount;
                    
                    // Pre-fill amount tendered for convenience
                    AmountTendered = TotalDue;
                }

                OnPropertyChanged(nameof(FinalTotal));
                OnPropertyChanged(nameof(ChangeDue));
            });

            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load bill: {ex.Message}";
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

            var app = (App)Microsoft.UI.Xaml.Application.Current;
            app.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                CalculatedSplitAmount = (double)result.AmountToPay;
                OnPropertyChanged(nameof(FinalTotal));
                OnPropertyChanged(nameof(ChangeDue));
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
        // Build stop session request
        var request = new StopSessionRequest
        {
            PaymentMethod = SelectedPaymentMethod,
            AmountTendered = SelectedPaymentMethod == PaymentMethod.Cash ? (decimal)AmountTendered : 0,
            TipAmount = SelectedPaymentMethod == PaymentMethod.Card ? (decimal)TipAmount : 0,
            DiscountAmount = (decimal)DiscountAmount,
            CustomerEmail = CustomerEmail
        };

        // Call backend to stop session (which processes payment)
        var result = await _tableApi.StopSessionAsync(SessionId, request);

        if (result.IsSuccessStatusCode && result.Content != null)
        {
            var bill = result.Content;
            
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            app.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                StatusMessage = $"Payment successful! Total: {bill.TotalAmount:C}";
                
                // Wait a moment for user to see success
                await Task.Delay(1500);
                
                // Navigate back to Payment Hub
                _shellViewModel.NavigateToPaymentHub();
            });
        }
        else
        {
            ErrorMessage = "Payment failed. Please try again.";
        }
    }

    private async Task ProcessPartialPaymentAsync()
    {
        // Build payment registration request
        var paymentLine = new RegisterPaymentLineDto
        {
            AmountPaid = (decimal)FinalTotal,
            PaymentMethod = SelectedPaymentMethod,
            ReferenceNumber = null,
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
            
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            app.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                StatusMessage = $"Partial payment successful! {transactionResult.Message}";
                
                // Wait a moment for user to see success
                await Task.Delay(1500);
                
                // Navigate back to Payment Hub
                _shellViewModel.NavigateToPaymentHub();
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
        _shellViewModel.NavigateToPaymentHub();
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
        
        OnPropertyChanged(nameof(ChangeDue));
    }

    partial void OnAmountTenderedChanged(double value)
    {
        OnPropertyChanged(nameof(ChangeDue));
    }

    partial void OnDiscountAmountChanged(double value)
    {
        OnPropertyChanged(nameof(FinalTotal));
        OnPropertyChanged(nameof(ChangeDue));
        
        // Adjust amount tendered if needed
        if (SelectedPaymentMethod == PaymentMethod.Cash && AmountTendered < FinalTotal)
        {
            AmountTendered = FinalTotal;
        }
    }
}
