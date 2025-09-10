using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using WinRT.Interop;

namespace MagiDesk.Frontend.Services
{
    public class ReceiptPrintService : IDisposable
    {
        private PrintManager? _printManager;
        private PrintDocument? _printDocument;
        private readonly Panel _printingPanel;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly Window? _window;
        private bool _disposed = false;
        private PrintReceiptData? _currentReceipt;
        private string _printTitle = "Receipt";
        private bool _isInitialized = false;

        public ReceiptPrintService(Panel printingPanel, DispatcherQueue dispatcherQueue, Window? window = null)
        {
            _printingPanel = printingPanel ?? throw new ArgumentNullException(nameof(printingPanel));
            _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
            _window = window;
        }

        /// <summary>
        /// CRITICAL: Initialize printing system with proper error handling and thread safety
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // CRITICAL: Ensure we're on the UI thread for all COM interop calls
                var initTcs = new TaskCompletionSource<bool>();
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        // CRITICAL: Ensure window is active and has proper context
                        if (_window != null)
                        {
                            // Activate window to ensure proper context
                            _window.Activate();
                            
                            // CRITICAL: Small delay to ensure window activation completes
                            await Task.Delay(100);
                        }

                        // CRITICAL: Initialize PrintManager with proper error handling
                        _printManager = PrintManager.GetForCurrentView();
                        if (_printManager == null)
                        {
                            throw new InvalidOperationException("PrintManager.GetForCurrentView() returned null - window context may not be properly set");
                        }

                        // CRITICAL: Register for print task requests
                        _printManager.PrintTaskRequested += OnPrintTaskRequested;

                        // CRITICAL: Create PrintDocument with proper event handling
                        _printDocument = new PrintDocument();
                        _printDocument.Paginate += OnPaginate;
                        _printDocument.GetPreviewPage += OnGetPreviewPage;
                        _printDocument.AddPages += OnAddPages;

