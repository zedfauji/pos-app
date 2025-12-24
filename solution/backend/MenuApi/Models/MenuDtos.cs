using MagiDesk.Shared.DTOs.Menu;
using MagiDesk.Shared.DTOs.Common;

namespace MenuApi.Models;

public sealed record MenuItemDetailsDto(MenuItemDto Item, IReadOnlyList<ModifierDto> Modifiers);

public sealed record ModifierDto(long Id, string Name, bool IsRequired, bool AllowMultiple, int? MaxSelections, IReadOnlyList<ModifierOptionDto> Options);

public sealed record ModifierOptionDto(long Id, string Name, decimal PriceDelta, bool IsAvailable, int SortOrder);

// MenuItemQueryDto is now in MagiDesk.Shared.DTOs.Menu


// Combo DTOs
public sealed record ComboItemLinkDto(Guid MenuItemId, int Quantity, bool IsRequired);
public sealed record ComboDto(long Id, string Name, string? Description, decimal Price, bool IsDiscountable, bool IsAvailable, string? PictureUrl, int Version);
public sealed record ComboDetailsDto(ComboDto Combo, IReadOnlyList<ComboItemLinkDto> Items);
public sealed record ComboQueryDto(string? Q, bool? AvailableOnly, int Page = 1, int PageSize = 50);
public sealed record CreateComboDto(string Name, string? Description, decimal Price, bool IsDiscountable, bool IsAvailable, string? PictureUrl, IReadOnlyList<ComboItemLinkDto> Items);
public sealed record UpdateComboDto(string? Name, string? Description, decimal? Price, bool? IsDiscountable, bool? IsAvailable, string? PictureUrl, IReadOnlyList<ComboItemLinkDto>? Items);

// Availability toggle
public sealed record AvailabilityUpdateDto(bool IsAvailable);

// Combo price compute response
public sealed record ComboItemPriceLineDto(Guid MenuItemId, int Quantity, decimal UnitPrice);
public sealed record ComboPriceResponseDto(decimal ComputedPrice, IReadOnlyList<ComboItemPriceLineDto> Items);

// History DTOs
public sealed record HistoryDto(long Id, string EntityType, long EntityId, string Action, int? Version, DateTimeOffset ChangedAt, string? ChangedBy, object? OldValue, object? NewValue);
public sealed record HistoryQueryDto(string EntityType, long EntityId, int Page = 1, int PageSize = 100);

// Modifier CRUD DTOs
public sealed record CreateModifierDto(string Name, string? Description, bool IsRequired, bool AllowMultiple, int? MinSelections, int? MaxSelections, IReadOnlyList<CreateModifierOptionDto> Options);
public sealed record UpdateModifierDto(string? Name, string? Description, bool? IsRequired, bool? AllowMultiple, int? MinSelections, int? MaxSelections, IReadOnlyList<CreateModifierOptionDto>? Options);
public sealed record CreateModifierOptionDto(string Name, decimal PriceDelta, bool IsAvailable, int SortOrder);
public sealed record UpdateModifierOptionDto(string? Name, decimal? PriceDelta, bool? IsAvailable, int? SortOrder);
public sealed record ModifierQueryDto(string? Q, int Page = 1, int PageSize = 50);
