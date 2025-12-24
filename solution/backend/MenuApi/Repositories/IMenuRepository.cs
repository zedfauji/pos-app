using MenuApi.Models;

namespace MenuApi.Repositories;

public interface IMenuRepository
{
    // Items
    Task<(IReadOnlyList<MenuItemDto> Items, int Total)> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct);
    Task<MenuItemDetailsDto?> GetItemAsync(Guid id, CancellationToken ct);
    Task<MenuItemDto> CreateItemAsync(CreateMenuItemDto dto, string user, CancellationToken ct);
    Task<MenuItemDto> UpdateItemAsync(Guid id, UpdateMenuItemDto dto, string user, CancellationToken ct);
    Task RestoreItemAsync(Guid id, string user, CancellationToken ct);
    Task DeleteItemAsync(Guid id, string user, CancellationToken ct);

    Task<bool> ExistsSkuAsync(string sku, Guid? excludeId, CancellationToken ct);
    Task<MenuItemDetailsDto?> GetItemBySkuAsync(string sku, CancellationToken ct);
    Task SetItemAvailabilityAsync(Guid id, bool isAvailable, string user, CancellationToken ct);

    // Combos
    Task<(IReadOnlyList<ComboDto> Items, int Total)> ListCombosAsync(ComboQueryDto query, CancellationToken ct);
    Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct);
    Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct);
    Task<ComboDto> UpdateComboAsync(long id, UpdateComboDto dto, string user, CancellationToken ct);
    Task DeleteComboAsync(long id, string user, CancellationToken ct);

    Task SetComboAvailabilityAsync(long id, bool isAvailable, string user, CancellationToken ct);
    Task<(decimal ComputedPrice, IReadOnlyList<(Guid MenuItemId, int Quantity, decimal UnitPrice)> Items)> ComputeComboPriceAsync(long id, CancellationToken ct);

    Task RollbackItemAsync(Guid id, int toVersion, string user, CancellationToken ct);
    Task RollbackComboAsync(long id, int toVersion, string user, CancellationToken ct);

    // History
    Task<(IReadOnlyList<HistoryDto> Items, int Total)> ListHistoryAsync(HistoryQueryDto query, CancellationToken ct);

    // Modifiers CRUD
    Task<(IReadOnlyList<ModifierDto> Items, int Total)> ListModifiersAsync(ModifierQueryDto query, CancellationToken ct);
    Task<ModifierDto?> GetModifierAsync(long id, CancellationToken ct);
    Task<ModifierDto> CreateModifierAsync(CreateModifierDto dto, string user, CancellationToken ct);
    Task<ModifierDto> UpdateModifierAsync(long id, UpdateModifierDto dto, string user, CancellationToken ct);
    Task DeleteModifierAsync(long id, string user, CancellationToken ct);
}
