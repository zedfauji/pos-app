using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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

    private async void PaymentsListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is ListViewItem container && args.Item is PaymentApiService.PaymentDto payment)
        {
            // Find the refund status badge in the container
            var badge = FindVisualChild<Border>(container, "RefundStatusBadge");
            if (badge != null)
            {
                // Check if payment has refunds
                var hasRefunds = ViewModel.HasRefunds(payment.PaymentId);
                var isFullyRefunded = ViewModel.IsFullyRefunded(payment.PaymentId);
                
                badge.Visibility = hasRefunds ? Visibility.Visible : Visibility.Collapsed;
                
                if (hasRefunds)
                {
                    var refundedAmount = ViewModel.GetRefundedAmount(payment.PaymentId);
                    var textBlock = FindVisualChild<TextBlock>(badge, null);
                    if (textBlock != null)
                    {
                        if (isFullyRefunded)
                            textBlock.Text = "Fully Refunded";
                        else
                            textBlock.Text = $"Refunded: {refundedAmount:C}";
                    }
                }
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string? name) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
            {
                if (name == null || (child as FrameworkElement)?.Name == name)
                    return t;
            }
            
            var childOfChild = FindVisualChild<T>(child, name);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
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
                // Get existing refunds to calculate remaining refundable amount
                var existingRefunds = await ViewModel.GetRefundsForPaymentAsync(payment.PaymentId);
                var totalRefunded = existingRefunds.Sum(r => r.RefundAmount);
                var remainingAmount = payment.AmountPaid - totalRefunded;

                if (remainingAmount <= 0)
                {
                    ShowErrorMessage($"This payment has already been fully refunded. Total refunded: {totalRefunded:C}");
                    return;
                }

                // Create refund dialog with input fields
                var refundDialog = new ContentDialog
                {
                    Title = "Process Refund",
                    PrimaryButtonText = "Process Refund",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                // Create dialog content
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.MinWidth = 400;
                grid.Padding = new Thickness(16);

                // Payment info
                var paymentInfo = new TextBlock
                {
                    Text = $"Payment ID: {payment.PaymentId}\nOriginal Amount: {payment.AmountPaid:C}\nAlready Refunded: {totalRefunded:C}\nRemaining: {remainingAmount:C}",
                    Margin = new Thickness(0, 0, 0, 16),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(paymentInfo, 0);
                grid.Children.Add(paymentInfo);

                // Refund amount label
                var amountLabel = new TextBlock
                {
                    Text = "Refund Amount",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid.SetRow(amountLabel, 1);
                grid.Children.Add(amountLabel);

                // Refund amount input
                var amountBox = new NumberBox
                {
                    Value = (double)remainingAmount,
                    Minimum = 0.01,
                    Maximum = (double)remainingAmount,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                Grid.SetRow(amountBox, 2);
                grid.Children.Add(amountBox);

                // Refund method label
                var methodLabel = new TextBlock
                {
                    Text = "Refund Method",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid.SetRow(methodLabel, 3);
                grid.Children.Add(methodLabel);

                // Refund method combo
                var methodCombo = new ComboBox
                {
                    SelectedIndex = 0,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                methodCombo.Items.Add(new ComboBoxItem { Content = "Original Payment Method", Tag = "original" });
                methodCombo.Items.Add(new ComboBoxItem { Content = "Cash", Tag = "cash" });
                methodCombo.Items.Add(new ComboBoxItem { Content = "Card", Tag = "card" });
                methodCombo.Items.Add(new ComboBoxItem { Content = "Wallet", Tag = "wallet" });
                Grid.SetRow(methodCombo, 4);
                grid.Children.Add(methodCombo);

                // Refund reason label
                var reasonLabel = new TextBlock
                {
                    Text = "Refund Reason *",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid.SetRow(reasonLabel, 5);
                grid.Children.Add(reasonLabel);

                // Refund reason input (required)
                var reasonBox = new TextBox
                {
                    PlaceholderText = "Enter reason for refund (required)...",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 80
                };
                Grid.SetRow(reasonBox, 6);
                grid.Children.Add(reasonBox);

                refundDialog.Content = grid;

                // Handle primary button click
                refundDialog.PrimaryButtonClick += async (s, args) =>
                {
                    var deferral = args.GetDeferral();
                    try
                    {
                        var refundAmount = (decimal)amountBox.Value;
                        var refundMethod = (methodCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "original";
                        var refundReason = reasonBox.Text.Trim();

                        // Validate refund amount
                        if (refundAmount <= 0 || refundAmount > remainingAmount)
                        {
                            args.Cancel = true;
                            ShowErrorMessage($"Invalid refund amount. Must be between $0.01 and {remainingAmount:C}");
                            return;
                        }

                        // Validate refund reason is required
                        if (string.IsNullOrWhiteSpace(refundReason))
                        {
                            args.Cancel = true;
                            ShowErrorMessage("Refund reason is required. Please provide a reason for the refund.");
                            return;
                        }

                        // Process refund (reason is already validated as non-empty)
                        var refund = await ViewModel.ProcessRefundAsync(payment.PaymentId, refundAmount, refundReason, refundMethod);

                        var successDialog = new ContentDialog
                        {
                            Title = "Refund Processed",
                            Content = $"Refund for payment {payment.PaymentId} has been processed successfully.\n\nRefund ID: {refund.RefundId}\nRefund Amount: {refund.RefundAmount:C}\nRefund Method: {refund.RefundMethod}",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await successDialog.ShowAsync();

                        // Refresh the payments list
                        await ViewModel.LoadPaymentsAsync();
                    }
                    catch (Exception ex)
                    {
                        args.Cancel = true;
                        ShowErrorMessage($"Failed to process refund: {ex.Message}");
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                };

                await refundDialog.ShowAsync();
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