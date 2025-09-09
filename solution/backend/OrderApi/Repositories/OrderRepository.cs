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

    public async Task<long> CreateOrderAsync(OrderDto order, IReadOnlyList<OrderItemDto> items, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string insOrder = @"INSERT INTO ord.orders(session_id, billing_id, table_id, server_id, server_name, status, subtotal, discount_total, tax_total, total, profit_total)
                                 VALUES(@sid, @bid, @tid, @srvId, @srvName, @st, @sub, @disc, @tax, @tot, @profit)
                                 RETURNING order_id";
        await using var cmd = new NpgsqlCommand(insOrder, conn, tx);
        cmd.Parameters.AddWithValue("@sid", order.SessionId);
        cmd.Parameters.AddWithValue("@bid", (object?)order.TableId ?? DBNull.Value); // Note: OrderDto uses TableId; billing id not exposed here
        cmd.Parameters.AddWithValue("@tid", order.TableId);
        cmd.Parameters.AddWithValue("@srvId", "");
        cmd.Parameters.AddWithValue("@srvName", (object?)"" ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@st", order.Status);
        cmd.Parameters.AddWithValue("@sub", order.Subtotal);
        cmd.Parameters.AddWithValue("@disc", order.DiscountTotal);
        cmd.Parameters.AddWithValue("@tax", order.TaxTotal);
        cmd.Parameters.AddWithValue("@tot", order.Total);
        cmd.Parameters.AddWithValue("@profit", order.ProfitTotal);
        var orderId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));

        if (items.Count > 0)
        {
            const string insItem = @"INSERT INTO ord.order_items(order_id, menu_item_id, combo_id, quantity, base_price, vendor_price, price_delta, line_discount, line_total, profit, selected_modifiers, snapshot_name, snapshot_sku, snapshot_category, snapshot_group, snapshot_version, snapshot_picture_url)
                                     VALUES(@oid, @mid, @cid, @qty, @base, @vendor, @delta, @ldis, @ltot, @profit, @mods, @sname, @ssku, @scat, @sgrp, @sver, @spic) RETURNING order_item_id";
            foreach (var it in items)
            {
                await using var ic = new NpgsqlCommand(insItem, conn, tx);
                ic.Parameters.AddWithValue("@oid", orderId);
                ic.Parameters.AddWithValue("@mid", (object?)it.MenuItemId ?? DBNull.Value);
                ic.Parameters.AddWithValue("@cid", (object?)it.ComboId ?? DBNull.Value);
                ic.Parameters.AddWithValue("@qty", it.Quantity);
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
        const string sql = @"SELECT order_id, session_id, table_id, status, subtotal, discount_total, tax_total, total, profit_total
                             FROM ord.orders WHERE order_id = @id AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        var order = new OrderDto(rdr.GetInt64(0), rdr.GetString(1), rdr.GetString(2), rdr.GetString(3), rdr.GetDecimal(4), rdr.GetDecimal(5), rdr.GetDecimal(6), rdr.GetDecimal(7), rdr.GetDecimal(8), new List<OrderItemDto>());
        await rdr.CloseAsync();
        const string items = @"SELECT order_item_id, menu_item_id, combo_id, quantity, base_price, price_delta, line_total, profit FROM ord.order_items WHERE order_id = @oid AND is_deleted = false";
        await using var icmd = new NpgsqlCommand(items, conn);
        icmd.Parameters.AddWithValue("@oid", order.Id);
        var list = new List<OrderItemDto>();
        await using var ir = await icmd.ExecuteReaderAsync(ct);
        while (await ir.ReadAsync(ct))
        {
            list.Add(new OrderItemDto(ir.GetInt64(0), ir.IsDBNull(1) ? null : ir.GetInt64(1), ir.IsDBNull(2) ? null : ir.GetInt64(2), ir.GetInt32(3), ir.GetDecimal(4), ir.GetDecimal(5), ir.GetDecimal(6), ir.GetDecimal(7)));
        }
        return order with { Items = list };
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(string sessionId, bool includeHistory, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = includeHistory ? "session_id = @sid" : "session_id = @sid AND status = 'open'";
        await using var cmd = new NpgsqlCommand($@"SELECT order_id, session_id, table_id, status, subtotal, discount_total, tax_total, total, profit_total
                                                  FROM ord.orders WHERE {where} AND is_deleted = false ORDER BY created_at DESC", conn);
        cmd.Parameters.AddWithValue("@sid", sessionId);
        var outList = new List<OrderDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            outList.Add(new OrderDto(rdr.GetInt64(0), rdr.GetString(1), rdr.GetString(2), rdr.GetString(3), rdr.GetDecimal(4), rdr.GetDecimal(5), rdr.GetDecimal(6), rdr.GetDecimal(7), rdr.GetDecimal(8), new List<OrderItemDto>()));
        }
        return outList;
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
}
