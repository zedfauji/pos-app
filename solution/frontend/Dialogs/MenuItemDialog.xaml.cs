using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Dialogs
{
    public sealed partial class MenuItemDialog : ContentDialog
    {
        public MenuItemDetailsViewModel Vm { get; }

        public MenuItemDialog(MenuItemVm item)
        {
            this.InitializeComponent();
            Vm = new MenuItemDetailsViewModel(item);
            this.DataContext = Vm;
            this.Loaded += async (_, __) => await SafeLoadAsync();
        }

        private async Task SafeLoadAsync()
        {
            try { await Vm.LoadAsync(); } catch { }
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (Vm.HasError)
            {
                args.Cancel = true;
                return;
            }

            if (!OrderContext.HasActiveOrder)
            {
                // No active order. Keep dialog open and show inline validation.
                Vm.GetType(); // no-op
                args.Cancel = true;
                return;
            }

            try
            {
                var ok = await Vm.AddToOrderAsync(OrderContext.CurrentOrderId!.Value);
                if (!ok) args.Cancel = true;
            }
            catch
            {
                args.Cancel = true;
            }
        }
    }
}
