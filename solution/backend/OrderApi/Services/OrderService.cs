using OrderApi.Models;
using OrderApi.Repositories;

namespace OrderApi.Services;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequestDto req, CancellationToken ct)
    {
        var pricedItems = new List<OrderItemDto>();
        foreach (var i in req.Items)
        {
            var qty = Math.Max(1, i.Quantity);
            if (i.MenuItemId is null && i.ComboId is null)
                throw new InvalidOperationException("ITEM_OR_COMBO_REQUIRED");

            if (i.MenuItemId is not null)
            {
                var (isAvail, _) = await _repo.GetMenuItemFlagsAsync(i.MenuItemId.Value, ct);
                if (!isAvail) throw new InvalidOperationException("ITEM_UNAVAILABLE");
                var snap = await _repo.GetMenuItemSnapshotAsync(i.MenuItemId.Value, ct);
                var delta = await _repo.ComputeModifierDeltaAsync(i.MenuItemId.Value, i.Modifiers, ct);
                var basePrice = snap.basePrice;
                var vendor = snap.vendorPrice;
                var lineTotal = (basePrice + delta) * qty;
                var profit = (basePrice + delta - vendor) * qty;
                pricedItems.Add(new OrderItemDto(0, i.MenuItemId, null, qty, basePrice, delta, lineTotal, profit));
            }
            else if (i.ComboId is not null)
            {
                var (isAvail, _) = await _repo.GetComboFlagsAsync(i.ComboId.Value, ct);
                if (!isAvail) throw new InvalidOperationException("COMBO_UNAVAILABLE");
                var allAvail = await _repo.ValidateComboItemsAvailabilityAsync(i.ComboId.Value, ct);
                if (!allAvail) throw new InvalidOperationException("COMBO_ITEMS_UNAVAILABLE");
                var (comboPrice, vendorSum) = await _repo.GetComboSnapshotAsync(i.ComboId.Value, ct);
                var basePrice = comboPrice;
                var delta = 0m; // combo-level modifiers not implemented
                var lineTotal = (basePrice + delta) * qty;
                var profit = (basePrice + delta - vendorSum) * qty;
                pricedItems.Add(new OrderItemDto(0, null, i.ComboId, qty, basePrice, delta, lineTotal, profit));
            }
        }

        var subtotal = pricedItems.Sum(x => x.LineTotal);
        var order = new OrderDto(0, req.SessionId, req.TableId, "open", subtotal, 0m, 0m, subtotal, pricedItems.Sum(x => x.Profit), pricedItems);

        // Inventory checks by SKU derived from menu snapshots
        var skuQty = new List<(string Sku, decimal Quantity)>();
        foreach (var it in pricedItems)
        {
            if (it.MenuItemId is not null)
            {
                var snap = await _repo.GetMenuItemSnapshotAsync(it.MenuItemId.Value, ct);
                skuQty.Add((snap.sku, it.Quantity));
            }
            else if (it.ComboId is not null)
            {
                // Expand combo to items: aggregate menu item SKUs by quantity via repository
                var links = await _repo.GetComboItemsAsync(it.ComboId.Value, ct);
                foreach (var link in links)
                {
                    var snap = await _repo.GetMenuItemSnapshotAsync(link.MenuItemId, ct);
                    skuQty.Add((snap.sku, it.Quantity * link.Quantity));
                }
            }
        }
        if (skuQty.Count > 0)
        {
            var ok = await _repo.CheckInventoryAvailabilityBySkuAsync(skuQty, ct);
            if (!ok) throw new InvalidOperationException("INSUFFICIENT_STOCK");
        }

        var orderId = await _repo.CreateOrderAsync(order, pricedItems, ct);
        await _repo.AppendLogAsync(orderId, "create", null, order, req.ServerId, ct);

        // Deduct inventory (best-effort; throw on failure to keep consistency)
        if (skuQty.Count > 0)
        {
            var skuQtyWithCost = skuQty.Select(x => (x.Sku, x.Quantity, (decimal?)null));
            await _repo.DeductInventoryBySkuAsync(orderId, skuQtyWithCost, ct);
        }

        var created = await _repo.GetOrderAsync(orderId, ct);
        return created!;
    }

    public Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
        => _repo.GetOrderAsync(orderId, ct);

    public Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(string sessionId, bool includeHistory, CancellationToken ct)
        => _repo.GetOrdersBySessionAsync(sessionId, includeHistory, ct);

    public async Task<OrderDto> AddItemsAsync(long orderId, IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct)
    {
        var mapped = new List<OrderItemDto>();
        foreach (var i in items)
        {
            var qty = Math.Max(1, i.Quantity);
            if (i.MenuItemId is null && i.ComboId is null)
                throw new InvalidOperationException("ITEM_OR_COMBO_REQUIRED");

            if (i.MenuItemId is not null)
            {
                var (isAvail, _) = await _repo.GetMenuItemFlagsAsync(i.MenuItemId.Value, ct);
                if (!isAvail) throw new InvalidOperationException("ITEM_UNAVAILABLE");
                var snap = await _repo.GetMenuItemSnapshotAsync(i.MenuItemId.Value, ct);
                var delta = await _repo.ComputeModifierDeltaAsync(i.MenuItemId.Value, i.Modifiers, ct);
                var basePrice = snap.basePrice;
                var vendor = snap.vendorPrice;
                var lineTotal = (basePrice + delta) * qty;
                var profit = (basePrice + delta - vendor) * qty;
                mapped.Add(new OrderItemDto(0, i.MenuItemId, null, qty, basePrice, delta, lineTotal, profit));
            }
            else if (i.ComboId is not null)
            {
                var (isAvail, _) = await _repo.GetComboFlagsAsync(i.ComboId.Value, ct);
                if (!isAvail) throw new InvalidOperationException("COMBO_UNAVAILABLE");
                var allAvail = await _repo.ValidateComboItemsAvailabilityAsync(i.ComboId.Value, ct);
                if (!allAvail) throw new InvalidOperationException("COMBO_ITEMS_UNAVAILABLE");
                var (comboPrice, vendorSum) = await _repo.GetComboSnapshotAsync(i.ComboId.Value, ct);
                var basePrice = comboPrice;
                var delta = 0m;
                var lineTotal = (basePrice + delta) * qty;
                var profit = (basePrice + delta - vendorSum) * qty;
                mapped.Add(new OrderItemDto(0, null, i.ComboId, qty, basePrice, delta, lineTotal, profit));
            }
        }

        await _repo.AddOrderItemsAsync(orderId, mapped, ct);
        await _repo.AppendLogAsync(orderId, "add_items", null, mapped, null, ct);
        await _repo.RecalculateTotalsAsync(orderId, ct);
        var updated = await _repo.GetOrderAsync(orderId, ct);
        return updated!;
    }

    public async Task<OrderDto> UpdateItemAsync(long orderId, UpdateOrderItemDto item, CancellationToken ct)
    {
        var existing = await _repo.GetOrderAsync(orderId, ct) ?? throw new KeyNotFoundException("Order not found");
        var cur = existing.Items.FirstOrDefault(x => x.Id == item.OrderItemId) ?? throw new KeyNotFoundException("Order item not found");
        var qty = item.Quantity ?? cur.Quantity;
        var newLineTotal = (cur.BasePrice + cur.PriceDelta) * qty;
        var newProfit = newLineTotal * 0.3m;
        var updated = cur with { Quantity = qty, LineTotal = newLineTotal, Profit = newProfit };
        await _repo.UpdateOrderItemAsync(orderId, updated, ct);
        await _repo.AppendLogAsync(orderId, "update_item", cur, updated, null, ct);
        await _repo.RecalculateTotalsAsync(orderId, ct);
        var outOrder = await _repo.GetOrderAsync(orderId, ct);
        return outOrder!;
    }

    public async Task<OrderDto> DeleteItemAsync(long orderId, long orderItemId, CancellationToken ct)
    {
        await _repo.SoftDeleteOrderItemAsync(orderId, orderItemId, ct);
        await _repo.AppendLogAsync(orderId, "delete_item", new { OrderItemId = orderItemId }, null, null, ct);
        await _repo.RecalculateTotalsAsync(orderId, ct);
        var outOrder = await _repo.GetOrderAsync(orderId, ct);
        return outOrder!;
    }

    public async Task<OrderDto> CloseOrderAsync(long orderId, CancellationToken ct)
    {
        await _repo.CloseOrderAsync(orderId, ct);
        await _repo.AppendLogAsync(orderId, "close", null, new { Status = "closed" }, null, ct);
        var outOrder = await _repo.GetOrderAsync(orderId, ct);
        return outOrder!;
    }

    public async Task<PagedResult<OrderLogDto>> ListLogsAsync(long orderId, int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await _repo.ListLogsAsync(orderId, page, pageSize, ct);
        return new PagedResult<OrderLogDto>(items, total);
    }

    public async Task RecalculateTotalsAsync(long orderId, CancellationToken ct)
    {
        await _repo.RecalculateTotalsAsync(orderId, ct);
    }
}
