using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using System;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class OrdersManagementPage : Page
    {
        public OrdersManagementViewModel ViewModel { get; } = new();

        public OrdersManagementPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            Loaded += OrdersManagementPage_Loaded;
        }

        private async void OrdersManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadOrdersAsync();
        }

        private async void ViewOrder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderByTableItem order)
            {
                await ViewModel.LoadOrderDetailsAsync(order);
            }
        }

        private async void MarkWaiting_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderByTableItem order)
            {
                await ViewModel.MarkOrderWaitingAsync(order);
                await ShowSuccessDialog($"Order {order.OrderId} has been marked as waiting.");
            }
        }

        private async void MarkDelivered_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderByTableItem order)
            {
                await ViewModel.MarkOrderDeliveredAsync(order);
                await ShowSuccessDialog($"Order {order.OrderId} has been marked as delivered.");
            }
        }

        private async void CloseOrder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderByTableItem order)
            {
                await ViewModel.CloseOrderAsync(order);
                await ShowSuccessDialog($"Order {order.OrderId} has been closed.");
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

        private async Task ShowSuccessDialog(string message)
        {
            var successDialog = new ContentDialog()
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await successDialog.ShowAsync();
        }
    }
}
