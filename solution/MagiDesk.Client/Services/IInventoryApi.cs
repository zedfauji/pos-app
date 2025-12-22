using MagiDesk.Shared.DTOs.Inventory;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services
{
    public interface IInventoryApi
    {
        [Get("/inventory")]
        Task<ApiResponse<IEnumerable<InventoryItemDto>>> GetItemsAsync();

        [Get("/inventory/{id}")]
        Task<ApiResponse<InventoryItemDto>> GetItemAsync(long id);

        [Post("/inventory")]
        Task<ApiResponse<object>> CreateItemAsync([Body] CreateInventoryItemDto request);

        [Post("/inventory/{id}/adjust")]
        Task<ApiResponse<object>> AdjustStockAsync(long id, [Body] AdjustStockDto request);

        [Delete("/inventory/{id}")]
        Task<ApiResponse<object>> DeleteItemAsync(long id);

        [Get("/inventory/stats")]
        Task<ApiResponse<MagiDesk.Shared.DTOs.Reports.InventoryStatsDto>> GetStatsAsync();
    }
}
