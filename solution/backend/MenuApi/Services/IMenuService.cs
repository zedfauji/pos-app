using MenuApi.Models;

namespace MenuApi.Services;

public interface IMenuService
{
    Task<PagedResult<MenuItemDto>> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct);
    Task<MenuItemDetailsDto?> GetItemAsync(Guid id, CancellationToken ct);
    Task<MenuItemDto> CreateItemAsync(CreateMenuItemDto dto, string user, CancellationToken ct);
    Task<MenuItemDto> UpdateItemAsync(Guid id, UpdateMenuItemDto dto, string user, CancellationToken ct);
    Task RestoreItemAsync(Guid id, string user, CancellationToken ct);
    Task DeleteItemAsync(Guid id, string user, CancellationToken ct);

    Task<bool> ExistsSkuAsync(string sku, Guid? excludeId, CancellationToken ct);
    Task<MenuItemDetailsDto?> GetItemBySkuAsync(string sku, CancellationToken ct);
    Task SetItemAvailabilityAsync(Guid id, bool isAvailable, string user, CancellationToken ct);

    Task<PagedResult<ComboDto>> ListCombosAsync(ComboQueryDto query, CancellationToken ct);
    Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct);
    Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct);
    Task<ComboDto> UpdateComboAsync(long id, UpdateComboDto dto, string user, CancellationToken ct);
    Task DeleteComboAsync(long id, string user, CancellationToken ct);

    Task SetComboAvailabilityAsync(long id, bool isAvailable, string user, CancellationToken ct);
    Task<(decimal ComputedPrice, IReadOnlyList<(Guid MenuItemId, int Quantity, decimal UnitPrice)> Items)> ComputeComboPriceAsync(long id, CancellationToken ct);

    Task RollbackItemAsync(Guid id, int toVersion, string user, CancellationToken ct);
    Task RollbackComboAsync(long id, int toVersion, string user, CancellationToken ct);
}

