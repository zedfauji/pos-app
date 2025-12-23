using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MagiDesk.Client.Views
{
    public sealed partial class TableManagementPage : Page
    {
        public TableManagementViewModel ViewModel { get; }

        public TableManagementPage()
        {
            this.InitializeComponent();
            ViewModel = App.GetService<TableManagementViewModel>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();
            }
        }
    }
}
