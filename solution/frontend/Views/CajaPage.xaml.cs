using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class CajaPage : Page
{
    public CajaViewModel ViewModel { get; }

    public CajaPage()
    {
        this.InitializeComponent();
        ViewModel = new CajaViewModel();
        DataContext = ViewModel;
        
        Loaded += async (s, e) => 
        {
            await ViewModel.LoadActiveSessionAsync();
            await ViewModel.LoadHistoryAsync();
        };
    }

    private async void OpenCajaButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenCajaDialog();
        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.OpeningAmount.HasValue)
        {
            var success = await ViewModel.OpenCajaAsync(dialog.OpeningAmount.Value, dialog.Notes);
            if (success)
            {
                await ViewModel.LoadActiveSessionAsync();
            }
        }
    }

    private async void CloseCajaButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveSession == null) return;

        var dialog = new CloseCajaDialog(ViewModel.ActiveSession);
        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.ClosingAmount.HasValue)
        {
            var success = await ViewModel.CloseCajaAsync(
                dialog.ClosingAmount.Value, 
                dialog.Notes, 
                dialog.ManagerOverride);
            if (success)
            {
                await ViewModel.LoadActiveSessionAsync();
                await ViewModel.LoadHistoryAsync();
            }
        }
    }

    private void ViewReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid sessionId)
        {
            // Navigate to report page
            if (App.MainWindow?.Content is Frame frame)
            {
                frame.Navigate(typeof(CajaReportsPage), sessionId);
            }
        }
    }
}
