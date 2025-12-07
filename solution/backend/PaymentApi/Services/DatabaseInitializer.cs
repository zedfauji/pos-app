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

-- Refunds table
create table if not exists pay.refunds (
  refund_id         uuid primary key default gen_random_uuid(),
  payment_id        uuid not null,
  billing_id        uuid not null,
  session_id        uuid not null,
  refund_amount     numeric(12,2) not null check (refund_amount > 0),
  refund_reason     text null,
  refund_method     text not null default 'original', -- 'original', 'cash', 'wallet', 'card', 'upi'
  external_ref      text null, -- For external payment processor refund reference
  meta              jsonb null,
  created_by        text null,
  created_at        timestamptz not null default now(),
  -- Constraints
  CONSTRAINT refunds_immutable_refund_id CHECK (refund_id IS NOT NULL),
  CONSTRAINT refunds_immutable_billing_id CHECK (billing_id IS NOT NULL AND billing_id != '00000000-0000-0000-0000-000000000000'::uuid),
  CONSTRAINT refunds_immutable_created_at CHECK (created_at IS NOT NULL)
);
create index if not exists ix_refunds_payment on pay.refunds(payment_id);
create index if not exists ix_refunds_billing on pay.refunds(billing_id);
create index if not exists ix_refunds_created_at on pay.refunds(created_at);

-- Create trigger to prevent updates to immutable fields in refunds
CREATE OR REPLACE FUNCTION pay.prevent_refund_updates()
RETURNS TRIGGER AS $$
BEGIN
  -- Prevent updates to refund_id
  IF OLD.refund_id != NEW.refund_id THEN
    RAISE EXCEPTION 'Refund ID is immutable and cannot be changed. Refund ID: %', OLD.refund_id;
  END IF;
  
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  -- Prevent updates to payment_id
  IF OLD.payment_id != NEW.payment_id THEN
    RAISE EXCEPTION 'Payment ID is immutable and cannot be changed. Payment ID: %', OLD.payment_id;
  END IF;
  
  -- Prevent updates to created_at
  IF OLD.created_at != NEW.created_at THEN
    RAISE EXCEPTION 'Created timestamp is immutable and cannot be changed. Created at: %', OLD.created_at;
  END IF;
  
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_prevent_refund_updates ON pay.refunds;
CREATE TRIGGER trg_prevent_refund_updates
  BEFORE UPDATE ON pay.refunds
  FOR EACH ROW
  EXECUTE FUNCTION pay.prevent_refund_updates();

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
        
        // Initialize caja schema
        await EnsureCajaSchemaAsync(conn, ct);
    }

    private static async Task EnsureCajaSchemaAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var cajaSchemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "CajaSchema.sql");
        
        if (File.Exists(cajaSchemaPath))
        {
            var cajaSchemaSql = await File.ReadAllTextAsync(cajaSchemaPath, ct);
            await using var cmd = new NpgsqlCommand(cajaSchemaSql, conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        else
        {
            // Fallback: inline schema creation
            const string fallbackSql = @"
CREATE SCHEMA IF NOT EXISTS caja;

CREATE TABLE IF NOT EXISTS caja.caja_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    opened_by_user_id TEXT NOT NULL,
    closed_by_user_id TEXT NULL,
    opened_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    closed_at TIMESTAMPTZ NULL,
    opening_amount NUMERIC(10,2) NOT NULL CHECK (opening_amount >= 0),
    closing_amount NUMERIC(10,2) NULL CHECK (closing_amount IS NULL OR closing_amount >= 0),
    system_calculated_total NUMERIC(10,2) NULL,
    difference NUMERIC(10,2) NULL,
    status TEXT NOT NULL CHECK (status IN ('open', 'closed')) DEFAULT 'open',
    notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS caja.caja_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    caja_session_id UUID NOT NULL REFERENCES caja.caja_sessions(id) ON DELETE CASCADE,
    transaction_id UUID NOT NULL,
    transaction_type TEXT NOT NULL CHECK (transaction_type IN ('sale', 'refund', 'tip', 'deposit', 'withdrawal')),
    amount NUMERIC(10,2) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_caja_sessions_status ON caja.caja_sessions(status);
CREATE INDEX IF NOT EXISTS idx_caja_sessions_opened_at ON caja.caja_sessions(opened_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS idx_caja_sessions_active 
ON caja.caja_sessions(status) 
WHERE status = 'open';
CREATE INDEX IF NOT EXISTS idx_caja_sessions_opened_by ON caja.caja_sessions(opened_by_user_id);
CREATE INDEX IF NOT EXISTS idx_caja_sessions_closed_by ON caja.caja_sessions(closed_by_user_id);

CREATE INDEX IF NOT EXISTS idx_caja_transactions_session ON caja.caja_transactions(caja_session_id);
CREATE INDEX IF NOT EXISTS idx_caja_transactions_type ON caja.caja_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_caja_transactions_timestamp ON caja.caja_transactions(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_caja_transactions_session_type ON caja.caja_transactions(caja_session_id, transaction_type);

ALTER TABLE pay.payments 
ADD COLUMN IF NOT EXISTS caja_session_id UUID NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'fk_payments_caja_session'
    ) THEN
        ALTER TABLE pay.payments
        ADD CONSTRAINT fk_payments_caja_session 
        FOREIGN KEY (caja_session_id) 
        REFERENCES caja.caja_sessions(id) 
        ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_payments_caja_session ON pay.payments(caja_session_id);

ALTER TABLE pay.refunds 
ADD COLUMN IF NOT EXISTS caja_session_id UUID NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'fk_refunds_caja_session'
    ) THEN
        ALTER TABLE pay.refunds
        ADD CONSTRAINT fk_refunds_caja_session 
        FOREIGN KEY (caja_session_id) 
        REFERENCES caja.caja_sessions(id) 
        ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_refunds_caja_session ON pay.refunds(caja_session_id);
";
            await using var cmd = new NpgsqlCommand(fallbackSql, conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
