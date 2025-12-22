using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace MagiDesk.Client.Views;

public sealed partial class PaymentWorkspacePage : Page
{
    public PaymentWorkspaceViewModel ViewModel { get; }
    
    // Static workaround for passing navigation parameters via ViewModel-based navigation
    public static PaymentWorkspaceNavParams? PendingNavParams { get; set; }

    public PaymentWorkspacePage()
    {
        this.InitializeComponent();
        
        // Get ViewModel from DI container
        ViewModel = App.GetService<PaymentWorkspaceViewModel>();
        DataContext = ViewModel;
        
        this.Loaded += PaymentWorkspacePage_Loaded;
    }

    private async void PaymentWorkspacePage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Check for pending nav params
        if (PendingNavParams != null)
        {
            await ViewModel.InitializeAsync(
                PendingNavParams.SessionId,
                PendingNavParams.BillingId,
                PendingNavParams.TableLabel);
            
            // Clear pending params
            PendingNavParams = null;
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Expect navigation parameter: { SessionId, BillingId, TableLabel }
        if (e.Parameter is PaymentWorkspaceNavParams navParams)
        {
            await ViewModel.InitializeAsync(
                navParams.SessionId, 
                navParams.BillingId, 
                navParams.TableLabel);
        }
    }

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return;

        // Clear and re-add to sync with ObservableCollection
        ViewModel.SelectedItems.Clear();
        
        if (sender is ListView listView)
        {
            foreach (var item in listView.SelectedItems)
            {
                if (item is MagiDesk.Shared.DTOs.Tables.ItemLine itemLine)
                {
                    ViewModel.SelectedItems.Add(itemLine);
                }
            }
        }
    }
}

/// <summary>
/// Navigation parameters for PaymentWorkspacePage
/// </summary>
public record PaymentWorkspaceNavParams(Guid SessionId, Guid BillingId, string TableLabel);
