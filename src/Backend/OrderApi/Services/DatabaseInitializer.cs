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
        const string sql = @"
create schema if not exists ord;

create table if not exists ord.orders (
  order_id            bigserial primary key,
  session_id          uuid not null,
  billing_id          uuid null,
  table_id            text not null,
  server_id           text not null,
  server_name         text null,
  status              text not null default 'open' check (status in ('open', 'waiting', 'delivered', 'closed')),
  delivery_status     text not null default 'pending' check (delivery_status in ('pending', 'partial', 'delivered')),
  is_deleted          boolean not null default false,
  subtotal            numeric(12,2) not null default 0.00,
  discount_total      numeric(12,2) not null default 0.00,
  tax_total           numeric(12,2) not null default 0.00,
  total               numeric(12,2) not null default 0.00,
  profit_total        numeric(12,2) not null default 0.00,
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now(),
  closed_at           timestamptz null
);
create index if not exists ix_orders_session on ord.orders(session_id) where is_deleted = false;
create index if not exists ix_orders_table on ord.orders(table_id) where is_deleted = false;
create index if not exists ix_orders_status on ord.orders(status);

create table if not exists ord.order_items (
  order_item_id       bigserial primary key,
  order_id            bigint not null references ord.orders(order_id) on delete cascade,
  menu_item_id        bigint null,
  combo_id            bigint null,
  quantity            int not null default 1,
  delivered_quantity  int not null default 0,
  base_price          numeric(12,2) not null,
  vendor_price        numeric(12,2) not null default 0.00,
  price_delta         numeric(12,2) not null default 0.00,
  line_discount       numeric(12,2) not null default 0.00,
  line_total          numeric(12,2) not null default 0.00,
  profit              numeric(12,2) not null default 0.00,
  is_deleted          boolean not null default false,
  notes               text null,
  snapshot_name       text null,
  snapshot_sku        text null,
  snapshot_category   text null,
  snapshot_group      text null,
  snapshot_version    int null,
  snapshot_picture_url text null,
  selected_modifiers  jsonb null,
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now()
);
create index if not exists ix_order_items_order on ord.order_items(order_id) where is_deleted = false;

create table if not exists ord.order_logs (
  log_id              bigserial primary key,
  order_id            bigint not null references ord.orders(order_id) on delete cascade,
  action              text not null,
  old_value           jsonb null,
  new_value           jsonb null,
  server_id           text null,
  created_at          timestamptz not null default now()
);
create index if not exists ix_order_logs_order on ord.order_logs(order_id);

-- Add delivery tracking columns if they don't exist
ALTER TABLE ord.orders ADD COLUMN IF NOT EXISTS delivery_status text NOT NULL DEFAULT 'pending';
ALTER TABLE ord.order_items ADD COLUMN IF NOT EXISTS delivered_quantity int NOT NULL DEFAULT 0;

-- Update status constraints
DO $$ 
BEGIN
  -- Add new status values if they don't exist
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'orders_status_check') THEN
    ALTER TABLE ord.orders ADD CONSTRAINT orders_status_check CHECK (status IN ('open', 'waiting', 'delivered', 'closed'));
  END IF;
  
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'orders_delivery_status_check') THEN
    ALTER TABLE ord.orders ADD CONSTRAINT orders_delivery_status_check CHECK (delivery_status IN ('pending', 'partial', 'delivered'));
  END IF;
END $$;

-- Migrate existing session_id and billing_id from TEXT to UUID if needed
DO $$ 
BEGIN
  -- Check if session_id column exists and is TEXT type
  IF EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'ord' 
      AND table_name = 'orders' 
      AND column_name = 'session_id' 
      AND data_type = 'text'
  ) THEN
    -- Convert TEXT session_id to UUID
    UPDATE ord.orders 
    SET session_id = session_id::uuid 
    WHERE session_id IS NOT NULL 
      AND session_id != '' 
      AND session_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Alter column type to UUID
    ALTER TABLE ord.orders ALTER COLUMN session_id TYPE uuid USING session_id::uuid;
  END IF;
  
  -- Check if billing_id column exists and is TEXT type
  IF EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'ord' 
      AND table_name = 'orders' 
      AND column_name = 'billing_id' 
      AND data_type = 'text'
  ) THEN
    -- Convert TEXT billing_id to UUID
    UPDATE ord.orders 
    SET billing_id = billing_id::uuid 
    WHERE billing_id IS NOT NULL 
      AND billing_id != '' 
      AND billing_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Alter column type to UUID
    ALTER TABLE ord.orders ALTER COLUMN billing_id TYPE uuid USING billing_id::uuid;
  END IF;
END $$;
";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
