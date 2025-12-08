using Npgsql;

namespace PaymentApi.Services;

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
                _logger.LogWarning("No NpgsqlDataSource configured; skipping payments schema initialization.");
                return;
            }
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await EnsureSchemaAsync(conn, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payments schema initialization skipped due to error. Service will continue to start.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureSchemaAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = @"
create schema if not exists pay;

create table if not exists pay.payments (
  payment_id       uuid primary key default gen_random_uuid(),
  session_id       uuid not null,
  billing_id       uuid not null,
  amount_paid      numeric(12,2) not null check (amount_paid >= 0),
  currency         text not null default 'USD',
  payment_method   text not null,
  discount_amount  numeric(12,2) not null default 0.00 check (discount_amount >= 0),
  discount_reason  text null,
  tip_amount       numeric(12,2) not null default 0.00 check (tip_amount >= 0),
  external_ref     text null,
  meta             jsonb null,
  created_by       text null,
  created_at       timestamptz not null default now(),
  -- Immutability constraints
  CONSTRAINT payments_immutable_billing_id CHECK (billing_id IS NOT NULL AND billing_id != '00000000-0000-0000-0000-000000000000'::uuid)
);
create index if not exists ix_payments_billing on pay.payments(billing_id);

-- Create trigger to prevent updates to payment_id and billing_id
CREATE OR REPLACE FUNCTION pay.prevent_payment_updates()
RETURNS TRIGGER AS $$
BEGIN
  -- Prevent updates to payment_id
  IF OLD.payment_id != NEW.payment_id THEN
    RAISE EXCEPTION 'Payment ID is immutable and cannot be changed. Payment ID: %', OLD.payment_id;
  END IF;
  
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  -- Prevent updates to created_at
  IF OLD.created_at != NEW.created_at THEN
    RAISE EXCEPTION 'Created timestamp is immutable and cannot be changed. Created at: %', OLD.created_at;
  END IF;
  
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_prevent_payment_updates ON pay.payments;
CREATE TRIGGER trg_prevent_payment_updates
  BEFORE UPDATE ON pay.payments
  FOR EACH ROW
  EXECUTE FUNCTION pay.prevent_payment_updates();

create table if not exists pay.bill_ledger (
  billing_id       text primary key,
  session_id       text not null,
  total_due        numeric(12,2) not null default 0.00,
  total_discount   numeric(12,2) not null default 0.00,
  total_paid       numeric(12,2) not null default 0.00,
  total_tip        numeric(12,2) not null default 0.00,
  status           text not null default 'unpaid',
  updated_at       timestamptz not null default now(),
  -- Immutability constraints
  CONSTRAINT bill_ledger_immutable_billing_id CHECK (billing_id IS NOT NULL AND billing_id != '')
);
create index if not exists ix_bill_ledger_status on pay.bill_ledger(status);

-- Create trigger to prevent updates to billing_id in bill_ledger
CREATE OR REPLACE FUNCTION pay.prevent_bill_ledger_updates()
RETURNS TRIGGER AS $$
BEGIN
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_prevent_bill_ledger_updates ON pay.bill_ledger;
CREATE TRIGGER trg_prevent_bill_ledger_updates
  BEFORE UPDATE ON pay.bill_ledger
  FOR EACH ROW
  EXECUTE FUNCTION pay.prevent_bill_ledger_updates();

create table if not exists pay.payment_logs (
  log_id           bigserial primary key,
  billing_id       text not null,
  session_id       text not null,
  action           text not null,
  old_value        jsonb null,
  new_value        jsonb null,
  server_id        text null,
  created_at       timestamptz not null default now(),
  -- Immutability constraints
  CONSTRAINT payment_logs_immutable_log_id CHECK (log_id > 0),
  CONSTRAINT payment_logs_immutable_billing_id CHECK (billing_id IS NOT NULL AND billing_id != ''),
  CONSTRAINT payment_logs_immutable_created_at CHECK (created_at IS NOT NULL)
);
create index if not exists ix_payment_logs_billing on pay.payment_logs(billing_id);

-- Create trigger to prevent updates to immutable fields in payment_logs
CREATE OR REPLACE FUNCTION pay.prevent_payment_logs_updates()
RETURNS TRIGGER AS $$
BEGIN
  -- Prevent updates to log_id
  IF OLD.log_id != NEW.log_id THEN
    RAISE EXCEPTION 'Log ID is immutable and cannot be changed. Log ID: %', OLD.log_id;
  END IF;
  
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  -- Prevent updates to created_at
  IF OLD.created_at != NEW.created_at THEN
    RAISE EXCEPTION 'Created timestamp is immutable and cannot be changed. Created at: %', OLD.created_at;
  END IF;
  
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_prevent_payment_logs_updates ON pay.payment_logs;
CREATE TRIGGER trg_prevent_payment_logs_updates
  BEFORE UPDATE ON pay.payment_logs
  FOR EACH ROW
  EXECUTE FUNCTION pay.prevent_payment_logs_updates();
";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
