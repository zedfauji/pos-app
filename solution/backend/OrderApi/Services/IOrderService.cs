using OrderApi.Models;

namespace OrderApi.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequestDto req, Guid? shiftId, CancellationToken ct);
    Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersByBillingIdAsync(Guid billingId, CancellationToken ct);
    Task<IReadOnlyList<OrderItemDto>> GetOrderItemsByBillingIdAsync(Guid billingId, CancellationToken ct);
    Task<OrderDto> AddItemsAsync(Guid orderId, IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct);
    Task<OrderDto> UpdateItemAsync(Guid orderId, UpdateOrderItemDto item, CancellationToken ct);
    Task<OrderDto> DeleteItemAsync(Guid orderId, Guid orderItemId, CancellationToken ct);
    Task<OrderDto> CloseOrderAsync(Guid orderId, CancellationToken ct);
    Task<OrderDto?> MarkItemsDeliveredAsync(Guid orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct);
    Task<OrderDto?> MarkOrderWaitingAsync(Guid orderId, CancellationToken ct);
    Task<PagedResult<OrderLogDto>> ListLogsAsync(Guid orderId, int page, int pageSize, CancellationToken ct);
    Task RecalculateTotalsAsync(Guid orderId, CancellationToken ct);
    
    // Analytics methods
    Task<OrderAnalyticsDto> GetOrderAnalyticsAsync(OrderAnalyticsRequestDto request, CancellationToken ct);
    Task<IReadOnlyList<OrderStatusSummaryDto>> GetOrderStatusSummaryAsync(CancellationToken ct);
    Task<IReadOnlyList<OrderTrendDto>> GetOrderTrendsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct);
}
