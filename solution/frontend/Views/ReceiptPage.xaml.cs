using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Shared.DTOs.Tables;
using System;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class ReceiptPage : Page
    {
        private BillResult? _bill;
        private Window? _parentWindow;
        private string? _sessionId;
        private decimal _totalDue;

        public ReceiptPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is BillResult bill)
            {
                _bill = bill;
                UpdateReceiptInfo();
            }
        }

        public void SetReceiptInfo(BillResult bill, string sessionId, decimal totalDue, Window parentWindow)
        {
            _bill = bill;
            _sessionId = sessionId;
            _totalDue = totalDue;
            _parentWindow = parentWindow;
            UpdateReceiptInfo();
        }

        private void UpdateReceiptInfo()
        {
            if (_bill != null)
            {
                ReceiptInfoText.Text = $"Receipt for Bill #{_bill.BillId}";
                BillIdText.Text = $"Bill ID: {_bill.BillId}";
                TableNumberText.Text = $"Table: {_bill.TableLabel}";
                TotalAmountText.Text = $"Amount: {_totalDue:C}";
                ItemCountText.Text = $"Items: {_bill.Items?.Count ?? 0}";
                PaymentMethodText.Text = $"Payment Method: Not specified";
            }
        }

        private async void PrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Simulate receipt printing
                var dialog = new ContentDialog()
                {
                    Title = "Print Receipt",
                    Content = $"Printing receipt for Bill #{_bill?.BillId}...",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await dialog.ShowAsync();
                
                // Close this window after printing
                CloseWindow();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Print Error",
                    Content = $"Failed to print receipt: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await errorDialog.ShowAsync();
            }
        }

        private async void PreviewReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Simulate receipt preview
                var dialog = new ContentDialog()
                {
                    Title = "Receipt Preview",
                    Content = $"Preview of receipt for Bill #{_bill?.BillId}\n\nAmount: {_totalDue:C}\nItems: {_bill?.Items?.Count ?? 0}\n\nThis would show a preview of the receipt.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Preview Error",
                    Content = $"Failed to preview receipt: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await errorDialog.ShowAsync();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            // Close this window by finding the parent window
            if (_parentWindow != null)
            {
                _parentWindow.Close();
            }
        }
    }
}