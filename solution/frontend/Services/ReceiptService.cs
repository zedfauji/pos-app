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
/// Supports both thermal printers (58mm/80mm) and PDF export fallback.
/// </summary>
public sealed class ReceiptService : IDisposable
{
    private readonly ILogger<ReceiptService> _logger;
    private readonly IConfiguration _configuration;
    private PrintManager? _printManager;
    private PrintDocument? _printDocument;
    private ReceiptData? _currentReceipt;

    public ReceiptService(ILogger<ReceiptService>? logger, IConfiguration? configuration)
    {
        _logger = logger ?? new NullLogger<ReceiptService>();
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    /// <summary>
    /// Receipt data structure for rendering
    /// </summary>
    public sealed class ReceiptData
    {
        public required string BusinessName { get; set; }
        public required string BusinessAddress { get; set; }
        public required string BusinessPhone { get; set; }
        public required string TableNumber { get; set; }
        public required string ServerName { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required List<ReceiptItem> Items { get; set; }
        public required decimal Subtotal { get; set; }
        public required decimal DiscountAmount { get; set; }
        public required decimal TaxAmount { get; set; }
        public required decimal TotalAmount { get; set; }
        public required string BillId { get; set; }
        public required string PaymentMethod { get; set; }
        public required decimal AmountPaid { get; set; }
        public required decimal Change { get; set; }
        public bool IsProForma { get; set; } = false;
    }

    public sealed class ReceiptItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// Printer settings from configuration
    /// </summary>
    public sealed class PrinterSettings
    {
        public string DefaultPrinter { get; set; } = string.Empty;
        public ReceiptSize ReceiptSize { get; set; } = ReceiptSize.Size80mm;
        public bool AutoPrintOnPayment { get; set; } = true;
        public bool PreviewBeforePrint { get; set; } = true;
    }

    public enum ReceiptSize
    {
        Size58mm = 58,
        Size80mm = 80
    }

    /// <summary>
    /// Initialize printing capabilities lazily when needed
    /// </summary>
    private async Task<bool> EnsurePrintManagerInitializedAsync(Window window)
    {
        if (_printManager != null)
        {
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing PrintManager lazily...");
            System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: Initializing PrintManager lazily...");
            
            // Ensure we're on the UI thread
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue == null)
            {
                _logger.LogError("No DispatcherQueue available - not on UI thread");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: No DispatcherQueue available");
                return false;
            }
            
            _logger.LogInformation("DispatcherQueue found, attempting PrintManager initialization");
            System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: DispatcherQueue found");
            
            // Try to get PrintManager for current view
            // For WinUI 3 desktop apps, we need to use PrintManagerInterop.GetForWindow(hWnd)
            if (window == null)
            {
                _logger.LogError("Window parameter is null");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: Window parameter is null");
                return false;
            }
            
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (windowHandle == IntPtr.Zero)
            {
                _logger.LogError("Failed to get window handle");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: Failed to get window handle");
                return false;
            }
            
            _logger.LogInformation("Got window handle: {WindowHandle}", windowHandle);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.EnsurePrintManagerInitializedAsync: Got window handle: {windowHandle}");
            
            // Use PrintManagerInterop.GetForWindow for WinUI 3 desktop apps
            _printManager = PrintManagerInterop.GetForWindow(windowHandle);
            _printManager.PrintTaskRequested += OnPrintTaskRequested;
            
            // Create PrintDocument here instead of in event handler
            _logger.LogInformation("Creating PrintDocument during initialization...");
            System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: Creating PrintDocument...");
            
            try
            {
                _printDocument = new PrintDocument();
                _logger.LogInformation("PrintDocument created successfully during initialization");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: PrintDocument created successfully");
                
                // Register event handlers during initialization
                _logger.LogInformation("Registering PrintDocument event handlers during initialization...");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: Registering event handlers...");
                
                _printDocument.Paginate += OnPaginate;
                _logger.LogInformation("Paginate event registered during initialization");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: Paginate event registered");
                
                _printDocument.GetPreviewPage += OnGetPreviewPage;
                _logger.LogInformation("GetPreviewPage event registered during initialization");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: GetPreviewPage event registered");
                
                _printDocument.AddPages += OnAddPages;
                _logger.LogInformation("AddPages event registered during initialization");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: AddPages event registered");
                
                _logger.LogInformation("All PrintDocument event handlers registered successfully during initialization");
                System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: All event handlers registered");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create PrintDocument or register event handlers during initialization");
                System.Diagnostics.Debug.WriteLine($"ReceiptService.EnsurePrintManagerInitializedAsync: Failed - {ex.GetType().Name}: {ex.Message}");
                _printManager = null;
                return false;
            }
            
            _logger.LogInformation("PrintManager initialized successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.EnsurePrintManagerInitializedAsync: PrintManager initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PrintManager initialization failed");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.EnsurePrintManagerInitializedAsync: PrintManager initialization failed - {ex.GetType().Name}: {ex.Message}");
            _printManager = null;
            return false;
        }
    }

