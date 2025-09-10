using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using System.Linq;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Panes
{
    /// <summary>
    /// Non-blocking Payment Pane for WinUI 3 Desktop Apps
    /// Slides in from the right side with smooth animations
    /// </summary>
    public sealed partial class PaymentPane : UserControl
    {
        private readonly ILogger<PaymentPane> _logger;
        private PaymentViewModel? _viewModel;
        private bool _isInitialized = false;

        public PaymentPane()
        {
            this.InitializeComponent();
            _logger = new Services.NullLogger<PaymentPane>();
            
            _logger.LogInformation("PaymentPane created");
        }

        /// <summary>
        /// Initialize the payment pane with billing data
        /// </summary>
        public async Task InitializeAsync(string billingId, string sessionId, decimal totalDue, object items)
        {
            try
            {
                _logger.LogInformation("Initializing PaymentPane for BillingId: {BillingId}, TotalDue: {TotalDue}", 
                    billingId, totalDue);

                if (_isInitialized)
                {
                    _logger.LogWarning("PaymentPane already initialized, skipping");
                    return;
                }

                // Validate required services
                if (App.Payments == null)
                {
                    throw new InvalidOperationException("PaymentApiService not available");
                }
                
                if (App.ReceiptService == null)
                {
                    throw new InvalidOperationException("ReceiptService not available");
                }

                // Create ViewModel with proper dependencies
                _viewModel = new PaymentViewModel(
                    App.Payments,
                    App.ReceiptService,
                    new Services.NullLogger<PaymentViewModel>(),
                    null // Configuration can be null for now
                );

                // Initialize ViewModel
                _viewModel.Initialize(billingId, sessionId, totalDue, items as IEnumerable<OrderItemLineVm>);
                
                // Set DataContext on UI thread to avoid COM interop issues
                DispatcherQueue.TryEnqueue(() =>
                {
                    this.DataContext = _viewModel;
                });

                // Setup event handlers
                SetupEventHandlers();

                _isInitialized = true;
                _logger.LogInformation("PaymentPane initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PaymentPane");
                throw;
            }
        }

        /// <summary>
        /// Setup event handlers for the pane
        /// </summary>
        private void SetupEventHandlers()
        {
            try
            {
                if (_viewModel == null) return;

                // Handle payment completion
                _viewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(PaymentViewModel.IsPaymentComplete) && _viewModel.IsPaymentComplete)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000); // Brief delay to show success
                            await ClosePaneAsync();
                        });
                    }
                };

                _logger.LogDebug("Event handlers setup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup event handlers");
                throw;
            }
        }

        /// <summary>
        /// Close the payment pane
        /// </summary>
        public async Task ClosePaneAsync()
        {
            try
            {
                _logger.LogInformation("Closing PaymentPane");
                
                if (App.PaneManager != null)
                {
                    await App.PaneManager.HidePaneAsync("PaymentPane");
                }
                
                _logger.LogInformation("PaymentPane closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close PaymentPane");
                throw;
            }
        }

        /// <summary>
        /// Handle confirm payment button click
        /// </summary>
        private async void OnConfirmPayment(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Confirm payment button clicked");

                if (_viewModel == null)
                {
                    _logger.LogError("ViewModel is null");
                    return;
                }

                // Disable button to prevent double-clicks
                if (sender is Button button)
                {
                    button.IsEnabled = false;
                }

                // Process payment
                var success = await _viewModel.ConfirmAsync();
                
                if (success)
                {
                    _logger.LogInformation("Payment processed successfully");
                    ShowSuccessMessage();
                    
                    // Auto-close after delay
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        await ClosePaneAsync();
                    });
                }
                else
                {
                    _logger.LogWarning("Payment processing failed: {Error}", _viewModel.Error);
                    ShowErrorMessage(_viewModel.Error ?? "Payment processing failed");
                    
                    // Re-enable button on failure
                    if (sender is Button btn)
                    {
                        btn.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment");
                ShowErrorMessage($"Payment failed: {ex.Message}");
                
                // Re-enable button on exception
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Handle print pro forma button click
        /// </summary>
        private async void OnPrintProForma(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Print pro forma button clicked");

                if (_viewModel == null)
                {
                    _logger.LogError("ViewModel is null");
                    return;
                }

                await _viewModel.PrintProFormaReceiptAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print pro forma receipt");
                ShowErrorMessage($"Failed to print receipt: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle export PDF button click
        /// </summary>
        private async void OnExportPdf(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Export PDF button clicked");

                if (_viewModel == null)
                {
                    _logger.LogError("ViewModel is null");
                    return;
                }

                var filePath = await _viewModel.ExportReceiptAsPdfAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    _logger.LogInformation("PDF exported successfully: {FilePath}", filePath);
                    ShowSuccessMessage($"PDF exported to: {filePath}");
                }
                else
                {
                    ShowErrorMessage("PDF export was cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export PDF");
                ShowErrorMessage($"Failed to export PDF: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle close button click
        /// </summary>
        private async void OnClose(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Close button clicked");
                await ClosePaneAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close pane");
            }
        }

        /// <summary>
        /// Handle exact amount button click
        /// </summary>
        private void OnExactAmount(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    AmountNumberBox.Value = (double)_viewModel.TotalDue;
                    _logger.LogInformation("Set exact amount: {Amount}", _viewModel.TotalDue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set exact amount");
            }
        }

        /// <summary>
        /// Handle tip percentage buttons
        /// </summary>
        private void OnTip10(object sender, RoutedEventArgs e)
        {
            SetTipPercentage(0.10);
        }

        private void OnTip15(object sender, RoutedEventArgs e)
        {
            SetTipPercentage(0.15);
        }

        private void OnTip20(object sender, RoutedEventArgs e)
        {
            SetTipPercentage(0.20);
        }

        private void SetTipPercentage(double percentage)
        {
            try
            {
                if (_viewModel != null)
                {
                    var tipAmount = (double)_viewModel.TotalDue * percentage;
                    TipNumberBox.Value = tipAmount;
                    _logger.LogInformation("Set tip percentage: {Percentage}% = {Amount}", percentage * 100, tipAmount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set tip percentage");
            }
        }

        /// <summary>
        /// Handle payment method changed
        /// </summary>
        private void OnPaymentMethodChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Payment method changed");
                // Payment method change is handled by ViewModel binding
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle payment method change");
            }
        }

        /// <summary>
        /// Show success message
        /// </summary>
        private void ShowSuccessMessage(string message = "Operation completed successfully!")
        {
            try
            {
                SuccessBorder.Visibility = Visibility.Visible;
                ErrorBorder.Visibility = Visibility.Collapsed;
                
                // Update success message if custom message provided
                if (message != "Operation completed successfully!")
                {
                    var successText = SuccessBorder.Child as StackPanel;
                    if (successText?.Children.Count > 1 && successText.Children[1] is TextBlock textBlock)
                    {
                        textBlock.Text = message;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show success message");
            }
        }

        /// <summary>
        /// Show error message
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            try
            {
                ErrorBorder.Visibility = Visibility.Visible;
                SuccessBorder.Visibility = Visibility.Collapsed;
                
                ErrorText.Text = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show error message");
            }
        }

        /// <summary>
        /// Hide all messages
        /// </summary>
        private void HideMessages()
        {
            try
            {
                ErrorBorder.Visibility = Visibility.Collapsed;
                SuccessBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide messages");
            }
        }
    }
}