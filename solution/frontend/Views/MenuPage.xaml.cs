using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class MenuPage : Page, IToolbarConsumer
    {
        public MenuViewModel Vm { get; } = new();
        public MenuPage()
        {
            this.InitializeComponent();
            this.DataContext = Vm;
        }

        public void OnAdd() { /* optional toolbar action for add */ }
        public void OnEdit() { }
        public void OnDelete() { }
        public void OnRefresh()
        {
            try
            {
                if (Vm?.RefreshCommand != null && Vm.RefreshCommand.CanExecute(null))
                {
                    Vm.RefreshCommand.Execute(null);
                }
            }
            catch { }
        }

        private async void ItemsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MenuItemVm item)
            {
                var dlg = new Dialogs.MenuItemDialog(item);
                _ = await dlg.ShowAsync();
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is MenuItemVm item)
            {
                var dlg = new Dialogs.MenuItemDialog(item);
                _ = await dlg.ShowAsync();
            }
        }
    }
}
