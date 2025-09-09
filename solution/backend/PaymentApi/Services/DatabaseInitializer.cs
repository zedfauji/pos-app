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
  payment_id       bigserial primary key,
  session_id       text not null,
  billing_id       text not null,
  amount_paid      numeric(12,2) not null check (amount_paid >= 0),
  currency         text not null default 'USD',
  payment_method   text not null,
  discount_amount  numeric(12,2) not null default 0.00 check (discount_amount >= 0),
  discount_reason  text null,
  tip_amount       numeric(12,2) not null default 0.00 check (tip_amount >= 0),
  external_ref     text null,
  meta             jsonb null,
  created_by       text null,
  created_at       timestamptz not null default now()
);
create index if not exists ix_payments_billing on pay.payments(billing_id);

create table if not exists pay.bill_ledger (
  billing_id       text primary key,
  session_id       text not null,
  total_due        numeric(12,2) not null default 0.00,
  total_discount   numeric(12,2) not null default 0.00,
  total_paid       numeric(12,2) not null default 0.00,
  total_tip        numeric(12,2) not null default 0.00,
  status           text not null default 'unpaid',
  updated_at       timestamptz not null default now()
);
create index if not exists ix_bill_ledger_status on pay.bill_ledger(status);

create table if not exists pay.payment_logs (
  log_id           bigserial primary key,
  billing_id       text not null,
  session_id       text not null,
  action           text not null,
  old_value        jsonb null,
  new_value        jsonb null,
  server_id        text null,
  created_at       timestamptz not null default now()
);
create index if not exists ix_payment_logs_billing on pay.payment_logs(billing_id);
";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
