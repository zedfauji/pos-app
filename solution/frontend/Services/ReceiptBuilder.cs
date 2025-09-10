using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services
{
    /// <summary>
    /// ReceiptBuilder class for generating PDF receipts using PDFSharp
    /// Supports 58mm and 80mm thermal printer formats
    /// </summary>
    public class ReceiptBuilder : IDisposable
    {
        private readonly ILogger<ReceiptBuilder> _logger;
        private PdfDocument? _document;
        private PdfPage? _page;
        private XGraphics? _graphics;
        private bool _disposed = false;

        // Receipt configuration
        public ReceiptConfiguration Configuration { get; set; } = new();

        // Current position tracking
        private double _currentY = 0;
        private double _pageHeight = 0;
        private double _pageWidth = 0;

        public ReceiptBuilder(ILogger<ReceiptBuilder>? logger = null)
        {
            _logger = logger ?? new NullLogger<ReceiptBuilder>();
        }

        /// <summary>
        /// Initialize the receipt builder with specified configuration
        /// </summary>
        public void Initialize(ReceiptConfiguration? configuration = null)
        {
            try
            {
                // Dispose existing resources before reinitializing
                DisposeResources();
                
                Configuration = configuration ?? new ReceiptConfiguration();
                
                // Validate configuration
                ValidateConfiguration();
                
                // Create new document
                _document = new PdfDocument();
                _page = _document.AddPage();
                
                // Set page size based on configuration
                SetPageSize();
                
                // Initialize graphics
                _graphics = XGraphics.FromPdfPage(_page);
                
                // Reset position
                _currentY = Configuration.MarginTop;
                
                _logger.LogInformation($"ReceiptBuilder initialized with {Configuration.Width}mm x {Configuration.Height}mm format");
            }
            catch (Exception ex)
            {
                // Ensure cleanup on failure
                DisposeResources();
                _logger.LogError(ex, "Failed to initialize ReceiptBuilder");
                throw new InvalidOperationException($"Failed to initialize ReceiptBuilder: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Draw business header information
        /// </summary>
        public void DrawHeader(string businessName, string? address = null, string? phone = null)
        {
            ValidateInitialized();
            
            try
            {
                // Business name (centered, bold)
                var businessFont = new XFont(Configuration.FontFamily, Configuration.HeaderFontSize, XFontStyle.Bold);
                var businessSize = _graphics!.MeasureString(businessName, businessFont);
                var businessX = (_pageWidth - businessSize.Width) / 2;
                
                _graphics.DrawString(businessName, businessFont, XBrushes.Black, businessX, _currentY);
                _currentY += businessSize.Height + Configuration.LineSpacing;
                
                // Address (centered)
                if (!string.IsNullOrEmpty(address))
                {
                    var addressFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Regular);
                    var addressSize = _graphics.MeasureString(address, addressFont);
                    var addressX = (_pageWidth - addressSize.Width) / 2;
                    
                    _graphics.DrawString(address, addressFont, XBrushes.Black, addressX, _currentY);
                    _currentY += addressSize.Height + Configuration.LineSpacing;
                }
                
                // Phone (centered)
                if (!string.IsNullOrEmpty(phone))
                {
                    var phoneFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Regular);
                    var phoneSize = _graphics.MeasureString(phone, phoneFont);
                    var phoneX = (_pageWidth - phoneSize.Width) / 2;
                    
                    _graphics.DrawString(phone, phoneFont, XBrushes.Black, phoneX, _currentY);
                    _currentY += phoneSize.Height + Configuration.LineSpacing;
                }
                
                // Separator line
                DrawSeparatorLine();
                
                _logger.LogDebug($"Drew header: {businessName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to draw header");
                throw new InvalidOperationException($"Failed to draw header: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Draw receipt type and metadata
        /// </summary>
        public void DrawReceiptInfo(string receiptType, string billId, DateTime date, string? tableNumber = null)
        {
            ValidateInitialized();
            
            try
            {
                var infoFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Regular);
                
                // Receipt type (centered, bold)
                var typeFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Bold);
                var typeSize = _graphics!.MeasureString(receiptType, typeFont);
                var typeX = (_pageWidth - typeSize.Width) / 2;
                
                _graphics.DrawString(receiptType, typeFont, XBrushes.Black, typeX, _currentY);
                _currentY += typeSize.Height + Configuration.LineSpacing;
                
                // Bill ID
                _graphics.DrawString($"Bill ID: {billId}", infoFont, XBrushes.Black, Configuration.MarginLeft, _currentY);
                _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                
                // Date
                _graphics.DrawString($"Date: {date:yyyy-MM-dd HH:mm}", infoFont, XBrushes.Black, Configuration.MarginLeft, _currentY);
                _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                
                // Table number (if provided)
                if (!string.IsNullOrEmpty(tableNumber))
                {
                    _graphics.DrawString($"Table: {tableNumber}", infoFont, XBrushes.Black, Configuration.MarginLeft, _currentY);
                    _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                }
                
                // Separator line
                DrawSeparatorLine();
                
                _logger.LogDebug($"Drew receipt info: {receiptType}, Bill: {billId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to draw receipt info");
                throw new InvalidOperationException($"Failed to draw receipt info: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Draw itemized order list
        /// </summary>
        public void DrawOrderItems(IEnumerable<ReceiptItem> items)
        {
            ValidateInitialized();
            
            try
            {
                var itemFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Regular);
                var priceFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Regular);
                
                // Items header
                var headerFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Bold);
                _graphics!.DrawString("Items:", headerFont, XBrushes.Black, Configuration.MarginLeft, _currentY);
                _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                
                foreach (var item in items)
                {
                    // Item name and quantity
                    var itemText = $"{item.Name} x{item.Quantity}";
                    _graphics.DrawString(itemText, itemFont, XBrushes.Black, Configuration.MarginLeft, _currentY);
                    
                    // Unit price and line total (right aligned)
                    var priceText = $"@{item.UnitPrice:C} = {item.LineTotal:C}";
                    var priceSize = _graphics.MeasureString(priceText, priceFont);
                    var priceX = _pageWidth - Configuration.MarginRight - priceSize.Width;
                    
                    _graphics.DrawString(priceText, priceFont, XBrushes.Black, priceX, _currentY);
                    _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                }
                
                // Separator line
                DrawSeparatorLine();
                
                _logger.LogDebug($"Drew {items.Count()} order items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to draw order items");
                throw new InvalidOperationException($"Failed to draw order items: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Draw totals section (subtotal, discounts, tax, total)
        /// </summary>
        public void DrawTotals(decimal subtotal, decimal discountAmount = 0, decimal taxAmount = 0, decimal totalAmount = 0)
        {
            ValidateInitialized();
            
            try
            {
                var totalFont = new XFont(Configuration.FontFamily, Configuration.BodyFontSize, XFontStyle.Regular);
                var grandTotalFont = new XFont(Configuration.FontFamily, Configuration.TotalFontSize, XFontStyle.Bold);
                
                // Subtotal
                if (subtotal > 0)
                {
                    var subtotalText = $"Subtotal: {subtotal:C}";
                    var subtotalSize = _graphics!.MeasureString(subtotalText, totalFont);
                    var subtotalX = _pageWidth - Configuration.MarginRight - subtotalSize.Width;
                    
                    _graphics.DrawString(subtotalText, totalFont, XBrushes.Black, subtotalX, _currentY);
                    _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                }
                
                // Discount
                if (discountAmount > 0)
                {
                    var discountText = $"Discount: -{discountAmount:C}";
                    var discountSize = _graphics.MeasureString(discountText, totalFont);
                    var discountX = _pageWidth - Configuration.MarginRight - discountSize.Width;
                    
                    _graphics.DrawString(discountText, totalFont, XBrushes.Black, discountX, _currentY);
                    _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                }
                
                // Tax
                if (taxAmount > 0)
                {
                    var taxText = $"Tax: {taxAmount:C}";
                    var taxSize = _graphics.MeasureString(taxText, totalFont);
                    var taxX = _pageWidth - Configuration.MarginRight - taxSize.Width;
                    
                    _graphics.DrawString(taxText, totalFont, XBrushes.Black, taxX, _currentY);
                    _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                }
                
                // Grand total
                if (totalAmount > 0)
                {
                    var totalText = $"Total: {totalAmount:C}";
                    var totalSize = _graphics.MeasureString(totalText, grandTotalFont);
                    var totalX = _pageWidth - Configuration.MarginRight - totalSize.Width;
                    
                    _graphics.DrawString(totalText, grandTotalFont, XBrushes.Black, totalX, _currentY);
                    _currentY += Configuration.LineHeight + Configuration.LineSpacing;
                }
                
                // Separator line
                DrawSeparatorLine();
                
                _logger.LogDebug($"Drew totals: Subtotal={subtotal:C}, Discount={discountAmount:C}, Tax={taxAmount:C}, Total={totalAmount:C}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to draw totals");
                throw new InvalidOperationException($"Failed to draw totals: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Draw footer message
        /// </summary>
        public void DrawFooter(string? footerText = null)
        {
            ValidateInitialized();
            
            try
            {
                if (!string.IsNullOrEmpty(footerText))
                {
                    var footerFont = new XFont(Configuration.FontFamily, Configuration.FooterFontSize, XFontStyle.Regular);
                    var footerSize = _graphics!.MeasureString(footerText, footerFont);
                    var footerX = (_pageWidth - footerSize.Width) / 2;
                    
                    _graphics.DrawString(footerText, footerFont, XBrushes.Black, footerX, _currentY);
                    _currentY += footerSize.Height + Configuration.LineSpacing;
                }
                
                _logger.LogDebug($"Drew footer: {footerText ?? "None"}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to draw footer");
                throw new InvalidOperationException($"Failed to draw footer: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Draw a separator line
        /// </summary>
        private void DrawSeparatorLine()
        {
            ValidateInitialized();
            
            try
            {
                var lineY = _currentY + Configuration.LineSpacing / 2;
                _graphics!.DrawLine(XPens.Black, Configuration.MarginLeft, lineY, _pageWidth - Configuration.MarginRight, lineY);
                _currentY += Configuration.LineSpacing;
                
                _logger.LogDebug("Drew separator line");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to draw separator line");
                throw new InvalidOperationException($"Failed to draw separator line: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save the PDF to a file
        /// </summary>
        public async Task SaveAsPdfAsync(string filePath)
        {
            ValidateInitialized();
            
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Save the document
                _document!.Save(filePath);
                
                _logger.LogInformation($"PDF saved successfully to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save PDF");
                throw new InvalidOperationException($"Failed to save PDF to {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the PDF document as a byte array
        /// </summary>
        public byte[] GetPdfBytes()
        {
            ValidateInitialized();
            
            try
            {
                using var stream = new MemoryStream();
                _document!.Save(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get PDF bytes");
                throw new InvalidOperationException($"Failed to get PDF bytes: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Print the PDF to a specific printer
        /// </summary>
        public async Task PrintAsync(string printerName)
        {
            ValidateInitialized();
            
            try
            {
                if (string.IsNullOrEmpty(printerName))
                    throw new ArgumentException("Printer name cannot be null or empty", nameof(printerName));
                
                // Get PDF bytes
                var pdfBytes = GetPdfBytes();
                
                // Create unique temporary file name to prevent race conditions
                var tempPath = Path.Combine(Path.GetTempPath(), $"receipt_{Guid.NewGuid():N}_{Environment.ProcessId}_{Environment.TickCount}.pdf");
                
                try
                {
                    // Save to temporary file
                    await File.WriteAllBytesAsync(tempPath, pdfBytes);
                    
                    // Use system print command
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true,
                        Verb = "print",
                        Arguments = $"/p /h"
                    };
                    
                    var process = System.Diagnostics.Process.Start(startInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        
                        // Check if print was successful
                        if (process.ExitCode != 0)
                        {
                            throw new InvalidOperationException($"Print process failed with exit code: {process.ExitCode}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to start print process");
                    }
                    
                    _logger.LogInformation($"PDF printed successfully to printer: {printerName}");
                }
                finally
                {
                    // Clean up temporary file with enhanced race condition handling
                    await CleanupTempFileAsync(tempPath);
                }
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM exception while printing PDF - printer driver issue");
                throw new InvalidOperationException($"Printer driver error for {printerName}: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied while printing PDF - insufficient permissions");
                throw new InvalidOperationException($"Access denied to printer {printerName}: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO exception while printing PDF - printer communication error");
                throw new InvalidOperationException($"Communication error with printer {printerName}: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument while printing PDF");
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while printing PDF");
                throw; // Re-throw invalid operation exceptions as-is
            }
            catch (SystemException ex)
            {
                _logger.LogError(ex, "System exception while printing PDF - printer unavailable");
                throw new InvalidOperationException($"Printer {printerName} is unavailable: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while printing PDF");
                throw new InvalidOperationException($"Unexpected error printing to {printerName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set page size based on configuration
        /// </summary>
        private void SetPageSize()
        {
            // Convert mm to points (1 mm = 2.834645669 points)
            const double mmToPoints = 2.834645669;
            
            _pageWidth = Configuration.Width * mmToPoints;
            _pageHeight = Configuration.Height * mmToPoints;
            
            _page!.Width = _pageWidth;
            _page.Height = _pageHeight;
            
            _logger.LogDebug($"Set page size: {Configuration.Width}mm x {Configuration.Height}mm ({_pageWidth:F2} x {_pageHeight:F2} points)");
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        private void ValidateConfiguration()
        {
            if (Configuration.Width <= 0)
                throw new ArgumentException("Width must be greater than 0", nameof(Configuration.Width));
            
            if (Configuration.Height <= 0)
                throw new ArgumentException("Height must be greater than 0", nameof(Configuration.Height));
            
            if (string.IsNullOrEmpty(Configuration.FontFamily))
                throw new ArgumentException("Font family cannot be null or empty", nameof(Configuration.FontFamily));
            
            if (Configuration.HeaderFontSize <= 0)
                throw new ArgumentException("Header font size must be greater than 0", nameof(Configuration.HeaderFontSize));
            
            if (Configuration.BodyFontSize <= 0)
                throw new ArgumentException("Body font size must be greater than 0", nameof(Configuration.BodyFontSize));
            
            if (Configuration.TotalFontSize <= 0)
                throw new ArgumentException("Total font size must be greater than 0", nameof(Configuration.TotalFontSize));
            
            if (Configuration.FooterFontSize <= 0)
                throw new ArgumentException("Footer font size must be greater than 0", nameof(Configuration.FooterFontSize));
            
            if (Configuration.MarginLeft < 0)
                throw new ArgumentException("Left margin cannot be negative", nameof(Configuration.MarginLeft));
            
            if (Configuration.MarginRight < 0)
                throw new ArgumentException("Right margin cannot be negative", nameof(Configuration.MarginRight));
            
            if (Configuration.MarginTop < 0)
                throw new ArgumentException("Top margin cannot be negative", nameof(Configuration.MarginTop));
            
            if (Configuration.MarginBottom < 0)
                throw new ArgumentException("Bottom margin cannot be negative", nameof(Configuration.MarginBottom));
            
            if (Configuration.LineHeight <= 0)
                throw new ArgumentException("Line height must be greater than 0", nameof(Configuration.LineHeight));
            
            if (Configuration.LineSpacing < 0)
                throw new ArgumentException("Line spacing cannot be negative", nameof(Configuration.LineSpacing));
        }

        /// <summary>
        /// Dispose PDFSharp resources safely
        /// </summary>
        private void DisposeResources()
        {
            try
            {
                _graphics?.Dispose();
                _graphics = null;
                
                // Note: PdfDocument and PdfPage don't need explicit disposal
                // They are managed by PDFSharp internally
                _document = null;
                _page = null;
                
                _logger.LogDebug("ReceiptBuilder resources disposed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing ReceiptBuilder resources");
            }
        }

        /// <summary>
        /// Enhanced temporary file cleanup with race condition handling
        /// </summary>
        private async Task CleanupTempFileAsync(string tempPath)
        {
            if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
                return;

            var retryCount = 0;
            const int maxRetries = 5;
            const int baseDelayMs = 50;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    // Use FileStream with exclusive access to check if file is locked
                    using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If we can open with exclusive access, file is not locked
                        fs.Close();
                    }
                    
                    // Now safe to delete
                    File.Delete(tempPath);
                    _logger.LogDebug($"Successfully deleted temp file: {tempPath}");
                    return; // Success, exit retry loop
                }
                catch (IOException ex) when (retryCount < maxRetries - 1)
                {
                    // File is locked by print process, wait and retry
                    retryCount++;
                    var delay = baseDelayMs * (int)Math.Pow(2, retryCount - 1); // Exponential backoff
                    await Task.Delay(delay);
                    _logger.LogWarning($"Temp file locked, retrying in {delay}ms (attempt {retryCount}/{maxRetries}): {ex.Message}");
                }
                catch (UnauthorizedAccessException ex) when (retryCount < maxRetries - 1)
                {
                    // Permission issue, wait and retry
                    retryCount++;
                    var delay = baseDelayMs * (int)Math.Pow(2, retryCount - 1);
                    await Task.Delay(delay);
                    _logger.LogWarning($"Temp file access denied, retrying in {delay}ms (attempt {retryCount}/{maxRetries}): {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to delete temp file after {retryCount + 1} attempts: {tempPath}");
                    break; // Don't retry for unexpected exceptions
                }
            }
            
            // Final attempt - log warning but don't throw
            _logger.LogWarning($"Could not delete temp file after {maxRetries} attempts: {tempPath}");
        }


        /// <summary>
        /// Validate that the builder is initialized
        /// </summary>
        private void ValidateInitialized()
        {
            if (_document == null || _page == null || _graphics == null)
                throw new InvalidOperationException("ReceiptBuilder not initialized. Call Initialize() first.");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _graphics?.Dispose();
                    _document?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing ReceiptBuilder");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// Configuration for receipt formatting
    /// </summary>
    public class ReceiptConfiguration
    {
        // Page dimensions (in mm)
        public double Width { get; set; } = 80; // Default to 80mm
        public double Height { get; set; } = 200; // Default height
        
        // Font settings
        public string FontFamily { get; set; } = "Arial";
        public double HeaderFontSize { get; set; } = 12;
        public double BodyFontSize { get; set; } = 10;
        public double TotalFontSize { get; set; } = 11;
        public double FooterFontSize { get; set; } = 9;
        
        // Margins (in mm)
        public double MarginLeft { get; set; } = 5;
        public double MarginRight { get; set; } = 5;
        public double MarginTop { get; set; } = 5;
        public double MarginBottom { get; set; } = 5;
        
        // Line spacing
        public double LineHeight { get; set; } = 12;
        public double LineSpacing { get; set; } = 2;
        
        // Preset configurations
        public static ReceiptConfiguration Get58mmConfiguration()
        {
            return new ReceiptConfiguration
            {
                Width = 58,
                Height = 200,
                FontFamily = "Arial",
                HeaderFontSize = 10,
                BodyFontSize = 8,
                TotalFontSize = 9,
                FooterFontSize = 7,
                MarginLeft = 3,
                MarginRight = 3,
                MarginTop = 3,
                MarginBottom = 3,
                LineHeight = 10,
                LineSpacing = 1
            };
        }
        
        public static ReceiptConfiguration Get80mmConfiguration()
        {
            return new ReceiptConfiguration
            {
                Width = 80,
                Height = 200,
                FontFamily = "Arial",
                HeaderFontSize = 12,
                BodyFontSize = 10,
                TotalFontSize = 11,
                FooterFontSize = 9,
                MarginLeft = 5,
                MarginRight = 5,
                MarginTop = 5,
                MarginBottom = 5,
                LineHeight = 12,
                LineSpacing = 2
            };
        }
    }

    /// <summary>
    /// Receipt item data structure
    /// </summary>
    public class ReceiptItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0;
        public decimal LineTotal { get; set; } = 0;
    }

    /// <summary>
    /// Null logger implementation
    /// </summary>
    public class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
