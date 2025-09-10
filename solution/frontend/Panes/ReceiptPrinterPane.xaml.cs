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
    /// Non-blocking Receipt Printer Pane for WinUI 3 Desktop Apps
    /// Provides print preview and printing options for receipts
    /// </summary>
    public sealed partial class ReceiptPrinterPane : UserControl
    {
        private readonly ILogger<ReceiptPrinterPane> _logger;
        private PaymentViewModel? _viewModel;
        private bool _isInitialized = false;
        private string? _currentPdfPath;

        public ReceiptPrinterPane()
        {
            this.InitializeComponent();
            _logger = new Services.NullLogger<ReceiptPrinterPane>();
            
            _logger.LogInformation("ReceiptPrinterPane created");
        }

        /// <summary>
        /// Initialize the receipt printer pane with billing data
        /// </summary>
        public async Task InitializeAsync(string billingId, string sessionId, decimal totalDue, object items)
        {
            try
            {
                _logger.LogInformation("Initializing ReceiptPrinterPane for BillingId: {BillingId}, TotalDue: {TotalDue}", 
                    billingId, totalDue);

                if (_isInitialized)
                {
                    _logger.LogWarning("ReceiptPrinterPane already initialized, skipping");
                    return;
                }

                // Create ViewModel with proper dependencies
                _viewModel = new PaymentViewModel(
                    App.Payments ?? throw new InvalidOperationException("PaymentApiService not available"),
                    App.ReceiptService ?? throw new InvalidOperationException("ReceiptService not available"),
                    new Services.NullLogger<PaymentViewModel>(),
                    null // Configuration can be null for now
                );

                // Initialize ViewModel
                _viewModel.Initialize(billingId, sessionId, totalDue, items as IEnumerable<OrderItemLineVm>);
                
                // Update UI with receipt information
                UpdateReceiptInfo(billingId, sessionId, totalDue, items);

                // Generate initial preview
                await RefreshPreviewAsync();

                _isInitialized = true;
                _logger.LogInformation("ReceiptPrinterPane initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ReceiptPrinterPane");
                throw;
            }
        }

        /// <summary>
        /// Update receipt information display
        /// </summary>
        private void UpdateReceiptInfo(string billingId, string sessionId, decimal totalDue, object items)
        {
            try
            {
                BillIdText.Text = billingId;
                TableNumberText.Text = "Table 1"; // This should come from session data
                TotalAmountText.Text = totalDue.ToString("C");
                
                if (items is IEnumerable<OrderItemLineVm> itemList)
                {
                    ItemCountText.Text = itemList.Count().ToString();
                }
                else
                {
                    ItemCountText.Text = "0";
                }

                // Show payment method if available
                if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.SelectedMethod))
                {
                    PaymentMethodLabel.Visibility = Visibility.Visible;
                    PaymentMethodText.Visibility = Visibility.Visible;
                    PaymentMethodText.Text = _viewModel.SelectedMethod;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update receipt info");
            }
        }

        /// <summary>
        /// Close the receipt printer pane
        /// </summary>
        public async Task ClosePaneAsync()
        {
            try
            {
                _logger.LogInformation("Closing ReceiptPrinterPane");
                
                if (App.PaneManager != null)
                {
                    await App.PaneManager.HidePaneAsync("ReceiptPrinterPane");
                }
                
                _logger.LogInformation("ReceiptPrinterPane closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close ReceiptPrinterPane");
                throw;
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
        /// Handle print button click
        /// </summary>
        private async void OnPrint(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Print button clicked");

                if (_viewModel == null)
                {
                    ShowErrorMessage("ViewModel not available");
                    return;
                }

                // Get selected receipt type
                var receiptType = GetSelectedReceiptType();
                
                // Generate PDF first
                string pdfPath;
                if (receiptType == "proforma")
                {
                    await _viewModel.PrintProFormaReceiptAsync();
                    pdfPath = await GenerateProFormaPdfAsync();
                }
                else
                {
                    await _viewModel.PrintFinalReceiptAsync();
                    pdfPath = await GenerateFinalReceiptPdfAsync();
                }

                if (!string.IsNullOrEmpty(pdfPath))
                {
                    _currentPdfPath = pdfPath;
                    ShowSuccessMessage($"Receipt printed successfully!");
                    
                    // Update preview
                    await RefreshPreviewAsync();
                }
                else
                {
                    ShowErrorMessage("Failed to generate receipt PDF");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print receipt");
                ShowErrorMessage($"Print failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle open PDF button click
        /// </summary>
        private async void OnOpenPdf(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Open PDF button clicked");

                if (string.IsNullOrEmpty(_currentPdfPath))
                {
                    ShowErrorMessage("No PDF available to open. Please print a receipt first.");
                    return;
                }

                // Open PDF in default application
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _currentPdfPath,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(startInfo);
                ShowSuccessMessage("PDF opened in default application");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open PDF");
                ShowErrorMessage($"Failed to open PDF: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle refresh preview button click
        /// </summary>
        private async void OnRefreshPreview(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Refresh preview button clicked");
                await RefreshPreviewAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh preview");
                ShowErrorMessage($"Failed to refresh preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh the receipt preview
        /// </summary>
        private async Task RefreshPreviewAsync()
        {
            try
            {
                if (_viewModel == null)
                {
                    PreviewText.Text = "No receipt data available";
                    return;
                }

                // Generate preview text
                var previewText = await GeneratePreviewTextAsync();
                PreviewText.Text = previewText;
                
                _logger.LogInformation("Preview refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh preview");
                PreviewText.Text = $"Error generating preview: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate preview text for the receipt
        /// </summary>
        private async Task<string> GeneratePreviewTextAsync()
        {
            try
            {
                if (_viewModel == null) return "No data available";

                var receiptType = GetSelectedReceiptType();
                var paperFormat = GetSelectedPaperFormat();
                
                var preview = new System.Text.StringBuilder();
                
                // Header
                preview.AppendLine("==========================================");
                preview.AppendLine("           MAGIDESK BILLIARD CLUB");
                preview.AppendLine("==========================================");
                preview.AppendLine();
                
                // Receipt info
                preview.AppendLine($"Bill ID: {BillIdText.Text}");
                preview.AppendLine($"Table: {TableNumberText.Text}");
                preview.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
                preview.AppendLine($"Type: {(receiptType == "proforma" ? "PRO FORMA" : "FINAL RECEIPT")}");
                preview.AppendLine($"Format: {paperFormat}");
                preview.AppendLine();
                
                // Items
                preview.AppendLine("ITEMS:");
                preview.AppendLine("------------------------------------------");
                
                if (_viewModel.Items != null)
                {
                    foreach (var item in _viewModel.Items)
                    {
                        preview.AppendLine($"{item.Name}");
                        preview.AppendLine($"  Qty: {item.Quantity} x {item.UnitPrice:C} = {item.LineTotal:C}");
                    }
                }
                
                preview.AppendLine("------------------------------------------");
                preview.AppendLine($"Subtotal: {_viewModel.TotalDue:C}");
                preview.AppendLine($"Tax: {_viewModel.TotalDue * 0.08m:C}");
                preview.AppendLine($"Total: {_viewModel.TotalDue * 1.08m:C}");
                
                if (receiptType == "final" && !string.IsNullOrEmpty(_viewModel.SelectedMethod))
                {
                    preview.AppendLine();
                    preview.AppendLine($"Payment Method: {_viewModel.SelectedMethod}");
                    preview.AppendLine($"Amount Paid: {_viewModel.Paid:C}");
                    preview.AppendLine($"Change: {Math.Max(0, _viewModel.Paid - _viewModel.TotalDue * 1.08m):C}");
                }
                
                preview.AppendLine();
                preview.AppendLine("==========================================");
                preview.AppendLine("        Thank you for your business!");
                preview.AppendLine("==========================================");
                
                return preview.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate preview text");
                return $"Error generating preview: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate pro forma PDF
        /// </summary>
        private async Task<string?> GenerateProFormaPdfAsync()
        {
            try
            {
                if (_viewModel == null) return null;
                
                // This would call the actual PDF generation
                // For now, return a placeholder path
                var fileName = $"proforma_{BillIdText.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "Receipts", fileName);
                
                _logger.LogInformation("Pro forma PDF would be generated at: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate pro forma PDF");
                return null;
            }
        }

        /// <summary>
        /// Generate final receipt PDF
        /// </summary>
        private async Task<string?> GenerateFinalReceiptPdfAsync()
        {
            try
            {
                if (_viewModel == null) return null;
                
                // This would call the actual PDF generation
                // For now, return a placeholder path
                var fileName = $"final_{BillIdText.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "Receipts", fileName);
                
                _logger.LogInformation("Final receipt PDF would be generated at: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate final receipt PDF");
                return null;
            }
        }

        /// <summary>
        /// Get selected receipt type
        /// </summary>
        private string GetSelectedReceiptType()
        {
            try
            {
                if (ReceiptTypeRadioButtons.SelectedItem is RadioButton selectedButton)
                {
                    return selectedButton.Tag?.ToString() ?? "proforma";
                }
                return "proforma";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get selected receipt type");
                return "proforma";
            }
        }

        /// <summary>
        /// Get selected paper format
        /// </summary>
        private string GetSelectedPaperFormat()
        {
            try
            {
                if (PaperFormatRadioButtons.SelectedItem is RadioButton selectedButton)
                {
                    return selectedButton.Tag?.ToString() ?? "58mm";
                }
                return "58mm";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get selected paper format");
                return "58mm";
            }
        }

        /// <summary>
        /// Show success message
        /// </summary>
        private void ShowSuccessMessage(string message)
        {
            try
            {
                StatusBorder.Visibility = Visibility.Visible;
                ErrorBorder.Visibility = Visibility.Collapsed;
                StatusText.Text = message;
                
                // Auto-hide after 3 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    HideMessages();
                });
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
                StatusBorder.Visibility = Visibility.Collapsed;
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
                StatusBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide messages");
            }
        }
    }
}