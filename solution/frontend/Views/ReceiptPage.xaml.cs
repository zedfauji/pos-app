using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;

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
            if (e.Parameter is ReceiptData data)
            {
                OrderIdText.Text = data.OrderId.ToString();
                ItemsList.ItemsSource = data.Items;
                SubtotalText.Text = $"Subtotal: {data.Subtotal:C}";
                DiscountText.Text = $"Discounts: -{data.DiscountTotal:C}";
                TaxText.Text = $"Taxes: {data.TaxTotal:C}";
                TotalText.Text = $"Total: {data.Total:C}";
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder: hook Windows printing or export to PDF logic here
        }
    }
}
