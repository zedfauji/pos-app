using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;

namespace MagiDesk.Client.Views
{
    public sealed partial class TableWorkspacePage : Page
    {
        public TableWorkspaceViewModel ViewModel => (TableWorkspaceViewModel)DataContext;

        public TableWorkspacePage()
        {
            this.InitializeComponent();
        }
    }
}
