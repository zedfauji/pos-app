using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MagiDesk.Client.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginViewModel ViewModel { get; }

        public LoginPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<LoginViewModel>();
        }
    }
}
