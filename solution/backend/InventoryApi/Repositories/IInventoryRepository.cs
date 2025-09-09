using MagiDesk.Shared.DTOs;

namespace InventoryApi.Repositories;

public interface IInventoryRepository
{
    Task<IEnumerable<ItemDto>> ListItemsAsync(string? vendorId, string? categoryId, bool? isMenuAvailable, string? search, CancellationToken ct);
    Task<ItemDto?> GetItemAsync(string id, CancellationToken ct);
    Task<ItemDto> CreateItemAsync(ItemDto dto, CancellationToken ct);
    Task<bool> UpdateItemAsync(string id, ItemDto dto, CancellationToken ct);
    Task<bool> DeleteItemAsync(string id, CancellationToken ct);
}

