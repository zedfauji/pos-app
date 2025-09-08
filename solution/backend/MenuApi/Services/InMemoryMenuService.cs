using MenuApi.Models;

namespace MenuApi.Services;

public sealed class InMemoryMenuService : IMenuService
{
    private readonly List<MenuItemDto> _items = new();
    private readonly List<ComboDto> _combos = new();
    private readonly Dictionary<long, List<ComboItemLinkDto>> _comboItems = new();

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

    public Task<PagedResult<ComboDto>> ListCombosAsync(ComboQueryDto query, CancellationToken ct)
    {
        var items = _combos.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var q = query.Q.Trim();
            items = items.Where(i => i.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        if (query.AvailableOnly == true)
        {
            items = items.Where(i => i.IsAvailable);
        }
        var total = items.Count();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<ComboDto>(pageItems, total));
    }

    public Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct)
    {
        var combo = _combos.FirstOrDefault(c => c.Id == id);
        if (combo is null) return Task.FromResult<ComboDetailsDto?>(null);
        var links = _comboItems.TryGetValue(id, out var list) ? list : new List<ComboItemLinkDto>();
        return Task.FromResult<ComboDetailsDto?>(new ComboDetailsDto(combo, links));
    }

    public Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct)
    {
        var id = _combos.Count == 0 ? 1 : _combos.Max(c => c.Id) + 1;
        var combo = new ComboDto(id, dto.Name, dto.Description, dto.Price, dto.IsDiscountable, dto.IsAvailable, dto.PictureUrl, 1);
        _combos.Add(combo);
        _comboItems[id] = dto.Items.ToList();
        return Task.FromResult(combo);
    }

    public Task<ComboDto> UpdateComboAsync(long id, UpdateComboDto dto, string user, CancellationToken ct)
    {
        var idx = _combos.FindIndex(c => c.Id == id);
        if (idx < 0) throw new KeyNotFoundException("Combo not found");
        var cur = _combos[idx];
        var updated = cur with
        {
            Name = dto.Name ?? cur.Name,
            Description = dto.Description ?? cur.Description,
            Price = dto.Price ?? cur.Price,
            IsDiscountable = dto.IsDiscountable ?? cur.IsDiscountable,
            IsAvailable = dto.IsAvailable ?? cur.IsAvailable,
            PictureUrl = dto.PictureUrl ?? cur.PictureUrl,
            Version = cur.Version + 1
        };
        _combos[idx] = updated;
        if (dto.Items is not null)
        {
            _comboItems[id] = dto.Items.ToList();
        }
        return Task.FromResult(updated);
    }

    public Task DeleteComboAsync(long id, string user, CancellationToken ct)
    {
        var idx = _combos.FindIndex(c => c.Id == id);
        if (idx >= 0)
        {
            _combos.RemoveAt(idx);
            _comboItems.Remove(id);
        }
        return Task.CompletedTask;
    }
}
