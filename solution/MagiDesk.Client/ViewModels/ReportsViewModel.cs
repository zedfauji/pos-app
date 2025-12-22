using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Reports;
using System.Threading.Tasks;

namespace MagiDesk.Client.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly ITableApi _tablesApi;
        private readonly IInventoryApi _inventoryApi;

        [ObservableProperty]
        private SalesStatsDto _salesStats = new();

        [ObservableProperty]
        private InventoryStatsDto _inventoryStats = new();

        [ObservableProperty]
        private bool _isLoading;

        public ReportsViewModel(ITableApi tablesApi, IInventoryApi inventoryApi)
        {
            _tablesApi = tablesApi;
            _inventoryApi = inventoryApi;
            RefreshCommand.Execute(null);
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsLoading = true;
            try
            {
                var salesStats = await _tablesApi.GetStatsAsync();
                
                var app = (App)Microsoft.UI.Xaml.Application.Current;
                app.MainWindow.DispatcherQueue.TryEnqueue(() => 
                {
                     if (salesStats != null)
                     {
                        SalesStats = salesStats;
                     }
                });

                var invResponse = await _inventoryApi.GetStatsAsync();

                app.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    if (invResponse.IsSuccessStatusCode && invResponse.Content != null)
                    {
                        InventoryStats = invResponse.Content;
                    }
                });
            }
            catch { }
            finally
            {
                var app = (App)Microsoft.UI.Xaml.Application.Current;
                app.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    IsLoading = false;
                });
            }
        }
    }
}
