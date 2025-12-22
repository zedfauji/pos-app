using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;

namespace MagiDesk.Client.Views
{
    public sealed partial class MenuEditorPage : Page
    {
        public MenuEditorViewModel ViewModel { get; }

        public MenuEditorPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<MenuEditorViewModel>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();
        }
    }
}
