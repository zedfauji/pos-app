using MagiDesk.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views
{
    public sealed partial class ReportsPage : Page
    {
        public ReportsViewModel ViewModel { get; }

        public ReportsPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<ReportsViewModel>();
        }
    }
}
