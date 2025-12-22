using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views.Dialogs;

public sealed partial class CloseShiftDialog : ContentDialog
{
    public CloseShiftViewModel ViewModel => (CloseShiftViewModel)DataContext;

    public CloseShiftDialog()
    {
        this.InitializeComponent();
    }
}
