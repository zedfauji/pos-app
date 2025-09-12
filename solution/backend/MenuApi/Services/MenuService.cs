using MenuApi.Models;
using MenuApi.Repositories;

namespace MenuApi.Services;

public sealed class MenuService : IMenuService
{
    private readonly IMenuRepository _repo;
    private readonly IInventoryService _inventoryService;

    public MenuService(IMenuRepository repo, IInventoryService inventoryService)
    {
        _repo = repo;
        _inventoryService = inventoryService;
    }

    public async Task<PagedResult<MenuItemDto>> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct)
    {
        var (items, total) = await _repo.ListItemsAsync(query, ct);
        
        // Filter drinks based on inventory availability
        var availableItems = new List<MenuItemDto>();
        foreach (var item in items)
        {
            // Check inventory for drinks (categories: Beverages, Drinks, etc.)
            if (IsDrinkCategory(item.Category))
            {
                var hasStock = await _inventoryService.CheckItemAvailabilityAsync(item.Sku, 1, ct);
                if (hasStock)
                {
                    availableItems.Add(item);
                }
                else
                {
                    // Mark drink as unavailable but still show it
                    var unavailableItem = item with { IsAvailable = false };
                    availableItems.Add(unavailableItem);
                }
            }
            else
            {
                // Food items don't need inventory checks
                availableItems.Add(item);
            }
        }
        
        return new PagedResult<MenuItemDto>(availableItems, availableItems.Count);
    }

    public Task<MenuItemDetailsDto?> GetItemAsync(long id, CancellationToken ct)
        => _repo.GetItemAsync(id, ct);

    public Task<MenuItemDto> CreateItemAsync(CreateMenuItemDto dto, string user, CancellationToken ct)
        => _repo.CreateItemAsync(dto, user, ct);

    public Task<MenuItemDto> UpdateItemAsync(long id, UpdateMenuItemDto dto, string user, CancellationToken ct)
        => _repo.UpdateItemAsync(id, dto, user, ct);

    public Task RestoreItemAsync(long id, string user, CancellationToken ct)
        => _repo.RestoreItemAsync(id, user, ct);

    public Task DeleteItemAsync(long id, string user, CancellationToken ct)
        => _repo.DeleteItemAsync(id, user, ct);

    public Task<bool> ExistsSkuAsync(string sku, long? excludeId, CancellationToken ct)
        => _repo.ExistsSkuAsync(sku, excludeId, ct);

    public Task<MenuItemDetailsDto?> GetItemBySkuAsync(string sku, CancellationToken ct)
        => _repo.GetItemBySkuAsync(sku, ct);

    public Task SetItemAvailabilityAsync(long id, bool isAvailable, string user, CancellationToken ct)
        => _repo.SetItemAvailabilityAsync(id, isAvailable, user, ct);

    public async Task<PagedResult<ComboDto>> ListCombosAsync(ComboQueryDto query, CancellationToken ct)
    {
        var (items, total) = await _repo.ListCombosAsync(query, ct);
        return new PagedResult<ComboDto>(items, total);
    }

    public Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct)
        => _repo.GetComboAsync(id, ct);

    public Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct)
        => _repo.CreateComboAsync(dto, user, ct);

    public Task<ComboDto> UpdateComboAsync(long id, UpdateComboDto dto, string user, CancellationToken ct)
        => _repo.UpdateComboAsync(id, dto, user, ct);

    public Task DeleteComboAsync(long id, string user, CancellationToken ct)
        => _repo.DeleteComboAsync(id, user, ct);

    public Task SetComboAvailabilityAsync(long id, bool isAvailable, string user, CancellationToken ct)
        => _repo.SetComboAvailabilityAsync(id, isAvailable, user, ct);

    public Task<(decimal ComputedPrice, IReadOnlyList<(long MenuItemId, int Quantity, decimal UnitPrice)> Items)> ComputeComboPriceAsync(long id, CancellationToken ct)
        => _repo.ComputeComboPriceAsync(id, ct);

    public Task RollbackItemAsync(long id, int toVersion, string user, CancellationToken ct)
        => _repo.RollbackItemAsync(id, toVersion, user, ct);

    public Task RollbackComboAsync(long id, int toVersion, string user, CancellationToken ct)
        => _repo.RollbackComboAsync(id, toVersion, user, ct);

    private static bool IsDrinkCategory(string category)
    {
        // Only treat pre-made drinks as inventory items
        // Coffee, tea, etc. are made-to-order like food items
        var preMadeDrinkCategories = new[] { "Alcohol", "Beer", "Soda", "Juice", "Water", "Bottled Drinks" };
        return preMadeDrinkCategories.Contains(category, StringComparer.OrdinalIgnoreCase);
    }
}
