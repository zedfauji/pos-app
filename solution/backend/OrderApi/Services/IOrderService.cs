using OrderApi.Models;

namespace OrderApi.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequestDto req, CancellationToken ct);
    Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersByBillingIdAsync(Guid billingId, CancellationToken ct);
    Task<OrderDto> AddItemsAsync(long orderId, IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct);
    Task<OrderDto> UpdateItemAsync(long orderId, UpdateOrderItemDto item, CancellationToken ct);
    Task<OrderDto> DeleteItemAsync(long orderId, long orderItemId, CancellationToken ct);
    Task<OrderDto> CloseOrderAsync(long orderId, CancellationToken ct);
    Task<OrderDto?> MarkItemsDeliveredAsync(long orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct);
    Task<OrderDto?> MarkOrderWaitingAsync(long orderId, CancellationToken ct);
    Task<PagedResult<OrderLogDto>> ListLogsAsync(long orderId, int page, int pageSize, CancellationToken ct);
    Task RecalculateTotalsAsync(long orderId, CancellationToken ct);
    
    // Analytics methods
    Task<OrderAnalyticsDto> GetOrderAnalyticsAsync(OrderAnalyticsRequestDto request, CancellationToken ct);
    Task<IReadOnlyList<OrderStatusSummaryDto>> GetOrderStatusSummaryAsync(CancellationToken ct);
    Task<IReadOnlyList<OrderTrendDto>> GetOrderTrendsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct);
}
