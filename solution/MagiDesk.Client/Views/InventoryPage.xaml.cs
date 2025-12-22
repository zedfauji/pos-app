using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views
{
    public sealed partial class InventoryPage : UserControl
    {
        public InventoryViewModel ViewModel { get; }

        public InventoryPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<InventoryViewModel>();
            this.DataContext = ViewModel;
        }
    }
}
