using MenuApi.Models;

namespace MenuApi.Services;

public sealed class InMemoryMenuService : IMenuService
{
    private readonly List<MenuItemDto> _items = new();

    public Task<PagedResult<MenuItemDto>> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct)
    {
        var items = _items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var q = query.Q.Trim();
            items = items.Where(i => i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || i.Sku.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            items = items.Where(i => string.Equals(i.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(query.Group))
        {
            items = items.Where(i => string.Equals(i.GroupName, query.Group, StringComparison.OrdinalIgnoreCase));
        }
        if (query.AvailableOnly == true)
        {
            items = items.Where(i => i.IsAvailable);
        }

        var total = items.Count();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<MenuItemDto>(pageItems, total));
    }

    public Task<MenuItemDetailsDto?> GetItemAsync(long id, CancellationToken ct)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is null) return Task.FromResult<MenuItemDetailsDto?>(null);
        var details = new MenuItemDetailsDto(item, Array.Empty<ModifierDto>());
        return Task.FromResult<MenuItemDetailsDto?>(details);
    }

    public Task<MenuItemDto> CreateItemAsync(CreateMenuItemDto dto, string user, CancellationToken ct)
    {
        if (_items.Any(i => string.Equals(i.Sku, dto.Sku, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("SKU already exists");

        var id = _items.Count == 0 ? 1 : _items.Max(i => i.Id) + 1;
        var newItem = new MenuItemDto(
            id,
            dto.Sku,
            dto.Name,
            dto.Description,
            dto.Category,
            dto.GroupName,
            dto.SellingPrice,
            dto.Price,
            dto.PictureUrl,
            dto.IsDiscountable,
            dto.IsPartOfCombo,
            dto.IsAvailable,
            1
        );
        _items.Add(newItem);
        return Task.FromResult(newItem);
    }

    public Task<MenuItemDto> UpdateItemAsync(long id, UpdateMenuItemDto dto, string user, CancellationToken ct)
    {
        var idx = _items.FindIndex(i => i.Id == id);
        if (idx < 0) throw new KeyNotFoundException("Item not found");
        var cur = _items[idx];
        var updated = cur with
        {
            Name = dto.Name ?? cur.Name,
            Description = dto.Description ?? cur.Description,
            Category = dto.Category ?? cur.Category,
            GroupName = dto.GroupName ?? cur.GroupName,
            SellingPrice = dto.SellingPrice ?? cur.SellingPrice,
            Price = dto.Price ?? cur.Price,
            PictureUrl = dto.PictureUrl ?? cur.PictureUrl,
            IsDiscountable = dto.IsDiscountable ?? cur.IsDiscountable,
            IsPartOfCombo = dto.IsPartOfCombo ?? cur.IsPartOfCombo,
            IsAvailable = dto.IsAvailable ?? cur.IsAvailable,
            Version = cur.Version + 1
        };
        _items[idx] = updated;
        return Task.FromResult(updated);
    }

    public Task RestoreItemAsync(long id, string user, CancellationToken ct)
    {
        // In-memory stub: nothing to do
        return Task.CompletedTask;
    }

    public Task DeleteItemAsync(long id, string user, CancellationToken ct)
    {
        var idx = _items.FindIndex(i => i.Id == id);
        if (idx >= 0) _items.RemoveAt(idx);
        return Task.CompletedTask;
    }
}
