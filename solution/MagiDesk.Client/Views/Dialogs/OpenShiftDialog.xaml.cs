using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views.Dialogs;

public sealed partial class OpenShiftDialog : ContentDialog
{
    public OpenShiftViewModel ViewModel => (OpenShiftViewModel)DataContext;

    public OpenShiftDialog()
    {
        this.InitializeComponent();
    }
}
