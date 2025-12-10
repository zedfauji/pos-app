namespace OrderApi.Models;

public sealed record ModifierSelectionDto(long ModifierId, long OptionId);

public sealed record CreateOrderItemDto(long? MenuItemId, long? ComboId, int Quantity, IReadOnlyList<ModifierSelectionDto> Modifiers);

public sealed record CreateOrderRequestDto(Guid SessionId, Guid? BillingId, string TableId, string ServerId, string? ServerName, IReadOnlyList<CreateOrderItemDto> Items);

public sealed record UpdateOrderItemDto(long OrderItemId, int? Quantity, IReadOnlyList<ModifierSelectionDto>? Modifiers);

public sealed record OrderItemDto(long Id, long? MenuItemId, long? ComboId, int Quantity, int DeliveredQuantity, decimal BasePrice, decimal PriceDelta, decimal LineTotal, decimal Profit);

public sealed record OrderDto(long Id, Guid SessionId, string TableId, string Status, string DeliveryStatus, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total, decimal ProfitTotal, IReadOnlyList<OrderItemDto> Items);

public sealed record OrderLogDto(long Id, long OrderId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);

public sealed record ItemDeliveryDto(long OrderItemId, int DeliveredQuantity);

public sealed record MarkDeliveredRequestDto(IReadOnlyList<ItemDeliveryDto> ItemDeliveries);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
