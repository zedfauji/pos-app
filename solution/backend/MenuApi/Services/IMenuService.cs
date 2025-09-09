using MenuApi.Models;

namespace MenuApi.Services;

public interface IMenuService
{
    Task<PagedResult<MenuItemDto>> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct);
    Task<MenuItemDetailsDto?> GetItemAsync(long id, CancellationToken ct);
    Task<MenuItemDto> CreateItemAsync(CreateMenuItemDto dto, string user, CancellationToken ct);
    Task<MenuItemDto> UpdateItemAsync(long id, UpdateMenuItemDto dto, string user, CancellationToken ct);
    Task RestoreItemAsync(long id, string user, CancellationToken ct);
    Task DeleteItemAsync(long id, string user, CancellationToken ct);

    Task<bool> ExistsSkuAsync(string sku, long? excludeId, CancellationToken ct);
    Task<MenuItemDetailsDto?> GetItemBySkuAsync(string sku, CancellationToken ct);
    Task SetItemAvailabilityAsync(long id, bool isAvailable, string user, CancellationToken ct);

    Task<PagedResult<ComboDto>> ListCombosAsync(ComboQueryDto query, CancellationToken ct);
    Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct);
    Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct);
    Task<ComboDto> UpdateComboAsync(long id, UpdateComboDto dto, string user, CancellationToken ct);
    Task DeleteComboAsync(long id, string user, CancellationToken ct);

    Task SetComboAvailabilityAsync(long id, bool isAvailable, string user, CancellationToken ct);
    Task<(decimal ComputedPrice, IReadOnlyList<(long MenuItemId, int Quantity, decimal UnitPrice)> Items)> ComputeComboPriceAsync(long id, CancellationToken ct);

    Task RollbackItemAsync(long id, int toVersion, string user, CancellationToken ct);
    Task RollbackComboAsync(long id, int toVersion, string user, CancellationToken ct);
}
