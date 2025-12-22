namespace MagiDesk.Shared.DTOs.Menu;

public sealed record MenuItemDto(long Id, string Sku, string Name, string? Description, string Category, string? GroupName, decimal SellingPrice, decimal? Price, string? PictureUrl, bool IsDiscountable, bool IsPartOfCombo, bool IsAvailable, int Version);

public sealed record CreateMenuItemDto(
    string Sku,
    string Name,
    string? Description,
    string Category,
    string? GroupName,
    decimal VendorPrice,
    decimal SellingPrice,
    decimal? Price,
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
    decimal? VendorPrice,
    decimal? SellingPrice,
    decimal? Price,
    string? PictureUrl,
    bool? IsDiscountable,
    bool? IsPartOfCombo,
    bool? IsAvailable
);



public sealed record MenuItemQueryDto(string? Q, string? Category, string? Group, bool? AvailableOnly, int Page = 1, int PageSize = 50);

