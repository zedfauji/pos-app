using OrderApi.Models;

namespace OrderApi.Repositories;

public interface IOrderRepository
{
    Task<Guid> CreateOrderAsync(OrderDto order, IReadOnlyList<OrderItemDto> items, Guid? billingId, Guid? shiftId, CancellationToken ct);
    Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersByBillingIdAsync(Guid billingId, CancellationToken ct);
    Task<IReadOnlyList<OrderItemDto>> GetOrderItemsByBillingIdAsync(Guid billingId, CancellationToken ct);
    Task AddOrderItemsAsync(Guid orderId, IReadOnlyList<OrderItemDto> items, CancellationToken ct);
    Task UpdateOrderItemAsync(Guid orderId, OrderItemDto item, CancellationToken ct);
    Task SoftDeleteOrderItemAsync(Guid orderId, Guid orderItemId, CancellationToken ct);
    Task CloseOrderAsync(Guid orderId, CancellationToken ct);
    Task AppendLogAsync(Guid orderId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct);
    Task ExecuteInTransactionAsync(Func<Npgsql.NpgsqlConnection, Npgsql.NpgsqlTransaction, CancellationToken, Task> action, CancellationToken ct);

    // Snapshots from Menu schema
    Task<(decimal basePrice, decimal vendorPrice, string name, string sku, string category, string? group, int version, string? picture)> GetMenuItemSnapshotAsync(Guid menuItemId, CancellationToken ct);
    Task<(decimal comboPrice, decimal vendorSum)> GetComboSnapshotAsync(long comboId, CancellationToken ct);
    Task<IReadOnlyList<(Guid MenuItemId, int Quantity)>> GetComboItemsAsync(long comboId, CancellationToken ct);

    // Pricing/validation helpers
    Task<(bool isAvailable, bool isDiscountable)> GetMenuItemFlagsAsync(Guid menuItemId, CancellationToken ct);
    Task<(bool isAvailable, bool isDiscountable)> GetComboFlagsAsync(long comboId, CancellationToken ct);
    Task<bool> ValidateComboItemsAvailabilityAsync(long comboId, CancellationToken ct);
    Task<decimal> ComputeModifierDeltaAsync(Guid menuItemId, IReadOnlyList<ModifierSelectionDto> selections, CancellationToken ct);

    // Logs + totals
    Task<(IReadOnlyList<OrderLogDto> Items, int Total)> ListLogsAsync(Guid orderId, int page, int pageSize, CancellationToken ct);
    Task RecalculateTotalsAsync(Guid orderId, CancellationToken ct);

    // Inventory integration (for drinks only)
    Task<bool> CheckInventoryAvailabilityBySkuAsync(IEnumerable<(string Sku, decimal Quantity)> items, CancellationToken ct);
    Task DeductInventoryBySkuAsync(Guid orderId, IEnumerable<(string Sku, decimal Quantity, decimal? UnitCost)> items, CancellationToken ct);

    // Delivery tracking
    Task MarkItemsDeliveredAsync(Guid orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct);
    Task UpdateOrderDeliveryStatusAsync(Guid orderId, string deliveryStatus, CancellationToken ct);
    Task UpdateOrderStatusAsync(Guid orderId, string status, CancellationToken ct);
    
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
