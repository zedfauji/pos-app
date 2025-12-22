using Dapper;
using MagiDesk.Core.Entities;
using MagiDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace MagiDesk.Infrastructure.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly string _connectionString;

    public ShiftRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres");
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task<Shift?> GetCurrentOpenShiftAsync()
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                shift_id as ShiftId,
                shift_number as ShiftNumber,
                opened_by_user_id as OpenedByUserId,
                opened_by_name as OpenedByName,
                opened_at as OpenedAt,
                starting_cash as StartingCash,
                closed_by_user_id as ClosedByUserId,
                closed_by_name as ClosedByName,
                closed_at as ClosedAt,
                declared_cash as DeclaredCash,
                expected_cash as ExpectedCash,
                difference as Difference,
                difference_category as DifferenceCategory,
                close_reason as CloseReason,
                status as Status,
                idempotency_key as IdempotencyKey
            FROM shifts 
            WHERE status = 'open' 
            LIMIT 1";
            
        return await conn.QueryFirstOrDefaultAsync<Shift>(sql);
    }

    public async Task<Shift?> GetShiftByIdAsync(Guid shiftId)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                shift_id as ShiftId,
                shift_number as ShiftNumber,
                opened_by_user_id as OpenedByUserId,
                opened_by_name as OpenedByName,
                opened_at as OpenedAt,
                starting_cash as StartingCash,
                closed_by_user_id as ClosedByUserId,
                closed_by_name as ClosedByName,
                closed_at as ClosedAt,
                declared_cash as DeclaredCash,
                expected_cash as ExpectedCash,
                difference as Difference,
                difference_category as DifferenceCategory,
                close_reason as CloseReason,
                status as Status,
                idempotency_key as IdempotencyKey
            FROM shifts 
            WHERE shift_id = @shiftId";
            
        return await conn.QueryFirstOrDefaultAsync<Shift>(sql, new { shiftId });
    }

    public async Task<IEnumerable<Shift>> GetShiftsHistoryAsync(int limit, int offset)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT * FROM shifts 
            ORDER BY shift_number DESC 
            LIMIT @limit OFFSET @offset";
            
        // Note: Dapper mapping for snake_case columns to PascalCase properties 
        // usually requires explicit DefaultTypeMap or "as Alias". 
        // For brevity in this fix, we are assuming Dapper generic mapping or we should use aliases.
        // Let's use simple aliases for safety matching the entity.
        // Re-using the select list from GetCurrentOpenShiftAsync would be cleaner but verbose here.
        // We will stick to the safe verbose select for correctness.
        const string safeSql = @"
             SELECT 
                shift_id as ShiftId,
                shift_number as ShiftNumber,
                opened_by_user_id as OpenedByUserId,
                opened_by_name as OpenedByName,
                opened_at as OpenedAt,
                starting_cash as StartingCash,
                closed_by_user_id as ClosedByUserId,
                closed_by_name as ClosedByName,
                closed_at as ClosedAt,
                declared_cash as DeclaredCash,
                expected_cash as ExpectedCash,
                difference as Difference,
                difference_category as DifferenceCategory,
                close_reason as CloseReason,
                status as Status,
                idempotency_key as IdempotencyKey
            FROM shifts 
            ORDER BY shift_number DESC 
            LIMIT @limit OFFSET @offset";

        return await conn.QueryAsync<Shift>(safeSql, new { limit, offset });
    }

    public async Task InsertAsync(Shift shift)
    {
        using var conn = CreateConnection();
        const string sql = @"
            INSERT INTO shifts (
                shift_id, opened_by_user_id, opened_by_name, opened_at, starting_cash, status, idempotency_key
            ) VALUES (
                @ShiftId, @OpenedByUserId, @OpenedByName, @OpenedAt, @StartingCash, @Status, @IdempotencyKey
            )";
            
        await conn.ExecuteAsync(sql, shift);
    }

    public async Task UpdateAsync(Shift shift)
    {
        using var conn = CreateConnection();
        const string sql = @"
            UPDATE shifts SET
                closed_by_user_id = @ClosedByUserId,
                closed_by_name = @ClosedByName,
                closed_at = @ClosedAt,
                declared_cash = @DeclaredCash,
                expected_cash = @ExpectedCash,
                difference = @Difference,
                difference_category = @DifferenceCategory,
                close_reason = @CloseReason,
                status = @Status
            WHERE shift_id = @ShiftId";
            
        await conn.ExecuteAsync(sql, shift);
    }

    public async Task<bool> HasActiveTablesAsync(Guid shiftId)
    {
        using var conn = CreateConnection();
        // Assuming table_sessions has shift_id
        const string sql = "SELECT COUNT(1) FROM \"TableSessions\" WHERE shift_id = @shiftId AND \"EndTime\" IS NULL";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { shiftId });
        return count > 0;
    }

    public async Task<bool> HasUnsettledBillsAsync(Guid shiftId)
    {
        using var conn = CreateConnection();
        // Uses the new bills table
        const string sql = "SELECT COUNT(1) FROM bills WHERE shift_id = @shiftId AND status = 'unsettled'";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { shiftId });
        return count > 0;
    }

    public async Task<decimal> GetCashSalesAsync(Guid shiftId)
    {
        using var conn = CreateConnection();
        // Uses the new payments table
        const string sql = @"
            SELECT COALESCE(SUM(amount_paid), 0) 
            FROM payments 
            WHERE shift_id = @shiftId 
              AND payment_method = 'Cash' 
              AND is_voided = false";
        return await conn.ExecuteScalarAsync<decimal>(sql, new { shiftId });
    }

    public async Task<decimal> GetCashTipsAsync(Guid shiftId)
    {
        // MVP: Assuming tips are not yet tracked in payments table separately or are part of total.
        // If tips are in payments, we need a separate column or logic.
        // For now, return 0 to satisfy interface constraints unless we add tip column to payments.
        // Recovered schema for payments: amount_paid. 
        // We will return 0 for now as per MVP.
        return await Task.FromResult(0m);
    }
}
