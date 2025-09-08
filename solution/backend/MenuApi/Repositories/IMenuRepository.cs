using MenuApi.Models;

namespace MenuApi.Repositories;

public interface IMenuRepository
{
    // Items
    Task<(IReadOnlyList<MenuItemDto> Items, int Total)> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct);
    Task<MenuItemDetailsDto?> GetItemAsync(long id, CancellationToken ct);
    Task<MenuItemDto> CreateItemAsync(CreateMenuItemDto dto, string user, CancellationToken ct);
    Task<MenuItemDto> UpdateItemAsync(long id, UpdateMenuItemDto dto, string user, CancellationToken ct);
    Task RestoreItemAsync(long id, string user, CancellationToken ct);
    Task DeleteItemAsync(long id, string user, CancellationToken ct);

    // Combos
    Task<(IReadOnlyList<ComboDto> Items, int Total)> ListCombosAsync(ComboQueryDto query, CancellationToken ct);
    Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct);
    Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct);
    Task<ComboDto> UpdateComboAsync(long id, UpdateComboDto dto, string user, CancellationToken ct);
    Task DeleteComboAsync(long id, string user, CancellationToken ct);
}
