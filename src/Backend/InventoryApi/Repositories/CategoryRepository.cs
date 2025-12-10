using Dapper;
using Npgsql;

namespace InventoryApi.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly NpgsqlDataSource _dataSource;
    public CategoryRepository(NpgsqlDataSource dataSource) { _dataSource = dataSource; }

    public async Task<IReadOnlyList<object>> ListAsync(CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync("select category_id, name, parent_id from inventory.inventory_categories order by name");
        return rows.ToList();
    }
}

