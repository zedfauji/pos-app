using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Dialogs
{
    public sealed partial class PaymentDialog : ContentDialog
    {
        public PaymentDialog()
        {
            DebugLogger.LogMethodEntry("PaymentDialog.Constructor");
            try
            {
                this.InitializeComponent();
                DebugLogger.LogStep("InitializeComponent", "Completed successfully");
                
                // Use the properly configured PaymentApiService from App instead of creating a new HttpClient
                this.DataContext = new PaymentViewModel(App.Payments ?? throw new InvalidOperationException("PaymentApiService not initialized"));
                DebugLogger.LogStep("DataContext", "PaymentViewModel created and assigned");
                
                this.Loaded += PaymentDialog_Loaded;
                DebugLogger.LogStep("Loaded event", "Event handler attached");
                
                DebugLogger.LogMethodExit("PaymentDialog.Constructor", "Success");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PaymentDialog.Constructor", ex);
                throw;
            }
        }

        public void SetBillData(BillResult bill)
        {
            DebugLogger.LogMethodEntry("SetBillData", $"BillId: {bill.BillId}, TotalAmount: {bill.TotalAmount}");
            try
            {
                if (DataContext is PaymentViewModel vm)
                {
                    DebugLogger.LogStep("SetBillData", "ViewModel found, converting BillResult to OrderItemLineVm");
                    
                    // Convert BillResult to the format expected by PaymentViewModel
                    var items = bill.Items.Select(item => new OrderItemLineVm
                    {
                        Name = item.name,
                        Quantity = item.quantity,
                        UnitPrice = item.price
                    });

                    DebugLogger.LogStep("SetBillData", $"Converted {items.Count()} items");

                    vm.Initialize(
                        billingId: bill.BillingId ?? bill.BillId.ToString(),
                        sessionId: bill.SessionId ?? bill.BillId.ToString(),
                        totalDue: bill.TotalAmount,
                        items: items
                    );
                    
                    DebugLogger.LogStep("SetBillData", "ViewModel initialized successfully");
                    DebugLogger.LogMethodExit("SetBillData", "Success");
                }
                else
                {
                    DebugLogger.LogStep("SetBillData", "ERROR: DataContext is not PaymentViewModel");
                    DebugLogger.LogMethodExit("SetBillData", "Failed - No ViewModel");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SetBillData", ex);
                throw;
            }
        }

        private async void PaymentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("PaymentDialog_Loaded");
            try
            {
                if (DataContext is PaymentViewModel vm)
                {
                    DebugLogger.LogStep("PaymentDialog_Loaded", "ViewModel found, calling LoadLedgerAsync");
                    await vm.LoadLedgerAsync();
                    DebugLogger.LogStep("PaymentDialog_Loaded", "LoadLedgerAsync completed");
                    DebugLogger.LogMethodExit("PaymentDialog_Loaded", "Success");
                }
                else
                {
                    DebugLogger.LogStep("PaymentDialog_Loaded", "ERROR: DataContext is not PaymentViewModel");
                    DebugLogger.LogMethodExit("PaymentDialog_Loaded", "Failed - No ViewModel");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PaymentDialog_Loaded", ex);
                
                // Show error if ledger loading fails
                try
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error Loading Payment Data",
                        Content = $"Failed to load payment ledger: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    DebugLogger.LogStep("PaymentDialog_Loaded", "Error dialog shown");
                }
                catch (Exception dialogEx)
                {
                    DebugLogger.LogException("PaymentDialog_Loaded.ErrorDialog", dialogEx);
                }
            }
        }

        private async void OnConfirm(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            DebugLogger.LogMethodEntry("OnConfirm");
            try
            {
                // Get the ViewModel
                if (DataContext is PaymentViewModel vm)
                {
                    DebugLogger.LogStep("OnConfirm", "ViewModel found, processing payment");
                    
                    try
                    {
                        // Process the payment
                        DebugLogger.LogStep("OnConfirm", "Calling vm.ConfirmAsync");
                        var ok = await vm.ConfirmAsync();
                        DebugLogger.LogStep("OnConfirm", $"vm.ConfirmAsync completed with result: {ok}");
                        
                        if (ok)
                        {
                            DebugLogger.LogStep("OnConfirm", "Payment succeeded, allowing dialog to close");
                            
                            // Payment succeeded - let the dialog close naturally
                            // Don't cancel the dialog, let it return Primary result
                            DebugLogger.LogMethodExit("OnConfirm", "Success");
                        }
                        else
                        {
                            DebugLogger.LogStep("OnConfirm", $"Payment failed: {vm.Error ?? "Unknown error"}");
                            
                            // Payment failed - cancel the dialog so it doesn't close
                            args.Cancel = true;
                            DebugLogger.LogStep("OnConfirm", "Dialog cancelled due to payment failure");
                            DebugLogger.LogMethodExit("OnConfirm", "Payment Failed");
                        }
                    }
                    catch (Exception paymentEx)
                    {
                        DebugLogger.LogException("OnConfirm.PaymentProcessing", paymentEx);
                        
                        // Payment processing failed - cancel the dialog
                        args.Cancel = true;
                        DebugLogger.LogStep("OnConfirm", "Dialog cancelled due to exception");
                        DebugLogger.LogMethodExit("OnConfirm", "Exception");
                    }
                }
                else
                {
                    DebugLogger.LogStep("OnConfirm", "ERROR: DataContext is not PaymentViewModel");
                    
                    // No ViewModel - cancel the dialog
                    args.Cancel = true;
                    DebugLogger.LogMethodExit("OnConfirm", "No ViewModel");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnConfirm", ex);
                
                // Any other error - cancel the dialog
                args.Cancel = true;
                DebugLogger.LogMethodExit("OnConfirm", "Critical Exception");
            }
        }

        private void OnAddSplit(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("OnAddSplit");
            try
            {
                if (DataContext is PaymentViewModel vm)
                {
                    vm.AddSplit();
                    DebugLogger.LogStep("OnAddSplit", "Split added successfully");
                    DebugLogger.LogMethodExit("OnAddSplit", "Success");
                }
                else
                {
                    DebugLogger.LogStep("OnAddSplit", "ERROR: DataContext is not PaymentViewModel");
                    DebugLogger.LogMethodExit("OnAddSplit", "Failed - No ViewModel");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnAddSplit", ex);
            }
        }

        private void OnRemoveSplit(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("OnRemoveSplit");
            try
            {
                if (DataContext is PaymentViewModel vm && (sender as Button)?.DataContext is PaymentLineVm line)
                {
                    vm.RemoveSplit(line);
                    DebugLogger.LogStep("OnRemoveSplit", "Split removed successfully");
                    DebugLogger.LogMethodExit("OnRemoveSplit", "Success");
                }
                else
                {
                    DebugLogger.LogStep("OnRemoveSplit", "ERROR: DataContext is not PaymentViewModel or line is null");
                    DebugLogger.LogMethodExit("OnRemoveSplit", "Failed - Invalid state");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnRemoveSplit", ex);
            }
        }

        private async void OnApplyDiscount(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("OnApplyDiscount");
            try
            {
                if (DataContext is PaymentViewModel vm)
                {
                    DebugLogger.LogStep("OnApplyDiscount", "ViewModel found, calling ApplyDiscountAsync");
                    await vm.ApplyDiscountAsync();
                    DebugLogger.LogStep("OnApplyDiscount", "ApplyDiscountAsync completed");
                    DebugLogger.LogMethodExit("OnApplyDiscount", "Success");
                }
                else
                {
                    DebugLogger.LogStep("OnApplyDiscount", "ERROR: DataContext is not PaymentViewModel");
                    DebugLogger.LogMethodExit("OnApplyDiscount", "Failed - No ViewModel");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnApplyDiscount", ex);
            }
        }
    }
}
