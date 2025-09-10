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
                var dlg = new Dialogs.PaymentDialog();
                if (dlg.DataContext is ViewModels.PaymentViewModel pvm)
                {
                    // No need to initialize printing here - App.ReceiptService is already initialized globally
                    
                    if (!string.IsNullOrWhiteSpace(billingId) && !string.IsNullOrWhiteSpace(sessionId))
                    {
                        pvm.Initialize(billingId!, sessionId!, totalDue, Vm.Items);
                    }
                }
                var result = await dlg.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Navigate to receipt preview
                    var frame = this.Parent as Frame;
                    frame ??= App.MainWindow?.Content as Frame;
                    // CRITICAL FIX: Remove Window.Current fallback to prevent race conditions
                    // Window.Current can be null during navigation, causing race conditions
                    frame ??= new Frame();
                    var receipt = new ReceiptData
                    {
                        OrderId = OrderContext.CurrentOrderId ?? 0,
                        Subtotal = Vm.Subtotal,
                        DiscountTotal = Vm.DiscountTotal,
                        TaxTotal = Vm.TaxTotal,
                        Total = Vm.Total,
                        Items = Vm.Items.ToList()
                    };
                    frame.Navigate(typeof(ReceiptPage), receipt);
                    if (this.Parent == null && App.MainWindow is not null)
                    {
                        App.MainWindow.Content = frame;
                    }
                }
            }
            catch { }
        }
    }
}
