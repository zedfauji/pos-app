using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class AllPaymentsPage : Page
{
    public AllPaymentsViewModel ViewModel { get; }

    public AllPaymentsPage()
    {
        this.InitializeComponent();
        ViewModel = new AllPaymentsViewModel(App.Payments ?? throw new InvalidOperationException("PaymentApiService not initialized"));
        DataContext = ViewModel;
        Loaded += AllPaymentsPage_Loaded;
    }

    private async void AllPaymentsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadPaymentsAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadPaymentsAsync();
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exportDialog = new ContentDialog
            {
                Title = "Export Payment History",
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
                ShowSuccessMessage("Payment history exported to CSV successfully!");
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await ViewModel.ExportToExcelAsync();
                ShowSuccessMessage("Payment history exported to Excel successfully!");
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Export failed: {ex.Message}");
        }
    }

    private void BackToPayments_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PaymentPage));
    }

    private void ToggleDebugInfo_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowDebugInfo = !ViewModel.ShowDebugInfo;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            ViewModel.SearchTerm = textBox.Text;
        }
    }

    private void PaymentMethodFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.PaymentMethodFilter = item.Tag?.ToString() ?? "";
        }
    }

    private void DateRangeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.DateRangeFilter = item.Tag?.ToString() ?? "";
        }
    }

    private void AmountRangeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.AmountRangeFilter = item.Tag?.ToString() ?? "";
        }
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearFilters();
        
        // Reset UI controls
        SearchBox.Text = "";
        PaymentMethodFilter.SelectedIndex = 0;
        DateRangeFilter.SelectedIndex = 0;
        AmountRangeFilter.SelectedIndex = 0;
    }

    private async void ViewPaymentDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PaymentApiService.PaymentDto payment)
        {
            try
            {
                var detailsDialog = new ContentDialog
                {
                    Title = $"Payment Details - {payment.PaymentId}",
                    Content = CreatePaymentDetailsContent(payment),
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await detailsDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to load payment details: {ex.Message}");
            }
        }
    }

    private async void ProcessRefund_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PaymentApiService.PaymentDto payment)
        {
            try
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Refund",
                    Content = $"Are you sure you want to refund payment {payment.PaymentId} for {payment.AmountPaid:C}?\n\nThis action cannot be undone.",
                    PrimaryButtonText = "Process Refund",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.ProcessRefundAsync(payment.PaymentId);
                    
                    var successDialog = new ContentDialog
                    {
                        Title = "Refund Processed",
                        Content = $"Refund for payment {payment.PaymentId} has been processed successfully.\n\nRefund amount: {payment.AmountPaid:C}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                    
                    // Refresh the payments list
                    await ViewModel.LoadPaymentsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to process refund: {ex.Message}");
            }
        }
    }

    private async void PrintReceipt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PaymentApiService.PaymentDto payment)
        {
            try
            {
                await ViewModel.PrintReceiptAsync(payment);
                ShowSuccessMessage($"Receipt for payment {payment.PaymentId} sent to printer successfully!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to print receipt: {ex.Message}");
            }
        }
    }

    private StackPanel CreatePaymentDetailsContent(PaymentApiService.PaymentDto payment)
    {
        var panel = new StackPanel();
        
        panel.Children.Add(new TextBlock { Text = $"Payment ID: {payment.PaymentId}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = $"Amount: {payment.AmountPaid:C}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Method: {payment.PaymentMethod}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Session ID: {payment.SessionId}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Created: {payment.CreatedAt:g}", Margin = new Thickness(0, 5, 0, 0) });
        panel.Children.Add(new TextBlock { Text = $"Created By: {payment.CreatedBy}", Margin = new Thickness(0, 5, 0, 0) });
        
        return panel;
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