                        _isInitialized = true;
                        initTcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        initTcs.SetException(new InvalidOperationException($"PrintManager initialization failed: {ex.Message}", ex));
                    }
                });

                await initTcs.Task;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize printing system: {ex.Message}", ex);
            }
        }

        public async Task<bool> PrintReceiptAsync(PrintReceiptData receiptData, string title)
        {
            try
            {
                // CRITICAL: Ensure initialization before printing
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

                _currentReceipt = receiptData;
                _printTitle = title;

                // CRITICAL: Ensure we're on the UI thread for all COM interop calls
                var printTcs = new TaskCompletionSource<bool>();
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        // CRITICAL: Ensure window is active before showing print UI
                        if (_window != null)
                        {
                            _window.Activate();
                            await Task.Delay(50); // Small delay for window activation
                        }

                        // CRITICAL: Show print UI with proper error handling
                        await PrintManager.ShowPrintUIAsync();
                        printTcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Print UI error: {ex.Message}");
                        printTcs.SetException(ex);
                    }
                });

                return await printTcs.Task;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to print receipt: {ex.Message}", ex);
            }
        }

        private void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            try
            {
                // CRITICAL: Create print task with proper error handling
                var printTask = args.Request.CreatePrintTask(_printTitle, sourceRequested =>
                {
                    try
                    {
                        sourceRequested.SetSource(_printDocument?.DocumentSource);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Print task source error: {ex.Message}");
                    }
                });

                // CRITICAL: Handle print task completion
                printTask.Completed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Print task completed: {e.Completion}");
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Print task creation failed: {ex.Message}");
            }
        }

        private void OnPaginate(object sender, PaginateEventArgs e)
        {
            try
            {
                // CRITICAL: Set page count with proper error handling
                _printDocument?.SetPreviewPageCount(1, PreviewPageCountType.Final);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pagination failed: {ex.Message}");
            }
        }

        private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            try
            {
                if (_currentReceipt != null)
                {
                    // CRITICAL: Create receipt visual with proper error handling
                    var receiptVisual = CreateReceiptVisual(_currentReceipt);
                    _printDocument?.SetPreviewPage(e.PageNumber, receiptVisual);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview page generation failed: {ex.Message}");
            }
        }

        private void OnAddPages(object sender, AddPagesEventArgs e)
        {
            try
            {
                if (_currentReceipt != null)
                {
                    // CRITICAL: Add pages with proper error handling
                    var receiptVisual = CreateReceiptVisual(_currentReceipt);
                    _printDocument?.AddPage(receiptVisual);
                }
                _printDocument?.AddPagesComplete();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Add pages failed: {ex.Message}");
            }
        }

        private FrameworkElement CreateReceiptVisual(PrintReceiptData receiptData)
        {
            try
            {
                var receiptGrid = new Grid
                {
                    Width = 300, // Receipt width
                    Background = new SolidColorBrush(Microsoft.UI.Colors.White)
                };

                var stackPanel = new StackPanel
                {
                    Margin = new Thickness(10)
                };

                // Business name
                if (!string.IsNullOrEmpty(receiptData.BusinessName))
                {
                    var businessNameText = new TextBlock
                    {
                        Text = receiptData.BusinessName,
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    stackPanel.Children.Add(businessNameText);
                }

                // Receipt type
                var receiptTypeText = new TextBlock
                {
                    Text = receiptData.IsProForma ? "PRO FORMA RECEIPT" : "RECEIPT",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stackPanel.Children.Add(receiptTypeText);

                // Bill ID
                var billIdText = new TextBlock
                {
                    Text = $"Bill ID: {receiptData.BillId}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(billIdText);

                // Date
                var dateText = new TextBlock
                {
                    Text = $"Date: {receiptData.Date:yyyy-MM-dd HH:mm}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stackPanel.Children.Add(dateText);

                // Items
                if (receiptData.Items != null && receiptData.Items.Any())
                {
                    var itemsHeader = new TextBlock
                    {
                        Text = "Items:",
                        FontSize = 12,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    stackPanel.Children.Add(itemsHeader);

                    foreach (var item in receiptData.Items)
                    {
                        var itemText = new TextBlock
                        {
                            Text = $"{item.Name} x{item.Quantity} @ {item.UnitPrice:C} = {item.LineTotal:C}",
                            FontSize = 11,
                            Margin = new Thickness(0, 0, 0, 2)
                        };
                        stackPanel.Children.Add(itemText);
                    }

                    // Separator line
                    var separator = new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
                        Margin = new Thickness(0, 10, 0, 10)
                    };
                    stackPanel.Children.Add(separator);
                }

                // Totals
                if (receiptData.DiscountAmount > 0)
                {
                    var discountText = new TextBlock
                    {
                        Text = $"Discount: -{receiptData.DiscountAmount:C}",
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 2)
                    };
                    stackPanel.Children.Add(discountText);
                }

                if (receiptData.TaxAmount > 0)
                {
                    var taxText = new TextBlock
                    {
                        Text = $"Tax: {receiptData.TaxAmount:C}",
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 2)
                    };
                    stackPanel.Children.Add(taxText);
                }

                var totalText = new TextBlock
                {
                    Text = $"Total: {receiptData.TotalAmount:C}",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Margin = new Thickness(0, 5, 0, 10)
                };
                stackPanel.Children.Add(totalText);

                // Footer
                if (!string.IsNullOrEmpty(receiptData.FooterText))
                {
                    var footerText = new TextBlock
                    {
                        Text = receiptData.FooterText,
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    stackPanel.Children.Add(footerText);
                }

                receiptGrid.Children.Add(stackPanel);
                return receiptGrid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateReceiptVisual error: {ex.Message}");
                // Return a simple error visual
                return new TextBlock { Text = "Error creating receipt visual", FontSize = 12 };
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // CRITICAL: Proper cleanup with error handling
                    if (_printManager != null)
                    {
                        _printManager.PrintTaskRequested -= OnPrintTaskRequested;
                        _printManager = null;
                    }

                    if (_printDocument != null)
                    {
                        _printDocument.Paginate -= OnPaginate;
                        _printDocument.GetPreviewPage -= OnGetPreviewPage;
                        _printDocument.AddPages -= OnAddPages;
                        _printDocument = null;
                    }

                    _isInitialized = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Dispose error: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }

    // Data structures for receipt printing
    public class PrintReceiptData
    {
        public string BusinessName { get; set; } = string.Empty;
        public string BillId { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsProForma { get; set; } = false;
        public List<PrintReceiptItem> Items { get; set; } = new List<PrintReceiptItem>();
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;
        public string FooterText { get; set; } = string.Empty;
    }

    public class PrintReceiptItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0;
        public decimal LineTotal { get; set; } = 0;
    }
}