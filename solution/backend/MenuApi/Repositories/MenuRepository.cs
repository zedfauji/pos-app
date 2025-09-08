using System.Text.Json;
using MenuApi.Models;
using Npgsql;

namespace MenuApi.Repositories;

public sealed class MenuRepository : IMenuRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public MenuRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Items
    public async Task<(IReadOnlyList<MenuItemDto> Items, int Total)> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = new List<string> { "is_deleted = false" };
        var cmd = new NpgsqlCommand();
        cmd.Connection = conn;
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where.Add("(name ILIKE @q OR sku_id ILIKE @q)");
            cmd.Parameters.AddWithValue("@q", $"%{query.Q.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            where.Add("category = @cat");
            cmd.Parameters.AddWithValue("@cat", query.Category!);
        }
        if (!string.IsNullOrWhiteSpace(query.Group))
        {
            where.Add("group_name = @grp");
            cmd.Parameters.AddWithValue("@grp", (object?)query.Group ?? DBNull.Value);
        }
        if (query.AvailableOnly == true)
        {
            where.Add("is_available = true");
        }
        var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : string.Empty;
        var limit = Math.Clamp(query.PageSize, 1, 200);
        var offset = (Math.Max(1, query.Page) - 1) * limit;
        cmd.CommandText = $@"SELECT menu_item_id, sku_id, name, description, category, group_name,
                                     selling_price, price, picture_url, is_discountable, is_part_of_combo, is_available, version
                              FROM menu.menu_items{whereSql}
                              ORDER BY name
                              LIMIT @limit OFFSET @offset";
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        var list = new List<MenuItemDto>();
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
        {
            while (await rdr.ReadAsync(ct))
            {
                list.Add(new MenuItemDto(
                    rdr.GetInt64(0), rdr.GetString(1), rdr.GetString(2), rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    rdr.GetString(4), rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    rdr.GetDecimal(6), rdr.IsDBNull(7) ? null : rdr.GetFieldValue<decimal?>(7),
                    rdr.IsDBNull(8) ? null : rdr.GetString(8), rdr.GetBoolean(9), rdr.GetBoolean(10), rdr.GetBoolean(11), rdr.GetInt32(12)));
            }
        }
        // total
        await using var cnt = new NpgsqlCommand($"SELECT COUNT(1) FROM menu.menu_items{whereSql}", conn);
        foreach (NpgsqlParameter p in cmd.Parameters)
        {
            if (p.ParameterName is "@limit" or "@offset") continue;
            cnt.Parameters.AddWithValue(p.ParameterName, p.Value);
        }
        var total = Convert.ToInt32(await cnt.ExecuteScalarAsync(ct));
        return (list, total);
    }

    public async Task<MenuItemDetailsDto?> GetItemAsync(long id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT menu_item_id, sku_id, name, description, category, group_name,
                                     selling_price, price, picture_url, is_discountable, is_part_of_combo, is_available, version
                              FROM menu.menu_items WHERE menu_item_id = @id AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        var item = new MenuItemDto(
            rdr.GetInt64(0), rdr.GetString(1), rdr.GetString(2), rdr.IsDBNull(3) ? null : rdr.GetString(3),
            rdr.GetString(4), rdr.IsDBNull(5) ? null : rdr.GetString(5),
            rdr.GetDecimal(6), rdr.IsDBNull(7) ? null : rdr.GetFieldValue<decimal?>(7),
            rdr.IsDBNull(8) ? null : rdr.GetString(8), rdr.GetBoolean(9), rdr.GetBoolean(10), rdr.GetBoolean(11), rdr.GetInt32(12));
        await rdr.CloseAsync();
        // modifiers (simplified: list all modifiers linked)
        const string modSql = @"SELECT m.modifier_id, m.name, m.is_required, m.allow_multiple, m.max_selections
                                FROM menu.menu_item_modifiers mm
                                JOIN menu.modifiers m ON mm.modifier_id = m.modifier_id
                                WHERE mm.menu_item_id = @id
                                ORDER BY mm.sort_order";
        await using var modCmd = new NpgsqlCommand(modSql, conn);
        modCmd.Parameters.AddWithValue("@id", id);
        var modifiers = new List<ModifierDto>();
        await using (var mr = await modCmd.ExecuteReaderAsync(ct))
        {
            while (await mr.ReadAsync(ct))
            {
                var modId = mr.GetInt64(0);
                var options = await GetModifierOptionsAsync(conn, modId, ct);
                modifiers.Add(new ModifierDto(modId, mr.GetString(1), mr.GetBoolean(2), mr.GetBoolean(3),
                    mr.IsDBNull(4) ? null : mr.GetInt32(4), options));
            }
        }
        return new MenuItemDetailsDto(item, modifiers);
    }

    private static async Task<IReadOnlyList<ModifierOptionDto>> GetModifierOptionsAsync(NpgsqlConnection conn, long modifierId, CancellationToken ct)
    {
        const string sql = @"SELECT option_id, name, price_delta, is_available, sort_order
                             FROM menu.modifier_options WHERE modifier_id = @mid ORDER BY sort_order";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@mid", modifierId);
        var list = new List<ModifierOptionDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new ModifierOptionDto(rdr.GetInt64(0), rdr.GetString(1), rdr.GetDecimal(2), rdr.GetBoolean(3), rdr.GetInt32(4)));
        }
        return list;
    }

    public async Task<MenuItemDto> CreateItemAsync(CreateMenuItemDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string insert = @"INSERT INTO menu.menu_items(sku_id, name, description, category, group_name, vendor_price, selling_price, price, picture_url, is_discountable, is_part_of_combo, is_available, version, created_by, updated_by)
                               VALUES(@sku, @name, @desc, @cat, @grp, @vprice, @sprice, @price, @pic, @disc, @combo, @avail, 1, @user, @user)
                               RETURNING menu_item_id, version";
        await using var cmd = new NpgsqlCommand(insert, conn, tx);
        cmd.Parameters.AddWithValue("@sku", dto.Sku);
        cmd.Parameters.AddWithValue("@name", dto.Name);
        cmd.Parameters.AddWithValue("@desc", (object?)dto.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cat", dto.Category);
        cmd.Parameters.AddWithValue("@grp", (object?)dto.GroupName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@vprice", dto.VendorPrice);
        cmd.Parameters.AddWithValue("@sprice", dto.SellingPrice);
        cmd.Parameters.AddWithValue("@price", (object?)dto.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pic", (object?)dto.PictureUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@disc", dto.IsDiscountable);
        cmd.Parameters.AddWithValue("@combo", dto.IsPartOfCombo);
        cmd.Parameters.AddWithValue("@avail", dto.IsAvailable);
        cmd.Parameters.AddWithValue("@user", (object?)user ?? DBNull.Value);
        long id; int version;
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
        {
            await rdr.ReadAsync(ct);
            id = rdr.GetInt64(0);
            version = rdr.GetInt32(1);
        }
        await LogHistoryAsync(conn, tx, "menu_item", id, "create", null, JsonSerializer.SerializeToElement(dto), version, user, ct);
        await tx.CommitAsync(ct);
        return new MenuItemDto(id, dto.Sku, dto.Name, dto.Description, dto.Category, dto.GroupName, dto.SellingPrice, dto.Price, dto.PictureUrl, dto.IsDiscountable, dto.IsPartOfCombo, dto.IsAvailable, version);
    }

    public async Task<MenuItemDto> UpdateItemAsync(long id, UpdateMenuItemDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        // Get existing
        var existing = await GetItemAsyncInternal(conn, id, ct);
        if (existing is null) throw new KeyNotFoundException("Menu item not found");
        const string update = @"UPDATE menu.menu_items SET
                                name = COALESCE(@name, name),
                                description = COALESCE(@desc, description),
                                category = COALESCE(@cat, category),
                                group_name = COALESCE(@grp, group_name),
                                selling_price = COALESCE(@sprice, selling_price),
                                price = COALESCE(@price, price),
                                picture_url = COALESCE(@pic, picture_url),
                                is_discountable = COALESCE(@disc, is_discountable),
                                is_part_of_combo = COALESCE(@combo, is_part_of_combo),
                                is_available = COALESCE(@avail, is_available),
                                version = version + 1,
                                updated_by = @user,
                                updated_at = now()
                               WHERE menu_item_id = @id
                               RETURNING version";
        await using var cmd = new NpgsqlCommand(update, conn, tx);
        cmd.Parameters.AddWithValue("@name", (object?)dto.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", (object?)dto.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cat", (object?)dto.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@grp", (object?)dto.GroupName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sprice", (object?)dto.SellingPrice ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@price", (object?)dto.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pic", (object?)dto.PictureUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@disc", (object?)dto.IsDiscountable ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@combo", (object?)dto.IsPartOfCombo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avail", (object?)dto.IsAvailable ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@user", (object?)user ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        var version = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        await LogHistoryAsync(conn, tx, "menu_item", id, "update", JsonSerializer.SerializeToElement(existing), JsonSerializer.SerializeToElement(dto), version, user, ct);
        await tx.CommitAsync(ct);
        // return merged
        var merged = new MenuItemDto(
            existing.Item.Id,
            existing.Item.Sku,
            dto.Name ?? existing.Item.Name,
            dto.Description ?? existing.Item.Description,
            dto.Category ?? existing.Item.Category,
            dto.GroupName ?? existing.Item.GroupName,
            dto.SellingPrice ?? existing.Item.SellingPrice,
            dto.Price ?? existing.Item.Price,
            dto.PictureUrl ?? existing.Item.PictureUrl,
            dto.IsDiscountable ?? existing.Item.IsDiscountable,
            dto.IsPartOfCombo ?? existing.Item.IsPartOfCombo,
            dto.IsAvailable ?? existing.Item.IsAvailable,
            version
        );
        return merged;
    }

    private async Task<MenuItemDetailsDto?> GetItemAsyncInternal(NpgsqlConnection conn, long id, CancellationToken ct)
    {
        const string sql = @"SELECT menu_item_id, sku_id, name, description, category, group_name,
                                     selling_price, price, picture_url, is_discountable, is_part_of_combo, is_available, version
                              FROM menu.menu_items WHERE menu_item_id = @id AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        var item = new MenuItemDto(
            rdr.GetInt64(0), rdr.GetString(1), rdr.GetString(2), rdr.IsDBNull(3) ? null : rdr.GetString(3),
            rdr.GetString(4), rdr.IsDBNull(5) ? null : rdr.GetString(5),
            rdr.GetDecimal(6), rdr.IsDBNull(7) ? null : rdr.GetFieldValue<decimal?>(7),
            rdr.IsDBNull(8) ? null : rdr.GetString(8), rdr.GetBoolean(9), rdr.GetBoolean(10), rdr.GetBoolean(11), rdr.GetInt32(12));
        await rdr.CloseAsync();
        return new MenuItemDetailsDto(item, Array.Empty<ModifierDto>());
    }

    public async Task RestoreItemAsync(long id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string sql = @"UPDATE menu.menu_items SET is_deleted = false, version = version + 1, updated_by = @user, updated_at = now() WHERE menu_item_id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@user", (object?)user ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        await LogHistoryAsync(conn, tx, "menu_item", id, "restore", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteItemAsync(long id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string sql = @"UPDATE menu.menu_items SET is_deleted = true, version = version + 1, updated_by = @user, updated_at = now() WHERE menu_item_id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@user", (object?)user ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        await LogHistoryAsync(conn, tx, "menu_item", id, "delete", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    // Combos
    public async Task<(IReadOnlyList<ComboDto> Items, int Total)> ListCombosAsync(ComboQueryDto query, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = new List<string> { "is_deleted = false" };
        var cmd = new NpgsqlCommand { Connection = conn };
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where.Add("name ILIKE @q");
            cmd.Parameters.AddWithValue("@q", $"%{query.Q.Trim()}%");
        }
        if (query.AvailableOnly == true) where.Add("is_available = true");
        var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : string.Empty;
        var limit = Math.Clamp(query.PageSize, 1, 200);
        var offset = (Math.Max(1, query.Page) - 1) * limit;
        cmd.CommandText = $@"SELECT combo_id, name, description, price, is_discountable, is_available, picture_url, version
                              FROM menu.combos{whereSql}
                              ORDER BY name
                              LIMIT @limit OFFSET @offset";
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        var list = new List<ComboDto>();
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
        {
            while (await rdr.ReadAsync(ct))
            {
                list.Add(new ComboDto(rdr.GetInt64(0), rdr.GetString(1), rdr.IsDBNull(2) ? null : rdr.GetString(2), rdr.GetDecimal(3), rdr.GetBoolean(4), rdr.GetBoolean(5), rdr.IsDBNull(6) ? null : rdr.GetString(6), rdr.GetInt32(7)));
            }
        }
        await using var cnt = new NpgsqlCommand($"SELECT COUNT(1) FROM menu.combos{whereSql}", conn);
        foreach (NpgsqlParameter p in cmd.Parameters)
        {
            if (p.ParameterName is "@limit" or "@offset") continue;
            cnt.Parameters.AddWithValue(p.ParameterName, p.Value);
        }
        var total = Convert.ToInt32(await cnt.ExecuteScalarAsync(ct));
        return (list, total);
    }

    public async Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT combo_id, name, description, price, is_discountable, is_available, picture_url, version
                              FROM menu.combos WHERE combo_id = @id AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        var combo = new ComboDto(rdr.GetInt64(0), rdr.GetString(1), rdr.IsDBNull(2) ? null : rdr.GetString(2), rdr.GetDecimal(3), rdr.GetBoolean(4), rdr.GetBoolean(5), rdr.IsDBNull(6) ? null : rdr.GetString(6), rdr.GetInt32(7));
        await rdr.CloseAsync();
        const string itemsSql = @"SELECT menu_item_id, quantity, is_required FROM menu.combo_items WHERE combo_id = @id";
        await using var icmd = new NpgsqlCommand(itemsSql, conn);
        icmd.Parameters.AddWithValue("@id", id);
        var items = new List<ComboItemLinkDto>();
        await using var ir = await icmd.ExecuteReaderAsync(ct);
        while (await ir.ReadAsync(ct))
        {
            items.Add(new ComboItemLinkDto(ir.GetInt64(0), ir.GetInt32(1), ir.GetBoolean(2)));
        }
        return new ComboDetailsDto(combo, items);
    }

    public async Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string sql = @"INSERT INTO menu.combos(name, description, price, is_discountable, is_available, picture_url, version, created_by, updated_by)
                             VALUES(@n, @d, @p, @disc, @avail, @pic, 1, @u, @u)
                             RETURNING combo_id, version";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@n", dto.Name);
        cmd.Parameters.AddWithValue("@d", (object?)dto.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", dto.Price);
        cmd.Parameters.AddWithValue("@disc", dto.IsDiscountable);
        cmd.Parameters.AddWithValue("@avail", dto.IsAvailable);
        cmd.Parameters.AddWithValue("@pic", (object?)dto.PictureUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", (object?)user ?? DBNull.Value);
        long id; int version;
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            await r.ReadAsync(ct);
            id = r.GetInt64(0);
            version = r.GetInt32(1);
        }
        if (dto.Items.Count > 0)
        {
            const string insItem = @"INSERT INTO menu.combo_items(combo_id, menu_item_id, quantity, is_required) VALUES(@c, @m, @q, @r)";
            foreach (var li in dto.Items)
            {
                await using var ic = new NpgsqlCommand(insItem, conn, tx);
                ic.Parameters.AddWithValue("@c", id);
                ic.Parameters.AddWithValue("@m", li.MenuItemId);
                ic.Parameters.AddWithValue("@q", li.Quantity);
                ic.Parameters.AddWithValue("@r", li.IsRequired);
                await ic.ExecuteNonQueryAsync(ct);
            }
        }
        await LogHistoryAsync(conn, tx, "combo", id, "create", null, JsonSerializer.SerializeToElement(dto), version, user, ct);
        await tx.CommitAsync(ct);
        return new ComboDto(id, dto.Name, dto.Description, dto.Price, dto.IsDiscountable, dto.IsAvailable, dto.PictureUrl, version);
    }

    public async Task<ComboDto> UpdateComboAsync(long id, UpdateComboDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string sql = @"UPDATE menu.combos SET
                                name = COALESCE(@n, name),
                                description = COALESCE(@d, description),
                                price = COALESCE(@p, price),
                                is_discountable = COALESCE(@disc, is_discountable),
                                is_available = COALESCE(@avail, is_available),
                                picture_url = COALESCE(@pic, picture_url),
                                version = version + 1,
                                updated_by = @u,
                                updated_at = now()
                             WHERE combo_id = @id
                             RETURNING name, description, price, is_discountable, is_available, picture_url, version";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@n", (object?)dto.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)dto.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", (object?)dto.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@disc", (object?)dto.IsDiscountable ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avail", (object?)dto.IsAvailable ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pic", (object?)dto.PictureUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", (object?)user ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) throw new KeyNotFoundException("Combo not found");
        var name = r.GetString(0);
        var desc = r.IsDBNull(1) ? null : r.GetString(1);
        var price = r.GetDecimal(2);
        var disc = r.GetBoolean(3);
        var avail = r.GetBoolean(4);
        var pic = r.IsDBNull(5) ? null : r.GetString(5);
        var version = r.GetInt32(6);
        await r.CloseAsync();
        if (dto.Items is not null)
        {
            // replace items
            const string del = "DELETE FROM menu.combo_items WHERE combo_id = @id";
            await using (var dc = new NpgsqlCommand(del, conn, tx)) { dc.Parameters.AddWithValue("@id", id); await dc.ExecuteNonQueryAsync(ct); }
            const string insItem = @"INSERT INTO menu.combo_items(combo_id, menu_item_id, quantity, is_required) VALUES(@c, @m, @q, @r)";
            foreach (var li in dto.Items)
            {
                await using var ic = new NpgsqlCommand(insItem, conn, tx);
                ic.Parameters.AddWithValue("@c", id);
                ic.Parameters.AddWithValue("@m", li.MenuItemId);
                ic.Parameters.AddWithValue("@q", li.Quantity);
                ic.Parameters.AddWithValue("@r", li.IsRequired);
                await ic.ExecuteNonQueryAsync(ct);
            }
        }
        await LogHistoryAsync(conn, tx, "combo", id, "update", null, JsonSerializer.SerializeToElement(dto), version, user, ct);
        await tx.CommitAsync(ct);
        return new ComboDto(id, name, desc, price, disc, avail, pic, version);
    }

    public async Task DeleteComboAsync(long id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string sql = @"UPDATE menu.combos SET is_deleted = true, version = version + 1, updated_by = @u, updated_at = now() WHERE combo_id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@u", (object?)user ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync(ct);
        await LogHistoryAsync(conn, tx, "combo", id, "delete", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    private static async Task LogHistoryAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string entityType, long entityId, string action, JsonElement? oldVal, JsonElement? newVal, int? version, string user, CancellationToken ct)
    {
        const string hist = @"INSERT INTO menu.menu_history(entity_type, entity_id, action, old_value, new_value, version, changed_by)
                             VALUES(@t, @i, @a, @o, @n, @v, @u)";
        await using var hc = new NpgsqlCommand(hist, conn, tx);
        hc.Parameters.AddWithValue("@t", entityType);
        hc.Parameters.AddWithValue("@i", entityId);
        hc.Parameters.AddWithValue("@a", action);
        hc.Parameters.AddWithValue("@o", oldVal.HasValue ? (object)oldVal.Value : DBNull.Value);
        hc.Parameters.AddWithValue("@n", newVal.HasValue ? (object)newVal.Value : DBNull.Value);
        hc.Parameters.AddWithValue("@v", (object?)version ?? DBNull.Value);
        hc.Parameters.AddWithValue("@u", (object?)user ?? DBNull.Value);
        await hc.ExecuteNonQueryAsync(ct);
    }
}
