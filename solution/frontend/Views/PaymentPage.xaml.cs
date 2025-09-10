using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Views;

public sealed partial class PaymentPage : Page
{
    public UnsettledBillsViewModel ViewModel { get; }

    public PaymentPage()
    {
        this.InitializeComponent();
        ViewModel = new UnsettledBillsViewModel(new TableRepository());
        DataContext = ViewModel;
        Loaded += PaymentPage_Loaded;
    }

    private async void PaymentPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadUnsettledBillsAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadUnsettledBillsAsync();
    }

        private async void ProcessPayment_Click(object sender, RoutedEventArgs e)
        {
            DebugLogger.LogMethodEntry("PaymentPage.ProcessPayment_Click");
            try
            {
                if (sender is Button button && button.Tag is BillResult bill)
                {
                    DebugLogger.LogStep("ProcessPayment_Click", $"Button clicked for Bill {bill.BillId}");
                    
                    // Show debug info dialog immediately
                    var debugDialog = new ContentDialog
                    {
                        Title = "Payment Debug Info",
                        Content = $"Starting payment process for Bill {bill.BillId}\n\nStep 1: Creating payment dialog...",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    DebugLogger.LogStep("ProcessPayment_Click", "Debug dialog created, showing it");
                    await debugDialog.ShowAsync();
                    DebugLogger.LogStep("ProcessPayment_Click", "Debug dialog shown");

                    try
                    {
                        // Step 1: Create payment dialog
                        DebugLogger.LogStep("ProcessPayment_Click", "Creating PaymentDialog");
                        var paymentDialog = new PaymentDialog();
                        DebugLogger.LogStep("ProcessPayment_Click", "PaymentDialog created, setting XamlRoot");
                        paymentDialog.XamlRoot = this.XamlRoot;
                        
                        // Initialize printing for the payment dialog
                        if (paymentDialog.DataContext is PaymentViewModel paymentVm)
                        {
                            // No need to initialize printing here - App.ReceiptService is already initialized globally
                            DebugLogger.LogStep("ProcessPayment_Click", "Using globally initialized ReceiptService");
                        }
                        
                        DebugLogger.LogStep("ProcessPayment_Click", "XamlRoot set, calling SetBillData");
                        paymentDialog.SetBillData(bill);
                        DebugLogger.LogStep("ProcessPayment_Click", "SetBillData completed");

                        // Step 2: Show payment dialog
                        DebugLogger.LogStep("ProcessPayment_Click", "Showing payment dialog");
                        var result = await paymentDialog.ShowAsync();
                        DebugLogger.LogStep("ProcessPayment_Click", $"Payment dialog closed with result: {result}");

                        // The PaymentDialog now processes payment and closes itself
                        // We need to show success/error messages after it closes
                        if (result == ContentDialogResult.Primary)
                        {
                            DebugLogger.LogStep("ProcessPayment_Click", "Payment dialog completed successfully");
                            
                            // Payment dialog completed successfully
                            var successDialog = new ContentDialog
                            {
                                Title = "Payment Complete",
                                Content = $"Payment for Bill {bill.BillId} has been processed!\n\nNow attempting to refresh the list...",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            DebugLogger.LogStep("ProcessPayment_Click", "Success dialog created, showing it");
                            await successDialog.ShowAsync();
                            DebugLogger.LogStep("ProcessPayment_Click", "Success dialog shown");

                            try
                            {
                                // Try to refresh the list
                                DebugLogger.LogStep("ProcessPayment_Click", "Attempting to refresh unsettled bills");
                                await ViewModel.LoadUnsettledBillsAsync();
                                DebugLogger.LogStep("ProcessPayment_Click", "Unsettled bills refreshed successfully");
                                
                                var finalDialog = new ContentDialog
                                {
                                    Title = "Complete",
                                    Content = $"Payment completed successfully!\nBill {bill.BillId} has been processed and the list has been refreshed.",
                                    CloseButtonText = "OK",
                                    XamlRoot = this.XamlRoot
                                };
                                DebugLogger.LogStep("ProcessPayment_Click", "Final dialog created, showing it");
                                await finalDialog.ShowAsync();
                                DebugLogger.LogStep("ProcessPayment_Click", "Final dialog shown");
                            }
                            catch (Exception refreshEx)
                            {
                                DebugLogger.LogException("ProcessPayment_Click.Refresh", refreshEx);
                                
                                // Refresh failed but payment succeeded
                                var refreshErrorDialog = new ContentDialog
                                {
                                    Title = "Payment Success (Refresh Failed)",
                                    Content = $"Payment for Bill {bill.BillId} was processed successfully!\n\nHowever, refreshing the list failed:\n{refreshEx.Message}\n\nYou can manually refresh the page to see the updated list.",
                                    CloseButtonText = "OK",
                                    XamlRoot = this.XamlRoot
                                };
                                DebugLogger.LogStep("ProcessPayment_Click", "Refresh error dialog created, showing it");
                                await refreshErrorDialog.ShowAsync();
                                DebugLogger.LogStep("ProcessPayment_Click", "Refresh error dialog shown");
                            }
                        }
                        else
                        {
                            DebugLogger.LogStep("ProcessPayment_Click", "Payment was cancelled or failed");
                            
                            // Payment was cancelled or failed
                            var cancelledDialog = new ContentDialog
                            {
                                Title = "Payment Cancelled",
                                Content = $"Payment dialog was closed without completing the payment.\nResult: {result}",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            DebugLogger.LogStep("ProcessPayment_Click", "Cancelled dialog created, showing it");
                            await cancelledDialog.ShowAsync();
                            DebugLogger.LogStep("ProcessPayment_Click", "Cancelled dialog shown");
                        }
                        
                        DebugLogger.LogMethodExit("ProcessPayment_Click", "Success");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("ProcessPayment_Click", ex);
                        
                        // Any other error
                        var errorDialog = new ContentDialog
                        {
                            Title = "Payment Error",
                            Content = $"An error occurred while processing payment for Bill {bill.BillId}:\n\n{ex.Message}\n\nThis error has been caught to prevent the application from crashing.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        DebugLogger.LogStep("ProcessPayment_Click", "Error dialog created, showing it");
                        await errorDialog.ShowAsync();
                        DebugLogger.LogStep("ProcessPayment_Click", "Error dialog shown");
                        DebugLogger.LogMethodExit("ProcessPayment_Click", "Exception");
                    }
                }
                else
                {
                    DebugLogger.LogStep("ProcessPayment_Click", "ERROR: Button or Tag is null");
                    DebugLogger.LogMethodExit("ProcessPayment_Click", "Failed - Invalid button state");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ProcessPayment_Click", ex);
                DebugLogger.LogMethodExit("ProcessPayment_Click", "Critical Exception");
            }
        }

    private async void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BillResult bill)
        {
            try
            {
                var detailsDialog = new ContentDialog
                {
                    Title = $"Bill Details - {bill.BillId}",
                    Content = CreateBillDetailsContent(bill),
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await detailsDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to load bill details: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    private StackPanel CreateBillDetailsContent(BillResult bill)
    {
        var panel = new StackPanel();
        
        panel.Children.Add(new TextBlock { Text = $"Bill ID: {bill.BillId}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = $"Table: {bill.TableLabel}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Total Amount: {bill.TotalAmount:C}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Server: {bill.ServerName}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Start Time: {bill.StartTime:g}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"End Time: {bill.EndTime:g}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Duration: {bill.TotalTimeMinutes} minutes", Margin = new Thickness(0, 5, 0, 0) });
        
        return panel;
    }
}