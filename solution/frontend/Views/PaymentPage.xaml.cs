using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Refresh bills list when returning to this page (e.g., after payment processing)
        _ = RefreshBillsAsync();
    }

    private async Task RefreshBillsAsync()
    {
        await ViewModel.LoadUnsettledBillsAsync();
        PopulateFilterOptions();
    }

    private async void PaymentPage_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshBillsAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadUnsettledBillsAsync();
        PopulateFilterOptions();
    }

    private async void ProcessPayment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugLogger.LogMethodEntry("PaymentPage.ProcessPayment_Click");
            
            // Validate caja session
            if (App.CajaService != null)
            {
                var activeSession = await App.CajaService.GetActiveSessionAsync();
                if (activeSession == null)
                {
                    var errorDialog = new ContentDialog()
                    {
                        Title = "Caja Cerrada",
                        Content = "Debe abrir la caja antes de procesar pagos. ¿Desea abrir la caja ahora?",
                        PrimaryButtonText = "Abrir Caja",
                        CloseButtonText = "Cancelar",
                        XamlRoot = this.XamlRoot
                    };
                    
                    var result = await errorDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        // Navigate to CajaPage
                        Frame.Navigate(typeof(CajaPage));
                    }
                    return;
                }
            }
            
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

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exportDialog = new ContentDialog
            {
                Title = "Export Unsettled Bills",
                Content = "Choose export format:",
                PrimaryButtonText = "CSV",
                SecondaryButtonText = "Excel",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await exportDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ExportToCsvAsync();
                ShowSuccessMessage("Bills exported to CSV successfully!");
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await ViewModel.ExportToExcelAsync();
                ShowSuccessMessage("Bills exported to Excel successfully!");
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Export failed: {ex.Message}");
        }
    }

    private void ViewAllPayments_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(AllPaymentsPage));
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            ViewModel.SearchTerm = textBox.Text;
        }
    }

    private void TableFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.TableFilter = item.Tag?.ToString() ?? "";
        }
    }

    private void ServerFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.ServerFilter = item.Tag?.ToString() ?? "";
        }
    }

    private void AmountRangeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.AmountRangeFilter = item.Tag?.ToString() ?? "";
        }
    }

    private async void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BillResult bill)
        {
            try
            {
                var detailsDialog = new BillDetailsDialog(bill)
                {
                    XamlRoot = this.XamlRoot
                };
                await detailsDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to load bill details: {ex.Message}");
            }
        }
    }

    private async void PrintBill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BillResult bill)
        {
            try
            {
                await ViewModel.PrintBillAsync(bill);
                ShowSuccessMessage($"Bill {bill.BillId} sent to printer successfully!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to print bill: {ex.Message}");
            }
        }
    }


    private void PopulateFilterOptions()
    {
        // Populate table filter
        var tableItems = TableFilter.Items.Cast<ComboBoxItem>().ToList();
        tableItems.Clear();
        tableItems.Add(new ComboBoxItem { Content = "All Tables", Tag = "" });
        
        var uniqueTables = ViewModel.UnsettledBills.Select(b => b.TableLabel).Distinct().OrderBy(t => t);
        foreach (var table in uniqueTables)
        {
            tableItems.Add(new ComboBoxItem { Content = table, Tag = table });
        }

        // Populate server filter
        var serverItems = ServerFilter.Items.Cast<ComboBoxItem>().ToList();
        serverItems.Clear();
        serverItems.Add(new ComboBoxItem { Content = "All Servers", Tag = "" });
        
        var uniqueServers = ViewModel.UnsettledBills.Select(b => b.ServerName).Distinct().OrderBy(s => s);
        foreach (var server in uniqueServers)
        {
            serverItems.Add(new ComboBoxItem { Content = server, Tag = server });
        }
    }

    private async void ShowSuccessMessage(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Success",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void ShowErrorMessage(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}