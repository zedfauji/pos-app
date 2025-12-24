using Npgsql;

namespace OrderApi.Services;

public sealed class DatabaseInitializer : IHostedService
{
    private readonly NpgsqlDataSource? _dataSource;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(NpgsqlDataSource? dataSource, ILogger<DatabaseInitializer> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_dataSource is null)
            {
                _logger.LogWarning("No NpgsqlDataSource configured; skipping orders schema initialization.");
                return;
            }
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await EnsureOrdersSchemaAsync(conn, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orders schema initialization skipped due to error. Service will continue to start.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureOrdersSchemaAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        // Schema adapted to match existing database structure:
        // - order_id is UUID (not bigserial)
        // - table_label (not table_id)
        // - Minimal columns to match what TablesApi created
        const string sql = @"
create schema if not exists ord;

-- The ord.orders table already exists with UUID order_id and table_label
-- Only add missing columns if needed, do not recreate
DO $$
BEGIN
  -- Add shift_id if not exists
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'ord' AND table_name = 'orders' AND column_name = 'shift_id'
  ) THEN
    ALTER TABLE ord.orders ADD COLUMN shift_id uuid;
  END IF;
END $$;

-- Create order_items table if not exists (uses UUID for order_id foreign key)
create table if not exists ord.order_items (
  order_item_id       uuid primary key default gen_random_uuid(),
  order_id            uuid not null references ord.orders(order_id) on delete cascade,
  menu_item_id        bigint null,
  combo_id            bigint null,
  quantity            int not null default 1,
  delivered_quantity  int not null default 0,
  base_price          numeric(12,2) not null default 0.00,
  price_delta         numeric(12,2) not null default 0.00,
  is_deleted          boolean not null default false,
  notes               text null,
  snapshot_name       text null,
  status              text not null default 'pending',
  created_at          timestamptz not null default now()
);
create index if not exists ix_order_items_order on ord.order_items(order_id) where is_deleted = false;

-- Create order_logs table if not exists
create table if not exists ord.order_logs (
  log_id              bigserial primary key,
  order_id            uuid not null references ord.orders(order_id) on delete cascade,
  action              text not null,
  old_value           jsonb null,
  new_value           jsonb null,
  server_id           text null,
  created_at          timestamptz not null default now()
);
create index if not exists ix_order_logs_order on ord.order_logs(order_id);
";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
