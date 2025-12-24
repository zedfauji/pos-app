using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views
{
    public sealed partial class SettingsPage : Page
    {
        private SettingsViewModel? _viewModel;
        public SettingsViewModel ViewModel => _viewModel ??= App.GetService<SettingsViewModel>() 
            ?? throw new InvalidOperationException("SettingsViewModel could not be resolved from DI container");

        public SettingsPage()
        {
            this.InitializeComponent();
            // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
            this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
        }
    }
}
