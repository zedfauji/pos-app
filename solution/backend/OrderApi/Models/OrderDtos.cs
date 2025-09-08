namespace OrderApi.Models;

public sealed record ModifierSelectionDto(long ModifierId, long OptionId);

public sealed record CreateOrderItemDto(long? MenuItemId, long? ComboId, int Quantity, IReadOnlyList<ModifierSelectionDto> Modifiers);

public sealed record CreateOrderRequestDto(string SessionId, string? BillingId, string TableId, string ServerId, string? ServerName, IReadOnlyList<CreateOrderItemDto> Items);

public sealed record UpdateOrderItemDto(long OrderItemId, int? Quantity, IReadOnlyList<ModifierSelectionDto>? Modifiers);

public sealed record OrderItemDto(long Id, long? MenuItemId, long? ComboId, int Quantity, decimal BasePrice, decimal PriceDelta, decimal LineTotal, decimal Profit);

public sealed record OrderDto(long Id, string SessionId, string TableId, string Status, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total, decimal ProfitTotal, IReadOnlyList<OrderItemDto> Items);
