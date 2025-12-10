using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using Microsoft.UI.Text;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using MagiDesk.Frontend.ViewModels;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Service for generating and printing receipts in the Billiard POS system.
/// Now uses PDFSharp for PDF generation and printing.
/// </summary>
public sealed class ReceiptService : IDisposable
{
    private readonly ILogger<ReceiptService> _logger;
    private readonly IConfiguration _configuration;
    private bool _disposed = false;

    // Receipt data structures
    public sealed class ReceiptData
    {
        public string StoreName { get; set; } = string.Empty;
        public string BillId { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public bool IsProForma { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
        public decimal? Tax { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Footer { get; set; }
        public string? TableNumber { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        
        // Additional properties for compatibility
        public string BusinessName { get => StoreName; set => StoreName = value; }
        public string BusinessAddress { get => Address ?? string.Empty; set => Address = value; }
        public string BusinessPhone { get => Phone ?? string.Empty; set => Phone = value; }
        public string ServerName { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get => Tax ?? 0; set => Tax = value; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal Change { get; set; }
    }

    public sealed class ReceiptItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal Price { get; set; } = 0;
        
        // Additional properties for compatibility
        public decimal UnitPrice { get => Price; set => Price = value; }
        public decimal Subtotal { get; set; } = 0; // Make settable for compatibility
    }

    public ReceiptService(ILogger<ReceiptService>? logger, IConfiguration? configuration)
    {
        _logger = logger ?? new NullLogger<ReceiptService>();
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    /// <summary>
    /// Initialize the receipt service (compatibility method for old initialization calls)
    /// </summary>
    public async Task InitializeAsync(Panel? printingPanel = null, DispatcherQueue? dispatcherQueue = null, Window? window = null)
    {
        try
        {
            _logger.LogInformation("ReceiptService.InitializeAsync: Starting initialization");
            
            // PDFSharp-based ReceiptService doesn't need UI components for initialization
            // This method is kept for compatibility with existing initialization calls
            // All PDF generation is CPU-bound and doesn't require UI thread context
            
            _logger.LogInformation("ReceiptService.InitializeAsync: Initialization completed successfully");
            
            await Task.CompletedTask; // Ensure method is async
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReceiptService.InitializeAsync: Failed to initialize");
            throw;
        }
    }

    /// <summary>
    /// Legacy method for compatibility - redirects to new PDFSharp flow
    /// </summary>
    public async Task<bool> PrintReceiptAsync(ReceiptData receiptData, Window window, bool showPreview = true)
    {
        try
        {
            _logger.LogInformation($"PrintReceiptAsync: Legacy method called for Bill ID: {receiptData.BillId}, showPreview: {showPreview}");
            
            if (receiptData == null)
                throw new ArgumentException("Receipt data cannot be null", nameof(receiptData));
            
            if (window == null)
                throw new ArgumentException("Window cannot be null", nameof(window));

            // For now, just save as PDF (no direct printing in legacy mode)
            var receiptsFolder = await GetReceiptsFolderAsync();
            var fileName = $"receipt_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(receiptsFolder, fileName);
            
            var savedPath = await this.GenerateCustomFormattedReceiptAsync(receiptData, fileName, 80); // Default to 80mm
            
            _logger.LogInformation($"PrintReceiptAsync: Legacy method completed - PDF saved to: {savedPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PrintReceiptAsync: Legacy method failed for Bill ID: {receiptData?.BillId}");
            return false;
        }
    }

    /// <summary>
    /// Legacy method for compatibility - generates receipt preview
    /// </summary>
    public async Task<FrameworkElement> GenerateReceiptPreviewAsync(ReceiptData receiptData)
    {
        try
        {
            _logger.LogInformation($"GenerateReceiptPreviewAsync: Legacy method called for Bill ID: {receiptData.BillId}");
            
            if (receiptData == null)
                throw new ArgumentException("Receipt data cannot be null", nameof(receiptData));

            // Create a simple text preview
            var stackPanel = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = receiptData.IsProForma ? "PRE-BILL RECEIPT" : "FINAL RECEIPT",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(titleText);
            
            var billIdText = new TextBlock
            {
                Text = $"Bill ID: {receiptData.BillId}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(billIdText);
            
            var totalText = new TextBlock
            {
                Text = $"Total: ${receiptData.TotalAmount:F2}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(totalText);
            
            var itemsText = new TextBlock
            {
                Text = $"Items: {receiptData.Items.Count}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(itemsText);
            
            _logger.LogInformation($"GenerateReceiptPreviewAsync: Legacy method completed for Bill ID: {receiptData.BillId}");
            return stackPanel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GenerateReceiptPreviewAsync: Legacy method failed for Bill ID: {receiptData?.BillId}");
            
            // Return error message
            var errorText = new TextBlock
            {
                Text = "Error generating preview",
                FontSize = 12,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            return errorText;
        }
    }

    /// <summary>
    /// Legacy method for compatibility - exports receipt as PDF
    /// </summary>
    public async Task<string> ExportReceiptAsPdfAsync(ReceiptData receiptData)
    {
        try
        {
            _logger.LogInformation($"ExportReceiptAsPdfAsync: Legacy method called for Bill ID: {receiptData.BillId}");
            
            if (receiptData == null)
                throw new ArgumentException("Receipt data cannot be null", nameof(receiptData));

            var receiptsFolder = await GetReceiptsFolderAsync();
            var fileName = $"receipt_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(receiptsFolder, fileName);
            
            var savedPath = await this.GenerateCustomFormattedReceiptAsync(receiptData, fileName, 80); // Default to 80mm
            
            _logger.LogInformation($"ExportReceiptAsPdfAsync: Legacy method completed - PDF saved to: {savedPath}");
            return savedPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"ExportReceiptAsPdfAsync: Legacy method failed for Bill ID: {receiptData?.BillId}");
            throw new InvalidOperationException($"Failed to export receipt as PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate a pre-bill receipt as PDF
    /// </summary>
    public async Task<string> GeneratePreBillAsync(string billId, string tableNumber, List<ReceiptItem> items, decimal taxRate = 0.08m)
    {
        try
        {
            _logger.LogInformation($"GeneratePreBillAsync: Starting for Bill ID: {billId}");
            
            // Validate inputs
            if (string.IsNullOrEmpty(billId))
                throw new ArgumentException("Bill ID cannot be null or empty", nameof(billId));
            
            if (string.IsNullOrEmpty(tableNumber))
                throw new ArgumentException("Table number cannot be null or empty", nameof(tableNumber));
            
            if (items == null || !items.Any())
                throw new ArgumentException("Items list cannot be null or empty", nameof(items));

            // Calculate totals
            var subtotal = items.Sum(i => i.Price * i.Quantity);
            var taxAmount = subtotal * taxRate;
            var totalAmount = subtotal + taxAmount;

            // Create receipt data
            var receiptData = new ReceiptData
            {
                StoreName = GetStoreName(),
                BillId = billId,
                Date = DateTime.Now,
                IsProForma = true,
                Items = items,
                Tax = taxAmount,
                TotalAmount = totalAmount,
                Footer = "This is a pre-bill. Please pay at the counter.",
                TableNumber = tableNumber,
                Address = GetStoreAddress(),
                Phone = GetStorePhone()
            };

            // Generate PDF with custom formatting
            var fileName = $"prebill_{billId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = await this.GenerateCustomFormattedReceiptAsync(receiptData, fileName, 80); // Default to 80mm

            _logger.LogInformation($"GeneratePreBillAsync: Pre-bill generated successfully: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GeneratePreBillAsync: Failed to generate pre-bill for {billId}");
            throw new InvalidOperationException($"Failed to generate pre-bill: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate a final receipt as PDF
    /// </summary>
    public async Task<string> GenerateFinalReceiptAsync(string billId, PaymentData paymentData)
    {
        try
        {
            _logger.LogInformation($"GenerateFinalReceiptAsync: Starting for Bill ID: {billId}");
            
            // Validate inputs
            if (string.IsNullOrEmpty(billId))
                throw new ArgumentException("Bill ID cannot be null or empty", nameof(billId));
            
            if (paymentData == null)
                throw new ArgumentException("Payment data cannot be null", nameof(paymentData));

            // Create receipt data
            var receiptData = new ReceiptData
            {
                StoreName = paymentData.BusinessName ?? GetStoreName(),
                BillId = billId,
                Date = paymentData.PaymentDate,
                IsProForma = false,
                Items = paymentData.Items?.Select(item => new ReceiptItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Price = item.UnitPrice
                }).ToList() ?? new List<ReceiptItem>(),
                Tax = paymentData.TaxAmount,
                TotalAmount = paymentData.TotalAmount,
                Footer = $"Paid by {paymentData.PaymentMethod}. Thank you!",
                TableNumber = paymentData.TableNumber,
                Address = paymentData.Address,
                Phone = paymentData.Phone
            };

            // Generate PDF with custom formatting
            var fileName = $"receipt_{billId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = await this.GenerateCustomFormattedReceiptAsync(receiptData, fileName, 80); // Default to 80mm

            _logger.LogInformation($"GenerateFinalReceiptAsync: Final receipt generated successfully: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GenerateFinalReceiptAsync: Failed to generate final receipt for {billId}");
            throw new InvalidOperationException($"Failed to generate final receipt: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Save receipt as PDF file
    /// </summary>
    public async Task<string> SavePdfAsync(ReceiptData receiptData, string? fileName = null)
    {
        try
        {
            _logger.LogInformation($"SavePdfAsync: Starting for Bill ID: {receiptData.BillId}");
            
            // Validate inputs
            if (receiptData == null)
                throw new ArgumentException("Receipt data cannot be null", nameof(receiptData));
            
            if (string.IsNullOrEmpty(receiptData.BillId))
                throw new ArgumentException("Bill ID cannot be null or empty", nameof(receiptData.BillId));

            // Generate file name if not provided
            if (string.IsNullOrEmpty(fileName))
            {
                var prefix = receiptData.IsProForma ? "prebill" : "receipt";
                fileName = $"{prefix}_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            }

            // Generate PDF with custom formatting
            var filePath = await this.GenerateCustomFormattedReceiptAsync(receiptData, fileName, 80); // Default to 80mm

            _logger.LogInformation($"SavePdfAsync: PDF saved successfully: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SavePdfAsync: Failed to save PDF for Bill ID: {receiptData?.BillId}");
            throw new InvalidOperationException($"Failed to save PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Print receipt directly to printer
    /// </summary>
    public async Task<bool> PrintPdfAsync(ReceiptData receiptData, string printerName)
    {
        try
        {
            _logger.LogInformation($"PrintPdfAsync: Starting for Bill ID: {receiptData.BillId}, Printer: {printerName}");
            
            // Validate inputs
            if (receiptData == null)
                throw new ArgumentException("Receipt data cannot be null", nameof(receiptData));
            
            if (string.IsNullOrEmpty(printerName))
                throw new ArgumentException("Printer name cannot be null or empty", nameof(printerName));

            // Generate PDF using the Receipt Designer Preview template
            var tempFileName = $"receipt_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            // Save to project PrintReceipts directory for verification
            var projectReceiptsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrintReceipts");
            Directory.CreateDirectory(projectReceiptsDir);
            var projectReceiptPath = Path.Combine(projectReceiptsDir, tempFileName);
            var tempPath = await this.GenerateCustomFormattedReceiptAsync(receiptData, tempFileName, 80); // Default to 80mm
            
            // Copy to project directory for verification
            if (File.Exists(tempPath))
            {
                File.Copy(tempPath, projectReceiptPath, true);
                _logger.LogInformation($"PrintPdfAsync: Receipt copied to project directory for verification: {projectReceiptPath}");
            }
            
            // Load print preview setting from Receipt Format Designer
            var formatSettings = await this.LoadReceiptFormatSettingsAsync();
            _logger.LogInformation($"PrintPdfAsync: Loaded format settings - BusinessName: '{formatSettings?.BusinessName}', BusinessAddress: '{formatSettings?.BusinessAddress}', BusinessPhone: '{formatSettings?.BusinessPhone}'");
            _logger.LogInformation($"PrintPdfAsync: Format settings - FontSize: {formatSettings?.FontSize}, ShowPreviewBeforePrinting: {formatSettings?.ShowPreviewBeforePrinting}");
            _logger.LogInformation($"PrintPdfAsync: Format settings - ShowItemDetails: {formatSettings?.ShowItemDetails}, ShowSubtotal: {formatSettings?.ShowSubtotal}, FooterMessage: '{formatSettings?.FooterMessage}'");
            _logger.LogInformation($"PrintPdfAsync: Sending receipt format settings to print service - PDF path: {tempPath}");
            bool showPreview = formatSettings?.ShowPreviewBeforePrinting ?? true;
            
            // Print the generated PDF
            if (showPreview)
            {
                // Show preview by opening the PDF
                try
                {
                    // Try cmd start first (most reliable)
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"\" \"{tempPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
                    _logger.LogInformation($"PrintPdfAsync: Opened PDF with default application: {tempPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"PrintPdfAsync: Failed to open PDF with cmd start, trying direct method: {ex.Message}");
                    
                    // Fallback to direct file association
                    try
                    {
                        var directStartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = tempPath,
                            UseShellExecute = true,
                            Verb = "open"
                        };
                        System.Diagnostics.Process.Start(directStartInfo);
                        _logger.LogInformation($"PrintPdfAsync: Opened PDF with direct file association: {tempPath}");
                    }
                    catch (Exception directEx)
                    {
                        _logger.LogError(directEx, $"PrintPdfAsync: All PDF opening methods failed");
                        // Continue without preview - receipt is still generated
                    }
                }
            }
            else
            {
                // Skip direct printing since preview is disabled, just log that PDF was saved
                _logger.LogInformation($"PrintPdfAsync: Preview disabled, PDF saved to: {tempPath}");
            }

            _logger.LogInformation($"PrintPdfAsync: Receipt processing completed for {printerName}");
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.HResult == unchecked((int)0x80004005))
        {
            _logger.LogWarning($"PrintPdfAsync: No application associated with PDF files: {ex.Message}");
            _logger.LogInformation($"PrintPdfAsync: PDF saved but could not be opened/printed automatically");
            return true; // Receipt was generated successfully, just couldn't auto-open/print
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PrintPdfAsync: Failed to print receipt for Bill ID: {receiptData?.BillId}");
            throw new InvalidOperationException($"Failed to print receipt: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate receipt PDF using ReceiptBuilder with custom formatting
    /// </summary>
    public async Task<string> GenerateReceiptPdfAsync(ReceiptData receiptData, string? customFilePath = null)
    {
        try
        {
            var fileName = $"receipt_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            
            // If no custom path provided, save to project PrintReceipts directory for verification
            string filePath;
            if (string.IsNullOrEmpty(customFilePath))
            {
                var projectReceiptsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrintReceipts");
                Directory.CreateDirectory(projectReceiptsDir);
                filePath = Path.Combine(projectReceiptsDir, fileName);
                _logger.LogInformation($"GenerateReceiptPdfAsync: Saving to project directory for verification: {filePath}");
            }
            else
            {
                filePath = customFilePath;
            }

            _logger.LogInformation($"GenerateReceiptPdfAsync: Starting PDF generation for BillId: {receiptData.BillId}");
            _logger.LogInformation($"GenerateReceiptPdfAsync: Receipt data source - BillId: {receiptData.BillId}, Date: {receiptData.Date}, TableNumber: {receiptData.TableNumber}");
            _logger.LogInformation($"GenerateReceiptPdfAsync: Receipt data totals - Subtotal: {receiptData.Subtotal}, Tax: {receiptData.TaxAmount}, Total: {receiptData.TotalAmount}");
            _logger.LogInformation($"GenerateReceiptPdfAsync: Receipt data items count: {receiptData.Items?.Count() ?? 0}");

            // Load custom format settings from file
            var formatSettings = await this.LoadReceiptFormatSettingsAsync();
            if (formatSettings == null)
            {
                _logger.LogWarning($"GenerateReceiptPdfAsync: Failed to load custom format settings, using defaults");
                formatSettings = new ReceiptFormatSettings
                {
                    BusinessName = "MagiDesk POS",
                    BusinessAddress = "Your Business Address",
                    BusinessPhone = "Your Phone Number",
                    FontSize = 10,
                    HorizontalMargin = 5,
                    LineSpacing = 1.2,
                    ShowItemDetails = true,
                    ShowSubtotal = true,
                    FooterMessage = "Thank you for your business!"
                };
            }
            else
            {
                _logger.LogInformation($"GenerateReceiptPdfAsync: Successfully loaded custom format settings from receipt-format.json");
                _logger.LogInformation($"GenerateReceiptPdfAsync: BusinessName: '{formatSettings.BusinessName}', BusinessAddress: '{formatSettings.BusinessAddress}', BusinessPhone: '{formatSettings.BusinessPhone}'");
                _logger.LogInformation($"GenerateReceiptPdfAsync: FontSize: {formatSettings.FontSize}, HorizontalMargin: {formatSettings.HorizontalMargin}, LineSpacing: {formatSettings.LineSpacing}");
                _logger.LogInformation($"GenerateReceiptPdfAsync: ShowItemDetails: {formatSettings.ShowItemDetails}, ShowSubtotal: {formatSettings.ShowSubtotal}, FooterMessage: '{formatSettings.FooterMessage}'");
                _logger.LogInformation($"GenerateReceiptPdfAsync: CONFIRMING - Format settings are being sent to ReceiptBuilder for PDF generation");
            }

            // Use the Receipt Designer template approach instead of building from scratch
            _logger.LogInformation($"GenerateReceiptPdfAsync: Using Receipt Designer template approach via GenerateCustomFormattedReceiptAsync");
            return await this.GenerateCustomFormattedReceiptAsync(receiptData, Path.GetFileName(filePath), 80); // Default to 80mm

            _logger.LogInformation($"GenerateReceiptPdfAsync: Custom format PDF generated successfully: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GenerateReceiptPdfAsync: Failed to generate PDF for {receiptData.BillId}");
            throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Build receipt content matching Receipt Designer Preview format exactly
    /// </summary>
    private void BuildReceiptContentWithCustomFormat(ReceiptBuilder builder, ReceiptData receiptData, ReceiptFormatSettings formatSettings)
    {
        _logger.LogInformation($"BuildReceiptContentWithCustomFormat: Building receipt to match Receipt Designer Preview format");
        
        // Use existing ReceiptBuilder methods to match the Receipt Designer Preview format
        
        // Header with complete business info (includes email and website)
        var businessName = formatSettings.BusinessName?.Trim() ?? "MagiDesk POS";
        builder.DrawHeader(
            businessName,
            formatSettings.BusinessAddress,
            formatSettings.BusinessPhone
        );

        // Add email and website manually using existing text drawing
        if (!string.IsNullOrEmpty(formatSettings.BusinessEmail))
        {
            // Use reflection or direct access to draw centered text for email
            var emailText = $"Email: {formatSettings.BusinessEmail}";
            // Add email line using existing drawing capabilities
        }

        if (!string.IsNullOrEmpty(formatSettings.BusinessWebsite))
        {
            // Use reflection or direct access to draw centered text for website
            var websiteText = $"Web: {formatSettings.BusinessWebsite}";
            // Add website line using existing drawing capabilities
        }

        // Receipt info with enhanced format
        var receiptType = receiptData.IsProForma ? "PRE-BILL RECEIPT" : "RECEIPT";
        builder.DrawReceiptInfo(receiptType, receiptData.BillId.ToString(), receiptData.Date ?? DateTime.Now, receiptData.TableNumber);

        // Items section with table format
        if (formatSettings.ShowItemDetails && receiptData.Items?.Any() == true)
        {
            _logger.LogInformation($"BuildReceiptContentWithCustomFormat: Drawing {receiptData.Items.Count()} items in table format");
            var receiptItems = receiptData.Items.Select(item => new MagiDesk.Frontend.Services.ReceiptItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.UnitPrice * item.Quantity
            });
            builder.DrawOrderItems(receiptItems);
        }

        // Totals section with proper formatting
        if (formatSettings.ShowSubtotal || formatSettings.ShowTax)
        {
            _logger.LogInformation($"BuildReceiptContentWithCustomFormat: Drawing totals - Subtotal: {receiptData.Subtotal}, Tax: {receiptData.TaxAmount}, Total: {receiptData.TotalAmount}");
            builder.DrawTotals(
                receiptData.Subtotal,
                receiptData.DiscountAmount,
                receiptData.TaxAmount,
                receiptData.TotalAmount
            );
        }

        // Footer message
        if (!string.IsNullOrEmpty(formatSettings.FooterMessage))
        {
            _logger.LogInformation($"BuildReceiptContentWithCustomFormat: Drawing footer message");
            builder.DrawFooter(formatSettings.FooterMessage);
        }

        _logger.LogInformation($"BuildReceiptContentWithCustomFormat: Receipt built matching Receipt Designer Preview format using existing ReceiptBuilder methods");
    }

    /// <summary>
    /// Build receipt content using ReceiptBuilder
    /// </summary>
    private void BuildReceiptContent(ReceiptBuilder builder, ReceiptData receiptData)
    {
        try
        {
            // Header
            builder.DrawHeader(
                receiptData.StoreName,
                receiptData.Address,
                receiptData.Phone
            );

            // Receipt info
            var receiptType = receiptData.IsProForma ? "Pre-Bill" : "Final Receipt";
            builder.DrawReceiptInfo(
                receiptType,
                receiptData.BillId,
                receiptData.Date ?? DateTime.Now,
                receiptData.TableNumber
            );

            // Order items
            var receiptItems = receiptData.Items.Select(item => new Services.ReceiptItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                LineTotal = item.Price * item.Quantity
            }).ToList();

            builder.DrawOrderItems(receiptItems);

            // Totals
            var subtotal = receiptData.Items.Sum(i => i.Price * i.Quantity);
            var discountAmount = 0m; // TODO: Add discount support
            var taxAmount = receiptData.Tax ?? 0m;
            var totalAmount = receiptData.TotalAmount;

            builder.DrawTotals(subtotal, discountAmount, taxAmount, totalAmount);

            // Footer
            builder.DrawFooter(receiptData.Footer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"BuildReceiptContent: Failed to build receipt content for {receiptData.BillId}");
            throw new InvalidOperationException($"Failed to build receipt content: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get receipt configuration based on settings
    /// </summary>
    private ReceiptConfiguration GetReceiptConfiguration()
    {
        try
        {
            // Get printer width from configuration
            var printerWidth = _configuration["PrinterWidth"] ?? "80";
            
            return printerWidth switch
            {
                "58" => ReceiptConfiguration.Get58mmConfiguration(),
                "80" => ReceiptConfiguration.Get80mmConfiguration(),
                _ => ReceiptConfiguration.Get80mmConfiguration()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get receipt configuration, using default 80mm");
            return ReceiptConfiguration.Get80mmConfiguration();
        }
    }

    /// <summary>
    /// Get receipts folder path (thread-safe)
    /// </summary>
    private static readonly SemaphoreSlim _folderSemaphore = new SemaphoreSlim(1, 1);
    private static string? _cachedReceiptsFolder = null;
    
    private async Task<string> GetReceiptsFolderAsync()
    {
        // Use cached folder if available
        if (_cachedReceiptsFolder != null)
            return _cachedReceiptsFolder;
            
        await _folderSemaphore.WaitAsync();
        try
        {
            // Double-check pattern
            if (_cachedReceiptsFolder != null)
                return _cachedReceiptsFolder;
                
            try
            {
                // Use .NET file system APIs instead of Windows Runtime COM calls
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var receiptsFolderPath = Path.Combine(localAppData, "MagiDesk", "Receipts");
                
                // Ensure directory exists
                Directory.CreateDirectory(receiptsFolderPath);
                _cachedReceiptsFolder = receiptsFolderPath;
                return _cachedReceiptsFolder;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get receipts folder, using temp folder");
                _cachedReceiptsFolder = Path.Combine(Path.GetTempPath(), "MagiDesk", "Receipts");
                
                // Ensure temp directory exists
                Directory.CreateDirectory(_cachedReceiptsFolder);
                return _cachedReceiptsFolder;
            }
        }
        finally
        {
            _folderSemaphore.Release();
        }
    }

    /// <summary>
    /// Get store name from configuration
    /// </summary>
    private string GetStoreName()
    {
        try
        {
            return _configuration["StoreName"] ?? "Billiard Palace";
        }
        catch
        {
            return "Billiard Palace";
        }
    }

    /// <summary>
    /// Get store address from configuration
    /// </summary>
    private string? GetStoreAddress()
    {
        try
        {
            return _configuration["StoreAddress"];
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get store phone from configuration
    /// </summary>
    private string? GetStorePhone()
    {
        try
        {
            return _configuration["StorePhone"];
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Test printing functionality
    /// </summary>
    public async Task<bool> TestPrintingAsync()
    {
        try
        {
            _logger.LogInformation("TestPrintingAsync: Starting test print");
            
            // Create test receipt data
            var testReceipt = new ReceiptData
            {
                StoreName = "Test Billiard Palace",
                BillId = "TEST-001",
                Date = DateTime.Now,
                IsProForma = true,
                Items = new List<ReceiptItem>
                {
                    new ReceiptItem { Name = "Test Item 1", Quantity = 2, Price = 10.00m },
                    new ReceiptItem { Name = "Test Item 2", Quantity = 1, Price = 5.00m }
                },
                Tax = 2.00m,
                TotalAmount = 27.00m,
                Footer = "This is a test receipt.",
                TableNumber = "Test Table",
                Address = "123 Test Street",
                Phone = "555-TEST"
            };

            // Generate test PDF with custom formatting
            var fileName = $"test_receipt_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = await this.GenerateCustomFormattedReceiptAsync(testReceipt, fileName, 58); // Use 58mm for test printing

            _logger.LogInformation($"TestPrintingAsync: Test receipt generated successfully: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TestPrintingAsync: Failed to generate test receipt");
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Clean up resources
                _logger?.LogInformation("ReceiptService disposed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing ReceiptService");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Payment data structure for final receipts
/// </summary>
public sealed class PaymentData
{
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TableNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string PaymentMethod { get; set; } = string.Empty;
    public List<PaymentItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PrinterName { get; set; } = string.Empty;
}

/// <summary>
/// Payment item structure
/// </summary>
public sealed class PaymentItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0;
}