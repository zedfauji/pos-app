using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MagiDesk.Frontend.Views;

public sealed partial class ReceiptSettingsPage : Page
{
    public ReceiptSettingsViewModel ViewModel { get; }

    public ReceiptSettingsPage()
    {
        this.InitializeComponent();
        
        // Get ViewModel from DI container or create with default services
        try
        {
            ViewModel = App.Services?.GetRequiredService<ReceiptSettingsViewModel>() 
                ?? new ReceiptSettingsViewModel(
                    App.ReceiptService ?? new ReceiptService(null, null),
                    App.SettingsApi ?? new SettingsApiService(new HttpClient(), null),
                    null,
                    null
                );
        }
        catch
        {
            // Fallback if DI is not available
            ViewModel = new ReceiptSettingsViewModel(
                App.ReceiptService ?? new ReceiptService(null, null),
                App.SettingsApi ?? new SettingsApiService(new HttpClient(), null),
                null,
                null
            );
        }
        
        this.DataContext = ViewModel;
    }
}
