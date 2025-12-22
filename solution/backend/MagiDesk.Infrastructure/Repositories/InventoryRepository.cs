using Dapper;
using MagiDesk.Core.Entities;
using MagiDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagiDesk.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres") 
                ?? configuration["Db:Postgres:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("INVENTORYAPI__CONNSTRING")
                ?? throw new InvalidOperationException("Missing Postgres Connection String");
        }

        private NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<InventoryItem>> GetAllAsync()
        {
            const string sql = @"SELECT id, name, unit, quantity, reorder_level as ReorderLevel, created_at as CreatedAt, updated_at as UpdatedAt 
                                 FROM inventory.items ORDER BY name";
            using var conn = CreateConnection();
            return await conn.QueryAsync<InventoryItem>(sql);
        }

        public async Task<InventoryItem?> GetByIdAsync(long id)
        {
            const string sql = @"SELECT id, name, unit, quantity, reorder_level as ReorderLevel, created_at as CreatedAt, updated_at as UpdatedAt 
                                 FROM inventory.items WHERE id = @Id";
            using var conn = CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<InventoryItem>(sql, new { Id = id });
        }

        public async Task<long> CreateAsync(InventoryItem item)
        {
            const string sql = @"INSERT INTO inventory.items (name, unit, quantity, reorder_level, created_at, updated_at)
                                 VALUES (@Name, @Unit, @Quantity, @ReorderLevel, now(), now())
                                 RETURNING id";
            using var conn = CreateConnection();
            return await conn.ExecuteScalarAsync<long>(sql, item);
        }

        public async Task UpdateAsync(InventoryItem item)
        {
            const string sql = @"UPDATE inventory.items 
                                 SET name = @Name, unit = @Unit, quantity = @Quantity, reorder_level = @ReorderLevel, updated_at = now()
                                 WHERE id = @Id";
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, item);
        }

        public async Task DeleteAsync(long id)
        {
            // We need to delete transactions first if we didn't set CASCADE on FK. 
            // In DatabaseInitializer we didn't explicitly set ON DELETE CASCADE for the FK.
            // So let's delete transactions first to be safe.
            const string sqlTx = "DELETE FROM inventory.transactions WHERE item_id = @Id";
            const string sqlItem = "DELETE FROM inventory.items WHERE id = @Id";
            
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();
            
            try 
            {
                await conn.ExecuteAsync(sqlTx, new { Id = id }, trans);
                await conn.ExecuteAsync(sqlItem, new { Id = id }, trans);
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task AddTransactionAsync(InventoryTransaction transaction)
        {
            const string sql = @"INSERT INTO inventory.transactions (item_id, change_amount, reason, created_at)
                                 VALUES (@ItemId, @ChangeAmount, @Reason, now())";
            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, transaction);
        }

        public async Task<MagiDesk.Shared.DTOs.Reports.InventoryStatsDto> GetStatsAsync()
        {
            const string sql = @"
                SELECT 
                    COUNT(1) as TotalItemCount,
                    COUNT(CASE WHEN quantity <= reorder_level THEN 1 END) as LowStockCount,
                    0 as TotalInventoryValue
                FROM inventory.items";
            
            using var conn = CreateConnection();
            return await conn.QuerySingleAsync<MagiDesk.Shared.DTOs.Reports.InventoryStatsDto>(sql);
        }
    }
}
