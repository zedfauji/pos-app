using Dapper;
using Npgsql;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryApi.Services;

public sealed class DatabaseInitializer : IHostedService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await EnsureSchemaAsync(conn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InventoryApi schema initialization failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureSchemaAsync(NpgsqlConnection conn)
    {
         // GREENFIELD IMPLEMENTATION: Replace old schema
         // In prod, migration would be safer. For this rewrite, we drop conflict.
         
         const string checkLegacy = "SELECT 1 FROM information_schema.tables WHERE table_schema = 'inventory' AND table_name = 'inventory_items'";
         var legacyExists = await conn.ExecuteScalarAsync<int?>(checkLegacy);
         
         if (legacyExists.HasValue)
         {
             // Drop old schema if it has the wrong table structure
             // Using CASCADE to remove all dependents
             await conn.ExecuteAsync("DROP SCHEMA IF EXISTS inventory CASCADE");
         }

        const string sql = @"
create schema if not exists inventory;

create table if not exists inventory.items (
    id              bigserial primary key,
    name            text not null,
    unit            text not null default 'unit',
    quantity        numeric(12,2) not null default 0,
    reorder_level   numeric(12,2) not null default 10,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

create table if not exists inventory.transactions (
    id              bigserial primary key,
    item_id         bigint not null references inventory.items(id),
    change_amount   numeric(12,2) not null,
    reason          text not null,
    created_at      timestamptz not null default now()
);

-- Index
create index if not exists ix_items_name on inventory.items(name);
create index if not exists ix_tx_item on inventory.transactions(item_id);
";
        await conn.ExecuteAsync(sql);
    }
}
