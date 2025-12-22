using Dapper;
using Npgsql;
using System.Data;

namespace TablesApi.Services;

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
            await EnsureOrdSchemaAsync(conn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TablesApi schema initialization failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureOrdSchemaAsync(NpgsqlConnection conn)
    {
        // 1. Reset Ord Schema (Local Dev only - ensuring clean slate for type fix)
        // In prod we would migrate.
        const string checkOrd = "SELECT 1 FROM information_schema.schemata WHERE schema_name = 'ord'";
        var exists = await conn.ExecuteScalarAsync<int?>(checkOrd);
        
        // If we need to force update types, simplistic approach: drop schema if exists
        // CAUTION: This wipes order history on restart. Acceptable for dev validation phase.
        // Better: Check column type.
        
        // REMOVED DESTRUCTIVE DROP for debugging persistence
        // const string dropSql = "DROP SCHEMA IF EXISTS ord CASCADE"; 
        // await conn.ExecuteAsync(dropSql);

        const string sql = @"
create schema if not exists ord;

create table if not exists ord.orders (
    order_id        uuid primary key,
    session_id      uuid not null,
    table_label     text not null,
    created_at      timestamptz not null default now(),
    status          text not null default 'submitted',
    is_deleted      boolean not null default false
);

create table if not exists ord.order_items (
    order_item_id   uuid primary key,
    order_id        uuid not null references ord.orders(order_id),
    menu_item_id    bigint not null, -- Changed from uuid to bigint to match MenuApi
    quantity        int not null default 1,
    base_price      numeric(12,2) not null default 0.00,
    price_delta     numeric(12,2) not null default 0.00,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    delivered_quantity int not null default 0,
    status          text not null default 'pending',
    snapshot_name   text null 
);

-- Index for session lookups
create index if not exists ix_orders_session on ord.orders(session_id);
create index if not exists ix_order_items_order on ord.order_items(order_id);

-- 2. Audit Table for Moves (Missing in dump)
create table if not exists public.table_session_moves (
    move_id         bigserial primary key,
    session_id      uuid not null,
    from_label      text not null,
    to_label        text not null,
    moved_at        timestamptz not null default now()
);
";
        await conn.ExecuteAsync(sql);
    }
}
