namespace OrderApi.Models;

public sealed record ModifierSelectionDto(Guid ModifierId, Guid OptionId);

public sealed record CreateOrderItemDto(Guid? MenuItemId, long? ComboId, int Quantity, IReadOnlyList<ModifierSelectionDto> Modifiers);

public sealed record CreateOrderRequestDto(Guid SessionId, Guid? BillingId, string TableId, string ServerId, string? ServerName, IReadOnlyList<CreateOrderItemDto> Items, decimal? DiscountTotal = null);

public sealed record UpdateOrderItemDto(Guid OrderItemId, int? Quantity, IReadOnlyList<ModifierSelectionDto>? Modifiers);

public sealed record OrderItemDto(Guid Id, Guid? MenuItemId, long? ComboId, int Quantity, int DeliveredQuantity, decimal BasePrice, decimal PriceDelta, decimal LineTotal, decimal Profit);

public sealed record OrderDto(Guid Id, Guid SessionId, string TableId, string Status, string DeliveryStatus, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total, decimal ProfitTotal, IReadOnlyList<OrderItemDto> Items);

public sealed record OrderLogDto(Guid Id, Guid OrderId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);

public sealed record ItemDeliveryDto(Guid OrderItemId, int DeliveredQuantity);

public sealed record MarkDeliveredRequestDto(IReadOnlyList<ItemDeliveryDto> ItemDeliveries);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
