using MagiDesk.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagiDesk.Core.Interfaces
{
    public interface IInventoryRepository
    {
        Task<IEnumerable<InventoryItem>> GetAllAsync();
        Task<InventoryItem?> GetByIdAsync(long id);
        Task<long> CreateAsync(InventoryItem item);
        Task UpdateAsync(InventoryItem item);
        Task DeleteAsync(long id);
        Task<MagiDesk.Shared.DTOs.Reports.InventoryStatsDto> GetStatsAsync();
        Task AddTransactionAsync(InventoryTransaction transaction);
    }
}
