using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views;

public sealed partial class ShiftControllerPage : UserControl
{
    public ShiftControllerViewModel ViewModel => (ShiftControllerViewModel)DataContext;

    public ShiftControllerPage()
    {
        this.InitializeComponent();
        this.Loaded += ShiftControllerPage_Loaded;
    }

    private async void ShiftControllerPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.LoadAsync();
        }
    }
}
