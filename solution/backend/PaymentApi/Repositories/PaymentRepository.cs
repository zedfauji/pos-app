using System.Text.Json;
using Dapper;
using Npgsql;
using PaymentApi.Models;
using MagiDesk.Shared.DTOs.Payments;

namespace PaymentApi.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly NpgsqlDataSource _dataSource;
    public PaymentRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task ExecuteInTransactionAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> action, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await action(conn, tx, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task InsertPaymentsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid sessionId, Guid billingId, string? serverId, IReadOnlyList<RegisterPaymentLineDto> lines, Guid? shiftId, CancellationToken ct)
    {
        const string sql = @"INSERT INTO pay.payments(
                                session_id, billing_id, amount_paid, method, discount_amount, 
                                discount_reason, tip_amount, external_reference, meta, created_by, shift_id)
                             VALUES(@SessionId, @BillingId, @AmountPaid, @Method, @DiscountAmount, 
                                    @DiscountReason, @TipAmount, @ExternalReference, @Meta::jsonb, @CreatedBy, @ShiftId)";

        foreach (var l in lines)
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                SessionId = sessionId,
                BillingId = billingId,
                l.AmountPaid,
                Method = l.PaymentMethod,
                l.DiscountAmount,
                DiscountReason = l.DiscountReason,
                l.TipAmount,
                ExternalReference = l.ExternalRef,
                Meta = l.Meta is null ? null : JsonSerializer.Serialize(l.Meta),
                CreatedBy = serverId,
                ShiftId = shiftId
            }, transaction: tx, cancellationToken: ct));
        }
    }

    public async Task<(decimal totalDue, decimal totalDiscount, decimal totalPaid, decimal totalTip, string status)> UpsertLedgerAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, Guid sessionId, Guid billingId, decimal? providedTotalDue, 
        (decimal addPaid, decimal addDiscount, decimal addTip) deltas, CancellationToken ct)
    {
        // 1. Get current state (lock row)
        const string sel = @"SELECT total_due as Due, total_discount as Disc, total_paid as Paid, total_tip as Tip, status as Status 
                             FROM pay.bill_ledger WHERE billing_id = @BillingId FOR UPDATE";
        
        var current = await conn.QuerySingleOrDefaultAsync<(decimal Due, decimal Disc, decimal Paid, decimal Tip, string Status)?>(
            new CommandDefinition(sel, new { BillingId = billingId }, transaction: tx, cancellationToken: ct));

        decimal due, disc, paid, tip;
        string status;

        if (current.HasValue)
        {
            due = current.Value.Due;
            disc = current.Value.Disc;
            paid = current.Value.Paid;
            tip = current.Value.Tip;
            status = current.Value.Status;
        }
        else
        {
            due = providedTotalDue ?? 0m; 
            disc = 0m; 
            paid = 0m; 
            tip = 0m; 
            status = "unpaid";
        }

        // 2. Apply deltas
        paid += deltas.addPaid;
        disc += deltas.addDiscount;
        tip += deltas.addTip;

        // 3. Compute new status
        var newStatus = (paid + disc >= due) ? "paid" : (paid + disc > 0m ? "partial" : "unpaid");
        
        System.Diagnostics.Debug.WriteLine($"PaymentRepository.UpsertLedgerAsync: BillingId={billingId}, Due={due}, Paid={paid}, Disc={disc}, Tip={tip}, Status={newStatus}");

        // 4. Upsert
        const string upsert = @"INSERT INTO pay.bill_ledger(billing_id, session_id, total_due, total_discount, total_paid, total_tip, status, updated_at)
                                VALUES(@BillingId, @SessionId, @Due, @Disc, @Paid, @Tip, @Status, now())
                                ON CONFLICT (billing_id) DO UPDATE SET
                                  session_id = EXCLUDED.session_id,
                                  total_due = EXCLUDED.total_due,
                                  total_discount = EXCLUDED.total_discount,
                                  total_paid = EXCLUDED.total_paid,
                                  total_tip = EXCLUDED.total_tip,
                                  status = EXCLUDED.status,
                                  updated_at = now()";
        
        await conn.ExecuteAsync(new CommandDefinition(upsert, new { 
            BillingId = billingId, 
            SessionId = sessionId, 
            Due = due, 
            Disc = disc, 
            Paid = paid, 
            Tip = tip, 
            Status = newStatus 
        }, transaction: tx, cancellationToken: ct));

        return (due, disc, paid, tip, newStatus);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
                                payment_id, 
                                session_id, 
                                billing_id, 
                                amount_paid, 
                                payment_method, 
                                discount_amount, 
                                discount_reason, 
                                tip_amount, 
                                external_ref, 
                                created_by, 
                                created_at,
                                meta
                             FROM pay.payments 
                             WHERE billing_id = @BillingId 
                             ORDER BY created_at";
        
        var results = await conn.QueryAsync(sql, new { BillingId = billingId });
        
        return results.Select(r => new PaymentDto(
            (Guid)r.payment_id,
            (Guid)r.session_id,
            (Guid)r.billing_id,
            (decimal)r.amount_paid,
            (string)r.payment_method,
            (decimal)r.discount_amount,
            (string?)r.discount_reason,
            (decimal)r.tip_amount,
            (string?)r.external_ref,
            (object?)r.meta, 
            (string?)r.created_by,
            (DateTimeOffset)r.created_at,
            null
        )).ToList();
    }

    public async Task<BillLedgerDto?> GetLedgerAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        // Map explicitly to constructor
        const string sql = @"SELECT billing_id, session_id, total_due, total_discount, total_paid, total_tip, status 
                             FROM pay.bill_ledger WHERE billing_id = @BillingId";
        
        // Note: Dapper maps columns to constructor parameters by name (case-insensitive) if we use <BillLedgerDto>
        return await conn.QuerySingleOrDefaultAsync<BillLedgerDto>(sql, new { BillingId = billingId });
    }

    public async Task AppendLogAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid billingId, Guid sessionId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct)
    {
        const string sql = @"INSERT INTO pay.payment_logs(billing_id, session_id, action, old_value, new_value, server_id)
                             VALUES(@BillingId, @SessionId, @Action, @OldValue::jsonb, @NewValue::jsonb, @ServerId)";
                             
        await conn.ExecuteAsync(new CommandDefinition(sql, new {
            BillingId = billingId,
            SessionId = sessionId,
            Action = action,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            ServerId = serverId
        }, transaction: tx, cancellationToken: ct));
    }

    public async Task<(IReadOnlyList<PaymentLogDto> Items, int Total)> ListLogsAsync(Guid billingId, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var limit = Math.Clamp(pageSize, 1, 200);
        var offset = (Math.Max(1, page) - 1) * limit;
        
        const string sql = @"SELECT log_id, billing_id, session_id, action, old_value, new_value, server_id, created_at
                             FROM pay.payment_logs WHERE billing_id = @BillingId 
                             ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset";
                             
        var items = (await conn.QueryAsync(sql, new { BillingId = billingId, Limit = limit, Offset = offset })).Select(r => new PaymentLogDto(
            (long)r.log_id,
            (Guid)r.billing_id,
            (Guid)r.session_id,
            (string)r.action,
            (object?)r.old_value,
            (object?)r.new_value,
            (string?)r.server_id,
            (DateTimeOffset)r.created_at
        )).ToList();

        const string cnt = "SELECT COUNT(1) FROM pay.payment_logs WHERE billing_id = @BillingId";
        var total = await conn.ExecuteScalarAsync<int>(cnt, new { BillingId = billingId });
        
        return (items, total);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(int limit, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
                                payment_id, session_id, billing_id, amount_paid, payment_method, 
                                discount_amount, discount_reason, tip_amount, external_ref, meta, 
                                created_by, created_at
                             FROM pay.payments 
                             ORDER BY created_at DESC LIMIT @Limit";

        var results = await conn.QueryAsync(sql, new { Limit = Math.Clamp(limit, 1, 10000) });

        return results.Select(r => new PaymentDto(
            (Guid)r.payment_id,
            (Guid)r.session_id,
            (Guid)r.billing_id,
            (decimal)r.amount_paid,
            (string)r.payment_method,
            (decimal)r.discount_amount,
            (string?)r.discount_reason,
            (decimal)r.tip_amount,
            (string?)r.external_ref,
            (object?)r.meta,
            (string?)r.created_by,
            (DateTimeOffset)r.created_at,
            null
        )).ToList();
    }
}
