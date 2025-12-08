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

    public async Task InsertPaymentsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid sessionId, Guid billingId, Guid? cajaSessionId, string? serverId, IReadOnlyList<RegisterPaymentLineDto> lines, CancellationToken ct)
    {
        const string ins = @"INSERT INTO pay.payments(session_id, billing_id, caja_session_id, amount_paid, payment_method, discount_amount, discount_reason, tip_amount, external_ref, meta, created_by)
                             VALUES(@sid, @bid, @cajaSid, @amt, @pm, @disc, @drea, @tip, @ext, @meta, @by)
                             RETURNING payment_id";
        foreach (var l in lines)
        {
            await using var cmd = new NpgsqlCommand(ins, conn, tx);
            cmd.Parameters.AddWithValue("@sid", sessionId);
            cmd.Parameters.AddWithValue("@bid", billingId);
            cmd.Parameters.AddWithValue("@cajaSid", (object?)cajaSessionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@amt", l.AmountPaid);
            cmd.Parameters.AddWithValue("@pm", l.PaymentMethod);
            cmd.Parameters.AddWithValue("@disc", l.DiscountAmount);
            cmd.Parameters.AddWithValue("@drea", (object?)l.DiscountReason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tip", l.TipAmount);
            cmd.Parameters.AddWithValue("@ext", (object?)l.ExternalRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@meta", l.Meta is null ? DBNull.Value : JsonSerializer.SerializeToElement(l.Meta));
            cmd.Parameters.AddWithValue("@by", (object?)serverId ?? DBNull.Value);
            
            var paymentId = (Guid)await cmd.ExecuteScalarAsync(ct);
            
            // Add caja transaction for sale
            if (cajaSessionId.HasValue)
            {
                const string cajaTransSql = @"
                    INSERT INTO caja.caja_transactions (caja_session_id, transaction_id, transaction_type, amount, timestamp)
                    VALUES (@cajaSid, @transId, 'sale', @amt, now())";
                await using var cajaCmd = new NpgsqlCommand(cajaTransSql, conn, tx);
                cajaCmd.Parameters.AddWithValue("@cajaSid", cajaSessionId.Value);
                cajaCmd.Parameters.AddWithValue("@transId", paymentId);
                cajaCmd.Parameters.AddWithValue("@amt", l.AmountPaid);
                await cajaCmd.ExecuteNonQueryAsync(ct);
                
                // Add tip as separate transaction if present
                if (l.TipAmount > 0)
                {
                    await using var tipCmd = new NpgsqlCommand(cajaTransSql.Replace("'sale'", "'tip'").Replace("@amt", "@tipAmt"), conn, tx);
                    tipCmd.Parameters.AddWithValue("@cajaSid", cajaSessionId.Value);
                    tipCmd.Parameters.AddWithValue("@transId", paymentId);
                    tipCmd.Parameters.AddWithValue("@tipAmt", l.TipAmount);
                    await tipCmd.ExecuteNonQueryAsync(ct);
                }
            }
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

        // Get total refunded amount for this billing_id to calculate net paid
        var totalRefunded = await GetTotalRefundedAmountByBillingIdAsync(conn, tx, billingId, ct);
        var netPaid = paid - totalRefunded;

        // compute status: 
        // - refunded: if netPaid <= 0 and was previously paid
        // - partial-refunded: if netPaid > 0 but less than due and was previously paid
        // - paid: if netPaid + disc >= due
        // - partial: if netPaid + disc > 0 but < due
        // - unpaid: otherwise
        string newStatus;
        if (netPaid <= 0m && (status == "paid" || status == "partial-refunded" || status == "refunded"))
        {
            newStatus = "refunded";
        }
        else if (netPaid > 0m && netPaid + disc < due && (status == "paid" || status == "partial-refunded" || status == "refunded"))
        {
            newStatus = "partial-refunded";
        }
        else if (netPaid + disc >= due)
        {
            newStatus = "paid";
        }
        else if (netPaid + disc > 0m)
        {
            newStatus = "partial";
        }
        else
        {
            newStatus = "unpaid";
        }
        
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
        try
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
                try
                {
                    // Try to read payment_id as UUID first, fallback to other types if needed
                    Guid paymentId;
                    if (rdr.GetFieldType(0) == typeof(Guid))
                    {
                        paymentId = rdr.GetFieldValue<Guid>(0);
                    }
                    else if (rdr.GetFieldType(0) == typeof(long) || rdr.GetFieldType(0) == typeof(int))
                    {
                        // Legacy support: if payment_id is stored as bigint/int, convert to Guid
                        var longId = rdr.GetInt64(0);
                        var bytes = BitConverter.GetBytes(longId);
                        var guidBytes = new byte[16];
                        Array.Copy(bytes, 0, guidBytes, 0, Math.Min(bytes.Length, 8));
                        paymentId = new Guid(guidBytes);
                    }
                    else
                    {
                        var strId = rdr.GetString(0);
                        paymentId = Guid.Parse(strId);
                    }
                    
                    list.Add(new PaymentDto(
                        paymentId,
                        rdr.GetFieldValue<Guid>(1), 
                        rdr.GetFieldValue<Guid>(2), 
                        rdr.GetDecimal(3), 
                        rdr.GetString(4), 
                        rdr.GetDecimal(5),
                        rdr.IsDBNull(6) ? null : rdr.GetString(6), 
                        rdr.GetDecimal(7), 
                        rdr.IsDBNull(8) ? null : rdr.GetString(8),
                        rdr.IsDBNull(9) ? null : rdr.GetFieldValue<object>(9), 
                        rdr.IsDBNull(10) ? null : rdr.GetString(10), 
                        rdr.GetFieldValue<DateTimeOffset>(11)));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PaymentRepository.ListPaymentsAsync: Error reading payment row: {ex.Message}");
                    continue;
                }
            }
            return list;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentRepository.ListPaymentsAsync: Exception: {ex.Message}");
            throw;
        }
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
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            const string sql = @"SELECT payment_id, session_id, billing_id, amount_paid, payment_method, discount_amount, discount_reason, tip_amount, external_ref, meta, created_by, created_at
                                 FROM pay.payments ORDER BY created_at DESC LIMIT @l";
            await using var cmd = new NpgsqlCommand(sql, conn);
            // Increased max limit to 10000 to show more payments
            cmd.Parameters.AddWithValue("@l", Math.Clamp(limit, 1, 10000));
            var list = new List<PaymentDto>();
            await using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                try
                {
                    // Try to read payment_id - handle both UUID and bigint types
                    Guid paymentId;
                    var fieldType = rdr.GetFieldType(0);
                    System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: payment_id field type = {fieldType?.FullName}");
                    
                    if (fieldType == typeof(Guid))
                    {
                        paymentId = rdr.GetFieldValue<Guid>(0);
                    }
                    else if (fieldType == typeof(long) || fieldType == typeof(int) || fieldType == typeof(short))
                    {
                        // Legacy support: if payment_id is stored as bigint/int, we need to handle this differently
                        // For now, throw a more descriptive error
                        var longId = rdr.GetInt64(0);
                        System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Found payment_id as long: {longId}");
                        // Create a deterministic Guid from the long value
                        var bytes = new byte[16];
                        var longBytes = BitConverter.GetBytes(longId);
                        Array.Copy(longBytes, 0, bytes, 0, Math.Min(longBytes.Length, 8));
                        // Fill remaining bytes with zeros for consistency
                        paymentId = new Guid(bytes);
                        System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Converted long {longId} to Guid {paymentId}");
                    }
                    else if (fieldType == typeof(string))
                    {
                        var strId = rdr.GetString(0);
                        paymentId = Guid.Parse(strId);
                    }
                    else
                    {
                        // Try to get as object and convert
                        var obj = rdr.GetValue(0);
                        System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: payment_id as object: {obj?.GetType().FullName} = {obj}");
                        if (obj is Guid guid)
                        {
                            paymentId = guid;
                        }
                        else if (obj is long l)
                        {
                            var bytes = new byte[16];
                            var longBytes = BitConverter.GetBytes(l);
                            Array.Copy(longBytes, 0, bytes, 0, Math.Min(longBytes.Length, 8));
                            paymentId = new Guid(bytes);
                        }
                        else
                        {
                            paymentId = Guid.Parse(obj?.ToString() ?? throw new InvalidOperationException($"Cannot convert payment_id from type {fieldType}"));
                        }
                    }
                    
                    list.Add(new PaymentDto(
                        paymentId, 
                        rdr.GetFieldValue<Guid>(1), 
                        rdr.GetFieldValue<Guid>(2), 
                        rdr.GetDecimal(3), 
                        rdr.GetString(4), 
                        rdr.GetDecimal(5),
                        rdr.IsDBNull(6) ? null : rdr.GetString(6), 
                        rdr.GetDecimal(7), 
                        rdr.IsDBNull(8) ? null : rdr.GetString(8),
                        rdr.IsDBNull(9) ? null : rdr.GetFieldValue<object>(9), 
                        rdr.IsDBNull(10) ? null : rdr.GetString(10), 
                        rdr.GetFieldValue<DateTimeOffset>(11)));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Error reading payment row: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Exception type: {ex.GetType().FullName}");
                    System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Inner exception: {ex.InnerException.Message}");
                    }
                    // Skip this row and continue
                    continue;
                }
            }
            System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Successfully loaded {list.Count} payments");
            return list;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"PaymentRepository.GetAllPaymentsAsync: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<RefundDto> InsertRefundAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid paymentId, Guid billingId, Guid sessionId, Guid? cajaSessionId, decimal refundAmount, string? refundReason, string refundMethod, string? externalRef, object? meta, string? createdBy, CancellationToken ct)
    {
        const string ins = @"INSERT INTO pay.refunds(payment_id, billing_id, session_id, caja_session_id, refund_amount, refund_reason, refund_method, external_ref, meta, created_by)
                            VALUES(@pid, @bid, @sid, @cajaSid, @amt, @reason, @method, @ext, @meta, @by)
                            RETURNING refund_id, payment_id, billing_id, session_id, refund_amount, refund_reason, refund_method, external_ref, meta, created_by, created_at";
        await using var cmd = new NpgsqlCommand(ins, conn, tx);
        cmd.Parameters.AddWithValue("@pid", paymentId);
        cmd.Parameters.AddWithValue("@bid", billingId);
        cmd.Parameters.AddWithValue("@sid", sessionId);
        cmd.Parameters.AddWithValue("@cajaSid", (object?)cajaSessionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@amt", refundAmount);
        cmd.Parameters.AddWithValue("@reason", (object?)refundReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@method", refundMethod);
        cmd.Parameters.AddWithValue("@ext", (object?)externalRef ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@meta", meta is null ? DBNull.Value : JsonSerializer.SerializeToElement(meta));
        cmd.Parameters.AddWithValue("@by", (object?)createdBy ?? DBNull.Value);
        
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct))
            throw new InvalidOperationException("Failed to insert refund");
        
        var refundId = rdr.GetFieldValue<Guid>(0);
        
        // Add caja transaction for refund
        if (cajaSessionId.HasValue)
        {
            const string cajaTransSql = @"
                INSERT INTO caja.caja_transactions (caja_session_id, transaction_id, transaction_type, amount, timestamp)
                VALUES (@cajaSid, @transId, 'refund', @amt, now())";
            await using var cajaCmd = new NpgsqlCommand(cajaTransSql, conn, tx);
            cajaCmd.Parameters.AddWithValue("@cajaSid", cajaSessionId.Value);
            cajaCmd.Parameters.AddWithValue("@transId", refundId);
            cajaCmd.Parameters.AddWithValue("@amt", refundAmount);
            await cajaCmd.ExecuteNonQueryAsync(ct);
        }
        
        return new RefundDto(
            refundId, // refund_id
            rdr.GetFieldValue<Guid>(1), // payment_id
            rdr.GetFieldValue<Guid>(2), // billing_id
            rdr.GetFieldValue<Guid>(3), // session_id
            rdr.GetDecimal(4), // refund_amount
            rdr.IsDBNull(5) ? null : rdr.GetString(5), // refund_reason
            rdr.GetString(6), // refund_method
            rdr.IsDBNull(7) ? null : rdr.GetString(7), // external_ref
            rdr.IsDBNull(8) ? null : rdr.GetFieldValue<object>(8), // meta
            rdr.IsDBNull(9) ? null : rdr.GetString(9), // created_by
            rdr.GetFieldValue<DateTimeOffset>(10) // created_at
        );
    }

    public async Task<IReadOnlyList<RefundDto>> GetRefundsByPaymentIdAsync(Guid paymentId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT refund_id, payment_id, billing_id, session_id, refund_amount, refund_reason, refund_method, external_ref, meta, created_by, created_at
                            FROM pay.refunds WHERE payment_id = @pid ORDER BY created_at DESC";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", paymentId);
        var list = new List<RefundDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new RefundDto(
                rdr.GetFieldValue<Guid>(0),
                rdr.GetFieldValue<Guid>(1),
                rdr.GetFieldValue<Guid>(2),
                rdr.GetFieldValue<Guid>(3),
                rdr.GetDecimal(4),
                rdr.IsDBNull(5) ? null : rdr.GetString(5),
                rdr.GetString(6),
                rdr.IsDBNull(7) ? null : rdr.GetString(7),
                rdr.IsDBNull(8) ? null : rdr.GetFieldValue<object>(8),
                rdr.IsDBNull(9) ? null : rdr.GetString(9),
                rdr.GetFieldValue<DateTimeOffset>(10)
            ));
        }
        return list;
    }

    public async Task<IReadOnlyList<RefundDto>> GetRefundsByBillingIdAsync(Guid billingId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT refund_id, payment_id, billing_id, session_id, refund_amount, refund_reason, refund_method, external_ref, meta, created_by, created_at
                            FROM pay.refunds WHERE billing_id = @bid ORDER BY created_at DESC";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bid", billingId);
        var list = new List<RefundDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new RefundDto(
                rdr.GetFieldValue<Guid>(0),
                rdr.GetFieldValue<Guid>(1),
                rdr.GetFieldValue<Guid>(2),
                rdr.GetFieldValue<Guid>(3),
                rdr.GetDecimal(4),
                rdr.IsDBNull(5) ? null : rdr.GetString(5),
                rdr.GetString(6),
                rdr.IsDBNull(7) ? null : rdr.GetString(7),
                rdr.IsDBNull(8) ? null : rdr.GetFieldValue<object>(8),
                rdr.IsDBNull(9) ? null : rdr.GetString(9),
                rdr.GetFieldValue<DateTimeOffset>(10)
            ));
        }
        return list;
    }

    public async Task<decimal> GetTotalRefundedAmountAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid paymentId, CancellationToken ct)
    {
        const string sql = "SELECT COALESCE(SUM(refund_amount), 0) FROM pay.refunds WHERE payment_id = @pid";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@pid", paymentId);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DBNull or null ? 0m : Convert.ToDecimal(result);
    }

    public async Task<decimal> GetTotalRefundedAmountByBillingIdAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid billingId, CancellationToken ct)
    {
        const string sql = "SELECT COALESCE(SUM(refund_amount), 0) FROM pay.refunds WHERE billing_id = @bid";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@bid", billingId);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DBNull or null ? 0m : Convert.ToDecimal(result);
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT payment_id, session_id, billing_id, amount_paid, payment_method, discount_amount, discount_reason, tip_amount, external_ref, meta, created_by, created_at
                           FROM pay.payments WHERE payment_id = @pid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", paymentId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        
        return new PaymentDto(
            rdr.GetFieldValue<Guid>(0),
            rdr.GetFieldValue<Guid>(1),
            rdr.GetFieldValue<Guid>(2),
            rdr.GetDecimal(3),
            rdr.GetString(4),
            rdr.GetDecimal(5),
            rdr.IsDBNull(6) ? null : rdr.GetString(6),
            rdr.GetDecimal(7),
            rdr.IsDBNull(8) ? null : rdr.GetString(8),
            rdr.IsDBNull(9) ? null : rdr.GetFieldValue<object>(9),
            rdr.IsDBNull(10) ? null : rdr.GetString(10),
            rdr.GetFieldValue<DateTimeOffset>(11)
        );
    }
}
