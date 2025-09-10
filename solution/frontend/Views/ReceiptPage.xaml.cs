using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class ReceiptPage : Page
    {
        public ReceiptPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // CRITICAL FIX: Handle both old ReceiptData and new Services.ReceiptService.ReceiptData
            if (e.Parameter is ReceiptData data)
            {
                OrderIdText.Text = data.OrderId.ToString();
                ItemsList.ItemsSource = data.Items;
                SubtotalText.Text = $"Subtotal: {data.Subtotal:C}";
                DiscountText.Text = $"Discounts: -{data.DiscountTotal:C}";
                TaxText.Text = $"Taxes: {data.TaxTotal:C}";
                TotalText.Text = $"Total: {data.Total:C}";
            }
            else if (e.Parameter is Services.ReceiptService.ReceiptData receiptData)
            {
                OrderIdText.Text = receiptData.BillId ?? "N/A";
                ItemsList.ItemsSource = receiptData.Items;
                SubtotalText.Text = $"Subtotal: {receiptData.Subtotal:C}";
                DiscountText.Text = $"Discounts: -{receiptData.DiscountAmount:C}";
                TaxText.Text = $"Taxes: {receiptData.TaxAmount:C}";
                TotalText.Text = $"Total: {receiptData.TotalAmount:C}";
            }
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // CRITICAL FIX: Implement actual printing functionality
                // This was a placeholder that could cause issues
                if (App.ReceiptService == null)
                {
                    await new ContentDialog
                    {
                        Title = "Error",
                        Content = "Receipt service not available. Please try again.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                    return;
                }

                // Get the receipt data from the page
                var receiptData = new Services.ReceiptService.ReceiptData
                {
                    BillId = OrderIdText.Text,
                    Subtotal = decimal.Parse(SubtotalText.Text.Replace("Subtotal: ", "").Replace("$", "")),
                    DiscountAmount = decimal.Parse(DiscountText.Text.Replace("Discounts: -", "").Replace("$", "")),
                    TaxAmount = decimal.Parse(TaxText.Text.Replace("Taxes: ", "").Replace("$", "")),
                    TotalAmount = decimal.Parse(TotalText.Text.Replace("Total: ", "").Replace("$", "")),
                    Items = (ItemsList.ItemsSource as List<OrderItemLineVm>)?.Select(item => new Services.ReceiptService.ReceiptItem
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        Price = item.UnitPrice,
                        Subtotal = item.LineTotal
                    }).ToList() ?? new List<Services.ReceiptService.ReceiptItem>()
                };

                // Print the receipt
                // CRITICAL FIX: Remove Window.Current usage to prevent COM exceptions in WinUI 3 Desktop Apps
                // Window.Current is a Windows Runtime COM interop call that causes Marshal.ThrowExceptionForHR errors
                var success = await App.ReceiptService.PrintReceiptAsync(receiptData, App.MainWindow, showPreview: true);
                
                if (!success)
                {
                    await new ContentDialog
                    {
                        Title = "Print Failed",
                        Content = "Failed to print receipt. Please try again.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = $"Print error: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }
    }
}
