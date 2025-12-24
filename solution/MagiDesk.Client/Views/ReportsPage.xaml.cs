using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views
{
    public sealed partial class ReportsPage : Page
    {
        private ReportsViewModel? _viewModel;
        public ReportsViewModel ViewModel => _viewModel ??= App.GetService<ReportsViewModel>() 
            ?? throw new InvalidOperationException("ReportsViewModel could not be resolved from DI container");

        public ReportsPage()
        {
            this.InitializeComponent();
            // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
            this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
        }
    }
}
