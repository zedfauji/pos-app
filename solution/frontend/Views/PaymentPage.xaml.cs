using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;
using MagiDesk.Shared.DTOs.Tables;
using System.Linq;

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
                    
                    DebugLogger.LogStep("ProcessPayment_Click", $"Starting payment process for Bill {bill.BillId}");

                    try
                    {
                        // Step 1: Show PaymentPane using PaneManager
                        DebugLogger.LogStep("ProcessPayment_Click", "Showing PaymentPane");
                        
                        if (App.PaneManager != null)
                        {
                            // Check if PaymentPane is already visible
                            if (App.PaneManager.IsPaneVisible("PaymentPane"))
                            {
                                DebugLogger.LogStep("ProcessPayment_Click", "PaymentPane already visible, hiding first");
                                await App.PaneManager.HidePaneAsync("PaymentPane");
                                await Task.Delay(100); // Brief delay for animation
                            }
                            
                            // Show PaymentPane
                            await App.PaneManager.ShowPaneAsync("PaymentPane");
                            DebugLogger.LogStep("ProcessPayment_Click", "PaymentPane shown");
                            
                            // Initialize the pane with billing data
                            var paymentPane = App.PaneManager.GetPane<MagiDesk.Frontend.Panes.PaymentPane>("PaymentPane");
                            
                            if (paymentPane != null)
                            {
                                DebugLogger.LogStep("ProcessPayment_Click", "Initializing PaymentPane with bill data");
                                await paymentPane.InitializeAsync(
                                    bill.BillId.ToString(),
                                    bill.SessionId?.ToString() ?? bill.BillId.ToString(),
                                    bill.TotalAmount,
                                    bill.Items?.Cast<object>().ToList() ?? new List<object>()
                                );
                                DebugLogger.LogStep("ProcessPayment_Click", "PaymentPane initialized successfully");
                            }
                            else
                            {
                                DebugLogger.LogStep("ProcessPayment_Click", "ERROR: PaymentPane not found");
                                throw new InvalidOperationException("PaymentPane not found in PaneManager");
                            }
                        }
                        else
                        {
                            DebugLogger.LogStep("ProcessPayment_Click", "ERROR: PaneManager not available");
                            throw new InvalidOperationException("PaneManager not available");
                        }

                        DebugLogger.LogStep("ProcessPayment_Click", "PaymentPane opened successfully");
                        
                        // PaymentPane is now open and will handle the payment process
                        // The pane will close itself when payment is complete
                        // No need to show a dialog - the pane should be visible
                        DebugLogger.LogStep("ProcessPayment_Click", "PaymentPane opened successfully");
                        
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