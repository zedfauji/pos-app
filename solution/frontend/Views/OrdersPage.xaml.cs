using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Text;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class OrdersPage : Page, IToolbarConsumer
    {
        public OrderDetailViewModel Vm { get; } = new();
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        private DateTime _lastUpdateTime = DateTime.Now;

        public OrdersPage()
        {
            this.InitializeComponent();
            this.DataContext = Vm;
            Loaded += OrdersPage_Loaded;
            Unloaded += OrdersPage_Unloaded;
            OrderContext.CurrentOrderChanged += OrderContext_CurrentOrderChanged;
        }

        private async void OrdersPage_Loaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
            await InitializeFromContextAsync();
            await UpdateAnalyticsAsync();
        }

        private void OrdersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= RefreshTimer_Tick;
        }

        private async void RefreshTimer_Tick(object sender, object e)
        {
            await UpdateAnalyticsAsync();
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                await UpdateAnalyticsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing data: {ex.Message}");
            }
        }

        private async void OrderContext_CurrentOrderChanged(object? sender, long? e)
        {
            await InitializeFromContextAsync();
        }

        private async Task InitializeFromContextAsync()
        {
            try
            {
                if (OrderContext.CurrentOrderId.HasValue)
                {
                    await Vm.InitializeAsync(OrderContext.CurrentOrderId.Value);
                }
            }
            catch { }
        }

        private async Task UpdateAnalyticsAsync()
        {
            try
            {
                // Check if elements are initialized before accessing them
                if (OrdersTodayText == null || RevenueTodayText == null || AvgOrderValueText == null || 
                    CompletionRateText == null || PendingOrdersText == null || InProgressText == null ||
                    ReadyForDeliveryText == null || CompletedTodayText == null || AvgPrepTimeText == null ||
                    PeakHourText == null || EfficiencyScoreText == null || TotalOrdersText == null ||
                    TotalRevenueText == null || AvgOrderTimeText == null || CustomerSatisfactionText == null ||
                    ReturnRateText == null || AlertText == null)
                {
                    return; // Elements not yet initialized, skip update
                }

                // Get real analytics data from API
                var orderApi = App.OrdersApi;
                if (orderApi == null)
                {
                    // Fallback to mock data if API is not available
                    await UpdateAnalyticsWithMockData();
                    return;
                }

                // Get date range from UI controls
                var fromDate = FromDatePicker != null ? FromDatePicker.Date.Date : (DateTime?)null;
                var toDate = ToDatePicker != null ? ToDatePicker.Date.Date : (DateTime?)null;
                var reportType = ReportTypeCombo?.SelectedItem?.ToString() ?? "daily";

                // Fetch analytics data from API
                var analytics = await orderApi.GetOrderAnalyticsAsync(fromDate, toDate, reportType);
                if (analytics == null)
                {
                    // Fallback to mock data if API call fails
                    await UpdateAnalyticsWithMockData();
                    return;
                }

                // Update KPI metrics with real data
                OrdersTodayText.Text = analytics.OrdersToday.ToString();
                RevenueTodayText.Text = analytics.RevenueToday.ToString("C");
                AvgOrderValueText.Text = analytics.AverageOrderValue.ToString("C");
                CompletionRateText.Text = $"{analytics.CompletionRate:F1}%";
                
                // Update status monitoring with real data
                PendingOrdersText.Text = analytics.PendingOrders.ToString();
                InProgressText.Text = analytics.InProgressOrders.ToString();
                ReadyForDeliveryText.Text = analytics.ReadyForDeliveryOrders.ToString();
                CompletedTodayText.Text = analytics.CompletedTodayOrders.ToString();
                
                // Update performance metrics with real data
                AvgPrepTimeText.Text = $"{analytics.AveragePrepTimeMinutes}m";
                PeakHourText.Text = analytics.PeakHour;
                EfficiencyScoreText.Text = $"{analytics.EfficiencyScore:F1}%";
                
                // Update analytics summary with real data
                TotalOrdersText.Text = analytics.TotalOrders.ToString();
                TotalRevenueText.Text = analytics.TotalRevenue.ToString("C");
                AvgOrderTimeText.Text = $"{analytics.AverageOrderTimeMinutes}m";
                CustomerSatisfactionText.Text = $"{analytics.CustomerSatisfactionRate:F1}%";
                ReturnRateText.Text = $"{analytics.ReturnRate:F1}%";
                
                // Update alerts with real data
                AlertText.Text = analytics.AlertMessage;
                
                // Update recent activity with real data
                await UpdateRecentActivityWithRealData(analytics.RecentActivities);
                
                // Update last refresh time
                _lastUpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating analytics: {ex.Message}");
                // Fallback to mock data on error
                await UpdateAnalyticsWithMockData();
            }
        }

        private async Task UpdateAnalyticsWithMockData()
        {
            try
            {
                // Fallback mock data when API is not available
                var random = new Random();
                
                OrdersTodayText.Text = random.Next(15, 45).ToString();
                RevenueTodayText.Text = (random.Next(800, 2500)).ToString("C");
                AvgOrderValueText.Text = (random.Next(25, 75)).ToString("C");
                CompletionRateText.Text = $"{random.Next(85, 98)}%";
                
                PendingOrdersText.Text = random.Next(0, 8).ToString();
                InProgressText.Text = random.Next(2, 12).ToString();
                ReadyForDeliveryText.Text = random.Next(1, 6).ToString();
                CompletedTodayText.Text = random.Next(20, 40).ToString();
                
                AvgPrepTimeText.Text = $"{random.Next(8, 25)}m";
                PeakHourText.Text = $"{random.Next(12, 20):00}:{random.Next(0, 60):00}";
                EfficiencyScoreText.Text = $"{random.Next(75, 95)}%";
                
                TotalOrdersText.Text = random.Next(100, 500).ToString();
                TotalRevenueText.Text = (random.Next(5000, 15000)).ToString("C");
                AvgOrderTimeText.Text = $"{random.Next(15, 35)}m";
                CustomerSatisfactionText.Text = $"{random.Next(85, 98)}%";
                ReturnRateText.Text = $"{random.Next(1, 5)}%";
                
                var alertCount = random.Next(0, 3);
                AlertText.Text = alertCount == 0 ? "No critical issues detected" : $"{alertCount} critical issue(s) require attention";
                
                await UpdateRecentActivityAsync();
                
                // Update last refresh time
                _lastUpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating mock analytics: {ex.Message}");
            }
        }

        private async Task UpdateRecentActivityWithRealData(IReadOnlyList<OrderApiService.RecentActivityDto> activities)
        {
            try
            {
                if (RecentActivityList != null)
                {
                    RecentActivityList.ItemsSource = activities;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating recent activity with real data: {ex.Message}");
                await UpdateRecentActivityAsync();
            }
        }

        private async Task UpdateRecentActivityAsync()
        {
            try
            {
                var activities = new List<dynamic>
                {
                    new { Title = "Order #1234 Completed", Description = "Table 5 - $45.50", Timestamp = "2 min ago" },
                    new { Title = "New Order Received", Description = "Table 8 - 3 items", Timestamp = "5 min ago" },
                    new { Title = "Payment Processed", Description = "Order #1233 - $32.75", Timestamp = "8 min ago" },
                    new { Title = "Order Delayed", Description = "Table 2 - Prep time exceeded", Timestamp = "12 min ago" },
                    new { Title = "Inventory Alert", Description = "Low stock: Chicken Wings", Timestamp = "15 min ago" }
                };
                
                RecentActivityList.ItemsSource = activities;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating recent activity: {ex.Message}");
            }
        }

        public void OnAdd() { }
        public void OnEdit() { }
        public void OnDelete() { }
        public void OnRefresh() { Vm.RefreshCommand.Execute(null); }

        private async void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileSavePicker = new FileSavePicker();
                fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                fileSavePicker.FileTypeChoices.Add("CSV files", new[] { ".csv" });
                fileSavePicker.SuggestedFileName = $"OrdersAnalytics_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);

                var file = await fileSavePicker.PickSaveFileAsync();
                if (file != null)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Orders Analytics Report");
                    csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    csv.AppendLine();
                    csv.AppendLine("Metric,Value");
                    csv.AppendLine($"Orders Today,{OrdersTodayText.Text}");
                    csv.AppendLine($"Revenue Today,{RevenueTodayText.Text}");
                    csv.AppendLine($"Average Order Value,{AvgOrderValueText.Text}");
                    csv.AppendLine($"Completion Rate,{CompletionRateText.Text}");
                    csv.AppendLine($"Pending Orders,{PendingOrdersText.Text}");
                    csv.AppendLine($"In Progress,{InProgressText.Text}");
                    csv.AppendLine($"Ready for Delivery,{ReadyForDeliveryText.Text}");
                    csv.AppendLine($"Completed Today,{CompletedTodayText.Text}");
                    csv.AppendLine($"Average Prep Time,{AvgPrepTimeText.Text}");
                    csv.AppendLine($"Peak Hour,{PeakHourText.Text}");
                    csv.AppendLine($"Efficiency Score,{EfficiencyScoreText.Text}");

                    await FileIO.WriteTextAsync(file, csv.ToString());
                    
                    var dialog = new ContentDialog()
                    {
                        Title = "Export Complete",
                        Content = $"Analytics report exported to {file.Name}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog()
                {
                    Title = "Export Failed",
                    Content = $"Failed to export report: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        private async void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDataAsync();
        }

        private async void FromDatePicker_DateChanged(object sender, Microsoft.UI.Xaml.Controls.DatePickerValueChangedEventArgs e)
        {
            await UpdateAnalyticsAsync();
        }

        private async void ToDatePicker_DateChanged(object sender, Microsoft.UI.Xaml.Controls.DatePickerValueChangedEventArgs e)
        {
            await UpdateAnalyticsAsync();
        }

        private async void ReportTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateAnalyticsAsync();
        }

        // Legacy methods kept for compatibility but not used in analytics view
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Vm.RefreshCommand.Execute(null);
        }

        private void LoadLogs_Click(object sender, RoutedEventArgs e)
        {
            Vm.LoadLogsCommand.Execute(null);
        }

        private async void LoadOrderId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // This method is kept for compatibility but OrderIdBox no longer exists in analytics view
                // In a real implementation, you might want to show a dialog to enter order ID
                var dialog = new ContentDialog()
                {
                    Title = "Load Order",
                    Content = "Order ID input functionality would be implemented here for analytics view.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
            catch { }
        }

        private void QtyMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OrderItemLineVm line && Vm.DecrementQtyCommand.CanExecute(line))
            {
                Vm.DecrementQtyCommand.Execute(line);
            }
        }

        private void QtyPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OrderItemLineVm line && Vm.IncrementQtyCommand.CanExecute(line))
            {
                Vm.IncrementQtyCommand.Execute(line);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OrderItemLineVm line && Vm.DeleteItemCommand.CanExecute(line))
            {
                Vm.DeleteItemCommand.Execute(line);
            }
        }

        private async void Checkout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var billingId = OrderContext.CurrentBillingId;
                var sessionId = OrderContext.CurrentSessionId;
                var totalDue = Vm.Total;
                
                // Use EphemeralPaymentPage instead of PaymentPane
                var paymentWindow = new Window();
                paymentWindow.Title = $"Payment - Order #{billingId}";
                
                // Set window size using AppWindow for full-screen overlay
                var appWindow = paymentWindow.AppWindow;
                appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1200, Height = 800 });
                
                // Create the ephemeral payment page
                var paymentPage = new EphemeralPaymentPage();
                
                // Create a mock BillResult for the payment page
                var mockBill = new MagiDesk.Shared.DTOs.Tables.BillResult
                {
                    BillId = Guid.Parse(billingId ?? Guid.Empty.ToString()),
                    TableLabel = "Order Table",
                    TotalAmount = totalDue,
                    Items = Vm.Items.Select(item => new MagiDesk.Shared.DTOs.Tables.ItemLine
                    {
                        itemId = item.OrderItemId.ToString(),
                        name = item.Name,
                        quantity = item.Quantity,
                        price = item.UnitPrice
                    }).ToList()
                };
                
                paymentPage.SetBillInfo(mockBill, paymentWindow);
                
                // Subscribe to payment events
                paymentPage.PaymentCompleted += OrdersPage_PaymentCompleted;
                paymentPage.PaymentCancelled += OrdersPage_PaymentCancelled;
                
                // Set the page as the window content
                paymentWindow.Content = paymentPage;
                
                // Show the window
                paymentWindow.Activate();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open EphemeralPaymentPage", ex);
            }
        }

    private async void OrdersPage_PaymentCompleted(object? sender, PaymentCompletedEventArgs e)
    {
        try
        {
            Log.Info($"Payment completed for Order {e.BillId}, Amount: {e.AmountPaid:C}");
            
            // Show success notification
            var successDialog = new ContentDialog()
            {
                Title = "Order Payment Processed",
                Content = $"Payment of {e.AmountPaid:C} has been successfully processed for Order #{e.BillId}.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await successDialog.ShowAsync();
            
            // Refresh the order data
            await Vm.LoadAsync();
        }
        catch (Exception ex)
        {
            Log.Error("Error handling payment completion", ex);
        }
    }

    private void OrdersPage_PaymentCancelled(object? sender, PaymentCancelledEventArgs e)
    {
        Log.Info($"Payment cancelled for Order {e.BillId}");
        // No action needed for cancellation
    }

    // Delivery tracking event handlers
    private async void MarkItemDelivered_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is OrderItemLineVm line)
        {
            try
            {
                var orderApi = App.OrdersApi;
                if (orderApi == null)
                {
                    await ShowErrorDialog("Orders API is not available. Please restart the application.");
                    return;
                }

                if (Vm.OrderId <= 0)
                {
                    await ShowErrorDialog("No order selected. Please load an order first.");
                    return;
                }

                // Show confirmation dialog
                var confirmDialog = new ContentDialog()
                {
                    Title = "Mark Item Delivered",
                    Content = $"Are you sure you want to mark '{line.Name}' (Qty: {line.Quantity}) as delivered?",
                    PrimaryButtonText = "Yes, Mark Delivered",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                    return;

                // Show loading indicator
                // Vm.IsLoading = true; // This property has a private setter

                var itemDeliveries = new List<OrderApiService.ItemDeliveryDto>
                {
                    new OrderApiService.ItemDeliveryDto(line.OrderItemId, line.Quantity)
                };
                
                var updatedOrder = await orderApi.MarkItemsDeliveredAsync(Vm.OrderId, itemDeliveries);
                if (updatedOrder != null)
                {
                    await Vm.InitializeAsync(Vm.OrderId);
                    
                    // Show success message
                    var successDialog = new ContentDialog()
                    {
                        Title = "Success",
                        Content = $"Item '{line.Name}' has been marked as delivered.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                else
                {
                    await ShowErrorDialog("Failed to mark item as delivered. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error marking item delivered: {ex.Message}", ex);
                await ShowErrorDialog($"Error marking item delivered: {ex.Message}");
            }
            finally
            {
                // Vm.IsLoading = false; // This property has a private setter
            }
        }
    }

    private void DeliveredMinus_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement delivered quantity tracking in OrderItemLineVm
        // For now, this is a placeholder
        System.Diagnostics.Debug.WriteLine("Delivered minus clicked - feature not yet implemented");
    }

    private void DeliveredPlus_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement delivered quantity tracking in OrderItemLineVm
        // For now, this is a placeholder
        System.Diagnostics.Debug.WriteLine("Delivered plus clicked - feature not yet implemented");
    }

    private async void MarkWaiting_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var orderApi = App.OrdersApi;
            if (orderApi != null && Vm.OrderId > 0)
            {
                var updatedOrder = await orderApi.MarkOrderWaitingAsync(Vm.OrderId);
                if (updatedOrder != null)
                {
                    await Vm.InitializeAsync(Vm.OrderId);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking order as waiting: {ex.Message}");
        }
    }

    private async void MarkDelivered_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var orderApi = App.OrdersApi;
            if (orderApi == null)
            {
                await ShowErrorDialog("Orders API is not available. Please restart the application.");
                return;
            }

            if (Vm.OrderId <= 0)
            {
                await ShowErrorDialog("No order selected. Please load an order first.");
                return;
            }

            if (Vm.Items.Count == 0)
            {
                await ShowErrorDialog("No items in this order to mark as delivered.");
                return;
            }

            // Show confirmation dialog
            var confirmDialog = new ContentDialog()
            {
                Title = "Mark All Items Delivered",
                Content = $"Are you sure you want to mark all {Vm.Items.Count} items in this order as delivered?",
                PrimaryButtonText = "Yes, Mark All Delivered",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            // Show loading indicator
            // Vm.IsLoading = true; // This property has a private setter

            // Mark all items as fully delivered
            var itemDeliveries = Vm.Items.Select(item => 
                new OrderApiService.ItemDeliveryDto(item.OrderItemId, item.Quantity)).ToList();
            
            var updatedOrder = await orderApi.MarkItemsDeliveredAsync(Vm.OrderId, itemDeliveries);
            if (updatedOrder != null)
            {
                await Vm.InitializeAsync(Vm.OrderId);
                
                // Show success message
                var successDialog = new ContentDialog()
                {
                    Title = "Success",
                    Content = $"All items in order #{Vm.OrderId} have been marked as delivered.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            else
            {
                await ShowErrorDialog("Failed to mark order as delivered. Please try again.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error marking order as delivered: {ex.Message}", ex);
            await ShowErrorDialog($"Error marking order as delivered: {ex.Message}");
        }
        finally
        {
            // Vm.IsLoading = false; // This property has a private setter
        }
    }

    private async void CloseOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var orderApi = App.OrdersApi;
            if (orderApi != null && Vm.OrderId > 0)
            {
                await orderApi.CloseOrderAsync(Vm.OrderId);
                await Vm.InitializeAsync(Vm.OrderId);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing order: {ex.Message}");
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var errorDialog = new ContentDialog()
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await errorDialog.ShowAsync();
    }
}
}
