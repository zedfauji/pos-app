using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using System.Text.Json;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Text;
using WinRT.Interop;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Null logger implementation for when DI is not available
/// </summary>
public class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

/// <summary>
/// Service for generating and printing receipts in the Billiard POS system.
/// Now uses CommunityToolkit PrintHelper for modern WinUI 3 printing.
/// </summary>
public sealed class ReceiptService : IDisposable
{
    private readonly ILogger<ReceiptService> _logger;
    private readonly IConfiguration _configuration;
    private ReceiptPrintService? _printService;
    private Panel? _printingPanel;
    private DispatcherQueue? _dispatcherQueue;

    public ReceiptService(ILogger<ReceiptService>? logger, IConfiguration? configuration)
    {
        _logger = logger ?? new NullLogger<ReceiptService>();
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    /// <summary>
    /// Initialize the print service with required UI components
    /// </summary>
    public void Initialize(Panel printingPanel, DispatcherQueue dispatcherQueue)
    {
        _logger.LogInformation("ReceiptService.Initialize: Starting initialization");
        System.Diagnostics.Debug.WriteLine("ReceiptService.Initialize: Starting initialization");
        
        try
        {
            _printingPanel = printingPanel ?? throw new ArgumentNullException(nameof(printingPanel));
            _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
            _printService = new ReceiptPrintService(_printingPanel, _dispatcherQueue);
            
            _logger.LogInformation("ReceiptService.Initialize: Initialization completed successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.Initialize: Initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReceiptService.Initialize: Failed to initialize");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.Initialize: Exception - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Receipt data structure for rendering
    /// </summary>
    public sealed class ReceiptData
    {
        public string? BillId { get; set; }
        public DateTime? Date { get; set; }
        public List<ReceiptItem>? Items { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Tax { get; set; }
        public bool IsProForma { get; set; }
        public string? StoreName { get; set; }
        public string? StoreAddress { get; set; }
        public string? StorePhone { get; set; }
        public string? Footer { get; set; }
        
        // Additional properties for compatibility
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public string? BusinessPhone { get; set; }
        public string? TableNumber { get; set; }
        public string? ServerName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? Change { get; set; }
    }

    /// <summary>
    /// Receipt item structure
    /// </summary>
    public sealed class ReceiptItem
    {
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        
        // Additional properties for compatibility
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// Print receipt using CommunityToolkit PrintHelper
    /// </summary>
    public async Task<bool> PrintReceiptAsync(ReceiptData receiptData, Window window, bool showPreview = true)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: Starting for Bill ID {receiptData?.BillId ?? "NULL"}, showPreview: {showPreview}");
            _logger.LogInformation("PrintReceiptAsync started for Bill ID: {BillId}, showPreview: {ShowPreview}", receiptData?.BillId ?? "NULL", showPreview);
            
            if (receiptData == null)
            {
                _logger.LogError("ReceiptData is null");
                System.Diagnostics.Debug.WriteLine("ReceiptService.PrintReceiptAsync: ReceiptData is null");
                return false;
            }

            if (_printService == null)
            {
                _logger.LogError("PrintService not initialized. Call Initialize() first.");
                System.Diagnostics.Debug.WriteLine("ReceiptService.PrintReceiptAsync: PrintService not initialized");
                return false;
            }
            
            _logger.LogInformation("ReceiptData details: BillId='{BillId}', ItemsCount={ItemsCount}, TotalAmount={TotalAmount}, IsProForma={IsProForma}", 
                receiptData.BillId, receiptData.Items?.Count ?? -1, receiptData.TotalAmount, receiptData.IsProForma);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: ReceiptData details - BillId='{receiptData.BillId}', ItemsCount={receiptData.Items?.Count ?? -1}, TotalAmount={receiptData.TotalAmount}, IsProForma={receiptData.IsProForma}");
            
            // Convert ReceiptData to the new format
            var printData = ConvertToPrintData(receiptData);
            
            var title = receiptData.IsProForma ? "Pro Forma Receipt" : "Receipt";
            await _printService.PrintReceiptAsync(printData, title);
            
            _logger.LogInformation("Print completed successfully for Bill ID: {BillId}", receiptData.BillId);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: Print completed successfully for Bill ID: {receiptData.BillId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print receipt for Bill ID: {BillId}", receiptData?.BillId ?? "NULL");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: Exception caught - {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: Stack trace - {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Convert old ReceiptData to new ReceiptData format for printing
    /// </summary>
    private PrintReceiptData ConvertToPrintData(ReceiptData receiptData)
    {
        return new PrintReceiptData
        {
            Header = receiptData.IsProForma ? "PRO FORMA RECEIPT" : "RECEIPT",
            StoreName = receiptData.StoreName,
            StoreAddress = receiptData.StoreAddress,
            StorePhone = receiptData.StorePhone,
            ReceiptNumber = receiptData.BillId,
            Date = receiptData.Date,
            Items = receiptData.Items?.Select(item => new PrintReceiptItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList(),
            Subtotal = receiptData.Subtotal,
            Tax = receiptData.Tax,
            Total = receiptData.TotalAmount,
            Footer = receiptData.Footer
        };
    }

    /// <summary>
    /// Generate receipt preview
    /// </summary>
    public async Task<UIElement?> GenerateReceiptPreviewAsync(ReceiptData receiptData)
    {
        try
        {
            if (_printService == null)
            {
                _logger.LogError("PrintService not initialized. Call Initialize() first.");
                return null;
            }

            // Convert to print data and create preview
            var printData = ConvertToPrintData(receiptData);
            // For now, return null as we don't have a preview method in ReceiptPrintService
            // This could be enhanced to create a preview element
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate receipt preview");
            return null;
        }
    }

    /// <summary>
    /// Export receipt as PDF
    /// </summary>
    public async Task<string?> ExportReceiptAsPdfAsync(ReceiptData receiptData)
    {
        try
        {
            // For now, return null as PDF export is not implemented
            // This could be enhanced to use a PDF library
            _logger.LogInformation("PDF export not implemented yet");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export receipt as PDF");
            return null;
        }
    }

    /// <summary>
    /// Test printing functionality with sample data
    /// </summary>
    public async Task TestPrintingAsync()
    {
        if (_printService == null)
        {
            throw new InvalidOperationException("PrintService not initialized. Call Initialize() first.");
        }

        await _printService.TestPrintingAsync();
    }

    public void Dispose()
    {
        _printService?.Dispose();
        _printService = null;
    }
}

/// <summary>
/// Receipt data structure for printing
/// </summary>
public class PrintReceiptData
{
    public string? Header { get; set; }
    public string? StoreName { get; set; }
    public string? StoreAddress { get; set; }
    public string? StorePhone { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTime? Date { get; set; }
    public List<PrintReceiptItem>? Items { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Total { get; set; }
    public string? Footer { get; set; }
}

/// <summary>
/// Receipt item for printing
/// </summary>
public class PrintReceiptItem
{
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}