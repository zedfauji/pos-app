using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MagiDesk.Client.Views
{
    public sealed partial class LoginPage : Page
    {
        private LoginViewModel? _viewModel;
        public LoginViewModel ViewModel => _viewModel ??= App.GetService<LoginViewModel>() 
            ?? throw new InvalidOperationException("LoginViewModel could not be resolved from DI container");

        public LoginPage()
        {
            this.InitializeComponent();
            // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
            this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
        }
    }
}
