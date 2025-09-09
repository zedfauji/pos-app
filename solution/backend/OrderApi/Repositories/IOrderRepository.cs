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

    // Snapshots from Menu schema
    Task<(decimal basePrice, decimal vendorPrice, string name, string sku, string category, string? group, int version, string? picture)> GetMenuItemSnapshotAsync(long menuItemId, CancellationToken ct);
    Task<(decimal comboPrice, decimal vendorSum)> GetComboSnapshotAsync(long comboId, CancellationToken ct);
    Task<IReadOnlyList<(long MenuItemId, int Quantity)>> GetComboItemsAsync(long comboId, CancellationToken ct);

    // Pricing/validation helpers
    Task<(bool isAvailable, bool isDiscountable)> GetMenuItemFlagsAsync(long menuItemId, CancellationToken ct);
    Task<(bool isAvailable, bool isDiscountable)> GetComboFlagsAsync(long comboId, CancellationToken ct);
    Task<bool> ValidateComboItemsAvailabilityAsync(long comboId, CancellationToken ct);
    Task<decimal> ComputeModifierDeltaAsync(long menuItemId, IReadOnlyList<ModifierSelectionDto> selections, CancellationToken ct);

    // Logs + totals
    Task<(IReadOnlyList<OrderLogDto> Items, int Total)> ListLogsAsync(long orderId, int page, int pageSize, CancellationToken ct);
    Task RecalculateTotalsAsync(long orderId, CancellationToken ct);

    // Inventory integration
    Task<bool> CheckInventoryAvailabilityAsync(IEnumerable<(Guid InventoryItemId, decimal Quantity)> items, CancellationToken ct);
    Task DeductInventoryAsync(long orderId, IEnumerable<(Guid InventoryItemId, decimal Quantity, decimal? UnitCost)> items, CancellationToken ct);

    // SKU-based integration (preferred for normalization with MenuApi)
    Task<bool> CheckInventoryAvailabilityBySkuAsync(IEnumerable<(string Sku, decimal Quantity)> items, CancellationToken ct);
    Task DeductInventoryBySkuAsync(long orderId, IEnumerable<(string Sku, decimal Quantity, decimal? UnitCost)> items, CancellationToken ct);
}
