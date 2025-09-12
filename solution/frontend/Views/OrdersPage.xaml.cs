using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class OrdersPage : Page, IToolbarConsumer
    {
        public OrderDetailViewModel Vm { get; } = new();

        public OrdersPage()
        {
            this.InitializeComponent();
            this.DataContext = Vm;
            Loaded += OrdersPage_Loaded;
            OrderContext.CurrentOrderChanged += OrderContext_CurrentOrderChanged;
        }

        private async void OrdersPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeFromContextAsync();
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

        public void OnAdd() { }
        public void OnEdit() { }
        public void OnDelete() { }
        public void OnRefresh() { Vm.RefreshCommand.Execute(null); }

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
                var val = OrderIdBox?.Value;
                if (val.HasValue && val.Value > 0)
                {
                    OrderContext.CurrentOrderId = (long)val.Value;
                    await InitializeFromContextAsync();
                }
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
                if (orderApi != null && Vm.OrderId > 0)
                {
                    var itemDeliveries = new List<OrderApiService.ItemDeliveryDto>
                    {
                        new OrderApiService.ItemDeliveryDto(line.OrderItemId, line.Quantity)
                    };
                    
                    var updatedOrder = await orderApi.MarkItemsDeliveredAsync(Vm.OrderId, itemDeliveries);
                    if (updatedOrder != null)
                    {
                        await Vm.InitializeAsync(Vm.OrderId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking item delivered: {ex.Message}");
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
            if (orderApi != null && Vm.OrderId > 0)
            {
                // Mark all items as fully delivered
                var itemDeliveries = Vm.Items.Select(item => 
                    new OrderApiService.ItemDeliveryDto(item.OrderItemId, item.Quantity)).ToList();
                
                var updatedOrder = await orderApi.MarkItemsDeliveredAsync(Vm.OrderId, itemDeliveries);
                if (updatedOrder != null)
                {
                    await Vm.InitializeAsync(Vm.OrderId);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking order as delivered: {ex.Message}");
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
}
}
