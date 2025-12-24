using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Reports;
using System.Threading.Tasks;
using System;
using Serilog;

namespace MagiDesk.Client.ViewModels
{
    public partial class ReportsViewModel : BaseViewModel
    {
        private readonly ITableApi _tablesApi;
        private readonly IInventoryApi _inventoryApi;
        private readonly IDispatcherService _dispatcherService;

        [ObservableProperty]
        private SalesStatsDto _salesStats = new();

        [ObservableProperty]
        private InventoryStatsDto _inventoryStats = new();

        public ReportsViewModel(ITableApi tablesApi, IInventoryApi inventoryApi, IDispatcherService dispatcherService)
        {
            _tablesApi = tablesApi;
            _inventoryApi = inventoryApi;
            _dispatcherService = dispatcherService;
            RefreshCommand.Execute(null);
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsLoading = true;
            try
            {
                var salesStats = await _tablesApi.GetStatsAsync();
                
                _dispatcherService.InvokeOnUIThread(() => 
                {
                     if (salesStats != null)
                     {
                        SalesStats = salesStats;
                     }
                });

                var invResponse = await _inventoryApi.GetStatsAsync();

                _dispatcherService.InvokeOnUIThread(() =>
                {
                    if (invResponse.IsSuccessStatusCode && invResponse.Content != null)
                    {
                        InventoryStats = invResponse.Content;
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load reports data");
                // Error handled silently - stats will remain null/empty
                // In future: Could show error message to user
            }
            finally
            {
                _dispatcherService.InvokeOnUIThread(() =>
                {
                    IsLoading = false;
                });
            }
        }
    }
}
