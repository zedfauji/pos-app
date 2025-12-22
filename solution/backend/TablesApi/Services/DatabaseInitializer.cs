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
        _logger.LogInformation(">>> STARTING DATABASE SCHEMA INITIALIZATION <<<");
        // REMOVED TRY-CATCH to allow startup crash on schema failure
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            
            _logger.LogInformation("Initializing ORD Schema...");
            await EnsureOrdSchemaAsync(conn);
            
            _logger.LogInformation("Initializing SHIFT Schema...");
            await EnsureShiftSchemaAsync(conn);
            
            _logger.LogInformation(">>> SCHEMA INITIALIZATION COMPLETE <<<");
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

    private async Task EnsureShiftSchemaAsync(NpgsqlConnection conn)
    {
        // GAP-03: Shift Controller Schema
        // Reads from solution/backend/shift-migration.sql if possible, or execute embedded SQL.
        // For simplicity and robustness in this fix, we will embed the SQL from the verified plan.
        
        const string sql = @"
-- DO NOT DROP - preserve shift data across restarts
-- 1. Create shifts table IF NOT EXISTS (safe, preserves data)
CREATE TABLE IF NOT EXISTS shifts (
    shift_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_number SERIAL NOT NULL,
    opened_by_user_id INTEGER NOT NULL REFERENCES ""Users""(""Id""),
    opened_by_name VARCHAR(100),
    opened_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    starting_cash DECIMAL(10,2) NOT NULL,
    closed_by_user_id INTEGER,
    closed_by_name VARCHAR(100),
    closed_at TIMESTAMPTZ,
    declared_cash DECIMAL(10,2),
    expected_cash DECIMAL(10,2),
    difference DECIMAL(10,2),
    difference_category VARCHAR(50),
    close_reason TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'open',
    idempotency_key UUID,
    UNIQUE(shift_number)
);

CREATE INDEX IF NOT EXISTS idx_shifts_status ON shifts(status);
CREATE INDEX IF NOT EXISTS idx_shifts_opened_at ON shifts(opened_at);

-- 3. Add shift_id to existing tables
ALTER TABLE ""TableSessions"" ADD COLUMN IF NOT EXISTS shift_id UUID REFERENCES shifts(shift_id);
ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS shift_id UUID REFERENCES shifts(shift_id);

-- 4. Create bills table
CREATE TABLE IF NOT EXISTS bills (
    bill_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id UUID NOT NULL REFERENCES shifts(shift_id),
    table_session_id INTEGER NOT NULL REFERENCES ""TableSessions""(""Id""),
    total_amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'unsettled',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_user_id INTEGER REFERENCES ""Users""(""Id"")
);

CREATE INDEX IF NOT EXISTS idx_bills_shift_id ON bills(shift_id);
CREATE INDEX IF NOT EXISTS idx_bills_table_session_id ON bills(table_session_id);

-- 5. Create payments table
CREATE TABLE IF NOT EXISTS payments (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id UUID NOT NULL REFERENCES shifts(shift_id),
    bill_id UUID NOT NULL REFERENCES bills(bill_id),
    amount_paid DECIMAL(10,2) NOT NULL,
    payment_method VARCHAR(50) NOT NULL,
    is_voided BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_user_id INTEGER REFERENCES ""Users""(""Id"")
);

CREATE INDEX IF NOT EXISTS idx_payments_shift_id ON payments(shift_id);
CREATE INDEX IF NOT EXISTS idx_payments_bill_id ON payments(bill_id);
";
        await conn.ExecuteAsync(sql);
    }
}
