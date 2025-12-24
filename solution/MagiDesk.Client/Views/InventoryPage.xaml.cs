using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views
{
    public sealed partial class InventoryPage : UserControl
    {
        private InventoryViewModel? _viewModel;
        public InventoryViewModel ViewModel => _viewModel ??= App.GetService<InventoryViewModel>() 
            ?? throw new InvalidOperationException("InventoryViewModel could not be resolved from DI container");

        public InventoryPage()
        {
            this.InitializeComponent();
            // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
            this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
        }
    }
}
