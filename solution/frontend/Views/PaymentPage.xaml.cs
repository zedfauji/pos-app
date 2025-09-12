using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;
using MagiDesk.Shared.DTOs.Tables;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Threading;

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
        try
        {
            DebugLogger.LogMethodEntry("PaymentPage.ProcessPayment_Click");
            
            if (sender is Button button && button.Tag is BillResult bill)
            {
                DebugLogger.LogStep("ProcessPayment_Click", $"Button clicked for Bill {bill.BillId}, Amount: {bill.TotalAmount}");
                
                // Navigate to EphemeralPaymentPage instead of opening new window
                Frame.Navigate(typeof(EphemeralPaymentPage), bill);
                
                DebugLogger.LogStep("ProcessPayment_Click", "Navigated to EphemeralPaymentPage");
                DebugLogger.LogMethodExit("ProcessPayment_Click", "Success");
            }
            else
            {
                DebugLogger.LogStep("ProcessPayment_Click", "ERROR: Invalid button or bill data");
                
                // Show error dialog instead of throwing exception
                var errorDialog = new ContentDialog()
                {
                    Title = "Invalid Data",
                    Content = "Invalid button or bill data. Please try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("ProcessPayment_Click", ex);
            
            // Show error dialog
            var errorDialog = new ContentDialog()
            {
                Title = "Error",
                Content = $"Failed to open payment page: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await errorDialog.ShowAsync();
        }
    }

    private async void PaymentPage_PaymentCompleted(object? sender, PaymentCompletedEventArgs e)
    {
        try
        {
            DebugLogger.LogStep("PaymentCompleted", $"Payment completed for Bill {e.BillId}, Amount: {e.AmountPaid:C}");
            
            // Show success notification
            var successDialog = new ContentDialog()
            {
                Title = "Payment Processed",
                Content = $"Payment of {e.AmountPaid:C} has been successfully processed for Bill #{e.BillId}.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await successDialog.ShowAsync();
            
            // Refresh the bills list to remove the paid bill
            await ViewModel.LoadUnsettledBillsAsync();
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("PaymentCompleted", ex);
        }
    }

    private void PaymentPage_PaymentCancelled(object? sender, PaymentCancelledEventArgs e)
    {
        DebugLogger.LogStep("PaymentCancelled", $"Payment cancelled for Bill {e.BillId}");
        // No action needed for cancellation
    }

    private async void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BillResult bill)
        {
            var dialog = new ContentDialog()
            {
                Title = "Bill Details",
                Content = $"Bill ID: {bill.BillId}\nTable: {bill.TableLabel}\nAmount: {bill.TotalAmount:C}\nServer: {bill.ServerName}\nStart Time: {bill.StartTime}",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            
            await dialog.ShowAsync();
        }
    }
}