using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;

namespace MagiDesk.Client.Views
{
    public sealed partial class MenuEditorPage : Page
    {
        private MenuEditorViewModel? _viewModel;
        public MenuEditorViewModel ViewModel => _viewModel ??= App.GetService<MenuEditorViewModel>() 
            ?? throw new InvalidOperationException("MenuEditorViewModel could not be resolved from DI container");

        public MenuEditorPage()
        {
            this.InitializeComponent();
            // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
            this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();
        }
    }
}
