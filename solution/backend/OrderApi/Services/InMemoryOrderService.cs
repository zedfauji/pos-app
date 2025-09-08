using OrderApi.Models;

namespace OrderApi.Services;

public sealed class InMemoryOrderService : IOrderService
{
    private long _nextOrderId = 1;
    private long _nextOrderItemId = 1;
    private readonly Dictionary<long, OrderDto> _orders = new();

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
            items.Add(new OrderItemDto(itemId, i.MenuItemId, i.ComboId, qty, basePrice, priceDelta, lineTotal, profit));
        }
        var subtotal = items.Sum(x => x.LineTotal);
        var order = new OrderDto(id, req.SessionId, req.TableId, "open", subtotal, 0m, 0m, subtotal, items.Sum(x => x.Profit), items);
        _orders[id] = order;
        return Task.FromResult(order);
    }

    public Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(string sessionId, bool includeHistory, CancellationToken ct)
    {
        var list = _orders.Values.Where(o => o.SessionId == sessionId).OrderBy(o => o.Id).ToList();
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
            itemList.Add(new OrderItemDto(itemId, i.MenuItemId, i.ComboId, qty, basePrice, priceDelta, lineTotal, profit));
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
}
