using Npgsql;
using PaymentApi.Models;

namespace PaymentApi.Repositories;

public sealed class CajaRepository : ICajaRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CajaRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<CajaSessionDto?> GetActiveSessionAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            SELECT id, opened_by_user_id, closed_by_user_id, opened_at, closed_at, 
                   opening_amount, closing_amount, system_calculated_total, difference, 
                   status, notes
            FROM caja.caja_sessions
            WHERE status = 'open'
            LIMIT 1";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        
        if (!await rdr.ReadAsync(ct))
            return null;

        return new CajaSessionDto(
            rdr.GetFieldValue<Guid>(0),
            rdr.GetString(1),
            rdr.IsDBNull(2) ? null : rdr.GetString(2),
            rdr.GetFieldValue<DateTimeOffset>(3),
            rdr.IsDBNull(4) ? null : rdr.GetFieldValue<DateTimeOffset>(4),
            rdr.GetDecimal(5),
            rdr.IsDBNull(6) ? null : rdr.GetDecimal(6),
            rdr.IsDBNull(7) ? null : rdr.GetDecimal(7),
            rdr.IsDBNull(8) ? null : rdr.GetDecimal(8),
            rdr.GetString(9),
            rdr.IsDBNull(10) ? null : rdr.GetString(10)
        );
    }

    public async Task<CajaSessionDto> OpenSessionAsync(CajaSessionDto session, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            INSERT INTO caja.caja_sessions (id, opened_by_user_id, opened_at, opening_amount, status, notes)
            VALUES (@id, @openedBy, @openedAt, @openingAmount, 'open', @notes)
            RETURNING id, opened_by_user_id, closed_by_user_id, opened_at, closed_at, 
                      opening_amount, closing_amount, system_calculated_total, difference, 
                      status, notes";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", session.Id);
        cmd.Parameters.AddWithValue("@openedBy", session.OpenedByUserId);
        cmd.Parameters.AddWithValue("@openedAt", session.OpenedAt);
        cmd.Parameters.AddWithValue("@openingAmount", session.OpeningAmount);
        cmd.Parameters.AddWithValue("@notes", (object?)session.Notes ?? DBNull.Value);
        
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct))
            throw new InvalidOperationException("Failed to create caja session");

        return new CajaSessionDto(
            rdr.GetFieldValue<Guid>(0),
            rdr.GetString(1),
            rdr.IsDBNull(2) ? null : rdr.GetString(2),
            rdr.GetFieldValue<DateTimeOffset>(3),
            rdr.IsDBNull(4) ? null : rdr.GetFieldValue<DateTimeOffset>(4),
            rdr.GetDecimal(5),
            rdr.IsDBNull(6) ? null : rdr.GetDecimal(6),
            rdr.IsDBNull(7) ? null : rdr.GetDecimal(7),
            rdr.IsDBNull(8) ? null : rdr.GetDecimal(8),
            rdr.GetString(9),
            rdr.IsDBNull(10) ? null : rdr.GetString(10)
        );
    }

    public async Task<CajaSessionDto> CloseSessionAsync(Guid sessionId, decimal closingAmount, decimal systemTotal, decimal difference, string? closedByUserId, string? notes, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            UPDATE caja.caja_sessions
            SET closed_by_user_id = @closedBy,
                closed_at = now(),
                closing_amount = @closingAmount,
                system_calculated_total = @systemTotal,
                difference = @difference,
                status = 'closed',
                notes = COALESCE(@notes, notes),
                updated_at = now()
            WHERE id = @id
            RETURNING id, opened_by_user_id, closed_by_user_id, opened_at, closed_at, 
                      opening_amount, closing_amount, system_calculated_total, difference, 
                      status, notes";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", sessionId);
        cmd.Parameters.AddWithValue("@closedBy", (object?)closedByUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@closingAmount", closingAmount);
        cmd.Parameters.AddWithValue("@systemTotal", systemTotal);
        cmd.Parameters.AddWithValue("@difference", difference);
        cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
        
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct))
            throw new InvalidOperationException("Failed to close caja session");

        return new CajaSessionDto(
            rdr.GetFieldValue<Guid>(0),
            rdr.GetString(1),
            rdr.IsDBNull(2) ? null : rdr.GetString(2),
            rdr.GetFieldValue<DateTimeOffset>(3),
            rdr.IsDBNull(4) ? null : rdr.GetFieldValue<DateTimeOffset>(4),
            rdr.GetDecimal(5),
            rdr.IsDBNull(6) ? null : rdr.GetDecimal(6),
            rdr.IsDBNull(7) ? null : rdr.GetDecimal(7),
            rdr.IsDBNull(8) ? null : rdr.GetDecimal(8),
            rdr.GetString(9),
            rdr.IsDBNull(10) ? null : rdr.GetString(10)
        );
    }

    public async Task<CajaSessionDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            SELECT id, opened_by_user_id, closed_by_user_id, opened_at, closed_at, 
                   opening_amount, closing_amount, system_calculated_total, difference, 
                   status, notes
            FROM caja.caja_sessions
            WHERE id = @id";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", sessionId);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        
        if (!await rdr.ReadAsync(ct))
            return null;

        return new CajaSessionDto(
            rdr.GetFieldValue<Guid>(0),
            rdr.GetString(1),
            rdr.IsDBNull(2) ? null : rdr.GetString(2),
            rdr.GetFieldValue<DateTimeOffset>(3),
            rdr.IsDBNull(4) ? null : rdr.GetFieldValue<DateTimeOffset>(4),
            rdr.GetDecimal(5),
            rdr.IsDBNull(6) ? null : rdr.GetDecimal(6),
            rdr.IsDBNull(7) ? null : rdr.GetDecimal(7),
            rdr.IsDBNull(8) ? null : rdr.GetDecimal(8),
            rdr.GetString(9),
            rdr.IsDBNull(10) ? null : rdr.GetString(10)
        );
    }

    public async Task<(IReadOnlyList<CajaSessionSummaryDto> Items, int Total)> GetSessionsAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var limit = Math.Clamp(pageSize, 1, 200);
        var offset = (Math.Max(1, page) - 1) * limit;

        var whereClause = "1=1";
        var parameters = new List<NpgsqlParameter>();
        
        if (startDate.HasValue)
        {
            whereClause += " AND opened_at >= @startDate";
            parameters.Add(new NpgsqlParameter("@startDate", startDate.Value));
        }
        
        if (endDate.HasValue)
        {
            whereClause += " AND opened_at <= @endDate";
            parameters.Add(new NpgsqlParameter("@endDate", endDate.Value));
        }

        var sql = $@"
            SELECT id, opened_at, closed_at, opening_amount, closing_amount, 
                   system_calculated_total, difference, status, opened_by_user_id, closed_by_user_id
            FROM caja.caja_sessions
            WHERE {whereClause}
            ORDER BY opened_at DESC
            LIMIT @limit OFFSET @offset";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        
        var items = new List<CajaSessionSummaryDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            items.Add(new CajaSessionSummaryDto(
                rdr.GetFieldValue<Guid>(0),
                rdr.GetFieldValue<DateTimeOffset>(1),
                rdr.IsDBNull(2) ? null : rdr.GetFieldValue<DateTimeOffset>(2),
                rdr.GetDecimal(3),
                rdr.IsDBNull(4) ? null : rdr.GetDecimal(4),
                rdr.IsDBNull(5) ? null : rdr.GetDecimal(5),
                rdr.IsDBNull(6) ? null : rdr.GetDecimal(6),
                rdr.GetString(7),
                rdr.GetString(8),
                rdr.IsDBNull(9) ? null : rdr.GetString(9)
            ));
        }

        var countSql = $"SELECT COUNT(1) FROM caja.caja_sessions WHERE {whereClause}";
        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddRange(parameters);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        return (items, total);
    }

    public async Task AddTransactionAsync(Guid cajaSessionId, Guid transactionId, string transactionType, decimal amount, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            INSERT INTO caja.caja_transactions (caja_session_id, transaction_id, transaction_type, amount, timestamp)
            VALUES (@sessionId, @transactionId, @type, @amount, now())";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@sessionId", cajaSessionId);
        cmd.Parameters.AddWithValue("@transactionId", transactionId);
        cmd.Parameters.AddWithValue("@type", transactionType);
        cmd.Parameters.AddWithValue("@amount", amount);
        
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<CajaTransactionDto>> GetTransactionsBySessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            SELECT id, caja_session_id, transaction_id, transaction_type, amount, timestamp
            FROM caja.caja_transactions
            WHERE caja_session_id = @sessionId
            ORDER BY timestamp";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@sessionId", sessionId);
        
        var items = new List<CajaTransactionDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            items.Add(new CajaTransactionDto(
                rdr.GetFieldValue<Guid>(0),
                rdr.GetFieldValue<Guid>(1),
                rdr.GetFieldValue<Guid>(2),
                rdr.GetString(3),
                rdr.GetDecimal(4),
                rdr.GetFieldValue<DateTimeOffset>(5)
            ));
        }

        return items;
    }

    public async Task<decimal> CalculateSystemTotalAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"
            SELECT COALESCE(SUM(
                CASE 
                    WHEN transaction_type IN ('sale', 'tip', 'deposit') THEN amount
                    WHEN transaction_type IN ('refund', 'withdrawal') THEN -amount
                    ELSE 0
                END
            ), 0) AS system_total
            FROM caja.caja_transactions
            WHERE caja_session_id = @sessionId";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@sessionId", sessionId);
        
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DBNull or null ? 0m : Convert.ToDecimal(result);
    }
}
