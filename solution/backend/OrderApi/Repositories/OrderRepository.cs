using System.Text.Json;
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

    public async Task<long> CreateOrderAsync(OrderDto order, IReadOnlyList<OrderItemDto> items, Guid? billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string insOrder = @"INSERT INTO ord.orders(session_id, billing_id, table_id, server_id, server_name, status, delivery_status, subtotal, discount_total, tax_total, total, profit_total)
                                 VALUES(@sid, @bid, @tid, @srvId, @srvName, @st, @deliveryStatus, @sub, @disc, @tax, @tot, @profit)
                                 RETURNING order_id";
        await using var cmd = new NpgsqlCommand(insOrder, conn, tx);
        cmd.Parameters.AddWithValue("@sid", order.SessionId);
        cmd.Parameters.AddWithValue("@bid", (object?)billingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tid", order.TableId);
        cmd.Parameters.AddWithValue("@srvId", "");
        cmd.Parameters.AddWithValue("@srvName", (object?)"" ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@st", order.Status);
        cmd.Parameters.AddWithValue("@deliveryStatus", order.DeliveryStatus);
        cmd.Parameters.AddWithValue("@sub", order.Subtotal);
        cmd.Parameters.AddWithValue("@disc", order.DiscountTotal);
        cmd.Parameters.AddWithValue("@tax", order.TaxTotal);
        cmd.Parameters.AddWithValue("@tot", order.Total);
        cmd.Parameters.AddWithValue("@profit", order.ProfitTotal);
        var orderId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));

        if (items.Count > 0)
        {
            const string insItem = @"INSERT INTO ord.order_items(order_id, menu_item_id, combo_id, quantity, delivered_quantity, base_price, vendor_price, price_delta, line_discount, line_total, profit, selected_modifiers, snapshot_name, snapshot_sku, snapshot_category, snapshot_group, snapshot_version, snapshot_picture_url)
                                     VALUES(@oid, @mid, @cid, @qty, @deliveredQty, @base, @vendor, @delta, @ldis, @ltot, @profit, @mods, @sname, @ssku, @scat, @sgrp, @sver, @spic) RETURNING order_item_id";
            foreach (var it in items)
            {
                await using var ic = new NpgsqlCommand(insItem, conn, tx);
                ic.Parameters.AddWithValue("@oid", orderId);
                ic.Parameters.AddWithValue("@mid", (object?)it.MenuItemId ?? DBNull.Value);
                ic.Parameters.AddWithValue("@cid", (object?)it.ComboId ?? DBNull.Value);
                ic.Parameters.AddWithValue("@qty", it.Quantity);
                ic.Parameters.AddWithValue("@deliveredQty", it.DeliveredQuantity);
                ic.Parameters.AddWithValue("@base", it.BasePrice);
                ic.Parameters.AddWithValue("@vendor", it.Profit >= 0 ? it.BasePrice * 0.7m : 0m); // placeholder vendor
                ic.Parameters.AddWithValue("@delta", it.PriceDelta);
                ic.Parameters.AddWithValue("@ldis", 0m);
                ic.Parameters.AddWithValue("@ltot", it.LineTotal);
                ic.Parameters.AddWithValue("@profit", it.Profit);
                ic.Parameters.AddWithValue("@mods", (object?)DBNull.Value);
                ic.Parameters.AddWithValue("@sname", (object?)null ?? DBNull.Value);
                ic.Parameters.AddWithValue("@ssku", (object?)null ?? DBNull.Value);
                ic.Parameters.AddWithValue("@scat", (object?)null ?? DBNull.Value);
                ic.Parameters.AddWithValue("@sgrp", (object?)null ?? DBNull.Value);
                ic.Parameters.AddWithValue("@sver", (object?)null ?? DBNull.Value);
                ic.Parameters.AddWithValue("@spic", (object?)null ?? DBNull.Value);
                await ic.ExecuteScalarAsync(ct);
            }
        }
        await tx.CommitAsync(ct);
        return orderId;
    }

    public async Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT order_id, session_id, table_id, status, delivery_status, subtotal, discount_total, tax_total, total, profit_total
                             FROM ord.orders WHERE order_id = @id AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        var order = new OrderDto(rdr.GetInt64(0), rdr.GetFieldValue<Guid>(1), rdr.GetString(2), rdr.GetString(3), rdr.GetString(4), rdr.GetDecimal(5), rdr.GetDecimal(6), rdr.GetDecimal(7), rdr.GetDecimal(8), rdr.GetDecimal(9), new List<OrderItemDto>());
        await rdr.CloseAsync();
        const string items = @"SELECT order_item_id, menu_item_id, combo_id, quantity, delivered_quantity, base_price, price_delta, line_total, profit FROM ord.order_items WHERE order_id = @oid AND is_deleted = false";
        await using var icmd = new NpgsqlCommand(items, conn);
        icmd.Parameters.AddWithValue("@oid", order.Id);
        var list = new List<OrderItemDto>();
        await using var ir = await icmd.ExecuteReaderAsync(ct);
        while (await ir.ReadAsync(ct))
        {
            list.Add(new OrderItemDto(ir.GetInt64(0), ir.IsDBNull(1) ? null : ir.GetInt64(1), ir.IsDBNull(2) ? null : ir.GetInt64(2), ir.GetInt32(3), ir.GetInt32(4), ir.GetDecimal(5), ir.GetDecimal(6), ir.GetDecimal(7), ir.GetDecimal(8)));
        }
        return order with { Items = list };
    }

    private async Task<List<OrderItemDto>> LoadOrderItemsAsync(long orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string items = @"SELECT order_item_id, menu_item_id, combo_id, quantity, delivered_quantity, base_price, price_delta, line_total, profit FROM ord.order_items WHERE order_id = @oid AND is_deleted = false";
        await using var icmd = new NpgsqlCommand(items, conn);
        icmd.Parameters.AddWithValue("@oid", orderId);
        var list = new List<OrderItemDto>();
        await using var ir = await icmd.ExecuteReaderAsync(ct);
        while (await ir.ReadAsync(ct))
        {
            list.Add(new OrderItemDto(ir.GetInt64(0), ir.IsDBNull(1) ? null : ir.GetInt64(1), ir.IsDBNull(2) ? null : ir.GetInt64(2), ir.GetInt32(3), ir.GetInt32(4), ir.GetDecimal(5), ir.GetDecimal(6), ir.GetDecimal(7), ir.GetDecimal(8)));
        }
        return list;
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = includeHistory ? "session_id = @sid" : "session_id = @sid AND status = 'open'";
        await using var cmd = new NpgsqlCommand($@"SELECT order_id, session_id, table_id, status, delivery_status, subtotal, discount_total, tax_total, total, profit_total
                                                  FROM ord.orders WHERE {where} AND is_deleted = false ORDER BY created_at DESC", conn);
        cmd.Parameters.AddWithValue("@sid", sessionId);
        var outList = new List<OrderDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            var orderId = rdr.GetInt64(0);
            var order = new OrderDto(orderId, rdr.GetFieldValue<Guid>(1), rdr.GetString(2), rdr.GetString(3), rdr.GetString(4), rdr.GetDecimal(5), rdr.GetDecimal(6), rdr.GetDecimal(7), rdr.GetDecimal(8), rdr.GetDecimal(9), new List<OrderItemDto>());
            var items = await LoadOrderItemsAsync(orderId, ct);
            outList.Add(order with { Items = items });
        }
        return outList;
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersByBillingIdAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"SELECT order_id, session_id, table_id, status, delivery_status, subtotal, discount_total, tax_total, total, profit_total
                                                  FROM ord.orders WHERE billing_id = @bid AND is_deleted = false ORDER BY created_at DESC", conn);
        cmd.Parameters.AddWithValue("@bid", billingId);
        var outList = new List<OrderDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            var orderId = rdr.GetInt64(0);
            var order = new OrderDto(orderId, rdr.GetFieldValue<Guid>(1), rdr.GetString(2), rdr.GetString(3), rdr.GetString(4), rdr.GetDecimal(5), rdr.GetDecimal(6), rdr.GetDecimal(7), rdr.GetDecimal(8), rdr.GetDecimal(9), new List<OrderItemDto>());
            var items = await LoadOrderItemsAsync(orderId, ct);
            outList.Add(order with { Items = items });
        }
        return outList;
    }

    public async Task<IReadOnlyList<OrderItemDto>> GetOrderItemsByBillingIdAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            SELECT oi.order_item_id, oi.menu_item_id, oi.combo_id, oi.quantity, oi.base_price, oi.vendor_price, 
                   oi.price_delta, oi.line_discount, oi.line_total, oi.profit,
                   COALESCE(oi.snapshot_name, mi.name, 'Unknown Item') as item_name
            FROM ord.order_items oi
            INNER JOIN ord.orders o ON oi.order_id = o.order_id
            LEFT JOIN menu.menu_items mi ON oi.menu_item_id = mi.menu_item_id
            WHERE o.billing_id = @billingId AND oi.is_deleted = false
            ORDER BY oi.created_at", conn);
        cmd.Parameters.AddWithValue("@billingId", billingId);
        
        var items = new List<OrderItemDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            var item = new OrderItemDto(
                Id: rdr.GetInt64(0),
                MenuItemId: rdr.IsDBNull(1) ? null : rdr.GetInt64(1),
                ComboId: rdr.IsDBNull(2) ? null : rdr.GetInt64(2),
                Quantity: rdr.GetInt32(3),
                DeliveredQuantity: 0, // Not tracked in this query
                BasePrice: rdr.GetDecimal(4),
                PriceDelta: rdr.GetDecimal(6),
                LineTotal: rdr.GetDecimal(8),
                Profit: rdr.GetDecimal(9)
            );
            items.Add(item);
        }
        return items;
    }

    public async Task AddOrderItemsAsync(long orderId, IReadOnlyList<OrderItemDto> items, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string insItem = @"INSERT INTO ord.order_items(order_id, menu_item_id, combo_id, quantity, base_price, vendor_price, price_delta, line_discount, line_total, profit)
                                 VALUES(@oid, @mid, @cid, @qty, @base, @vendor, @delta, @ldis, @ltot, @profit)";
        foreach (var it in items)
        {
            await using var ic = new NpgsqlCommand(insItem, conn, tx);
            ic.Parameters.AddWithValue("@oid", orderId);
            ic.Parameters.AddWithValue("@mid", (object?)it.MenuItemId ?? DBNull.Value);
            ic.Parameters.AddWithValue("@cid", (object?)it.ComboId ?? DBNull.Value);
            ic.Parameters.AddWithValue("@qty", it.Quantity);
            ic.Parameters.AddWithValue("@base", it.BasePrice);
            ic.Parameters.AddWithValue("@vendor", it.Profit >= 0 ? it.BasePrice * 0.7m : 0m);
            ic.Parameters.AddWithValue("@delta", it.PriceDelta);
            ic.Parameters.AddWithValue("@ldis", 0m);
            ic.Parameters.AddWithValue("@ltot", it.LineTotal);
            ic.Parameters.AddWithValue("@profit", it.Profit);
            await ic.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task UpdateOrderItemAsync(long orderId, OrderItemDto item, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string upd = @"UPDATE ord.order_items SET quantity = @q, line_total = @lt, profit = @p WHERE order_item_id = @id AND order_id = @oid";
        await using var cmd = new NpgsqlCommand(upd, conn);
        cmd.Parameters.AddWithValue("@q", item.Quantity);
        cmd.Parameters.AddWithValue("@lt", item.LineTotal);
        cmd.Parameters.AddWithValue("@p", item.Profit);
        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.AddWithValue("@oid", orderId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SoftDeleteOrderItemAsync(long orderId, long orderItemId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string del = @"UPDATE ord.order_items SET is_deleted = true WHERE order_item_id = @id AND order_id = @oid";
        await using var cmd = new NpgsqlCommand(del, conn);
        cmd.Parameters.AddWithValue("@id", orderItemId);
        cmd.Parameters.AddWithValue("@oid", orderId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task CloseOrderAsync(long orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string upd = @"UPDATE ord.orders SET status = 'closed', closed_at = now(), updated_at = now() WHERE order_id = @id";
        await using var cmd = new NpgsqlCommand(upd, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task AppendLogAsync(long orderId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string ins = @"INSERT INTO ord.order_logs(order_id, action, old_value, new_value, server_id) VALUES(@oid, @a, @o, @n, @sid)";
        await using var cmd = new NpgsqlCommand(ins, conn);
        cmd.Parameters.AddWithValue("@oid", orderId);
        cmd.Parameters.AddWithValue("@a", action);
        cmd.Parameters.AddWithValue("@o", oldValue is null ? DBNull.Value : JsonSerializer.SerializeToElement(oldValue));
        cmd.Parameters.AddWithValue("@n", newValue is null ? DBNull.Value : JsonSerializer.SerializeToElement(newValue));
        cmd.Parameters.AddWithValue("@sid", (object?)serverId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<(decimal basePrice, decimal vendorPrice, string name, string sku, string category, string? group, int version, string? picture)> GetMenuItemSnapshotAsync(long menuItemId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT selling_price, vendor_price, name, sku_id, category, group_name, version, picture_url FROM menu.menu_items WHERE menu_item_id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", menuItemId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) throw new KeyNotFoundException("Menu item not found");
        return (rdr.GetDecimal(0), rdr.GetDecimal(1), rdr.GetString(2), rdr.GetString(3), rdr.GetString(4), rdr.IsDBNull(5) ? null : rdr.GetString(5), rdr.GetInt32(6), rdr.IsDBNull(7) ? null : rdr.GetString(7));
    }

    public async Task<(decimal comboPrice, decimal vendorSum)> GetComboSnapshotAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string priceSql = @"SELECT price FROM menu.combos WHERE combo_id = @id";
        await using var c1 = new NpgsqlCommand(priceSql, conn);
        c1.Parameters.AddWithValue("@id", comboId);
        var price = Convert.ToDecimal(await c1.ExecuteScalarAsync(ct));
        const string vSum = @"SELECT COALESCE(SUM(mi.vendor_price * ci.quantity),0) FROM menu.combo_items ci JOIN menu.menu_items mi ON mi.menu_item_id = ci.menu_item_id WHERE ci.combo_id = @id";
        await using var c2 = new NpgsqlCommand(vSum, conn);
        c2.Parameters.AddWithValue("@id", comboId);
        var vendor = Convert.ToDecimal(await c2.ExecuteScalarAsync(ct));
        return (price, vendor);
    }

    public async Task<IReadOnlyList<(long MenuItemId, int Quantity)>> GetComboItemsAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT menu_item_id, quantity FROM menu.combo_items WHERE combo_id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", comboId);
        var list = new List<(long, int)>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add((rdr.GetInt64(0), rdr.GetInt32(1)));
        }
        return list;
    }

    public async Task<(bool isAvailable, bool isDiscountable)> GetMenuItemFlagsAsync(long menuItemId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "SELECT is_available, is_discountable FROM menu.menu_items WHERE menu_item_id = @id AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", menuItemId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) throw new KeyNotFoundException("Menu item not found");
        return (rdr.GetBoolean(0), rdr.GetBoolean(1));
    }

    public async Task<(bool isAvailable, bool isDiscountable)> GetComboFlagsAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "SELECT is_available, is_discountable FROM menu.combos WHERE combo_id = @id AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", comboId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) throw new KeyNotFoundException("Combo not found");
        return (rdr.GetBoolean(0), rdr.GetBoolean(1));
    }

    public async Task<bool> ValidateComboItemsAvailabilityAsync(long comboId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT COUNT(*) FILTER (WHERE mi.is_available = false) AS unavailable_count
                             FROM menu.combo_items ci JOIN menu.menu_items mi ON mi.menu_item_id = ci.menu_item_id
                             WHERE ci.combo_id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", comboId);
        var cnt = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        return cnt == 0;
    }

    public async Task<decimal> ComputeModifierDeltaAsync(long menuItemId, IReadOnlyList<ModifierSelectionDto> selections, CancellationToken ct)
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
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@item", menuItemId);
        cmd.Parameters.AddWithValue("@optIds", optionIds);
        var sum = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToDecimal(sum);
    }

    public async Task<(IReadOnlyList<OrderLogDto> Items, int Total)> ListLogsAsync(long orderId, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var limit = Math.Clamp(pageSize, 1, 200);
        var offset = (Math.Max(1, page) - 1) * limit;
        const string sql = @"SELECT log_id, order_id, action, old_value, new_value, server_id, created_at
                             FROM ord.order_logs WHERE order_id = @oid
                             ORDER BY created_at DESC LIMIT @l OFFSET @o";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@oid", orderId);
        cmd.Parameters.AddWithValue("@l", limit);
        cmd.Parameters.AddWithValue("@o", offset);
        var list = new List<OrderLogDto>();
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
        {
            while (await rdr.ReadAsync(ct))
            {
                list.Add(new OrderLogDto(
                    rdr.GetInt64(0), rdr.GetInt64(1), rdr.GetString(2),
                    rdr.IsDBNull(3) ? null : rdr.GetFieldValue<object>(3),
                    rdr.IsDBNull(4) ? null : rdr.GetFieldValue<object>(4),
                    rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    rdr.GetFieldValue<DateTimeOffset>(6)
                ));
            }
        }
        const string cnt = "SELECT COUNT(1) FROM ord.order_logs WHERE order_id = @oid";
        await using var ccmd = new NpgsqlCommand(cnt, conn);
        ccmd.Parameters.AddWithValue("@oid", orderId);
        var total = Convert.ToInt32(await ccmd.ExecuteScalarAsync(ct));
        return (list, total);
    }

    public async Task RecalculateTotalsAsync(long orderId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string sumSql = @"SELECT COALESCE(SUM(line_total),0), COALESCE(SUM(profit),0)
                               FROM ord.order_items WHERE order_id = @oid AND is_deleted = false";
        await using var sumCmd = new NpgsqlCommand(sumSql, conn, tx);
        sumCmd.Parameters.AddWithValue("@oid", orderId);
        decimal subtotal = 0m, profit = 0m;
        await using (var rdr = await sumCmd.ExecuteReaderAsync(ct))
        {
            if (await rdr.ReadAsync(ct))
            {
                subtotal = rdr.IsDBNull(0) ? 0m : rdr.GetDecimal(0);
                profit = rdr.IsDBNull(1) ? 0m : rdr.GetDecimal(1);
            }
        }
        // Keep existing discount and tax
        const string getDT = "SELECT discount_total, tax_total FROM ord.orders WHERE order_id = @oid";
        await using var dtCmd = new NpgsqlCommand(getDT, conn, tx);
        dtCmd.Parameters.AddWithValue("@oid", orderId);
        decimal discount = 0m, tax = 0m;
        await using (var rr = await dtCmd.ExecuteReaderAsync(ct))
        {
            if (await rr.ReadAsync(ct))
            {
                discount = rr.GetDecimal(0);
                tax = rr.GetDecimal(1);
            }
        }
        var total = subtotal - discount + tax;
        const string upd = @"UPDATE ord.orders SET subtotal = @sub, profit_total = @prof, total = @tot, updated_at = now() WHERE order_id = @oid";
        await using var updCmd = new NpgsqlCommand(upd, conn, tx);
        updCmd.Parameters.AddWithValue("@sub", subtotal);
        updCmd.Parameters.AddWithValue("@prof", profit);
        updCmd.Parameters.AddWithValue("@tot", total);
        updCmd.Parameters.AddWithValue("@oid", orderId);
        await updCmd.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task MarkItemsDeliveredAsync(long orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        foreach (var delivery in itemDeliveries)
        {
            const string updateSql = @"UPDATE ord.order_items 
                                     SET delivered_quantity = @deliveredQty, updated_at = now() 
                                     WHERE order_item_id = @orderItemId AND order_id = @orderId";
            await using var cmd = new NpgsqlCommand(updateSql, conn, tx);
            cmd.Parameters.AddWithValue("@deliveredQty", delivery.DeliveredQuantity);
            cmd.Parameters.AddWithValue("@orderItemId", delivery.OrderItemId);
            cmd.Parameters.AddWithValue("@orderId", orderId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        
        await tx.CommitAsync(ct);
    }

    public async Task UpdateOrderDeliveryStatusAsync(long orderId, string deliveryStatus, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string updateSql = @"UPDATE ord.orders 
                                 SET delivery_status = @deliveryStatus, updated_at = now() 
                                 WHERE order_id = @orderId";
        await using var cmd = new NpgsqlCommand(updateSql, conn);
        cmd.Parameters.AddWithValue("@deliveryStatus", deliveryStatus);
        cmd.Parameters.AddWithValue("@orderId", orderId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateOrderStatusAsync(long orderId, string status, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string updateSql = @"UPDATE ord.orders 
                                 SET status = @status, updated_at = now() 
                                 WHERE order_id = @orderId";
        await using var cmd = new NpgsqlCommand(updateSql, conn);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@orderId", orderId);
        await cmd.ExecuteNonQueryAsync(ct);
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
        const string ordersTodaySql = @"SELECT COUNT(*) FROM ord.orders 
                                       WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false";
        await using var ordersTodayCmd = new NpgsqlCommand(ordersTodaySql, conn);
        var ordersToday = Convert.ToInt32(await ordersTodayCmd.ExecuteScalarAsync(ct));

        // Revenue Today
        const string revenueTodaySql = @"SELECT COALESCE(SUM(total), 0) FROM ord.orders 
                                       WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false";
        await using var revenueTodayCmd = new NpgsqlCommand(revenueTodaySql, conn);
        var revenueToday = Convert.ToDecimal(await revenueTodayCmd.ExecuteScalarAsync(ct));

        // Average Order Value
        var averageOrderValue = ordersToday > 0 ? revenueToday / ordersToday : 0m;

        // Completion Rate (orders closed vs total)
        const string completionRateSql = @"SELECT 
            COUNT(*) FILTER (WHERE status = 'closed') as completed,
            COUNT(*) as total
            FROM ord.orders 
            WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false";
        await using var completionRateCmd = new NpgsqlCommand(completionRateSql, conn);
        await using var completionRateRdr = await completionRateCmd.ExecuteReaderAsync(ct);
        var completionRate = 0m;
        if (await completionRateRdr.ReadAsync(ct))
        {
            var completed = completionRateRdr.GetInt32(0);
            var total = completionRateRdr.GetInt32(1);
            completionRate = total > 0 ? (decimal)completed / total * 100 : 0m;
        }
        await completionRateRdr.CloseAsync();

        // Status Monitoring (scoped to selected date range)
        const string statusSql = @"SELECT 
            COUNT(*) FILTER (WHERE status = 'open' AND delivery_status = 'pending' AND created_at >= @fromDate AND created_at < @toDatePlusOne) as pending,
            COUNT(*) FILTER (WHERE status = 'open' AND delivery_status = 'in_progress' AND created_at >= @fromDate AND created_at < @toDatePlusOne) as in_progress,
            COUNT(*) FILTER (WHERE status = 'open' AND delivery_status = 'ready' AND created_at >= @fromDate AND created_at < @toDatePlusOne) as ready,
            COUNT(*) FILTER (WHERE status = 'closed' AND closed_at IS NOT NULL AND closed_at >= @fromDate AND closed_at < @toDatePlusOne) as completed_in_range
            FROM ord.orders 
            WHERE is_deleted = false";
        await using var statusCmd = new NpgsqlCommand(statusSql, conn);
        statusCmd.Parameters.AddWithValue("@fromDate", fromDate);
        statusCmd.Parameters.AddWithValue("@toDatePlusOne", toDate.AddDays(1));
        await using var statusRdr = await statusCmd.ExecuteReaderAsync(ct);
        var pendingOrders = 0;
        var inProgressOrders = 0;
        var readyForDeliveryOrders = 0;
        var completedTodayOrders = 0;
        if (await statusRdr.ReadAsync(ct))
        {
            pendingOrders = statusRdr.GetInt32(0);
            inProgressOrders = statusRdr.GetInt32(1);
            readyForDeliveryOrders = statusRdr.GetInt32(2);
            completedTodayOrders = statusRdr.GetInt32(3);
        }
        await statusRdr.CloseAsync();

        // Performance Metrics (simplified calculations)
        const string prepTimeSql = @"SELECT AVG(EXTRACT(EPOCH FROM (closed_at - created_at))/60) 
                                   FROM ord.orders 
                                   WHERE status = 'closed' AND closed_at IS NOT NULL 
                                   AND DATE(closed_at) = CURRENT_DATE AND is_deleted = false";
        await using var prepTimeCmd = new NpgsqlCommand(prepTimeSql, conn);
        var avgPrepTimeResult = await prepTimeCmd.ExecuteScalarAsync(ct);
        var averagePrepTimeMinutes = avgPrepTimeResult != DBNull.Value ? Convert.ToInt32(Convert.ToDouble(avgPrepTimeResult)) : 15;

        // Peak Hour (simplified - using most common hour)
        const string peakHourSql = @"SELECT EXTRACT(HOUR FROM created_at) as hour, COUNT(*) as count
                                   FROM ord.orders 
                                   WHERE DATE(created_at) = CURRENT_DATE AND is_deleted = false
                                   GROUP BY EXTRACT(HOUR FROM created_at)
                                   ORDER BY count DESC LIMIT 1";
        await using var peakHourCmd = new NpgsqlCommand(peakHourSql, conn);
        await using var peakHourRdr = await peakHourCmd.ExecuteReaderAsync(ct);
        var peakHour = "14:00"; // Default
        if (await peakHourRdr.ReadAsync(ct))
        {
            var hour = Convert.ToInt32(peakHourRdr.GetDouble(0));
            peakHour = $"{hour:00}:00";
        }
        await peakHourRdr.CloseAsync();

        // Efficiency Score (simplified calculation)
        var efficiencyScore = Math.Min(95m, Math.Max(70m, completionRate + (ordersToday > 0 ? 10m : 0m)));

        // Total Orders and Revenue for period
        const string totalSql = @"SELECT COUNT(*), COALESCE(SUM(total), 0) 
                                FROM ord.orders 
                                WHERE created_at >= @fromDate AND created_at <= @toDate AND is_deleted = false";
        await using var totalCmd = new NpgsqlCommand(totalSql, conn);
        totalCmd.Parameters.AddWithValue("@fromDate", fromDate);
        totalCmd.Parameters.AddWithValue("@toDate", toDate.AddDays(1)); // Include end date
        await using var totalRdr = await totalCmd.ExecuteReaderAsync(ct);
        var totalOrders = 0;
        var totalRevenue = 0m;
        if (await totalRdr.ReadAsync(ct))
        {
            totalOrders = totalRdr.GetInt32(0);
            totalRevenue = totalRdr.GetDecimal(1);
        }
        await totalRdr.CloseAsync();

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
            END as title,
            'Table ' || table_id || ' - $' || total::text as description,
            CASE 
                WHEN created_at > NOW() - INTERVAL '1 minute' THEN 'Just now'
                WHEN created_at > NOW() - INTERVAL '1 hour' THEN EXTRACT(MINUTE FROM (NOW() - created_at))::text || ' min ago'
                WHEN created_at > NOW() - INTERVAL '1 day' THEN EXTRACT(HOUR FROM (NOW() - created_at))::text || ' hour ago'
                ELSE created_at::date::text
            END as timestamp,
            CASE 
                WHEN status = 'closed' THEN 'completed'
                WHEN delivery_status = 'pending' THEN 'received'
                WHEN delivery_status = 'in_progress' THEN 'in_progress'
                WHEN delivery_status = 'ready' THEN 'ready'
                ELSE 'updated'
            END as activity_type
            FROM ord.orders 
            WHERE is_deleted = false
            ORDER BY created_at DESC 
            LIMIT @limit";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@limit", Math.Clamp(limit, 1, 50));
        
        var activities = new List<RecentActivityDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            activities.Add(new RecentActivityDto(
                rdr.GetString(0),
                rdr.GetString(1),
                rdr.GetString(2),
                rdr.GetString(3)
            ));
        }
        
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
            END as status,
            COUNT(*) as count,
            ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 1) as percentage
            FROM ord.orders 
            WHERE is_deleted = false
            GROUP BY 
                CASE 
                    WHEN status = 'open' AND delivery_status = 'pending' THEN 'Pending'
                    WHEN status = 'open' AND delivery_status = 'in_progress' THEN 'In Progress'
                    WHEN status = 'open' AND delivery_status = 'ready' THEN 'Ready'
                    WHEN status = 'closed' THEN 'Completed'
                    ELSE 'Other'
                END
            ORDER BY count DESC";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        var summaries = new List<OrderStatusSummaryDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            summaries.Add(new OrderStatusSummaryDto(
                rdr.GetString(0),
                rdr.GetInt32(1),
                rdr.GetDecimal(2)
            ));
        }
        
        return summaries;
    }

    public async Task<IReadOnlyList<OrderTrendDto>> GetOrderTrendsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
            DATE(created_at) as date,
            COUNT(*) as order_count,
            COALESCE(SUM(total), 0) as revenue,
            CASE 
                WHEN COUNT(*) > 0 THEN COALESCE(SUM(total), 0) / COUNT(*)
                ELSE 0
            END as avg_order_value
            FROM ord.orders 
            WHERE created_at >= @fromDate AND created_at <= @toDate AND is_deleted = false
            GROUP BY DATE(created_at)
            ORDER BY date";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@fromDate", fromDate);
        cmd.Parameters.AddWithValue("@toDate", toDate.AddDays(1)); // Include end date
        
        var trends = new List<OrderTrendDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            trends.Add(new OrderTrendDto(
                rdr.GetDateTime(0),
                rdr.GetInt32(1),
                rdr.GetDecimal(2),
                rdr.GetDecimal(3)
            ));
        }
        
        return trends;
    }
}
