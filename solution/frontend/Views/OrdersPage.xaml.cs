using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using System;
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
                
                // Use PaymentPane instead of PaymentDialog
                if (App.PaneManager != null)
                {
                    // Get the registered PaymentPane
                    // Check if PaymentPane is visible
                    if (App.PaneManager.IsPaneVisible("PaymentPane"))
                    {
                        Log.Warning("PaymentPane is already visible");
                        return;
                    }
                    
                    // Show PaymentPane
                    await App.PaneManager.ShowPaneAsync("PaymentPane");
                    
                    // Initialize the pane with billing data
                    var paymentPane = App.PaneManager.GetPane<MagiDesk.Frontend.Panes.PaymentPane>("PaymentPane");
                    
                    if (paymentPane != null && !string.IsNullOrWhiteSpace(billingId) && !string.IsNullOrWhiteSpace(sessionId))
                    {
                        await paymentPane.InitializeAsync(billingId!, sessionId!, totalDue, Vm.Items);
                    }
                }
                else
                {
                    Log.Error("PaneManager not available");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open PaymentPane", ex);
            }
        }
    }
}
