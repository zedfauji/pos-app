using OrderApi.Models;

namespace OrderApi.Services;

public sealed class InMemoryOrderService : IOrderService
{
    private long _nextOrderId = 1;
    private long _nextOrderItemId = 1;
    private readonly Dictionary<long, OrderDto> _orders = new();
    private readonly Dictionary<long, List<OrderLogDto>> _logs = new();

    public Task<OrderDto> CreateOrderAsync(CreateOrderRequestDto req, CancellationToken ct)
    {
        var id = _nextOrderId++;
        var items = new List<OrderItemDto>();
        foreach (var i in req.Items)
        {
            var itemId = _nextOrderItemId++;
            var qty = Math.Max(1, i.Quantity);
            decimal basePrice = 10m; // placeholder price
            decimal priceDelta = 0m; // modifiers ignored in-memory
            decimal lineTotal = (basePrice + priceDelta) * qty;
            decimal profit = lineTotal * 0.3m; // placeholder profit
            items.Add(new OrderItemDto(itemId, i.MenuItemId, i.ComboId, qty, 0, basePrice, priceDelta, lineTotal, profit));
        }
        var subtotal = items.Sum(x => x.LineTotal);
        var order = new OrderDto(id, req.SessionId, req.TableId, "open", "pending", subtotal, 0m, 0m, subtotal, items.Sum(x => x.Profit), items);
        _orders[id] = order;
        _logs[id] = new List<OrderLogDto>();
        return Task.FromResult(order);
    }

