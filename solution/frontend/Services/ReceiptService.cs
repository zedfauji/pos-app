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
            System.Diagnostics.Debug.WriteLine("ReceiptService.InitializeAsync: Starting initialization");
            
            // PDFSharp-based ReceiptService doesn't need UI components for initialization
            // This method is kept for compatibility with existing initialization calls
            // All PDF generation is CPU-bound and doesn't require UI thread context
            
            _logger.LogInformation("ReceiptService.InitializeAsync: Initialization completed successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.InitializeAsync: Initialization completed successfully");
            
            await Task.CompletedTask; // Ensure method is async
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReceiptService.InitializeAsync: Failed to initialize");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.InitializeAsync: Exception - {ex.Message}");
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
            
            await SavePdfAsync(receiptData, filePath);
            
            _logger.LogInformation($"PrintReceiptAsync: Legacy method completed - PDF saved to: {filePath}");
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
            
            await SavePdfAsync(receiptData, filePath);
            
            _logger.LogInformation($"ExportReceiptAsPdfAsync: Legacy method completed - PDF saved to: {filePath}");
            return filePath;
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

            // Generate PDF
            var fileName = $"prebill_{billId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = await GenerateReceiptPdfAsync(receiptData, fileName);

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

            // Generate PDF
            var fileName = $"receipt_{billId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = await GenerateReceiptPdfAsync(receiptData, fileName);

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

            // Generate PDF
            var filePath = await GenerateReceiptPdfAsync(receiptData, fileName);

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

            // Generate PDF and print it directly
            using var builder = new ReceiptBuilder(_logger as ILogger<ReceiptBuilder> ?? new NullLogger<ReceiptBuilder>());
            builder.Initialize(GetReceiptConfiguration());
            
            // Build receipt content
            BuildReceiptContent(builder, receiptData);
            
            // Print to specified printer
            await builder.PrintAsync(printerName);

            _logger.LogInformation($"PrintPdfAsync: Receipt printed successfully to {printerName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PrintPdfAsync: Failed to print receipt for Bill ID: {receiptData?.BillId}");
            throw new InvalidOperationException($"Failed to print receipt: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate receipt PDF using ReceiptBuilder
    /// </summary>
    private async Task<string> GenerateReceiptPdfAsync(ReceiptData receiptData, string fileName)
    {
        try
        {
            // Get receipts folder
            var receiptsFolder = await GetReceiptsFolderAsync();
            var filePath = Path.Combine(receiptsFolder, fileName);

            // Generate PDF using ReceiptBuilder
            using var builder = new ReceiptBuilder(_logger as ILogger<ReceiptBuilder> ?? new NullLogger<ReceiptBuilder>());
            builder.Initialize(GetReceiptConfiguration());
            
            // Build receipt content
            BuildReceiptContent(builder, receiptData);
            
            // Save PDF
            await builder.SaveAsPdfAsync(filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GenerateReceiptPdfAsync: Failed to generate PDF for {receiptData.BillId}");
            throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
        }
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

            // Generate test PDF
            var fileName = $"test_receipt_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = await GenerateReceiptPdfAsync(testReceipt, fileName);

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