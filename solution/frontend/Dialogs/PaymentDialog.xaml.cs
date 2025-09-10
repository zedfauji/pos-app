using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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
                
                // Always use the global ReceiptService - it should never be null
                var receiptService = App.ReceiptService ?? throw new InvalidOperationException("ReceiptService not initialized in App");
                
                var paymentViewModel = new PaymentViewModel(
                    App.Payments ?? throw new InvalidOperationException("PaymentApiService not initialized"),
                    receiptService,
                    App.Services?.GetRequiredService<ILogger<PaymentViewModel>>(),
                    App.Services?.GetRequiredService<IConfiguration>()
                );
                
                this.DataContext = paymentViewModel;
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
                            
                            // NEW: Print final receipt after successful payment
                            try
                            {
                                DebugLogger.LogStep("OnConfirm", "Printing final receipt after payment");
                                await vm.PrintFinalReceiptAsync();
                                DebugLogger.LogStep("OnConfirm", "Final receipt printed successfully");
                            }
                            catch (Exception printEx)
                            {
                                DebugLogger.LogException("OnConfirm.FinalReceiptPrint", printEx);
                                DebugLogger.LogStep("OnConfirm", $"Final receipt printing failed: {printEx.Message}");
                                // Don't cancel the dialog for print failures - payment was successful
                            }
                            
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

        private async void OnRefreshReceiptPreview(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("OnRefreshReceiptPreview");
            try
            {
                if (DataContext is PaymentViewModel vm)
                {
                    DebugLogger.LogStep("OnRefreshReceiptPreview", "ViewModel found, refreshing receipt preview");
                    await vm.RefreshReceiptPreviewAsync();
                    DebugLogger.LogStep("OnRefreshReceiptPreview", "Receipt preview refreshed");
                    DebugLogger.LogMethodExit("OnRefreshReceiptPreview", "Success");
                }
                else
                {
                    DebugLogger.LogStep("OnRefreshReceiptPreview", "ERROR: DataContext is not PaymentViewModel");
                    DebugLogger.LogMethodExit("OnRefreshReceiptPreview", "Failed - No ViewModel");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnRefreshReceiptPreview", ex);
            }
        }

    private async void OnPrintProForma(object sender, ContentDialogButtonClickEventArgs e)
    {
        DebugLogger.LogMethodEntry("OnPrintProForma");
        try
        {
            if (DataContext is PaymentViewModel vm)
            {
                DebugLogger.LogStep("OnPrintProForma", "ViewModel found, printing pro forma receipt");
                await vm.PrintProFormaReceiptAsync();
                DebugLogger.LogStep("OnPrintProForma", "Pro forma receipt printed");
                DebugLogger.LogMethodExit("OnPrintProForma", "Success");
            }
            else
            {
                DebugLogger.LogStep("OnPrintProForma", "ERROR: DataContext is not PaymentViewModel");
                DebugLogger.LogMethodExit("OnPrintProForma", "Failed - No ViewModel");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OnPrintProForma", ex);
        }
    }

    private async void OnPrintProFormaButton(object sender, RoutedEventArgs e)
    {
        DebugLogger.LogMethodEntry("OnPrintProFormaButton");
        try
        {
            if (DataContext is PaymentViewModel vm)
            {
                DebugLogger.LogStep("OnPrintProFormaButton", "ViewModel found, printing pro forma receipt");
                DebugLogger.LogStep("OnPrintProFormaButton", $"ViewModel BillingId: {vm.BillingId}");
                DebugLogger.LogStep("OnPrintProFormaButton", $"ViewModel TotalDue: {vm.TotalDue}");
                try
                {
                    await vm.PrintProFormaReceiptAsync();
                    DebugLogger.LogStep("OnPrintProFormaButton", "Pro forma receipt printed successfully");
                }
                catch (Exception printEx)
                {
                    DebugLogger.LogException("OnPrintProFormaButton.PrintProFormaReceiptAsync", printEx);
                    DebugLogger.LogStep("OnPrintProFormaButton", $"PrintProFormaReceiptAsync failed: {printEx.Message}");
                    // Show error dialog to user
                    var errorDialog = new ContentDialog
                    {
                        Title = "Print Error",
                        Content = $"Failed to print receipt: {printEx.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
                DebugLogger.LogMethodExit("OnPrintProFormaButton", "Success");
            }
            else
            {
                DebugLogger.LogStep("OnPrintProFormaButton", "ERROR: DataContext is not PaymentViewModel");
                DebugLogger.LogMethodExit("OnPrintProFormaButton", "Failed - No ViewModel");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("OnPrintProFormaButton", ex);
            // Show error dialog to user
            var errorDialog = new ContentDialog
            {
                Title = "Print Error",
                Content = $"Failed to print receipt: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

        private async void OnExportReceiptPdf(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("OnExportReceiptPdf");
            try
            {
                if (DataContext is PaymentViewModel vm)
                {
                    DebugLogger.LogStep("OnExportReceiptPdf", "ViewModel found, exporting receipt as PDF");
                    var filePath = await vm.ExportReceiptAsPdfAsync();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        DebugLogger.LogStep("OnExportReceiptPdf", $"Receipt exported to: {filePath}");
                        
                        // Show success message
                        var successDialog = new ContentDialog
                        {
                            Title = "Receipt Exported",
                            Content = $"Receipt has been exported to:\n{filePath}",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await successDialog.ShowAsync();
                    }
                    else
                    {
                        DebugLogger.LogStep("OnExportReceiptPdf", "PDF export cancelled by user");
                    }
                    DebugLogger.LogMethodExit("OnExportReceiptPdf", "Success");
                }
                else
                {
                    DebugLogger.LogStep("OnExportReceiptPdf", "ERROR: DataContext is not PaymentViewModel");
                    DebugLogger.LogMethodExit("OnExportReceiptPdf", "Failed - No ViewModel");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnExportReceiptPdf", ex);
                
                // Show error message
                var errorDialog = new ContentDialog
                {
                    Title = "Export Error",
                    Content = $"Failed to export receipt: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Handles quick tip button clicks (10%, 15%, 20%)
        /// Calculates tip amount based on percentage of bill total
        /// </summary>
        private void OnQuickTip(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("OnQuickTip");
            try
            {
                if (sender is Button button && button.Tag is string percentageStr && 
                    DataContext is PaymentViewModel vm)
                {
                    // Parse percentage from button tag
                    if (double.TryParse(percentageStr, out double percentage))
                    {
                        // Calculate tip amount based on bill total
                        double tipAmount = (double)(vm.TotalDue * (decimal)percentage) / 100.0;
                        
                        // Set the tip amount in the ViewModel
                        vm.Tip = tipAmount;
                        
                        DebugLogger.LogStep("OnQuickTip", $"Set tip to {percentage}% = ${tipAmount:F2}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnQuickTip", ex);
            }
        }

        /// <summary>
        /// Handles custom tip button click
        /// Focuses the tip input field for manual entry
        /// </summary>
        private void OnCustomTip(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("OnCustomTip");
            try
            {
                // Find the tip NumberBox and focus it
                var tipBox = FindName("TipNumberBox") as NumberBox;
                if (tipBox != null)
                {
                    tipBox.Focus(FocusState.Programmatic);
                }
                else
                {
                    // If we can't find the specific NumberBox, just log that custom tip was clicked
                    DebugLogger.LogStep("OnCustomTip", "Custom tip button clicked - user can enter amount manually");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("OnCustomTip", ex);
            }
        }
    }
}