    public Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory, CancellationToken ct)
    {
        var list = _orders.Values.Where(o => o.SessionId == sessionId).OrderBy(o => o.Id).ToList();
        return Task.FromResult((IReadOnlyList<OrderDto>)list);
    }

    public Task<IReadOnlyList<OrderDto>> GetOrdersByBillingIdAsync(Guid billingId, CancellationToken ct)
    {
        // In-memory implementation: return empty list since billing IDs aren't tracked in memory
        // In a real implementation, this would query orders by billing_id
        var list = new List<OrderDto>();
        return Task.FromResult((IReadOnlyList<OrderDto>)list);
    }

    public Task<OrderDto> AddItemsAsync(long orderId, IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct)
    {
        if (!_orders.TryGetValue(orderId, out var order)) throw new KeyNotFoundException("Order not found");
        var itemList = order.Items.ToList();
        foreach (var i in items)
        {
            var itemId = _nextOrderItemId++;
            var qty = Math.Max(1, i.Quantity);
            decimal basePrice = 10m;
            decimal priceDelta = 0m;
            decimal lineTotal = (basePrice + priceDelta) * qty;
            decimal profit = lineTotal * 0.3m;
            itemList.Add(new OrderItemDto(itemId, i.MenuItemId, i.ComboId, qty, 0, basePrice, priceDelta, lineTotal, profit));
        }
        var subtotal = itemList.Sum(x => x.LineTotal);
        var updated = order with { Subtotal = subtotal, Total = subtotal, ProfitTotal = itemList.Sum(x => x.Profit), Items = itemList };
        _orders[orderId] = updated;
        return Task.FromResult(updated);
    }

    public Task<OrderDto> UpdateItemAsync(long orderId, UpdateOrderItemDto item, CancellationToken ct)
    {
        if (!_orders.TryGetValue(orderId, out var order)) throw new KeyNotFoundException("Order not found");
        var list = order.Items.ToList();
        var idx = list.FindIndex(x => x.Id == item.OrderItemId);
        if (idx < 0) throw new KeyNotFoundException("Order item not found");
        var current = list[idx];
        var qty = item.Quantity.HasValue ? Math.Max(1, item.Quantity.Value) : current.Quantity;
        var lineTotal = (current.BasePrice + current.PriceDelta) * qty;
        var profit = lineTotal * 0.3m;
        list[idx] = current with { Quantity = qty, LineTotal = lineTotal, Profit = profit };
        var subtotal = list.Sum(x => x.LineTotal);
        var updated = order with { Subtotal = subtotal, Total = subtotal, ProfitTotal = list.Sum(x => x.Profit), Items = list };
        _orders[orderId] = updated;
        return Task.FromResult(updated);
    }

    public Task<OrderDto> DeleteItemAsync(long orderId, long orderItemId, CancellationToken ct)
    {
        if (!_orders.TryGetValue(orderId, out var order)) throw new KeyNotFoundException("Order not found");
        var list = order.Items.Where(i => i.Id != orderItemId).ToList();
        var subtotal = list.Sum(x => x.LineTotal);
        var updated = order with { Subtotal = subtotal, Total = subtotal, ProfitTotal = list.Sum(x => x.Profit), Items = list };
        _orders[orderId] = updated;
        return Task.FromResult(updated);
    }

    public Task<OrderDto> CloseOrderAsync(long orderId, CancellationToken ct)
    {
        if (!_orders.TryGetValue(orderId, out var order)) throw new KeyNotFoundException("Order not found");
        var closed = order with { Status = "closed" };
        _orders[orderId] = closed;
        return Task.FromResult(closed);
    }

    public Task<PagedResult<OrderLogDto>> ListLogsAsync(long orderId, int page, int pageSize, CancellationToken ct)
    {
        if (!_logs.TryGetValue(orderId, out var list)) list = new List<OrderLogDto>();
        var limit = Math.Clamp(pageSize, 1, 200);
        var offset = (Math.Max(1, page) - 1) * limit;
        var items = list.OrderByDescending(l => l.CreatedAt).Skip(offset).Take(limit).ToList();
        return Task.FromResult(new PagedResult<OrderLogDto>(items, list.Count));
    }

    public Task RecalculateTotalsAsync(long orderId, CancellationToken ct)
    {
        // In-memory recalculation
        if (_orders.TryGetValue(orderId, out var order))
        {
            var subtotal = order.Items.Sum(x => x.LineTotal);
            var profit = order.Items.Sum(x => x.Profit);
            _orders[orderId] = order with { Subtotal = subtotal, ProfitTotal = profit, Total = subtotal };
        }
        return Task.CompletedTask;
    }

    public Task<OrderDto?> MarkItemsDeliveredAsync(long orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct)
    {
        if (!_orders.TryGetValue(orderId, out var order)) 
            return Task.FromResult<OrderDto?>(null);

        // In-memory implementation: just update the order status
        // In a real implementation, this would update delivered quantities in the database
        var updated = order with { DeliveryStatus = "partial" };
        _orders[orderId] = updated;
        
        return Task.FromResult<OrderDto?>(updated);
    }

    public Task<OrderDto?> MarkOrderWaitingAsync(long orderId, CancellationToken ct)
    {
        if (!_orders.TryGetValue(orderId, out var order)) 
            return Task.FromResult<OrderDto?>(null);

        var updated = order with { Status = "waiting" };
        _orders[orderId] = updated;
        
        return Task.FromResult<OrderDto?>(updated);
    }

    // Analytics methods - simplified in-memory implementations
    public Task<OrderAnalyticsDto> GetOrderAnalyticsAsync(OrderAnalyticsRequestDto request, CancellationToken ct)
    {
        var today = DateTime.Today;
        var ordersToday = _orders.Values.Count(o => o.Status != "deleted");
        var revenueToday = _orders.Values.Where(o => o.Status != "deleted").Sum(o => o.Total);
        var averageOrderValue = ordersToday > 0 ? revenueToday / ordersToday : 0m;
        
        var pendingOrders = _orders.Values.Count(o => o.Status == "open" && o.DeliveryStatus == "pending");
        var inProgressOrders = _orders.Values.Count(o => o.Status == "open" && o.DeliveryStatus == "in_progress");
        var readyForDeliveryOrders = _orders.Values.Count(o => o.Status == "open" && o.DeliveryStatus == "ready");
        var completedTodayOrders = _orders.Values.Count(o => o.Status == "closed");
        
        var completionRate = ordersToday > 0 ? (decimal)completedTodayOrders / ordersToday * 100 : 0m;
        
        var recentActivities = new List<RecentActivityDto>
        {
            new("Order #1 Completed", "Table 5 - $45.50", "2 min ago", "completed"),
            new("New Order Received", "Table 8 - 3 items", "5 min ago", "received"),
            new("Order In Progress", "Table 2 - Prep started", "8 min ago", "in_progress")
        };

        var analytics = new OrderAnalyticsDto(
            OrdersToday: ordersToday,
            RevenueToday: revenueToday,
            AverageOrderValue: averageOrderValue,
            CompletionRate: completionRate,
            PendingOrders: pendingOrders,
            InProgressOrders: inProgressOrders,
            ReadyForDeliveryOrders: readyForDeliveryOrders,
            CompletedTodayOrders: completedTodayOrders,
            AveragePrepTimeMinutes: 15,
            PeakHour: "14:00",
            EfficiencyScore: Math.Min(95m, completionRate + 10m),
            TotalOrders: ordersToday,
            TotalRevenue: revenueToday,
            AverageOrderTimeMinutes: 20,
            CustomerSatisfactionRate: Math.Min(98m, completionRate + 5m),
            ReturnRate: Math.Max(1m, Math.Min(5m, 100m - completionRate)),
            AlertCount: pendingOrders > 5 ? 1 : 0,
            AlertMessage: pendingOrders > 5 ? "1 critical issue(s) require attention" : "No critical issues detected",
            RecentActivities: recentActivities
        );

        return Task.FromResult(analytics);
    }

    public Task<IReadOnlyList<OrderStatusSummaryDto>> GetOrderStatusSummaryAsync(CancellationToken ct)
    {
        var summaries = new List<OrderStatusSummaryDto>
        {
            new("Pending", _orders.Values.Count(o => o.Status == "open" && o.DeliveryStatus == "pending"), 25m),
            new("In Progress", _orders.Values.Count(o => o.Status == "open" && o.DeliveryStatus == "in_progress"), 35m),
            new("Ready", _orders.Values.Count(o => o.Status == "open" && o.DeliveryStatus == "ready"), 20m),
            new("Completed", _orders.Values.Count(o => o.Status == "closed"), 20m)
        };

        return Task.FromResult((IReadOnlyList<OrderStatusSummaryDto>)summaries);
    }

    public Task<IReadOnlyList<OrderTrendDto>> GetOrderTrendsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var trends = new List<OrderTrendDto>();
        var currentDate = fromDate;
        
        while (currentDate <= toDate)
        {
            var dayOrders = _orders.Values.Count(o => o.Status != "deleted");
            var dayRevenue = _orders.Values.Where(o => o.Status != "deleted").Sum(o => o.Total);
            var avgOrderValue = dayOrders > 0 ? dayRevenue / dayOrders : 0m;
            
            trends.Add(new OrderTrendDto(currentDate, dayOrders, dayRevenue, avgOrderValue));
            currentDate = currentDate.AddDays(1);
        }

        return Task.FromResult((IReadOnlyList<OrderTrendDto>)trends);
    }
}
