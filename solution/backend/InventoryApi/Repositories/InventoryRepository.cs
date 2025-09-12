using Dapper;
using MagiDesk.Shared.DTOs;
using Npgsql;

namespace InventoryApi.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly NpgsqlDataSource _dataSource;
    public InventoryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IEnumerable<ItemDto>> ListItemsAsync(string? vendorId, string? categoryId, bool? isMenuAvailable, string? search, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var sql = @"select item_id::text as Id, sku as Sku, name as Name, coalesce(selling_price,0) as Price, coalesce(quantity_on_hand,0) as Stock
                    from inventory.v_items_current";
        var where = new List<string>();
        var param = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(vendorId)) { where.Add("vendor_id = @vendorId"); param.Add("vendorId", vendorId); }
        if (!string.IsNullOrWhiteSpace(categoryId)) { where.Add("category_id = @categoryId"); param.Add("categoryId", categoryId); }
        if (isMenuAvailable.HasValue) { where.Add("is_menu_available = @isMenuAvailable"); param.Add("isMenuAvailable", isMenuAvailable.Value); }
        if (!string.IsNullOrWhiteSpace(search)) { where.Add("(sku ilike @q or name ilike @q)"); param.Add("q", "%" + search + "%"); }
        if (where.Count > 0) sql += " where " + string.Join(" and ", where);
        sql += " order by name asc limit 500";
        var rows = await conn.QueryAsync<ItemDto>(sql, param);
        return rows;
    }

    public async Task<ItemDto?> GetItemAsync(string id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"select item_id::text as Id, sku as Sku, name as Name, coalesce(selling_price,0) as Price, coalesce(quantity_on_hand,0) as Stock
                             from inventory.v_items_current where item_id = @id";
        return await conn.QuerySingleOrDefaultAsync<ItemDto>(sql, new { id });
    }

    public async Task<ItemDto> CreateItemAsync(ItemDto dto, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"insert into inventory.inventory_items (vendor_id, category_id, sku, name, description, buying_price, selling_price, is_menu_available)
                             values (@VendorId, @CategoryId, @Sku, @Name, @Description, @BuyingPrice, @SellingPrice, @IsMenuAvailable)
                             returning item_id";
        var id = await conn.ExecuteScalarAsync<Guid>(sql, new
        {
            VendorId = (Guid?)null,
            CategoryId = (Guid?)null,
            dto.Sku,
            dto.Name,
            Description = (string?)null,
            BuyingPrice = dto.Price, // placeholder mapping
            SellingPrice = dto.Price,
            IsMenuAvailable = true
        });
        dto.Id = id.ToString();
        return dto;
    }

    public async Task<bool> UpdateItemAsync(string id, ItemDto dto, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"update inventory.inventory_items set sku=@Sku, name=@Name, description=@Description, selling_price=@SellingPrice, is_active=@IsActive, updated_at=now()
                             where item_id=@Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = Guid.Parse(id), dto.Sku, dto.Name, Description = (string?)null, SellingPrice = dto.Price, IsActive = true });
        return rows > 0;
    }

    public async Task<bool> DeleteItemAsync(string id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"update inventory.inventory_items set is_active=false, updated_at=now() where item_id=@Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = Guid.Parse(id) });
        return rows > 0;
    }
}

