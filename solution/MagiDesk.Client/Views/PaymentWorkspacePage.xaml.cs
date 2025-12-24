using MagiDesk.Client.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace MagiDesk.Client.Views;

public sealed partial class PaymentWorkspacePage : Page
{
    private PaymentWorkspaceViewModel? _viewModel;
    public PaymentWorkspaceViewModel ViewModel => _viewModel ??= App.GetService<PaymentWorkspaceViewModel>()
        ?? throw new InvalidOperationException("PaymentWorkspaceViewModel could not be resolved from DI container");

    public PaymentWorkspacePage()
    {
        this.InitializeComponent();
        // ViewModel will be resolved lazily when DataContext is set or ViewModel is accessed
        this.Loaded += (s, e) => { if (this.DataContext == null) this.DataContext = ViewModel; };
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
