using OrderApi.Models;

namespace OrderApi.Repositories;

public interface IOrderRepository
{
    Task<long> CreateOrderAsync(OrderDto order, IReadOnlyList<OrderItemDto> items, CancellationToken ct);
    Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(string sessionId, bool includeHistory, CancellationToken ct);
    Task AddOrderItemsAsync(long orderId, IReadOnlyList<OrderItemDto> items, CancellationToken ct);
    Task UpdateOrderItemAsync(long orderId, OrderItemDto item, CancellationToken ct);
    Task SoftDeleteOrderItemAsync(long orderId, long orderItemId, CancellationToken ct);
    Task CloseOrderAsync(long orderId, CancellationToken ct);
    Task AppendLogAsync(long orderId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct);
    Task ExecuteInTransactionAsync(Func<Npgsql.NpgsqlConnection, Npgsql.NpgsqlTransaction, CancellationToken, Task> action, CancellationToken ct);
}
