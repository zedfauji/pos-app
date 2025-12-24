using System.Text.Json;
using Dapper;
using Npgsql;
using OrderApi.Models;

namespace OrderApi.Repositories;

public sealed partial class OrderRepository : IOrderRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IHttpClientFactory _httpFactory;

    public OrderRepository(NpgsqlDataSource dataSource, IHttpClientFactory httpFactory)
    {
        _dataSource = dataSource;
        _httpFactory = httpFactory;
    }

    public async Task ExecuteInTransactionAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> action, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await action(conn, tx, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }


    public async Task<Guid> CreateOrderAsync(OrderDto order, IReadOnlyList<OrderItemDto> items, Guid? billingId, Guid? shiftId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        // AUTO-BILLING: If billingId is missing, find or create an open bill for this session
        var effectiveBillingId = billingId;
        if (effectiveBillingId == null)
        {
            const string findBill = "SELECT bill_id FROM billing.bills WHERE session_id = @sid AND status = 'AwaitingPayment' LIMIT 1";
            effectiveBillingId = await conn.ExecuteScalarAsync<Guid?>(findBill, new { sid = order.SessionId }, tx);

            if (effectiveBillingId == null)
            {
                effectiveBillingId = Guid.NewGuid();
                // Ensure shift_id is provided. If null (shouldn't be due to Attribute), use a fallback or fail.
                var billShift = shiftId ?? throw new InvalidOperationException("Shift ID required for billing");

                // Get table_id from session's table_label by joining with tables table
                const string getTableIdSql = @"
                    SELECT COALESCE(t.table_id, '00000000-0000-0000-0000-000000000001'::uuid)
                    FROM public.""TableSessions"" ts
                    LEFT JOIN public.tables t ON t.table_number = ts.table_label
                    WHERE ts.session_id = @sid
                    LIMIT 1";
                
                var tableId = await conn.ExecuteScalarAsync<Guid?>(getTableIdSql, new { sid = order.SessionId }, tx);
                if (tableId == null || tableId == Guid.Empty)
                {
                    tableId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                }

                const string createBill = @"INSERT INTO billing.bills (bill_id, billing_id, session_id, table_id, total_amount, items_total, status) 
                                            VALUES (@bid, @bid, @sid, @tid, 0, 0, 'AwaitingPayment'::billing.bill_status)";

                await conn.ExecuteAsync(createBill, new 
                { 
                    bid = effectiveBillingId, 
                    sid = order.SessionId,
                    tid = tableId.Value
                }, tx);
            }
        }

        const string insOrder = @"INSERT INTO orders.orders(session_id, billing_id, table_id, server_id, server_name, status, delivery_status, subtotal, discount, tax, total, profit_total, shift_id)
                                 VALUES(@sid, @bid, @tid, @srvId, @srvName, @st::orders.order_status, @deliveryStatus, @sub, @disc, @tax, @tot, @profit, @shid)
                                 RETURNING order_id";
                                 
        var orderId = await conn.ExecuteScalarAsync<Guid>(insOrder, new
        {
            sid = order.SessionId,
            bid = effectiveBillingId,
            tid = order.TableId,
            srvId = "",
            srvName = (string?)null,
            st = order.Status,
            deliveryStatus = order.DeliveryStatus,
            sub = order.Subtotal,
            disc = order.DiscountTotal,
            tax = order.TaxTotal,
            tot = order.Total,
            profit = order.ProfitTotal,
            shid = shiftId
        }, tx);

        if (items.Count > 0)
        {
            const string insItem = @"INSERT INTO orders.order_items(order_id, menu_item_id, combo_id, quantity, delivered_quantity, base_price, vendor_price, price_delta, line_discount, line_total, profit, modifiers, snapshot_name, snapshot_sku, snapshot_category, snapshot_group, snapshot_version, snapshot_picture_url, menu_item_version, name)
                                     VALUES(@oid, @mid, @cid, @qty, @deliveredQty, @base, @vendor, @delta, @ldis, @ltot, @profit, @mods, @sname, @ssku, @scat, @sgrp, @sver, @spic, @mver, @itemname) RETURNING order_item_id";
            foreach (var it in items)
            {
                await conn.ExecuteScalarAsync<Guid>(insItem, new
                {
                    oid = orderId,
                    mid = it.MenuItemId,
                    cid = it.ComboId, // Now correctly long? to match database bigint
                    qty = it.Quantity,
                    deliveredQty = it.DeliveredQuantity,
                    @base = it.BasePrice,
                    vendor = it.Profit >= 0 ? it.BasePrice * 0.7m : 0m,
                    delta = it.PriceDelta,
                    ldis = 0m,
                    ltot = it.LineTotal,
                    profit = it.Profit,
                    mods = (object?)null,
                    sname = (string?)null,
                    ssku = (string?)null,
                    scat = (string?)null,
                    sgrp = (string?)null,
                    sver = (int?)null,
                    spic = (string?)null,
                    mver = 1,
                    itemname = "Unknown"
                }, tx);
            }
        }
        await tx.CommitAsync(ct);
        return orderId;

    }

    public async Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT order_id AS Id, session_id AS SessionId, table_id AS TableId, status AS Status, delivery_status AS DeliveryStatus, 
                                    subtotal AS Subtotal, discount AS DiscountTotal, tax AS TaxTotal, total AS Total, profit_total AS ProfitTotal
                             FROM orders.orders WHERE order_id = @id AND is_deleted = false";
        
        var row = await conn.QueryFirstOrDefaultAsync<OrderDbDto>(sql, new { id = orderId });
        if (row is null) return null;
        
        var items = await LoadOrderItemsAsync(row.Id, ct);
        
        return new OrderDto(
            row.Id, 
            row.SessionId, 
            row.TableId, 
            row.Status, 
            row.DeliveryStatus, 
            row.Subtotal, 
            row.DiscountTotal, 
            row.TaxTotal, 
            row.Total, 
            row.ProfitTotal, 
            items
        );
    }
    
    // Private record for DB mapping to avoid Constructor mismatch with Items list
    private record OrderDbDto(Guid Id, Guid SessionId, string TableId, string Status, string DeliveryStatus, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total, decimal ProfitTotal);

    private async Task<List<OrderItemDto>> LoadOrderItemsAsync(Guid orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string items = @"SELECT 
                                    order_item_id AS Id, 
                                    menu_item_id AS MenuItemId, 
                                    combo_id AS ComboId,
                                    quantity AS Quantity, 
                                    delivered_quantity AS DeliveredQuantity, 
                                    base_price AS BasePrice, 
                                    price_delta AS PriceDelta, 
                                    line_total AS LineTotal, 
                                    profit AS Profit 
                               FROM orders.order_items WHERE order_id = @oid AND is_deleted = false";
        
        var list = (await conn.QueryAsync<OrderItemDto>(items, new { oid = orderId })).ToList();
        return list;
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = includeHistory ? "session_id = @sid" : "session_id = @sid AND status = 'open'";
        var sql = $@"SELECT order_id AS Id, session_id AS SessionId, table_id AS TableId, status AS Status, delivery_status AS DeliveryStatus,
                            subtotal AS Subtotal, discount AS DiscountTotal, tax AS TaxTotal, total AS Total, profit_total AS ProfitTotal
                     FROM orders.orders WHERE {where} AND is_deleted = false ORDER BY created_at DESC";
        
        var orders = (await conn.QueryAsync<OrderDto>(sql, new { sid = sessionId })).ToList();
        var outList = new List<OrderDto>();
        
        foreach (var order in orders)
        {
            var items = await LoadOrderItemsAsync(order.Id, ct);
            outList.Add(order with { Items = items });
        }
        return outList;
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersByBillingIdAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT order_id AS Id, session_id AS SessionId, table_id AS TableId, status AS Status, delivery_status AS DeliveryStatus,
                                    subtotal AS Subtotal, discount AS DiscountTotal, tax AS TaxTotal, total AS Total, profit_total AS ProfitTotal
                             FROM orders.orders WHERE billing_id = @bid AND is_deleted = false ORDER BY created_at DESC";
        
        var orders = (await conn.QueryAsync<OrderDto>(sql, new { bid = billingId })).ToList();
        var outList = new List<OrderDto>();
        
        foreach (var order in orders)
        {
            var items = await LoadOrderItemsAsync(order.Id, ct);
            outList.Add(order with { Items = items });
        }
        return outList;
    }

    public async Task<IReadOnlyList<OrderItemDto>> GetOrderItemsByBillingIdAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            SELECT 
                oi.order_item_id AS Id, 
                oi.menu_item_id AS MenuItemId, 
                oi.combo_id AS ComboId,
                oi.quantity AS Quantity, 
                0 AS DeliveredQuantity, 
                oi.base_price AS BasePrice, 
                oi.price_delta AS PriceDelta, 
                oi.line_total AS LineTotal, 
                oi.profit AS Profit
            FROM orders.order_items oi
            INNER JOIN orders.orders o ON oi.order_id = o.order_id
            WHERE o.billing_id = @billingId AND oi.is_deleted = false
            ORDER BY oi.created_at";
        
        var items = (await conn.QueryAsync<OrderItemDto>(sql, new { billingId })).ToList();
        return items;
    }

    public async Task AddOrderItemsAsync(Guid orderId, IReadOnlyList<OrderItemDto> items, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string insItem = @"INSERT INTO orders.order_items(order_id, menu_item_id, combo_id, quantity, base_price, vendor_price, price_delta, line_discount, line_total, profit, menu_item_version, name)
                                 VALUES(@oid, @mid, @cid, @qty, @base, @vendor, @delta, @ldis, @ltot, @profit, 1, 'Unknown')";
        foreach (var it in items)
        {
            await conn.ExecuteAsync(insItem, new
            {
                oid = orderId,
                mid = it.MenuItemId,
                cid = it.ComboId,
                qty = it.Quantity,
                @base = it.BasePrice,
                vendor = it.Profit >= 0 ? it.BasePrice * 0.7m : 0m,
                delta = it.PriceDelta,
                ldis = 0m,
                ltot = it.LineTotal,
                profit = it.Profit
            }, tx);
        }
        await tx.CommitAsync(ct);
    }

    public async Task UpdateOrderItemAsync(Guid orderId, OrderItemDto item, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string upd = @"UPDATE orders.order_items SET quantity = @q, line_total = @lt, profit = @p WHERE order_item_id = @id AND order_id = @oid";
        await conn.ExecuteAsync(upd, new { q = item.Quantity, lt = item.LineTotal, p = item.Profit, id = item.Id, oid = orderId });
    }

    public async Task SoftDeleteOrderItemAsync(Guid orderId, Guid orderItemId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string del = @"UPDATE orders.order_items SET is_deleted = true WHERE order_item_id = @id AND order_id = @oid";
        await conn.ExecuteAsync(del, new { id = orderItemId, oid = orderId });
    }

    public async Task CloseOrderAsync(Guid orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string upd = @"UPDATE orders.orders SET status = 'closed', closed_at = now(), updated_at = now() WHERE order_id = @id";
        await conn.ExecuteAsync(upd, new { id = orderId });
    }

    public async Task AppendLogAsync(Guid orderId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string ins = @"INSERT INTO orders.order_logs(order_id, action, old_value, new_value, server_id) VALUES(@oid, @a, @o, @n, @sid)";
        await conn.ExecuteAsync(ins, new
        {
            oid = orderId,
            a = action,
            o = oldValue is null ? (object?)null : JsonSerializer.SerializeToElement(oldValue),
            n = newValue is null ? (object?)null : JsonSerializer.SerializeToElement(newValue),
            sid = serverId
        });
    }

    public async Task<(decimal basePrice, decimal vendorPrice, string name, string sku, string category, string? group, int version, string? picture)> GetMenuItemSnapshotAsync(Guid menuItemId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT base_price AS selling_price, 0 AS vendor_price, name, sku AS sku_id, category, group_name, version, picture_url FROM menu.menu_items WHERE menu_item_id = @id";
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id = menuItemId });
        if (result == null) throw new KeyNotFoundException("Menu item not found");
        return ((decimal)result.selling_price, (decimal)result.vendor_price, (string)result.name, (string)result.sku_id, 
                (string)result.category, result.group_name, (int)result.version, result.picture_url);
    }

    public async Task<(decimal comboPrice, decimal vendorSum)> GetComboSnapshotAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string priceSql = @"SELECT price FROM menu.combos WHERE combo_id = @id";
        var price = await conn.ExecuteScalarAsync<decimal>(priceSql, new { id = comboId });
        
        const string vSum = @"SELECT COALESCE(SUM(mi.base_price * ci.quantity),0) FROM menu.combo_items ci JOIN menu.menu_items mi ON mi.menu_item_id = ci.menu_item_id WHERE ci.combo_id = @id";
        var vendor = await conn.ExecuteScalarAsync<decimal>(vSum, new { id = comboId });
        return (price, vendor);
    }

    public async Task<IReadOnlyList<(Guid MenuItemId, int Quantity)>> GetComboItemsAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT menu_item_id AS MenuItemId, quantity AS Quantity FROM menu.combo_items WHERE combo_id = @id";
        var results = await conn.QueryAsync<(Guid MenuItemId, int Quantity)>(sql, new { id = comboId });
        return results.ToList();
    }

    public async Task<(bool isAvailable, bool isDiscountable)> GetMenuItemFlagsAsync(Guid menuItemId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "SELECT is_available AS IsAvailable, is_discountable AS IsDiscountable FROM menu.menu_items WHERE menu_item_id = @id";
        var result = await conn.QueryFirstOrDefaultAsync<(bool IsAvailable, bool IsDiscountable)>(sql, new { id = menuItemId });
        if (result == default) throw new KeyNotFoundException("Menu item not found");
        return result;
    }

    public async Task<(bool isAvailable, bool isDiscountable)> GetComboFlagsAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "SELECT is_available AS IsAvailable, is_discountable AS IsDiscountable FROM menu.combos WHERE combo_id = @id AND is_deleted = false";
        var result = await conn.QueryFirstOrDefaultAsync<(bool IsAvailable, bool IsDiscountable)>(sql, new { id = comboId });
        if (result == default) throw new KeyNotFoundException("Combo not found");
        return result;
    }

    public async Task<bool> ValidateComboItemsAvailabilityAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT COUNT(*) FILTER (WHERE mi.is_available = false) AS unavailable_count
                             FROM menu.combo_items ci JOIN menu.menu_items mi ON mi.menu_item_id = ci.menu_item_id
                             WHERE ci.combo_id = @id";
        var cnt = await conn.ExecuteScalarAsync<int>(sql, new { id = comboId });
        return cnt == 0;
    }

    public async Task<decimal> ComputeModifierDeltaAsync(Guid menuItemId, IReadOnlyList<ModifierSelectionDto> selections, CancellationToken ct)
    {
        if (selections.Count == 0) return 0m;
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        // Ensure selected modifiers belong to the item
        const string sql = @"SELECT COALESCE(SUM(mo.price_delta),0)
                             FROM menu.modifier_options mo
                             JOIN menu.modifiers m ON m.modifier_id = mo.modifier_id
                             JOIN menu.menu_item_modifiers mm ON mm.modifier_id = m.modifier_id AND mm.menu_item_id = @item
                             WHERE mo.option_id = ANY(@optIds)";
        var optionIds = selections.Select(s => s.OptionId).Distinct().ToArray();
        var sum = await conn.ExecuteScalarAsync<decimal>(sql, new { item = menuItemId, optIds = optionIds });
        return sum;
    }

    public async Task<(IReadOnlyList<OrderLogDto> Items, int Total)> ListLogsAsync(Guid orderId, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var limit = Math.Clamp(pageSize, 1, 200);
        var offset = (Math.Max(1, page) - 1) * limit;
        const string sql = @"SELECT log_id AS Id, order_id AS OrderId, action AS Action, old_value AS OldValue, new_value AS NewValue, 
                                    server_id AS ServerId, created_at AS CreatedAt
                             FROM orders.order_logs WHERE order_id = @oid
                             ORDER BY created_at DESC LIMIT @l OFFSET @o";
        
        var list = (await conn.QueryAsync<OrderLogDto>(sql, new { oid = orderId, l = limit, o = offset })).ToList();
        
        const string cnt = "SELECT COUNT(1) FROM orders.order_logs WHERE order_id = @oid";
        var total = await conn.ExecuteScalarAsync<int>(cnt, new { oid = orderId });
        return (list, total);
    }

    public async Task RecalculateTotalsAsync(Guid orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string sumSql = @"SELECT COALESCE(SUM(line_total),0), COALESCE(SUM(profit),0)
                               FROM orders.order_items WHERE order_id = @oid AND is_deleted = false";
        
        var result = await conn.QueryFirstOrDefaultAsync<(decimal subtotal, decimal profit)>(sumSql, new { oid = orderId }, tx);
        var subtotal = result.subtotal;
        var profit = result.profit;
        
        // Keep existing discount and tax
        const string getDT = "SELECT discount, tax FROM orders.orders WHERE order_id = @oid";
        var dtResult = await conn.QueryFirstOrDefaultAsync<(decimal discount_total, decimal tax_total)>(getDT, new { oid = orderId }, tx);
        var discount = dtResult.discount_total;
        var tax = dtResult.tax_total;
        
        var total = subtotal - discount + tax;
        const string upd = @"UPDATE orders.orders SET subtotal = @sub, profit_total = @prof, total = @tot, updated_at = now() WHERE order_id = @oid";
        await conn.ExecuteAsync(upd, new { sub = subtotal, prof = profit, tot = total, oid = orderId }, tx);
        await tx.CommitAsync(ct);
    }


    public async Task MarkItemsDeliveredAsync(Guid orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        foreach (var delivery in itemDeliveries)
        {
            const string updateSql = @"UPDATE orders.order_items 
                                     SET delivered_quantity = @deliveredQty, updated_at = now() 
                                     WHERE order_item_id = @orderItemId AND order_id = @orderId";
            await conn.ExecuteAsync(updateSql, new 
            { 
                deliveredQty = delivery.DeliveredQuantity, 
                orderItemId = delivery.OrderItemId, 
                orderId 
            }, tx);
        }
        
        await tx.CommitAsync(ct);
    }

    public async Task UpdateOrderDeliveryStatusAsync(Guid orderId, string deliveryStatus, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string updateSql = @"UPDATE orders.orders 
                                 SET delivery_status = @deliveryStatus, updated_at = now() 
                                 WHERE order_id = @orderId";
        await conn.ExecuteAsync(updateSql, new { deliveryStatus, orderId });
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string status, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string updateSql = @"UPDATE orders.orders 
                                 SET status = @status, updated_at = now() 
                                 WHERE order_id = @orderId";
        await conn.ExecuteAsync(updateSql, new { status, orderId });
    }

    // Analytics methods
    public async Task<(int OrdersToday, decimal RevenueToday, decimal AverageOrderValue, decimal CompletionRate,
          int PendingOrders, int InProgressOrders, int ReadyForDeliveryOrders, int CompletedTodayOrders,
          int AveragePrepTimeMinutes, string PeakHour, decimal EfficiencyScore,
          int TotalOrders, decimal TotalRevenue, int AverageOrderTimeMinutes, 
          decimal CustomerSatisfactionRate, decimal ReturnRate, int AlertCount, string AlertMessage)> 
        GetOrderAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        // Orders Today
        const string ordersTodaySql = @"SELECT COUNT(*) FROM orders.orders 
                                       WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false";
        var ordersToday = await conn.ExecuteScalarAsync<int>(ordersTodaySql, null);

        // Revenue Today
        const string revenueTodaySql = @"SELECT COALESCE(SUM(total), 0) FROM orders.orders 
                                       WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false";
        var revenueToday = await conn.ExecuteScalarAsync<decimal>(revenueTodaySql, null);

        // Average Order Value
        var averageOrderValue = ordersToday > 0 ? revenueToday / ordersToday : 0m;

        // Completion Rate (orders closed vs total)
        const string completionRateSql = @"SELECT 
            COUNT(*) FILTER (WHERE status = 'closed') as completed,
            COUNT(*) as total
            FROM orders.orders 
            WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false";
        
        var crResult = await conn.QueryFirstOrDefaultAsync<(int completed, int total)>(completionRateSql, null);
        var completionRate = crResult.total > 0 ? (decimal)crResult.completed / crResult.total * 100 : 0m;

        // Status Monitoring (scoped to selected date range)
        const string statusSql = @"SELECT 
            COUNT(*) FILTER (WHERE status = 'open' AND delivery_status = 'pending' AND created_at >= @fromDate AND created_at < @toDatePlusOne) as pending,
            COUNT(*) FILTER (WHERE status = 'open' AND delivery_status = 'in_progress' AND created_at >= @fromDate AND created_at < @toDatePlusOne) as in_progress,
            COUNT(*) FILTER (WHERE status = 'open' AND delivery_status = 'ready' AND created_at >= @fromDate AND created_at < @toDatePlusOne) as ready,
            COUNT(*) FILTER (WHERE status = 'closed' AND closed_at IS NOT NULL AND closed_at >= @fromDate AND closed_at < @toDatePlusOne) as completed_in_range
            FROM orders.orders 
            WHERE is_deleted = false";
            
        var statusResult = await conn.QueryFirstOrDefaultAsync<(int pending, int inProgress, int ready, int completedInRange)>(statusSql, new { fromDate, toDatePlusOne = toDate.AddDays(1) });
        var pendingOrders = statusResult.pending;
        var inProgressOrders = statusResult.inProgress;
        var readyForDeliveryOrders = statusResult.ready;
        var completedTodayOrders = statusResult.completedInRange;

        // Performance Metrics (simplified calculations)
        const string prepTimeSql = @"SELECT AVG(EXTRACT(EPOCH FROM (closed_at - created_at))/60) 
                                   FROM orders.orders 
                                   WHERE status = 'closed' AND closed_at IS NOT NULL 
                                   AND DATE(closed_at) = CURRENT_DATE AND is_deleted = false";
        var avgPrepTimeResult = await conn.ExecuteScalarAsync<double?>(prepTimeSql, null);
        var averagePrepTimeMinutes = avgPrepTimeResult.HasValue ? Convert.ToInt32(avgPrepTimeResult.Value) : 15;

        // Peak Hour (simplified - using most common hour)
        const string peakHourSql = @"SELECT EXTRACT(HOUR FROM created_at) as hour, COUNT(*) as count
                                   FROM orders.orders 
                                   WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false
                                   GROUP BY EXTRACT(HOUR FROM created_at)
                                   ORDER BY count DESC LIMIT 1";
        var peakHourResult = await conn.ExecuteScalarAsync<double?>(peakHourSql, null);
        var peakHour = peakHourResult.HasValue ? $"{peakHourResult.Value:00}:00" : "14:00";

        // Efficiency Score (simplified calculation)
        var efficiencyScore = Math.Min(95m, Math.Max(70m, completionRate + (ordersToday > 0 ? 10m : 0m)));

        // Total Orders and Revenue for period
        const string totalSql = @"SELECT COUNT(*) AS Count, COALESCE(SUM(total), 0) AS Revenue 
                                FROM orders.orders 
                                WHERE created_at >= @fromDate AND created_at <= @toDate AND is_deleted = false";
        var totalResult = await conn.QueryFirstOrDefaultAsync<(int Count, decimal Revenue)>(totalSql, new { fromDate, toDate = toDate.AddDays(1) });
        var totalOrders = totalResult.Count;
        var totalRevenue = totalResult.Revenue;

        // Average Order Time (simplified)
        var averageOrderTimeMinutes = totalOrders > 0 ? averagePrepTimeMinutes : 20;

        // Customer Satisfaction Rate (simplified - based on completion rate)
        var customerSatisfactionRate = Math.Min(98m, completionRate + 5m);

        // Return Rate (simplified - very low for food service)
        var returnRate = Math.Max(1m, Math.Min(5m, 100m - completionRate));

        // Alert Count and Message
        var alertCount = 0;
        var alertMessage = "No critical issues detected";
        
        if (pendingOrders > 10)
        {
            alertCount++;
            alertMessage = $"{alertCount} critical issue(s) require attention";
        }
        if (completionRate < 80m)
        {
            alertCount++;
            alertMessage = $"{alertCount} critical issue(s) require attention";
        }
        if (averagePrepTimeMinutes > 30)
        {
            alertCount++;
            alertMessage = $"{alertCount} critical issue(s) require attention";
        }

        return (ordersToday, revenueToday, averageOrderValue, completionRate,
                pendingOrders, inProgressOrders, readyForDeliveryOrders, completedTodayOrders,
                averagePrepTimeMinutes, peakHour, efficiencyScore,
                totalOrders, totalRevenue, averageOrderTimeMinutes,
                customerSatisfactionRate, returnRate, alertCount, alertMessage);
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentOrderActivitiesAsync(int limit, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
            'Order #' || order_id || ' ' || 
            CASE 
                WHEN status = 'closed' THEN 'Completed'
                WHEN delivery_status = 'pending' THEN 'Received'
                WHEN delivery_status = 'in_progress' THEN 'In Progress'
                WHEN delivery_status = 'ready' THEN 'Ready for Delivery'
                ELSE 'Updated'
            END as Title,
            'Table ' || table_id || ' - $' || total::text as Description,
            CASE 
                WHEN created_at > NOW() - INTERVAL '1 minute' THEN 'Just now'
                WHEN created_at > NOW() - INTERVAL '1 hour' THEN EXTRACT(MINUTE FROM (NOW() - created_at))::text || ' min ago'
                WHEN created_at > NOW() - INTERVAL '1 day' THEN EXTRACT(HOUR FROM (NOW() - created_at))::text || ' hour ago'
                ELSE created_at::date::text
            END as Timestamp,
            CASE 
                WHEN status = 'closed' THEN 'completed'
                WHEN delivery_status = 'pending' THEN 'received'
                WHEN delivery_status = 'in_progress' THEN 'in_progress'
                WHEN delivery_status = 'ready' THEN 'ready'
                ELSE 'updated'
            END as ActivityType
            FROM orders.orders 
            WHERE is_deleted = false
            ORDER BY created_at DESC 
            LIMIT @limit";
        
        var activities = (await conn.QueryAsync<RecentActivityDto>(sql, new { limit = Math.Clamp(limit, 1, 50) })).ToList();
        return activities;
    }

    public async Task<IReadOnlyList<OrderStatusSummaryDto>> GetOrderStatusSummaryAsync(CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
            CASE 
                WHEN status = 'open' AND delivery_status = 'pending' THEN 'Pending'
                WHEN status = 'open' AND delivery_status = 'in_progress' THEN 'In Progress'
                WHEN status = 'open' AND delivery_status = 'ready' THEN 'Ready'
                WHEN status = 'closed' THEN 'Completed'
                ELSE 'Other'
            END as Status,
            COUNT(*) as Count,
            ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 1) as Percentage
            FROM orders.orders 
            WHERE is_deleted = false
            GROUP BY 
                CASE 
                    WHEN status = 'open' AND delivery_status = 'pending' THEN 'Pending'
                    WHEN status = 'open' AND delivery_status = 'in_progress' THEN 'In Progress'
                    WHEN status = 'open' AND delivery_status = 'ready' THEN 'Ready'
                    WHEN status = 'closed' THEN 'Completed'
                    ELSE 'Other'
                END
            ORDER BY Count DESC";
        
        var summaries = (await conn.QueryAsync<OrderStatusSummaryDto>(sql, null)).ToList();
        return summaries;
    }

    public async Task<IReadOnlyList<OrderTrendDto>> GetOrderTrendsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
            DATE(created_at) as Date,
            COUNT(*) as OrderCount,
            COALESCE(SUM(total), 0) as Revenue,
            CASE 
                WHEN COUNT(*) > 0 THEN COALESCE(SUM(total), 0) / COUNT(*)
                ELSE 0
            END as AverageOrderValue
            FROM orders.orders 
            WHERE created_at >= @fromDate AND created_at <= @toDate AND is_deleted = false
            GROUP BY DATE(created_at)
            ORDER BY Date";
        
        var trends = (await conn.QueryAsync<OrderTrendDto>(sql, new { fromDate, toDate = toDate.AddDays(1) })).ToList();
        return trends;
    }
}
