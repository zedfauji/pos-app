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

        public ReceiptPrintService(Panel printingPanel, DispatcherQueue dispatcherQueue, Window? window = null)
        {
            _printingPanel = printingPanel ?? throw new ArgumentNullException(nameof(printingPanel));
            _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
            _window = window;
        }

        public async Task PrintReceiptAsync(PrintReceiptData receiptData, string title = "Receipt")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReceiptPrintService));

            try
            {
                // Initialize print manager if not already done
                if (_printManager == null)
                {
                    try
                    {
                        // CRITICAL FIX: Ensure PrintManager.GetForCurrentView() is called from UI thread
                        // This is a COM interop call that requires UI thread context
                        var dispatcherQueue = _dispatcherQueue ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                        if (dispatcherQueue == null)
                        {
                            throw new InvalidOperationException("No DispatcherQueue available. PrintManager.GetForCurrentView() requires UI thread context.");
                        }
                        
                        // Use DispatcherQueue to ensure we're on the UI thread
                        var tcs = new TaskCompletionSource<PrintManager?>();
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            try
                            {
                                _printManager = PrintManager.GetForCurrentView();
                                if (_printManager == null)
                                {
                                    tcs.SetException(new InvalidOperationException("PrintManager.GetForCurrentView() returned null"));
                                    return;
                                }
                                _printManager.PrintTaskRequested += OnPrintTaskRequested;
                                tcs.SetResult(_printManager);
                            }
                            catch (Exception ex)
                            {
                                tcs.SetException(ex);
                            }
                        });
                        
                        _printManager = await tcs.Task;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to initialize PrintManager: {ex.Message}", ex);
                    }
                }

                if (_printDocument == null)
                {
                    _printDocument = new PrintDocument();
                    _printDocument.Paginate += OnPaginate;
                    _printDocument.GetPreviewPage += OnGetPreviewPage;
                    _printDocument.AddPages += OnAddPages;
                }

                // Store the receipt data for printing
                _currentReceipt = receiptData;
                _printTitle = title;

                // Show print UI - CRITICAL FIX: Ensure this COM interop call is on UI thread
                var showPrintDispatcherQueue = _dispatcherQueue ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (showPrintDispatcherQueue == null)
                {
                    throw new InvalidOperationException("No DispatcherQueue available. PrintManager.ShowPrintUIAsync() requires UI thread context.");
                }
                
                var showPrintTcs = new TaskCompletionSource<bool>();
                showPrintDispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await PrintManager.ShowPrintUIAsync();
                        showPrintTcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        showPrintTcs.SetException(ex);
                    }
                });
                await showPrintTcs.Task;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to print receipt: {ex.Message}", ex);
            }
        }

        public async Task PrintMultipleReceiptsAsync(IEnumerable<PrintReceiptData> receipts, string title = "Receipts")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReceiptPrintService));

            try
            {
                // For multiple receipts, we'll print them one by one
                foreach (var receipt in receipts)
                {
                    await PrintReceiptAsync(receipt, title);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to print receipts: {ex.Message}", ex);
            }
        }

        private void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            if (_currentReceipt == null) return;

            var printTask = args.Request.CreatePrintTask(_printTitle, async (sourceArgs) =>
            {
                sourceArgs.SetSource(_printDocument?.DocumentSource);
            });
        }

        private void OnPaginate(object sender, PaginateEventArgs e)
        {
            if (_currentReceipt == null) return;

            var printDocument = sender as PrintDocument;
            if (printDocument == null) return;

            // Create the receipt element
            var receiptElement = CreateReceiptElement(_currentReceipt);
            
            // Add to printing panel
            _printingPanel.Children.Clear();
            _printingPanel.Children.Add(receiptElement);

            // Set print options
            printDocument.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            var printDocument = sender as PrintDocument;
            if (printDocument == null) return;

            printDocument.SetPreviewPage(e.PageNumber, _printingPanel);
        }

        private void OnAddPages(object sender, AddPagesEventArgs e)
        {
            var printDocument = sender as PrintDocument;
            if (printDocument == null) return;

            printDocument.AddPage(_printingPanel);
            printDocument.AddPagesComplete();
        }

        private FrameworkElement CreateReceiptElement(PrintReceiptData receiptData)
        {
            var mainGrid = new Grid
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                Padding = new Thickness(20)
            };

            // Define rows for header, content, and footer
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Store info
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Items
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Totals
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // Header
            var headerText = new TextBlock
            {
                Text = receiptData.Header ?? "RECEIPT",
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(headerText, 0);
            mainGrid.Children.Add(headerText);

            // Store information
            var storeInfoPanel = CreateStoreInfoPanel(receiptData);
            Grid.SetRow(storeInfoPanel, 1);
            mainGrid.Children.Add(storeInfoPanel);

            // Items
            var itemsPanel = CreateItemsPanel(receiptData);
            Grid.SetRow(itemsPanel, 2);
            mainGrid.Children.Add(itemsPanel);

            // Totals
            var totalsPanel = CreateTotalsPanel(receiptData);
            Grid.SetRow(totalsPanel, 3);
            mainGrid.Children.Add(totalsPanel);

            // Footer
            var footerPanel = CreateFooterPanel(receiptData);
            Grid.SetRow(footerPanel, 4);
            mainGrid.Children.Add(footerPanel);

            return mainGrid;
        }

        private Panel CreateStoreInfoPanel(PrintReceiptData receiptData)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            if (!string.IsNullOrEmpty(receiptData.StoreName))
            {
                var storeNameText = new TextBlock
                {
                    Text = receiptData.StoreName,
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                panel.Children.Add(storeNameText);
            }

            if (!string.IsNullOrEmpty(receiptData.StoreAddress))
            {
                var addressText = new TextBlock
                {
                    Text = receiptData.StoreAddress,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                panel.Children.Add(addressText);
            }

            if (!string.IsNullOrEmpty(receiptData.StorePhone))
            {
                var phoneText = new TextBlock
                {
                    Text = receiptData.StorePhone,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                panel.Children.Add(phoneText);
            }

            // Receipt info
            var receiptInfoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            if (!string.IsNullOrEmpty(receiptData.ReceiptNumber))
            {
                var receiptNumberText = new TextBlock
                {
                    Text = $"Receipt #: {receiptData.ReceiptNumber}",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 20, 0)
                };
                receiptInfoPanel.Children.Add(receiptNumberText);
            }

            if (receiptData.Date.HasValue)
            {
                var dateText = new TextBlock
                {
                    Text = $"Date: {receiptData.Date.Value:yyyy-MM-dd HH:mm}",
                    FontSize = 12
                };
                receiptInfoPanel.Children.Add(dateText);
            }

            panel.Children.Add(receiptInfoPanel);

            return panel;
        }

        private Panel CreateItemsPanel(PrintReceiptData receiptData)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            if (receiptData.Items?.Any() == true)
            {
                // Header
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var itemHeader = new TextBlock
                {
                    Text = "Item",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 12
                };
                Grid.SetColumn(itemHeader, 0);
                headerGrid.Children.Add(itemHeader);

                var qtyHeader = new TextBlock
                {
                    Text = "Qty",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(qtyHeader, 1);
                headerGrid.Children.Add(qtyHeader);

                var priceHeader = new TextBlock
                {
                    Text = "Price",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(priceHeader, 2);
                headerGrid.Children.Add(priceHeader);

                panel.Children.Add(headerGrid);

                // Separator line
                var separator = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
                    Margin = new Thickness(0, 5, 0, 5)
                };
                panel.Children.Add(separator);

                // Items
                foreach (var item in receiptData.Items)
                {
                    var itemGrid = new Grid();
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var itemName = new TextBlock
                    {
                        Text = item.Name ?? "Unknown Item",
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    Grid.SetColumn(itemName, 0);
                    itemGrid.Children.Add(itemName);

                    var itemQty = new TextBlock
                    {
                        Text = item.Quantity.ToString(),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    Grid.SetColumn(itemQty, 1);
                    itemGrid.Children.Add(itemQty);

                    var itemPrice = new TextBlock
                    {
                        Text = item.Price.ToString("C"),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetColumn(itemPrice, 2);
                    itemGrid.Children.Add(itemPrice);

                    panel.Children.Add(itemGrid);
                }
            }

            return panel;
        }

        private Panel CreateTotalsPanel(PrintReceiptData receiptData)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 10)
            };

            // Separator line
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(separator);

            // Subtotal
            if (receiptData.Subtotal.HasValue)
            {
                var subtotalGrid = new Grid();
                subtotalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                subtotalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var subtotalLabel = new TextBlock
                {
                    Text = "Subtotal:",
                    FontSize = 12
                };
                Grid.SetColumn(subtotalLabel, 0);
                subtotalGrid.Children.Add(subtotalLabel);

                var subtotalValue = new TextBlock
                {
                    Text = receiptData.Subtotal.Value.ToString("C"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(subtotalValue, 1);
                subtotalGrid.Children.Add(subtotalValue);

                panel.Children.Add(subtotalGrid);
            }

            // Tax
            if (receiptData.Tax.HasValue)
            {
                var taxGrid = new Grid();
                taxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                taxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var taxLabel = new TextBlock
                {
                    Text = "Tax:",
                    FontSize = 12
                };
                Grid.SetColumn(taxLabel, 0);
                taxGrid.Children.Add(taxLabel);

                var taxValue = new TextBlock
                {
                    Text = receiptData.Tax.Value.ToString("C"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(taxValue, 1);
                taxGrid.Children.Add(taxValue);

                panel.Children.Add(taxGrid);
            }

            // Total
            if (receiptData.Total.HasValue)
            {
                var totalGrid = new Grid();
                totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var totalLabel = new TextBlock
                {
                    Text = "Total:",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                Grid.SetColumn(totalLabel, 0);
                totalGrid.Children.Add(totalLabel);

                var totalValue = new TextBlock
                {
                    Text = receiptData.Total.Value.ToString("C"),
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(totalValue, 1);
                totalGrid.Children.Add(totalValue);

                panel.Children.Add(totalGrid);
            }

            return panel;
        }

        private Panel CreateFooterPanel(PrintReceiptData receiptData)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 20, 0, 0)
            };

            if (!string.IsNullOrEmpty(receiptData.Footer))
            {
                var footerText = new TextBlock
                {
                    Text = receiptData.Footer,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                panel.Children.Add(footerText);
            }

            // Thank you message
            var thankYouText = new TextBlock
            {
                Text = "Thank you for your business!",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            panel.Children.Add(thankYouText);

            return panel;
        }

        /// <summary>
        /// Test method to verify printing functionality with sample data
        /// </summary>
        public async Task TestPrintingAsync()
        {
            var testReceipt = new PrintReceiptData
            {
                Header = "TEST RECEIPT",
                StoreName = "Test Store",
                StoreAddress = "123 Test Street, Test City",
                StorePhone = "(555) 123-4567",
                ReceiptNumber = "TEST-001",
                Date = DateTime.Now,
                Items = new List<PrintReceiptItem>
                {
                    new PrintReceiptItem { Name = "Test Item 1", Quantity = 2, Price = 10.50m },
                    new PrintReceiptItem { Name = "Test Item 2", Quantity = 1, Price = 25.00m },
                    new PrintReceiptItem { Name = "Test Item 3", Quantity = 3, Price = 5.75m }
                },
                Subtotal = 52.75m,
                Tax = 5.28m,
                Total = 58.03m,
                Footer = "Thank you for testing our printing system!"
            };

            await PrintReceiptAsync(testReceipt, "Test Receipt");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_printManager != null)
                {
                    _printManager.PrintTaskRequested -= OnPrintTaskRequested;
                }
                if (_printDocument != null)
                {
                    _printDocument.Paginate -= OnPaginate;
                    _printDocument.GetPreviewPage -= OnGetPreviewPage;
                    _printDocument.AddPages -= OnAddPages;
                }
                _printDocument = null;
                _printManager = null;
                _disposed = true;
            }
        }
    }
}