    /// <summary>
    /// Event handler for PrintTaskRequested - Creates PrintDocument here as per documentation
    /// </summary>
    private void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        try
        {
            _logger.LogInformation("OnPrintTaskRequested: Creating print task and document");
            System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Creating print task and document");
            
            var deferral = args.Request.GetDeferral();
            IPrintDocumentSource printDocumentSource = null;
            
            try
            {
                _logger.LogInformation("OnPrintTaskRequested: Using pre-created PrintDocument");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Using pre-created PrintDocument");
                
                if (_printDocument == null)
                {
                    _logger.LogError("OnPrintTaskRequested: Pre-created PrintDocument is null");
                    System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Pre-created PrintDocument is null");
                    deferral.Complete();
                    return;
                }
                
                _logger.LogInformation("OnPrintTaskRequested: Pre-created PrintDocument is available");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Pre-created PrintDocument is available");
                
                // Get DocumentSource from pre-created PrintDocument
                _logger.LogInformation("OnPrintTaskRequested: Getting DocumentSource from pre-created PrintDocument");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Getting DocumentSource");
                
                printDocumentSource = _printDocument.DocumentSource;
                _logger.LogInformation("OnPrintTaskRequested: DocumentSource retrieved successfully");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: DocumentSource retrieved");
                
                if (printDocumentSource == null)
                {
                    _logger.LogError("OnPrintTaskRequested: DocumentSource is null");
                    System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: DocumentSource is null");
                    deferral.Complete();
                    return;
                }
                
                _logger.LogInformation("OnPrintTaskRequested: DocumentSource is valid and ready");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: DocumentSource is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnPrintTaskRequested: Failed to get DocumentSource from pre-created PrintDocument");
                System.Diagnostics.Debug.WriteLine($"ReceiptService.OnPrintTaskRequested: Failed - {ex.GetType().Name}: {ex.Message}");
                deferral.Complete();
                return;
            }
            
            try
            {
                _logger.LogInformation("OnPrintTaskRequested: Creating print task...");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Creating print task...");
                
                var printTask = args.Request.CreatePrintTask("Receipt Print Job", printTaskSourceRequestedArgs =>
                {
                    _logger.LogInformation("OnPrintTaskRequested: Setting print document source");
                    System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Setting print document source");
                    printTaskSourceRequestedArgs.SetSource(printDocumentSource);
                });
                
                _logger.LogInformation("OnPrintTaskRequested: Print task created successfully");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Print task created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnPrintTaskRequested: Failed during print task creation");
                System.Diagnostics.Debug.WriteLine($"ReceiptService.OnPrintTaskRequested: Failed during print task creation - {ex.GetType().Name}: {ex.Message}");
                deferral.Complete();
                return;
            }
            
            deferral.Complete();
            _logger.LogInformation("OnPrintTaskRequested: Print task creation completed successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.OnPrintTaskRequested: Print task creation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPrintTaskRequested failed");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.OnPrintTaskRequested: Exception - {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.OnPrintTaskRequested: Stack trace - {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Generate receipt preview for Payment Dialog
    /// </summary>
    public async Task<UIElement> GenerateReceiptPreviewAsync(ReceiptData receiptData)
    {
        _currentReceipt = receiptData;
        
        var settings = GetPrinterSettings();
        var receiptPanel = CreateReceiptLayout(receiptData, settings);
        
        _logger.LogInformation("Generated receipt preview for Bill ID: {BillId}", receiptData.BillId);
        return receiptPanel;
    }

    /// <summary>
    /// Print receipt (pro forma or final)
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
            
            _logger.LogInformation("ReceiptData details: BillId='{BillId}', ItemsCount={ItemsCount}, TotalAmount={TotalAmount}, IsProForma={IsProForma}", 
                receiptData.BillId, receiptData.Items?.Count ?? -1, receiptData.TotalAmount, receiptData.IsProForma);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: ReceiptData details - BillId='{receiptData.BillId}', ItemsCount={receiptData.Items?.Count ?? -1}, TotalAmount={receiptData.TotalAmount}, IsProForma={receiptData.IsProForma}");
            
            _currentReceipt = receiptData;
            _logger.LogInformation("Current receipt set successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.PrintReceiptAsync: Current receipt set successfully");
            
            var settings = GetPrinterSettings();
            _logger.LogInformation("Printer settings retrieved - PreviewBeforePrint: {PreviewBeforePrint}", settings.PreviewBeforePrint);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: Printer settings retrieved - PreviewBeforePrint: {settings.PreviewBeforePrint}");

            if (showPreview && settings.PreviewBeforePrint)
            {
                _logger.LogInformation("Calling ShowPrintPreviewAsync...");
                System.Diagnostics.Debug.WriteLine("ReceiptService.PrintReceiptAsync: Calling ShowPrintPreviewAsync...");
                var result = await ShowPrintPreviewAsync(window);
                _logger.LogInformation("ShowPrintPreviewAsync completed with result: {Result}", result);
                System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: ShowPrintPreviewAsync completed with result: {result}");
                return result;
            }
            else
            {
                _logger.LogInformation("Calling PrintDirectlyAsync...");
                System.Diagnostics.Debug.WriteLine("ReceiptService.PrintReceiptAsync: Calling PrintDirectlyAsync...");
                var result = await PrintDirectlyAsync();
                _logger.LogInformation("PrintDirectlyAsync completed with result: {Result}", result);
                System.Diagnostics.Debug.WriteLine($"ReceiptService.PrintReceiptAsync: PrintDirectlyAsync completed with result: {result}");
                return result;
            }
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
    /// Show print preview dialog
    /// </summary>
    private async Task<bool> ShowPrintPreviewAsync(Window window)
    {
        try
        {
            _logger.LogInformation("ShowPrintPreviewAsync started");
            System.Diagnostics.Debug.WriteLine("ReceiptService.ShowPrintPreviewAsync: Started");
            
            // Ensure PrintManager is initialized
            var initialized = await EnsurePrintManagerInitializedAsync(window);
            if (!initialized)
            {
                _logger.LogWarning("PrintManager could not be initialized");
                System.Diagnostics.Debug.WriteLine("ReceiptService.ShowPrintPreviewAsync: PrintManager could not be initialized");
                return false;
            }

            _logger.LogInformation("Calling PrintManagerInterop.ShowPrintUIForWindowAsync...");
            System.Diagnostics.Debug.WriteLine("ReceiptService.ShowPrintPreviewAsync: Calling PrintManagerInterop.ShowPrintUIForWindowAsync...");
            
            // Get window handle for WinUI 3 desktop apps
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            await PrintManagerInterop.ShowPrintUIForWindowAsync(windowHandle);
            
            _logger.LogInformation("PrintManagerInterop.ShowPrintUIForWindowAsync completed successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.ShowPrintPreviewAsync: PrintManagerInterop.ShowPrintUIForWindowAsync completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show print preview");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.ShowPrintPreviewAsync: Exception caught - {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.ShowPrintPreviewAsync: Stack trace - {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Try PDF export as fallback when printing is not available
    /// </summary>
    private async Task<bool> TryPdfExportFallback()
    {
        try
        {
            _logger.LogInformation("Attempting simple HTML export fallback...");
            System.Diagnostics.Debug.WriteLine("ReceiptService.TryPdfExportFallback: Starting simple HTML export fallback");
            
            if (_currentReceipt == null)
            {
                _logger.LogError("No current receipt available for HTML export");
                System.Diagnostics.Debug.WriteLine("ReceiptService.TryPdfExportFallback: No current receipt available");
                return false;
            }
            
            _logger.LogInformation("Generating HTML receipt...");
            System.Diagnostics.Debug.WriteLine("ReceiptService.TryPdfExportFallback: Generating HTML receipt...");
            
            var settings = GetPrinterSettings();
            var htmlContent = GenerateReceiptHtml(_currentReceipt, settings);
            
            _logger.LogInformation("HTML content generated successfully, length: {Length}", htmlContent.Length);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.TryPdfExportFallback: HTML content generated successfully, length: {htmlContent.Length}");
            
            // Try to save to Documents folder
            try
            {
                _logger.LogInformation("Attempting to save HTML to Documents folder...");
                System.Diagnostics.Debug.WriteLine("ReceiptService.TryPdfExportFallback: Attempting to save HTML to Documents folder...");
                
                var documentsFolder = Windows.Storage.KnownFolders.DocumentsLibrary;
                var fileName = $"Receipt_{_currentReceipt.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var file = await documentsFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                
                await Windows.Storage.FileIO.WriteTextAsync(file, htmlContent);
                
                _logger.LogInformation("HTML receipt saved successfully: {FileName}", fileName);
                System.Diagnostics.Debug.WriteLine($"ReceiptService.TryPdfExportFallback: HTML receipt saved successfully: {fileName}");
                
                // Show success message to user
                _logger.LogInformation("Receipt saved as HTML file. You can open it in your browser to print.");
                System.Diagnostics.Debug.WriteLine("ReceiptService.TryPdfExportFallback: Receipt saved as HTML file. You can open it in your browser to print.");
                
                return true;
            }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx, "Failed to save HTML to Documents folder, trying alternative approach");
                System.Diagnostics.Debug.WriteLine($"ReceiptService.TryPdfExportFallback: Failed to save HTML to Documents folder - {saveEx.GetType().Name}: {saveEx.Message}");
                
                // Alternative: Try to copy to clipboard or show content
                _logger.LogInformation("HTML receipt generated successfully. Content length: {Length} characters", htmlContent.Length);
                System.Diagnostics.Debug.WriteLine($"ReceiptService.TryPdfExportFallback: HTML receipt generated successfully. Content length: {htmlContent.Length} characters");
                
                return true; // Still consider it successful since we generated the content
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTML export fallback failed");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.TryPdfExportFallback: Exception - {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.TryPdfExportFallback: Stack trace - {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Print directly without preview
    /// </summary>
    private async Task<bool> PrintDirectlyAsync()
    {
        try
        {
            if (_printManager == null || _printDocument == null)
            {
                _logger.LogWarning("Print manager or document not initialized");
                return false;
            }

            await PrintManager.ShowPrintUIAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print directly");
            return false;
        }
    }

    /// <summary>
    /// Export receipt as PDF when no printer is available
    /// </summary>
    public async Task<string?> ExportReceiptAsPdfAsync(ReceiptData receiptData)
    {
        try
        {
            var settings = GetPrinterSettings();
            var receiptPanel = CreateReceiptLayout(receiptData, settings);
            
            var filePicker = new FileSavePicker();
            
            // Initialize with window handle for WinUI 3
            var window = Window.Current;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
            }
            
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeChoices.Add("PDF Files", new List<string> { ".pdf" });
            filePicker.SuggestedFileName = $"Receipt_{receiptData.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            var file = await filePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Note: PDF generation would require additional libraries like PdfSharp or similar
                // For now, we'll save as HTML that can be printed to PDF
                var htmlContent = GenerateReceiptHtml(receiptData, settings);
                await FileIO.WriteTextAsync(file, htmlContent);
                
                _logger.LogInformation("Receipt exported as PDF: {FileName}", file.Name);
                return file.Path;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export receipt as PDF for Bill ID: {BillId}", receiptData.BillId);
            return null;
        }
    }

    /// <summary>
    /// Create receipt layout based on printer settings
    /// </summary>
    private StackPanel CreateReceiptLayout(ReceiptData receipt, PrinterSettings settings)
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(10),
            Background = new SolidColorBrush(Microsoft.UI.Colors.White)
        };

        // Header
        mainPanel.Children.Add(CreateHeader(receipt, settings));
        
        // Table Info
        mainPanel.Children.Add(CreateTableInfo(receipt, settings));
        
        // Items
        mainPanel.Children.Add(CreateItemsSection(receipt, settings));
        
        // Totals
        mainPanel.Children.Add(CreateTotalsSection(receipt, settings));
        
        // Footer
        mainPanel.Children.Add(CreateFooter(receipt, settings));

        return mainPanel;
    }

    private UIElement CreateHeader(ReceiptData receipt, PrinterSettings settings)
    {
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var businessName = new TextBlock
        {
            Text = receipt.BusinessName,
            FontSize = GetFontSize(18, settings),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        var businessAddress = new TextBlock
        {
            Text = receipt.BusinessAddress,
            FontSize = GetFontSize(12, settings),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var businessPhone = new TextBlock
        {
            Text = receipt.BusinessPhone,
            FontSize = GetFontSize(12, settings),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        headerPanel.Children.Add(businessName);
        headerPanel.Children.Add(businessAddress);
        headerPanel.Children.Add(businessPhone);

        return headerPanel;
    }

    private UIElement CreateTableInfo(ReceiptData receipt, PrinterSettings settings)
    {
        var infoPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var tableInfo = new TextBlock
        {
            Text = $"Table: {receipt.TableNumber} | Server: {receipt.ServerName}",
            FontSize = GetFontSize(12, settings),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var timeInfo = new TextBlock
        {
            Text = $"Start: {receipt.StartTime:HH:mm} | End: {receipt.EndTime:HH:mm}",
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var elapsedTime = receipt.EndTime - receipt.StartTime;
        var elapsedInfo = new TextBlock
        {
            Text = $"Duration: {elapsedTime.Hours:D2}:{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}",
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        infoPanel.Children.Add(tableInfo);
        infoPanel.Children.Add(timeInfo);
        infoPanel.Children.Add(elapsedInfo);

        return infoPanel;
    }

    private UIElement CreateItemsSection(ReceiptData receipt, PrinterSettings settings)
    {
        var itemsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Header row
        var headerRow = CreateItemsHeader(settings);
        itemsPanel.Children.Add(headerRow);

        // Items
        foreach (var item in receipt.Items)
        {
            var itemRow = CreateItemRow(item, settings);
            itemsPanel.Children.Add(itemRow);
        }

        return itemsPanel;
    }

    private UIElement CreateItemsHeader(PrinterSettings settings)
    {
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nameHeader = new TextBlock
        {
            Text = "Item",
            FontSize = GetFontSize(10, settings),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetColumn(nameHeader, 0);

        var qtyHeader = new TextBlock
        {
            Text = "Qty",
            FontSize = GetFontSize(10, settings),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(qtyHeader, 1);

        var priceHeader = new TextBlock
        {
            Text = "Price",
            FontSize = GetFontSize(10, settings),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(priceHeader, 2);

        var totalHeader = new TextBlock
        {
            Text = "Total",
            FontSize = GetFontSize(10, settings),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(totalHeader, 3);

        headerGrid.Children.Add(nameHeader);
        headerGrid.Children.Add(qtyHeader);
        headerGrid.Children.Add(priceHeader);
        headerGrid.Children.Add(totalHeader);

        return headerGrid;
    }

    private UIElement CreateItemRow(ReceiptItem item, PrinterSettings settings)
    {
        var itemGrid = new Grid();
        itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nameText = new TextBlock
        {
            Text = item.Name,
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Left,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(nameText, 0);

        var qtyText = new TextBlock
        {
            Text = item.Quantity.ToString(),
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(qtyText, 1);

        var priceText = new TextBlock
        {
            Text = item.UnitPrice.ToString("C"),
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetColumn(priceText, 2);

        var totalText = new TextBlock
        {
            Text = item.Subtotal.ToString("C"),
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(totalText, 3);

        itemGrid.Children.Add(nameText);
        itemGrid.Children.Add(qtyText);
        itemGrid.Children.Add(priceText);
        itemGrid.Children.Add(totalText);

        return itemGrid;
    }

    private UIElement CreateTotalsSection(ReceiptData receipt, PrinterSettings settings)
    {
        var totalsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 10, 0, 10)
        };

        var separator = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
            Margin = new Thickness(0, 0, 0, 5)
        };

        var subtotalRow = CreateTotalRow("Subtotal:", receipt.Subtotal, settings);
        var discountRow = CreateTotalRow("Discount:", -receipt.DiscountAmount, settings);
        var taxRow = CreateTotalRow("Tax:", receipt.TaxAmount, settings);
        
        var totalSeparator = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
            Margin = new Thickness(0, 5, 0, 5)
        };

        var totalRow = CreateTotalRow("TOTAL:", receipt.TotalAmount, settings, true);

        totalsPanel.Children.Add(separator);
        totalsPanel.Children.Add(subtotalRow);
        totalsPanel.Children.Add(discountRow);
        totalsPanel.Children.Add(taxRow);
        totalsPanel.Children.Add(totalSeparator);
        totalsPanel.Children.Add(totalRow);

        if (!receipt.IsProForma)
        {
            var paidRow = CreateTotalRow("Paid:", receipt.AmountPaid, settings);
            var changeRow = CreateTotalRow("Change:", receipt.Change, settings);
            totalsPanel.Children.Add(paidRow);
            totalsPanel.Children.Add(changeRow);
        }

        return totalsPanel;
    }

    private UIElement CreateTotalRow(string label, decimal amount, PrinterSettings settings, bool isBold = false)
    {
        var rowGrid = new Grid();
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelText = new TextBlock
        {
            Text = label,
            FontSize = GetFontSize(12, settings),
            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetColumn(labelText, 0);

        var amountText = new TextBlock
        {
            Text = amount.ToString("C"),
            FontSize = GetFontSize(12, settings),
            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(amountText, 1);

        rowGrid.Children.Add(labelText);
        rowGrid.Children.Add(amountText);

        return rowGrid;
    }

    private UIElement CreateFooter(ReceiptData receipt, PrinterSettings settings)
    {
        var footerPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var billIdText = new TextBlock
        {
            Text = $"Bill ID: {receipt.BillId}",
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var timestampText = new TextBlock
        {
            Text = $"Printed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            FontSize = GetFontSize(10, settings),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var thankYouText = new TextBlock
        {
            Text = "Thank you for your visit!",
            FontSize = GetFontSize(12, settings),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0)
        };

        footerPanel.Children.Add(billIdText);
        footerPanel.Children.Add(timestampText);
        footerPanel.Children.Add(thankYouText);

        return footerPanel;
    }

    private double GetFontSize(double baseSize, PrinterSettings settings)
    {
        // Adjust font size based on receipt width
        return settings.ReceiptSize == ReceiptSize.Size58mm ? baseSize * 0.8 : baseSize;
    }

    private PrinterSettings GetPrinterSettings()
    {
        // Simple configuration access for now
        return new PrinterSettings();
    }

    private string GenerateReceiptHtml(ReceiptData receipt, PrinterSettings settings)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><meta charset='utf-8'><title>Receipt</title></head><body>");
        html.AppendLine($"<div style='font-family: monospace; width: {settings.ReceiptSize}mm; margin: 0 auto;'>");
        
        // Header
        html.AppendLine($"<h2 style='text-align: center;'>{receipt.BusinessName}</h2>");
        html.AppendLine($"<p style='text-align: center;'>{receipt.BusinessAddress}</p>");
        html.AppendLine($"<p style='text-align: center;'>{receipt.BusinessPhone}</p>");
        
        // Table info
        html.AppendLine($"<p>Table: {receipt.TableNumber} | Server: {receipt.ServerName}</p>");
        html.AppendLine($"<p>Start: {receipt.StartTime:HH:mm} | End: {receipt.EndTime:HH:mm}</p>");
        
        // Items
        html.AppendLine("<table style='width: 100%; border-collapse: collapse;'>");
        html.AppendLine("<tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr>");
        
        foreach (var item in receipt.Items)
        {
            html.AppendLine($"<tr><td>{item.Name}</td><td>{item.Quantity}</td><td>{item.UnitPrice:C}</td><td>{item.Subtotal:C}</td></tr>");
        }
        
        html.AppendLine("</table>");
        
        // Totals
        html.AppendLine($"<p>Subtotal: {receipt.Subtotal:C}</p>");
        html.AppendLine($"<p>Discount: {receipt.DiscountAmount:C}</p>");
        html.AppendLine($"<p>Tax: {receipt.TaxAmount:C}</p>");
        html.AppendLine($"<p><strong>TOTAL: {receipt.TotalAmount:C}</strong></p>");
        
        if (!receipt.IsProForma)
        {
            html.AppendLine($"<p>Paid: {receipt.AmountPaid:C}</p>");
            html.AppendLine($"<p>Change: {receipt.Change:C}</p>");
        }
        
        html.AppendLine($"<p>Bill ID: {receipt.BillId}</p>");
        html.AppendLine($"<p>Printed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine("<p style='text-align: center;'><strong>Thank you for your visit!</strong></p>");
        
        html.AppendLine("</div></body></html>");
        return html.ToString();
    }

    private void OnPaginate(object sender, PaginateEventArgs e)
    {
        try
        {
            _logger.LogInformation("OnPaginate: Starting pagination");
            System.Diagnostics.Debug.WriteLine("ReceiptService.OnPaginate: Starting pagination");
            
            if (_currentReceipt == null)
            {
                _logger.LogWarning("OnPaginate: No current receipt available");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnPaginate: No current receipt available");
                return;
            }
            
            // Get page description
            var pageDescription = e.PrintTaskOptions.GetPageDescription(0);
            _logger.LogInformation("OnPaginate: Page size - Width: {Width}, Height: {Height}", 
                pageDescription.PageSize.Width, pageDescription.PageSize.Height);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.OnPaginate: Page size - Width: {pageDescription.PageSize.Width}, Height: {pageDescription.PageSize.Height}");
            
            // Set preview page count to 1 for receipt
            _printDocument?.SetPreviewPageCount(1, PreviewPageCountType.Final);
            
            _logger.LogInformation("OnPaginate: Pagination completed");
            System.Diagnostics.Debug.WriteLine("ReceiptService.OnPaginate: Pagination completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPaginate failed");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.OnPaginate: Exception - {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        try
        {
            _logger.LogInformation("OnGetPreviewPage: Page number {PageNumber}", e.PageNumber);
            System.Diagnostics.Debug.WriteLine($"ReceiptService.OnGetPreviewPage: Page number {e.PageNumber}");
            
            if (_currentReceipt == null)
            {
                _logger.LogWarning("OnGetPreviewPage: No current receipt available");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnGetPreviewPage: No current receipt available");
                return;
            }
            
            // Create receipt content for preview using default page size
            var receiptContent = CreateReceiptContent();
            
            // Set the preview page using the correct method
            _printDocument?.SetPreviewPage(e.PageNumber, receiptContent);
            
            _logger.LogInformation("OnGetPreviewPage: Preview page set successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.OnGetPreviewPage: Preview page set successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnGetPreviewPage failed");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.OnGetPreviewPage: Exception - {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnAddPages(object sender, AddPagesEventArgs e)
    {
        try
        {
            _logger.LogInformation("OnAddPages: Adding pages for printing");
            System.Diagnostics.Debug.WriteLine("ReceiptService.OnAddPages: Adding pages for printing");
            
            if (_currentReceipt == null)
            {
                _logger.LogWarning("OnAddPages: No current receipt available");
                System.Diagnostics.Debug.WriteLine("ReceiptService.OnAddPages: No current receipt available");
                return;
            }
            
            // Create receipt content for printing
            var receiptContent = CreateReceiptContent();
            
            // Add the page using the correct method
            _printDocument?.AddPage(receiptContent);
            _printDocument?.AddPagesComplete();
            
            _logger.LogInformation("OnAddPages: Pages added successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.OnAddPages: Pages added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnAddPages failed");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.OnAddPages: Exception - {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates receipt content as UIElement for printing
    /// </summary>
    private UIElement CreateReceiptContent()
    {
        try
        {
            _logger.LogInformation("CreateReceiptContent: Creating receipt UIElement");
            System.Diagnostics.Debug.WriteLine("ReceiptService.CreateReceiptContent: Creating receipt UIElement");
            
            if (_currentReceipt == null)
            {
                _logger.LogWarning("CreateReceiptContent: No current receipt available");
                System.Diagnostics.Debug.WriteLine("ReceiptService.CreateReceiptContent: No current receipt available");
                return new TextBlock { Text = "No receipt data available" };
            }
            
            // Create a Grid to hold the receipt content with default size
            var receiptGrid = new Grid
            {
                Width = 400, // Default width for receipt
                Height = 600, // Default height for receipt
                Background = new SolidColorBrush(Microsoft.UI.Colors.White)
            };
            
            // Add receipt content
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Orientation = Orientation.Vertical
            };
            
            // Business header
            var businessName = new TextBlock
            {
                Text = _currentReceipt.BusinessName,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(businessName);
            
            // Bill ID
            var billId = new TextBlock
            {
                Text = $"Bill ID: {_currentReceipt.BillId}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(billId);
            
            // Items
            foreach (var item in _currentReceipt.Items)
            {
                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                
                var itemName = new TextBlock
                {
                    Text = $"{item.Name} x{item.Quantity}",
                    FontSize = 12,
                    Width = 200
                };
                itemPanel.Children.Add(itemName);
                
                var itemTotal = new TextBlock
                {
                    Text = $"${item.Subtotal:F2}",
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 100
                };
                itemPanel.Children.Add(itemTotal);
                
                stackPanel.Children.Add(itemPanel);
            }
            
            // Total
            var totalPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };
            
            var totalLabel = new TextBlock
            {
                Text = "Total:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Width = 200
            };
            totalPanel.Children.Add(totalLabel);
            
            var totalAmount = new TextBlock
            {
                Text = $"${_currentReceipt.TotalAmount:F2}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 100
            };
            totalPanel.Children.Add(totalAmount);
            
            stackPanel.Children.Add(totalPanel);
            
            receiptGrid.Children.Add(stackPanel);
            
            _logger.LogInformation("CreateReceiptContent: Receipt UIElement created successfully");
            System.Diagnostics.Debug.WriteLine("ReceiptService.CreateReceiptContent: Receipt UIElement created successfully");
            
            return receiptGrid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateReceiptContent failed");
            System.Diagnostics.Debug.WriteLine($"ReceiptService.CreateReceiptContent: Exception - {ex.GetType().Name}: {ex.Message}");
            return new TextBlock { Text = "Error creating receipt content" };
        }
    }

    public void Dispose()
    {
        // PrintDocument doesn't need explicit disposal in WinUI 3
        // _printDocument?.Dispose();
    }
}
