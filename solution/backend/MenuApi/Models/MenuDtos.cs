namespace MenuApi.Models;

public sealed record MenuItemDto(long Id, string Sku, string Name, string? Description, string Category, string? GroupName, decimal SellingPrice, decimal? Price, string? PictureUrl, bool IsDiscountable, bool IsPartOfCombo, bool IsAvailable, int Version);

public sealed record MenuItemDetailsDto(MenuItemDto Item, IReadOnlyList<ModifierDto> Modifiers);

public sealed record ModifierDto(long Id, string Name, bool IsRequired, bool AllowMultiple, int? MaxSelections, IReadOnlyList<ModifierOptionDto> Options);

public sealed record ModifierOptionDto(long Id, string Name, decimal PriceDelta, bool IsAvailable, int SortOrder);

public sealed record MenuItemQueryDto(string? Q, string? Category, string? Group, bool? AvailableOnly, int Page = 1, int PageSize = 50);

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

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

// Combo DTOs
public sealed record ComboItemLinkDto(long MenuItemId, int Quantity, bool IsRequired);
public sealed record ComboDto(long Id, string Name, string? Description, decimal Price, bool IsDiscountable, bool IsAvailable, string? PictureUrl, int Version);
public sealed record ComboDetailsDto(ComboDto Combo, IReadOnlyList<ComboItemLinkDto> Items);
public sealed record ComboQueryDto(string? Q, bool? AvailableOnly, int Page = 1, int PageSize = 50);
public sealed record CreateComboDto(string Name, string? Description, decimal Price, bool IsDiscountable, bool IsAvailable, string? PictureUrl, IReadOnlyList<ComboItemLinkDto> Items);
public sealed record UpdateComboDto(string? Name, string? Description, decimal? Price, bool? IsDiscountable, bool? IsAvailable, string? PictureUrl, IReadOnlyList<ComboItemLinkDto>? Items);

// History DTOs
public sealed record HistoryDto(long Id, string EntityType, long EntityId, string Action, int? Version, DateTimeOffset ChangedAt, string? ChangedBy, object? OldValue, object? NewValue);
public sealed record HistoryQueryDto(string EntityType, long EntityId, int Page = 1, int PageSize = 100);

// Modifier CRUD DTOs
public sealed record CreateModifierDto(string Name, string? Description, bool IsRequired, bool AllowMultiple, int? MinSelections, int? MaxSelections, IReadOnlyList<CreateModifierOptionDto> Options);
public sealed record UpdateModifierDto(string? Name, string? Description, bool? IsRequired, bool? AllowMultiple, int? MinSelections, int? MaxSelections, IReadOnlyList<CreateModifierOptionDto>? Options);
public sealed record CreateModifierOptionDto(string Name, decimal PriceDelta, bool IsAvailable, int SortOrder);
public sealed record UpdateModifierOptionDto(string? Name, decimal? PriceDelta, bool? IsAvailable, int? SortOrder);
public sealed record ModifierQueryDto(string? Q, int Page = 1, int PageSize = 50);
