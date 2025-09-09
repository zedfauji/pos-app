using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Dialogs
{
    public sealed partial class PaymentDialog : ContentDialog
    {
        public PaymentDialog()
        {
            this.InitializeComponent();
            this.DataContext = new PaymentViewModel();
            this.Loaded += PaymentDialog_Loaded;
        }

        private async void PaymentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is PaymentViewModel vm)
                {
                    await vm.LoadLedgerAsync();
                }
            }
            catch { }
        }

        private async void OnConfirm(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (DataContext is PaymentViewModel vm)
            {
                var ok = await vm.ConfirmAsync();
                if (!ok)
                {
                    args.Cancel = true;
                }
            }
        }

        private void OnAddSplit(object sender, RoutedEventArgs e)
        {
            if (DataContext is PaymentViewModel vm)
            {
                vm.AddSplit();
            }
        }

        private void OnRemoveSplit(object sender, RoutedEventArgs e)
        {
            if (DataContext is PaymentViewModel vm && (sender as Button)?.DataContext is PaymentLineVm line)
            {
                vm.RemoveSplit(line);
            }
        }

        private async void OnApplyDiscount(object sender, RoutedEventArgs e)
        {
            if (DataContext is PaymentViewModel vm)
            {
                await vm.ApplyDiscountAsync();
            }
        }
    }
}
