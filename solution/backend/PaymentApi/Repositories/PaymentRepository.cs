using System.Text.Json;
using Npgsql;
using PaymentApi.Models;

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

    public async Task InsertPaymentsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid sessionId, Guid billingId, string? serverId, IReadOnlyList<RegisterPaymentLineDto> lines, CancellationToken ct)
    {
        const string ins = @"INSERT INTO pay.payments(session_id, billing_id, amount_paid, payment_method, discount_amount, discount_reason, tip_amount, external_ref, meta, created_by)
                             VALUES(@sid, @bid, @amt, @pm, @disc, @drea, @tip, @ext, @meta, @by)";
        foreach (var l in lines)
        {
            await using var cmd = new NpgsqlCommand(ins, conn, tx);
            cmd.Parameters.AddWithValue("@sid", sessionId);
            cmd.Parameters.AddWithValue("@bid", billingId);
            cmd.Parameters.AddWithValue("@amt", l.AmountPaid);
            cmd.Parameters.AddWithValue("@pm", l.PaymentMethod);
            cmd.Parameters.AddWithValue("@disc", l.DiscountAmount);
            cmd.Parameters.AddWithValue("@drea", (object?)l.DiscountReason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tip", l.TipAmount);
            cmd.Parameters.AddWithValue("@ext", (object?)l.ExternalRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@meta", l.Meta is null ? DBNull.Value : JsonSerializer.SerializeToElement(l.Meta));
            cmd.Parameters.AddWithValue("@by", (object?)serverId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<(decimal totalDue, decimal totalDiscount, decimal totalPaid, decimal totalTip, string status)> UpsertLedgerAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid sessionId, Guid billingId, decimal? providedTotalDue, (decimal addPaid, decimal addDiscount, decimal addTip) deltas, CancellationToken ct)
    {
        // Upsert ledger row
        const string sel = "SELECT total_due, total_discount, total_paid, total_tip, status FROM pay.bill_ledger WHERE billing_id = @bid FOR UPDATE";
        await using var sc = new NpgsqlCommand(sel, conn, tx);
        sc.Parameters.AddWithValue("@bid", billingId);
        decimal due, disc, paid, tip; string status;
        await using var rdr = await sc.ExecuteReaderAsync(ct);
        if (await rdr.ReadAsync(ct))
        {
            due = rdr.GetDecimal(0);
            disc = rdr.GetDecimal(1);
            paid = rdr.GetDecimal(2);
            tip = rdr.GetDecimal(3);
            status = rdr.GetString(4);
        }
        else
        {
            due = providedTotalDue ?? 0m; disc = 0m; paid = 0m; tip = 0m; status = "unpaid";
        }
        await rdr.CloseAsync();

        // apply deltas
        paid += deltas.addPaid;
        disc += deltas.addDiscount;
        tip += deltas.addTip;

        // compute status: paid if paid+disc >= due; partial if >0 else unpaid
        var newStatus = (paid + disc >= due) ? "paid" : (paid + disc > 0m ? "partial" : "unpaid");
        
        System.Diagnostics.Debug.WriteLine($"PaymentRepository.UpsertLedgerAsync: BillingId={billingId}, Due={due}, Paid={paid}, Disc={disc}, Tip={tip}, Status={newStatus}");

        const string upsert = @"INSERT INTO pay.bill_ledger(billing_id, session_id, total_due, total_discount, total_paid, total_tip, status, updated_at)
                               VALUES(@bid, @sid, @due, @disc, @paid, @tip, @st, now())
                               ON CONFLICT (billing_id) DO UPDATE SET
                                 session_id = EXCLUDED.session_id,
                                 total_due = EXCLUDED.total_due,
                                 total_discount = EXCLUDED.total_discount,
                                 total_paid = EXCLUDED.total_paid,
                                 total_tip = EXCLUDED.total_tip,
                                 status = EXCLUDED.status,
                                 updated_at = now()";
        await using var uc = new NpgsqlCommand(upsert, conn, tx);
        uc.Parameters.AddWithValue("@bid", billingId);
        uc.Parameters.AddWithValue("@sid", sessionId);
        uc.Parameters.AddWithValue("@due", due);
        uc.Parameters.AddWithValue("@disc", disc);
        uc.Parameters.AddWithValue("@paid", paid);
        uc.Parameters.AddWithValue("@tip", tip);
        uc.Parameters.AddWithValue("@st", newStatus);
        await uc.ExecuteNonQueryAsync(ct);

        return (due, disc, paid, tip, newStatus);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT payment_id, session_id, billing_id, amount_paid, payment_method, discount_amount, discount_reason, tip_amount, external_ref, meta, created_by, created_at
                             FROM pay.payments WHERE billing_id = @bid ORDER BY created_at";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bid", billingId);
        var list = new List<PaymentDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new PaymentDto(
                rdr.GetInt64(0), rdr.GetFieldValue<Guid>(1), rdr.GetFieldValue<Guid>(2), rdr.GetDecimal(3), rdr.GetString(4), rdr.GetDecimal(5),
                rdr.IsDBNull(6) ? null : rdr.GetString(6), rdr.GetDecimal(7), rdr.IsDBNull(8) ? null : rdr.GetString(8),
                rdr.IsDBNull(9) ? null : rdr.GetFieldValue<object>(9), rdr.IsDBNull(10) ? null : rdr.GetString(10), rdr.GetFieldValue<DateTimeOffset>(11)));
        }
        return list;
    }

    public async Task<BillLedgerDto?> GetLedgerAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT billing_id, session_id, total_due, total_discount, total_paid, total_tip, status FROM pay.bill_ledger WHERE billing_id = @bid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bid", billingId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        return new BillLedgerDto(rdr.GetFieldValue<Guid>(0), rdr.GetFieldValue<Guid>(1), rdr.GetDecimal(2), rdr.GetDecimal(3), rdr.GetDecimal(4), rdr.GetDecimal(5), rdr.GetString(6));
    }

    public async Task AppendLogAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid billingId, Guid sessionId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct)
    {
        const string ins = @"INSERT INTO pay.payment_logs(billing_id, session_id, action, old_value, new_value, server_id)
                             VALUES(@bid, @sid, @a, @o, @n, @srv)";
        await using var cmd = new NpgsqlCommand(ins, conn, tx);
        cmd.Parameters.AddWithValue("@bid", billingId);
        cmd.Parameters.AddWithValue("@sid", sessionId);
        cmd.Parameters.AddWithValue("@a", action);
        cmd.Parameters.AddWithValue("@o", oldValue is null ? DBNull.Value : JsonSerializer.SerializeToElement(oldValue));
        cmd.Parameters.AddWithValue("@n", newValue is null ? DBNull.Value : JsonSerializer.SerializeToElement(newValue));
        cmd.Parameters.AddWithValue("@srv", (object?)serverId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<(IReadOnlyList<PaymentLogDto> Items, int Total)> ListLogsAsync(Guid billingId, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var limit = Math.Clamp(pageSize, 1, 200);
        var offset = (Math.Max(1, page) - 1) * limit;
        const string sql = @"SELECT log_id, billing_id, session_id, action, old_value, new_value, server_id, created_at
                             FROM pay.payment_logs WHERE billing_id = @bid ORDER BY created_at DESC LIMIT @l OFFSET @o";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bid", billingId);
        cmd.Parameters.AddWithValue("@l", limit);
        cmd.Parameters.AddWithValue("@o", offset);
        var list = new List<PaymentLogDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new PaymentLogDto(
                rdr.GetInt64(0), rdr.GetFieldValue<Guid>(1), rdr.GetFieldValue<Guid>(2), rdr.GetString(3),
                rdr.IsDBNull(4) ? null : rdr.GetFieldValue<object>(4), rdr.IsDBNull(5) ? null : rdr.GetFieldValue<object>(5),
                rdr.IsDBNull(6) ? null : rdr.GetString(6), rdr.GetFieldValue<DateTimeOffset>(7)));
        }
        const string cnt = "SELECT COUNT(1) FROM pay.payment_logs WHERE billing_id = @bid";
        await using var cc = new NpgsqlCommand(cnt, conn);
        cc.Parameters.AddWithValue("@bid", billingId);
        var total = Convert.ToInt32(await cc.ExecuteScalarAsync(ct));
        return (list, total);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(int limit, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT payment_id, session_id, billing_id, amount_paid, payment_method, discount_amount, discount_reason, tip_amount, external_ref, meta, created_by, created_at
                             FROM pay.payments ORDER BY created_at DESC LIMIT @l";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@l", Math.Clamp(limit, 1, 1000));
        var list = new List<PaymentDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new PaymentDto(
                rdr.GetInt64(0), rdr.GetFieldValue<Guid>(1), rdr.GetFieldValue<Guid>(2), rdr.GetDecimal(3), rdr.GetString(4), rdr.GetDecimal(5),
                rdr.IsDBNull(6) ? null : rdr.GetString(6), rdr.GetDecimal(7), rdr.IsDBNull(8) ? null : rdr.GetString(8),
                rdr.IsDBNull(9) ? null : rdr.GetFieldValue<object>(9), rdr.IsDBNull(10) ? null : rdr.GetString(10), rdr.GetFieldValue<DateTimeOffset>(11)));
        }
        return list;
    }
}
