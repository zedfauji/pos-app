using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MagiDesk.Client.Views
{
    public sealed partial class TableManagementPage : Page
    {
        private TableManagementViewModel? _viewModel;
        public TableManagementViewModel ViewModel => _viewModel ??= App.GetService<TableManagementViewModel>() 
            ?? throw new InvalidOperationException("TableManagementViewModel could not be resolved from DI container");

        public TableManagementPage()
        {
            this.InitializeComponent();
            // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
            this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
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
