using OrderApi.Models;

namespace OrderApi.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequestDto req, CancellationToken ct);
    Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(string sessionId, bool includeHistory, CancellationToken ct);
    Task<OrderDto> AddItemsAsync(long orderId, IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct);
    Task<OrderDto> UpdateItemAsync(long orderId, UpdateOrderItemDto item, CancellationToken ct);
    Task<OrderDto> DeleteItemAsync(long orderId, long orderItemId, CancellationToken ct);
    Task<OrderDto> CloseOrderAsync(long orderId, CancellationToken ct);
    Task<PagedResult<OrderLogDto>> ListLogsAsync(long orderId, int page, int pageSize, CancellationToken ct);
    Task RecalculateTotalsAsync(long orderId, CancellationToken ct);
}
