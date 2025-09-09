using Dapper;
using MagiDesk.Shared.DTOs;
using Npgsql;

namespace InventoryApi.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly NpgsqlDataSource _dataSource;
    public VendorRepository(NpgsqlDataSource dataSource) { _dataSource = dataSource; }

    public async Task<IReadOnlyList<VendorDto>> ListAsync(CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "select vendor_id as Id, name as Name, contact_info as ContactInfo, status as Status from inventory.vendors order by name";
        var items = await conn.QueryAsync<VendorDto>(sql);
        return items.ToList();
    }

    public async Task<VendorDto?> GetAsync(string id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "select vendor_id as Id, name as Name, contact_info as ContactInfo, status as Status from inventory.vendors where vendor_id=@Id";
        return await conn.QuerySingleOrDefaultAsync<VendorDto>(sql, new { Id = Guid.Parse(id) });
    }

    public async Task<VendorDto> CreateAsync(VendorDto dto, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "insert into inventory.vendors (name, contact_info, status) values (@Name, @ContactInfo, @Status) returning vendor_id";
        var id = await conn.ExecuteScalarAsync<Guid>(sql, new { dto.Name, dto.ContactInfo, dto.Status });
        dto.Id = id.ToString();
        return dto;
    }

    public async Task<bool> UpdateAsync(string id, VendorDto dto, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "update inventory.vendors set name=@Name, contact_info=@ContactInfo, status=@Status where vendor_id=@Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = Guid.Parse(id), dto.Name, dto.ContactInfo, dto.Status });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "delete from inventory.vendors where vendor_id=@Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = Guid.Parse(id) });
        return rows > 0;
    }
}

