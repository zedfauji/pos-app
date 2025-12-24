namespace MagiDesk.Shared.DTOs.Menu;

// Matches menu.menu_items table: menu_item_id (uuid), sku, name, description, base_price, category, group_name, etc.
public sealed record MenuItemDto(
    Guid Id, 
    string Sku, 
    string Name, 
    string? Description, 
    string Category, 
    string? GroupName, 
    decimal BasePrice, 
    string? PictureUrl, 
    bool IsDiscountable, 
    bool IsPartOfCombo, 
    bool IsAvailable, 
    int Version
);

public sealed record CreateMenuItemDto(
    string Sku,
    string Name,
    string? Description,
    string Category,
    string? GroupName,
    decimal BasePrice,
    string? PictureUrl,
    bool IsDiscountable = true,
    bool IsPartOfCombo = false,
    bool IsAvailable = true
);

public sealed record UpdateMenuItemDto(
    string? Name,
    string? Description,
    string? Category,
    string? GroupName,
    decimal? BasePrice,
    string? PictureUrl,
    bool? IsDiscountable,
    bool? IsPartOfCombo,
    bool? IsAvailable
);

public sealed record MenuItemQueryDto(string? Q, string? Category, string? Group, bool? AvailableOnly, int Page = 1, int PageSize = 50);


