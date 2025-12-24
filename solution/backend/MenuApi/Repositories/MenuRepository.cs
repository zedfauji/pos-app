using System.Text.Json;
using MenuApi.Models;
using Npgsql;
using Dapper;

namespace MenuApi.Repositories;

public sealed class MenuRepository : IMenuRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public MenuRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // History
    public async Task<(IReadOnlyList<HistoryDto> Items, int Total)> ListHistoryAsync(HistoryQueryDto query, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var limit = Math.Clamp(query.PageSize, 1, 200);
        var offset = (Math.Max(1, query.Page) - 1) * limit;
        
        const string sql = @"SELECT history_id AS Id, entity_type AS EntityType, entity_id AS EntityId, action AS Action, 
                                    version AS Version, changed_at AS ChangedAt, changed_by AS ChangedBy, 
                                    old_value AS OldValue, new_value AS NewValue
                             FROM menu.menu_history WHERE entity_type = @t AND entity_id = @i
                             ORDER BY changed_at DESC LIMIT @l OFFSET @o";
        
        var list = (await conn.QueryAsync<HistoryDto>(sql, new { t = query.EntityType, i = query.EntityId, l = limit, o = offset })).ToList();
        
        const string cnt = "SELECT COUNT(1) FROM menu.menu_history WHERE entity_type = @t AND entity_id = @i";
        var total = await conn.ExecuteScalarAsync<int>(cnt, new { t = query.EntityType, i = query.EntityId });
        
        return (list, total);
    }

    // Modifiers CRUD
    public async Task<(IReadOnlyList<ModifierDto> Items, int Total)> ListModifiersAsync(ModifierQueryDto query, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = new List<string>();
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where.Add("m.name ILIKE @q");
            parameters.Add("q", $"%{query.Q.Trim()}%");
        }
        
        var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : string.Empty;
        var limit = Math.Clamp(query.PageSize, 1, 200);
        var offset = (Math.Max(1, query.Page) - 1) * limit;
        
        parameters.Add("l", limit);
        parameters.Add("o", offset);
        
        // Use JOIN to get modifiers and their options in one query
        var sql = $@"SELECT m.modifier_id, m.name, m.description, m.is_required, m.allow_multiple, m.max_selections,
                            o.option_id, o.name as option_name, o.price_delta, o.is_available, o.sort_order
                     FROM menu.modifiers m
                     LEFT JOIN menu.modifier_options o ON m.modifier_id = o.modifier_id
                     {whereSql}
                     ORDER BY m.name, o.sort_order LIMIT @l OFFSET @o";
        
        var modifierDict = new Dictionary<long, ModifierDto>();
        var results = await conn.QueryAsync(sql, parameters);
        
        foreach (var row in results)
        {
            var modifierId = (long)row.modifier_id;
            if (!modifierDict.ContainsKey(modifierId))
            {
                var options = new List<ModifierOptionDto>();
                modifierDict[modifierId] = new ModifierDto(
                    modifierId,
                    (string)row.name,
                    (bool)row.is_required,
                    (bool)row.allow_multiple,
                    row.max_selections as int?,
                    options
                );
            }
            
            // Add option if it exists (LEFT JOIN might return null)
            if (row.option_id != null)
            {
                var option = new ModifierOptionDto(
                    (long)row.option_id,
                    (string)row.option_name,
                    (decimal)row.price_delta,
                    (bool)row.is_available,
                    (int)row.sort_order
                );
                ((List<ModifierOptionDto>)modifierDict[modifierId].Options).Add(option);
            }
        }
        
        var list = modifierDict.Values.ToList();
        
        // Get total count
        var cntSql = $"SELECT COUNT(1) FROM menu.modifiers{whereSql}";
        var total = await conn.ExecuteScalarAsync<int>(cntSql, parameters);
        
        return (list, total);
    }

    public async Task<ModifierDto?> GetModifierAsync(long id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT m.modifier_id, m.name, m.description, m.is_required, m.allow_multiple, m.max_selections,
                                    o.option_id, o.name as option_name, o.price_delta, o.is_available, o.sort_order
                             FROM menu.modifiers m
                             LEFT JOIN menu.modifier_options o ON m.modifier_id = o.modifier_id
                             WHERE m.modifier_id = @id
                             ORDER BY o.sort_order";
        
        ModifierDto? modifier = null;
        var options = new List<ModifierOptionDto>();
        
        await foreach (var row in conn.QueryUnbufferedAsync(sql, new { id }))
        {
            if (modifier == null)
            {
                modifier = new ModifierDto(
                    (long)row.modifier_id,
                    (string)row.name,
                    (bool)row.is_required,
                    (bool)row.allow_multiple,
                    row.max_selections as int?,
                    options
                );
            }
            
            // Add option if it exists (LEFT JOIN might return null)
            if (row.option_id != null)
            {
                var option = new ModifierOptionDto(
                    (long)row.option_id,
                    (string)row.option_name,
                    (decimal)row.price_delta,
                    (bool)row.is_available,
                    (int)row.sort_order
                );
                options.Add(option);
            }
        }
        
        return modifier;
    }

    public async Task<ModifierDto> CreateModifierAsync(CreateModifierDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string ins = @"INSERT INTO menu.modifiers(name, description, is_required, allow_multiple, min_selections, max_selections)
                            VALUES(@n, @d, @r, @m, @min, @max) RETURNING modifier_id";
        
        var id = await conn.ExecuteScalarAsync<long>(ins, new
        {
            n = dto.Name,
            d = dto.Description,
            r = dto.IsRequired,
            m = dto.AllowMultiple,
            min = dto.MinSelections,
            max = dto.MaxSelections
        }, tx);
        
        foreach (var opt in dto.Options)
        {
            const string insOpt = @"INSERT INTO menu.modifier_options(modifier_id, name, price_delta, is_available, sort_order)
                                   VALUES(@mid, @name, @delta, @avail, @sort)";
            
            await conn.ExecuteAsync(insOpt, new
            {
                mid = id,
                name = opt.Name,
                delta = opt.PriceDelta,
                avail = opt.IsAvailable,
                sort = opt.SortOrder
            }, tx);
        }
        
        await LogHistoryAsync(conn, tx, "modifier", id, "create", null, JsonSerializer.SerializeToElement(dto), null, user, ct);
        await tx.CommitAsync(ct);
        
        var result = await GetModifierAsync(id, ct);
        return result!;
    }

    public async Task<ModifierDto> UpdateModifierAsync(long id, UpdateModifierDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string upd = @"UPDATE menu.modifiers SET name = COALESCE(@n, name), description = COALESCE(@d, description), 
                                   is_required = COALESCE(@r, is_required), allow_multiple = COALESCE(@m, allow_multiple), 
                                   min_selections = COALESCE(@min, min_selections), max_selections = COALESCE(@max, max_selections) 
                            WHERE modifier_id = @id";
        
        await conn.ExecuteAsync(upd, new
        {
            n = dto.Name,
            d = dto.Description,
            r = dto.IsRequired,
            m = dto.AllowMultiple,
            min = dto.MinSelections,
            max = dto.MaxSelections,
            id
        }, tx);
        
        if (dto.Options is not null)
        {
            const string del = "DELETE FROM menu.modifier_options WHERE modifier_id = @id";
            await conn.ExecuteAsync(del, new { id }, tx);
            
            foreach (var opt in dto.Options)
            {
                const string insOpt = @"INSERT INTO menu.modifier_options(modifier_id, name, price_delta, is_available, sort_order)
                                       VALUES(@mid, @name, @delta, @avail, @sort)";
                
                await conn.ExecuteAsync(insOpt, new
                {
                    mid = id,
                    name = opt.Name,
                    delta = opt.PriceDelta,
                    avail = opt.IsAvailable,
                    sort = opt.SortOrder
                }, tx);
            }
        }
        
        await LogHistoryAsync(conn, tx, "modifier", id, "update", null, JsonSerializer.SerializeToElement(dto), null, user, ct);
        await tx.CommitAsync(ct);
        
        var result = await GetModifierAsync(id, ct);
        return result!;
    }

    public async Task DeleteModifierAsync(long id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string delOpts = "DELETE FROM menu.modifier_options WHERE modifier_id = @id";
        await conn.ExecuteAsync(delOpts, new { id }, tx);
        
        const string delMod = "DELETE FROM menu.modifiers WHERE modifier_id = @id";
        await conn.ExecuteAsync(delMod, new { id }, tx);
        
        await LogHistoryAsync(conn, tx, "modifier", id, "delete", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    // Items
    public async Task<(IReadOnlyList<MenuItemDto> Items, int Total)> ListItemsAsync(MenuItemQueryDto query, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = new List<string> { "is_current_version = true" };
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where.Add("(name ILIKE @q OR sku ILIKE @q)");
            parameters.Add("q", $"%{query.Q.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            where.Add("category = @cat");
            parameters.Add("cat", query.Category);
        }
        if (!string.IsNullOrWhiteSpace(query.Group))
        {
            where.Add("group_name = @grp");
            parameters.Add("grp", query.Group);
        }
        if (query.AvailableOnly == true)
        {
            where.Add("is_available = true");
        }
        
        var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : string.Empty;
        var limit = Math.Clamp(query.PageSize, 1, 200);
        var offset = (Math.Max(1, query.Page) - 1) * limit;
        
        parameters.Add("limit", limit);
        parameters.Add("offset", offset);
        
        var sql = $@"SELECT menu_item_id AS Id, sku, name, description, category, group_name AS GroupName,
                            base_price AS BasePrice, picture_url AS PictureUrl, is_discountable AS IsDiscountable, 
                            is_part_of_combo AS IsPartOfCombo, is_available AS IsAvailable, version
                     FROM menu.menu_items{whereSql}
                     ORDER BY name
                     LIMIT @limit OFFSET @offset";
        
        var list = (await conn.QueryAsync<MagiDesk.Shared.DTOs.Menu.MenuItemDto>(sql, parameters)).ToList();
        
        var cntSql = $"SELECT COUNT(1) FROM menu.menu_items{whereSql}";
        var total = await conn.ExecuteScalarAsync<int>(cntSql, parameters);
        
        return (list, total);
    }

    public async Task<MenuItemDetailsDto?> GetItemAsync(Guid id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        const string sql = @"SELECT menu_item_id AS Id, sku, name, description, category, group_name AS GroupName,
                                    base_price AS BasePrice, picture_url AS PictureUrl, is_discountable AS IsDiscountable, 
                                    is_part_of_combo AS IsPartOfCombo, is_available AS IsAvailable, version
                             FROM menu.menu_items WHERE menu_item_id = @id AND is_current_version = true";
        
        var item = await conn.QuerySingleOrDefaultAsync<MagiDesk.Shared.DTOs.Menu.MenuItemDto>(sql, new { id });
        if (item == null) return null;
        
        // modifiers (simplified: list all modifiers linked)
        const string modSql = @"SELECT m.modifier_id AS Id, m.name, m.is_required AS IsRequired, m.allow_multiple AS AllowMultiple, 
                                       m.max_selections AS MaxSelections
                                FROM menu.menu_item_modifiers mm
                                JOIN menu.modifiers m ON mm.modifier_id = m.modifier_id
                                WHERE mm.menu_item_id = @id
                                ORDER BY mm.sort_order";
        
        var modifiers = new List<ModifierDto>();
        await foreach (var modRow in conn.QueryUnbufferedAsync(modSql, new { id }))
        {
            var modId = (long)modRow.Id;
            var options = await GetModifierOptionsAsync(conn, modId, ct);
            modifiers.Add(new ModifierDto(
                modId, 
                (string)modRow.name, 
                (bool)modRow.IsRequired, 
                (bool)modRow.AllowMultiple,
                modRow.MaxSelections as int?, 
                options));
        }
        
        return new MenuItemDetailsDto(item, modifiers);
    }

    private static async Task<IReadOnlyList<ModifierOptionDto>> GetModifierOptionsAsync(NpgsqlConnection conn, long modifierId, CancellationToken ct)
    {
        const string sql = @"SELECT option_id AS Id, name, price_delta AS PriceDelta, is_available AS IsAvailable, sort_order AS SortOrder
                             FROM menu.modifier_options WHERE modifier_id = @mid ORDER BY sort_order";
        
        var list = (await conn.QueryAsync<ModifierOptionDto>(sql, new { mid = modifierId })).ToList();
        return list;
    }

    public async Task<MagiDesk.Shared.DTOs.Menu.MenuItemDto> CreateItemAsync(MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string insert = @"INSERT INTO menu.menu_items(sku, name, description, category, group_name, base_price, picture_url, is_discountable, is_part_of_combo, is_available, version, is_current_version)
                               VALUES(@sku, @name, @desc, @cat, @grp, @price, @pic, @disc, @combo, @avail, 1, true)
                               RETURNING menu_item_id, version";
        
        // Link to inventory item by SKU if exists
        Guid? inventoryItemId = null;
        const string invLookup = @"select item_id from inventory.inventory_items where lower(sku)=lower(@sku) and is_active=true limit 1";
        
        var obj = await conn.ExecuteScalarAsync<Guid?>(invLookup, new { sku = dto.Sku }, tx);
        if (obj.HasValue) inventoryItemId = obj.Value;
        
        var result = await conn.QuerySingleAsync(insert, new
        {
            sku = dto.Sku,
            name = dto.Name,
            desc = dto.Description,
            cat = dto.Category,
            grp = dto.GroupName,
            price = dto.BasePrice,
            pic = dto.PictureUrl,
            disc = dto.IsDiscountable,
            combo = dto.IsPartOfCombo,
            avail = dto.IsAvailable
        }, tx);
        
        Guid id = result.menu_item_id;
        int version = result.version;
        
        await LogHistoryAsync(conn, tx, "menu_item", id, "create", null, JsonSerializer.SerializeToElement(dto), version, user, ct);
        await tx.CommitAsync(ct);
        
        return new MagiDesk.Shared.DTOs.Menu.MenuItemDto(id, dto.Sku, dto.Name, dto.Description, dto.Category, dto.GroupName, dto.BasePrice, dto.PictureUrl, dto.IsDiscountable, dto.IsPartOfCombo, dto.IsAvailable, version);
    }

    public async Task<MagiDesk.Shared.DTOs.Menu.MenuItemDto> UpdateItemAsync(Guid id, MagiDesk.Shared.DTOs.Menu.UpdateMenuItemDto dto, string user, CancellationToken ct)
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
                                base_price = COALESCE(@price, base_price),
                                picture_url = COALESCE(@pic, picture_url),
                                is_discountable = COALESCE(@disc, is_discountable),
                                is_part_of_combo = COALESCE(@combo, is_part_of_combo),
                                is_available = COALESCE(@avail, is_available),
                                version = version + 1,
                                updated_at = now()
                               WHERE menu_item_id = @id AND is_current_version = true
                               RETURNING version";
        
        // Re-link inventory by SKU from existing item (Update DTO has no SKU)
        Guid? invId = null;
        const string invLookup2 = @"select item_id from inventory.inventory_items where lower(sku)=lower(@sku) and is_active=true limit 1";
        
        var obj = await conn.ExecuteScalarAsync<Guid?>(invLookup2, new { sku = existing.Item.Sku }, tx);
        if (obj.HasValue) invId = obj.Value;
        
        var version = await conn.ExecuteScalarAsync<int>(update, new
        {
            name = dto.Name,
            desc = dto.Description,
            cat = dto.Category,
            grp = dto.GroupName,
            price = dto.BasePrice,
            pic = dto.PictureUrl,
            disc = dto.IsDiscountable,
            combo = dto.IsPartOfCombo,
            avail = dto.IsAvailable,
            id
        }, tx);
        
        await LogHistoryAsync(conn, tx, "menu_item", id, "update", JsonSerializer.SerializeToElement(existing), JsonSerializer.SerializeToElement(dto), version, user, ct);
        await tx.CommitAsync(ct);
        
        // return merged
        var merged = new MagiDesk.Shared.DTOs.Menu.MenuItemDto(
            existing.Item.Id,
            existing.Item.Sku,
            dto.Name ?? existing.Item.Name,
            dto.Description ?? existing.Item.Description,
            dto.Category ?? existing.Item.Category,
            dto.GroupName ?? existing.Item.GroupName,
            dto.BasePrice ?? existing.Item.BasePrice,
            dto.PictureUrl ?? existing.Item.PictureUrl,
            dto.IsDiscountable ?? existing.Item.IsDiscountable,
            dto.IsPartOfCombo ?? existing.Item.IsPartOfCombo,
            dto.IsAvailable ?? existing.Item.IsAvailable,
            version
        );
        return merged;
    }

    // Duplicate SKU check (case-insensitive, excluding soft-deleted)
    public async Task<bool> ExistsSkuAsync(string sku, Guid? excludeId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        const string sql = @"SELECT EXISTS(
                                SELECT 1 FROM menu.menu_items
                                WHERE lower(sku) = lower(@sku)
                                  AND is_current_version = true
                                  AND (@excl IS NULL OR menu_item_id <> @excl)
                              )";
        
        var exists = await conn.ExecuteScalarAsync<bool>(sql, new { sku, excl = excludeId });
        return exists;
    }

    public async Task<MenuItemDetailsDto?> GetItemBySkuAsync(string sku, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        const string sql = @"SELECT menu_item_id FROM menu.menu_items WHERE lower(sku) = lower(@sku) AND is_current_version = true";
        
        var idObj = await conn.ExecuteScalarAsync<Guid?>(sql, new { sku });
        if (!idObj.HasValue) return null;
        
        return await GetItemAsync(idObj.Value, ct);
    }

    public async Task SetItemAvailabilityAsync(Guid id, bool isAvailable, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string sql = @"UPDATE menu.menu_items SET is_available = @a, version = version + 1, updated_by = @u, updated_at = now() WHERE menu_item_id = @id";
        
        await conn.ExecuteAsync(sql, new { a = isAvailable, u = user, id }, tx);
        await LogHistoryAsync(conn, tx, "menu_item", id, "availability", null, JsonSerializer.SerializeToElement(new { isAvailable }), null, user, ct);
        await tx.CommitAsync(ct);
    }

    public async Task SetComboAvailabilityAsync(long id, bool isAvailable, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string sql = @"UPDATE menu.combos SET is_available = @a, version = version + 1, updated_by = @u, updated_at = now() WHERE combo_id = @id";
        
        await conn.ExecuteAsync(sql, new { a = isAvailable, u = user, id }, tx);
        await LogHistoryAsync(conn, tx, "combo", id, "availability", null, JsonSerializer.SerializeToElement(new { isAvailable }), null, user, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<(decimal ComputedPrice, IReadOnlyList<(Guid MenuItemId, int Quantity, decimal UnitPrice)> Items)> ComputeComboPriceAsync(long id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        const string itemsSql = @"SELECT ci.menu_item_id AS MenuItemId, ci.quantity, mi.base_price AS unit_price
                                  FROM menu.combo_items ci
                                  JOIN menu.menu_items mi ON mi.menu_item_id = ci.menu_item_id
                                  WHERE ci.combo_id = @id";
        
        var rows = (await conn.QueryAsync(itemsSql, new { id })).ToList();
        var list = new List<(Guid, int, decimal)>();
        
        foreach (var row in rows)
        {
            list.Add(((Guid)row.MenuItemId, (int)row.quantity, (decimal)row.unit_price));
        }
        
        var total = list.Sum(t => t.Item2 * t.Item3);
        return (total, list.Select(t => (t.Item1, t.Item2, t.Item3)).ToList());
    }

    public async Task RollbackItemAsync(Guid id, int toVersion, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string hsql = @"SELECT action, old_value, new_value FROM menu.menu_history WHERE entity_type = 'menu_item' AND entity_id = @id AND version = @v ORDER BY changed_at LIMIT 1";
        
        var hrow = await conn.QuerySingleOrDefaultAsync(hsql, new { id, v = toVersion }, tx);
        if (hrow == null) throw new InvalidOperationException("Target version not found for item");
        
        string action = hrow.action;
        object? old = hrow.old_value;
        object? nwe = hrow.new_value;
        
        if (action != "update" || old is null)
            throw new InvalidOperationException("Rollback supported only for versions recorded as 'update'.");
        
        // old contains snapshot of previous item; extract fields
        // We expect old JSON with keys matching CreateMenuItemDto fields
        const string upd = @"UPDATE menu.menu_items SET
                                name = COALESCE((@o->>'Name')::text, name),
                                description = (@o->>'Description'),
                                category = COALESCE((@o->>'Category')::text, category),
                                group_name = (@o->>'GroupName'),
                                base_price = COALESCE((@o->>'BasePrice')::numeric, base_price),
                                picture_url = (@o->>'PictureUrl'),
                                is_discountable = COALESCE((@o->>'IsDiscountable')::boolean, is_discountable),
                                is_part_of_combo = COALESCE((@o->>'IsPartOfCombo')::boolean, is_part_of_combo),
                                is_available = COALESCE((@o->>'IsAvailable')::boolean, is_available),
                                version = @v,
                                updated_by = @u,
                                updated_at = now()
                             WHERE menu_item_id = @id";
        
        await conn.ExecuteAsync(upd, new { o = old, v = toVersion, u = user, id }, tx);
        await LogHistoryAsync(conn, tx, "menu_item", id, "rollback", null, null, toVersion, user, ct);
        await tx.CommitAsync(ct);
    }

    public async Task RollbackComboAsync(long id, int toVersion, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string hsql = @"SELECT action, old_value FROM menu.menu_history WHERE entity_type = 'combo' AND entity_id = @id AND version = @v ORDER BY changed_at LIMIT 1";
        
        var hrow = await conn.QuerySingleOrDefaultAsync(hsql, new { id, v = toVersion }, tx);
        if (hrow == null) throw new InvalidOperationException("Target version not found for combo");
        
        string action = hrow.action;
        object? old = hrow.old_value;
        
        if (action != "update" || old is null)
            throw new InvalidOperationException("Rollback supported only for versions recorded as 'update'.");
        
        const string upd = @"UPDATE menu.combos SET
                                name = COALESCE((@o->>'Name')::text, name),
                                description = (@o->>'Description'),
                                price = COALESCE((@o->>'Price')::numeric, price),
                                is_discountable = COALESCE((@o->>'IsDiscountable')::boolean, is_discountable),
                                is_available = COALESCE((@o->>'IsAvailable')::boolean, is_available),
                                picture_url = (@o->>'PictureUrl'),
                                version = @v,
                                updated_by = @u,
                                updated_at = now()
                             WHERE combo_id = @id";
        
        await conn.ExecuteAsync(upd, new { o = old, v = toVersion, u = user, id }, tx);
        await LogHistoryAsync(conn, tx, "combo", id, "rollback", null, null, toVersion, user, ct);
        await tx.CommitAsync(ct);
    }

    private async Task<MenuItemDetailsDto?> GetItemAsyncInternal(NpgsqlConnection conn, Guid id, CancellationToken ct)
    {
        const string sql = @"SELECT menu_item_id AS Id, sku, name, description, category, group_name AS GroupName,
                                    base_price AS BasePrice, picture_url AS PictureUrl, is_discountable AS IsDiscountable, 
                                    is_part_of_combo AS IsPartOfCombo, is_available AS IsAvailable, version
                             FROM menu.menu_items WHERE menu_item_id = @id AND is_current_version = true";
        
        var item = await conn.QuerySingleOrDefaultAsync<MagiDesk.Shared.DTOs.Menu.MenuItemDto>(sql, new { id });
        if (item == null) return null;
        
        return new MenuItemDetailsDto(item, Array.Empty<ModifierDto>());
    }

    public async Task RestoreItemAsync(Guid id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string sql = @"UPDATE menu.menu_items SET is_deleted = false, version = version + 1, updated_by = @user, updated_at = now() WHERE menu_item_id = @id";
        
        await conn.ExecuteAsync(sql, new { id, user }, tx);
        await LogHistoryAsync(conn, tx, "menu_item", id, "restore", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteItemAsync(Guid id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string sql = @"UPDATE menu.menu_items SET is_deleted = true, version = version + 1, updated_by = @user, updated_at = now() WHERE menu_item_id = @id";
        
        await conn.ExecuteAsync(sql, new { id, user }, tx);
        await LogHistoryAsync(conn, tx, "menu_item", id, "delete", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    // Combos
    public async Task<(IReadOnlyList<ComboDto> Items, int Total)> ListCombosAsync(ComboQueryDto query, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var where = new List<string> { "is_deleted = false" };
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where.Add("name ILIKE @q");
            parameters.Add("q", $"%{query.Q.Trim()}%");
        }
        if (query.AvailableOnly == true) where.Add("is_available = true");
        
        var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : string.Empty;
        var limit = Math.Clamp(query.PageSize, 1, 200);
        var offset = (Math.Max(1, query.Page) - 1) * limit;
        
        parameters.Add("limit", limit);
        parameters.Add("offset", offset);
        
        var sql = $@"SELECT combo_id AS Id, name, description, price, is_discountable AS IsDiscountable, 
                            is_available AS IsAvailable, picture_url AS PictureUrl, version
                     FROM menu.combos{whereSql}
                     ORDER BY name
                     LIMIT @limit OFFSET @offset";
        
        var list = (await conn.QueryAsync<ComboDto>(sql, parameters)).ToList();
        
        var cntSql = $"SELECT COUNT(1) FROM menu.combos{whereSql}";
        var total = await conn.ExecuteScalarAsync<int>(cntSql, parameters);
        
        return (list, total);
    }

    public async Task<ComboDetailsDto?> GetComboAsync(long id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        const string sql = @"SELECT combo_id AS Id, name, description, price, is_discountable AS IsDiscountable, 
                                    is_available AS IsAvailable, picture_url AS PictureUrl, version
                             FROM menu.combos WHERE combo_id = @id AND is_deleted = false";
        
        var combo = await conn.QuerySingleOrDefaultAsync<ComboDto>(sql, new { id });
        if (combo == null) return null;
        
        const string itemsSql = @"SELECT menu_item_id AS MenuItemId, quantity, is_required AS IsRequired FROM menu.combo_items WHERE combo_id = @id";
        var items = (await conn.QueryAsync<ComboItemLinkDto>(itemsSql, new { id })).ToList();
        
        return new ComboDetailsDto(combo, items);
    }

    public async Task<ComboDto> CreateComboAsync(CreateComboDto dto, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string sql = @"INSERT INTO menu.combos(name, description, price, is_discountable, is_available, picture_url, version, created_by, updated_by)
                             VALUES(@n, @d, @p, @disc, @avail, @pic, 1, @u, @u)
                             RETURNING combo_id, version";
        
        var result = await conn.QuerySingleAsync(sql, new
        {
            n = dto.Name,
            d = dto.Description,
            p = dto.Price,
            disc = dto.IsDiscountable,
            avail = dto.IsAvailable,
            pic = dto.PictureUrl,
            u = user
        }, tx);
        
        long id = result.combo_id;
        int version = result.version;
        
        if (dto.Items.Count > 0)
        {
            const string insItem = @"INSERT INTO menu.combo_items(combo_id, menu_item_id, quantity, is_required) VALUES(@c, @m, @q, @r)";
            
            foreach (var li in dto.Items)
            {
                await conn.ExecuteAsync(insItem, new
                {
                    c = id,
                    m = li.MenuItemId,
                    q = li.Quantity,
                    r = li.IsRequired
                }, tx);
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
                             RETURNING name, description, price, is_discountable AS IsDiscountable, is_available AS IsAvailable, picture_url AS PictureUrl, version";
        
        var result = await conn.QuerySingleOrDefaultAsync(sql, new
        {
            n = dto.Name,
            d = dto.Description,
            p = dto.Price,
            disc = dto.IsDiscountable,
            avail = dto.IsAvailable,
            pic = dto.PictureUrl,
            u = user,
            id
        }, tx);
        
        if (result == null) throw new KeyNotFoundException("Combo not found");
        
        string name = result.name;
        string? desc = result.description;
        decimal price = result.price;
        bool discountable = result.IsDiscountable;
        bool available = result.IsAvailable;
        string? pictureUrl = result.PictureUrl;
        int version = result.version;
        
        if (dto.Items is not null)
        {
            const string delItems = "DELETE FROM menu.combo_items WHERE combo_id = @id";
            await conn.ExecuteAsync(delItems, new { id }, tx);
            
            if (dto.Items.Count > 0)
            {
                const string insItem = @"INSERT INTO menu.combo_items(combo_id, menu_item_id, quantity, is_required) VALUES(@c, @m, @q, @r)";
                
                foreach (var li in dto.Items)
                {
                    await conn.ExecuteAsync(insItem, new
                    {
                        c = id,
                        m = li.MenuItemId,
                        q = li.Quantity,
                        r = li.IsRequired
                    }, tx);
                }
            }
        }
        
        await LogHistoryAsync(conn, tx, "combo", id, "update", null, JsonSerializer.SerializeToElement(dto), version, user, ct);
        await tx.CommitAsync(ct);
        
        return new ComboDto(id, name, desc, price, discountable, available, pictureUrl, version);
    }

    public async Task DeleteComboAsync(long id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string sql = @"UPDATE menu.combos SET is_deleted = true, version = version + 1, updated_by = @user, updated_at = now() WHERE combo_id = @id";
        
        await conn.ExecuteAsync(sql, new { id, user }, tx);
        await LogHistoryAsync(conn, tx, "combo", id, "delete", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    public async Task RestoreComboAsync(long id, string user, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        
        const string sql = @"UPDATE menu.combos SET is_deleted = false, version = version + 1, updated_by = @user, updated_at = now() WHERE combo_id = @id";
        
        await conn.ExecuteAsync(sql, new { id, user }, tx);
        await LogHistoryAsync(conn, tx, "combo", id, "restore", null, null, null, user, ct);
        await tx.CommitAsync(ct);
    }

    private static async Task LogHistoryAsync(NpgsqlConnection conn, NpgsqlTransaction? tx, string entityType, object entityId, string action, JsonElement? oldValue, JsonElement? newValue, int? version, string? changedBy, CancellationToken ct)
    {
        const string sql = @"INSERT INTO menu.menu_history(entity_type, entity_id, action, version, old_value, new_value, changed_by)
                            VALUES(@type, @eid, @action, @ver, @old::jsonb, @new::jsonb, @by)";
        
        await conn.ExecuteAsync(sql, new
        {
            type = entityType,
            eid = entityId,
            action,
            ver = version,
            old = oldValue.HasValue ? oldValue.Value.ToString() : null,
            @new = newValue.HasValue ? newValue.Value.ToString() : null,
            by = changedBy
        }, tx);
    }
}
