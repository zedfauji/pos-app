using OrderApi.Models;

namespace OrderApi.Repositories;

public interface IOrderRepository
{
    Task<long> CreateOrderAsync(OrderDto order, IReadOnlyList<OrderItemDto> items, Guid? billingId, CancellationToken ct);
    Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersByBillingIdAsync(Guid billingId, CancellationToken ct);
    Task<IReadOnlyList<OrderItemDto>> GetOrderItemsByBillingIdAsync(Guid billingId, CancellationToken ct);
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

    // Inventory integration (for drinks only)
    Task<bool> CheckInventoryAvailabilityBySkuAsync(IEnumerable<(string Sku, decimal Quantity)> items, CancellationToken ct);
    Task DeductInventoryBySkuAsync(long orderId, IEnumerable<(string Sku, decimal Quantity, decimal? UnitCost)> items, CancellationToken ct);

    // Delivery tracking
    Task MarkItemsDeliveredAsync(long orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct);
    Task UpdateOrderDeliveryStatusAsync(long orderId, string deliveryStatus, CancellationToken ct);
    Task UpdateOrderStatusAsync(long orderId, string status, CancellationToken ct);
    
    // Analytics methods
    Task<(int OrdersToday, decimal RevenueToday, decimal AverageOrderValue, decimal CompletionRate,
          int PendingOrders, int InProgressOrders, int ReadyForDeliveryOrders, int CompletedTodayOrders,
          int AveragePrepTimeMinutes, string PeakHour, decimal EfficiencyScore,
          int TotalOrders, decimal TotalRevenue, int AverageOrderTimeMinutes, 
          decimal CustomerSatisfactionRate, decimal ReturnRate, int AlertCount, string AlertMessage)> 
        GetOrderAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct);
    
    Task<IReadOnlyList<RecentActivityDto>> GetRecentOrderActivitiesAsync(int limit, CancellationToken ct);
    Task<IReadOnlyList<OrderStatusSummaryDto>> GetOrderStatusSummaryAsync(CancellationToken ct);
    Task<IReadOnlyList<OrderTrendDto>> GetOrderTrendsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct);
}
