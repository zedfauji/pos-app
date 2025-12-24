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
        var conn = configuration.GetConnectionString("Postgres");
        
        if (string.IsNullOrWhiteSpace(conn))
        {
            conn = configuration["Postgres:LocalConnectionString"];
        }
        
        if (string.IsNullOrWhiteSpace(conn))
        {
            conn = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(conn))
        {
            throw new InvalidOperationException("Postgres connection string is missing or empty. Checked: ConnectionStrings:Postgres, Postgres:LocalConnectionString, ConnectionStrings:DefaultConnection");
        }
        
        _connectionString = conn;
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
            FROM public.shifts 
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
            FROM public.shifts 
            WHERE shift_id = @shiftId";
            
        return await conn.QueryFirstOrDefaultAsync<Shift>(sql, new { shiftId });
    }

    public async Task<IEnumerable<Shift>> GetShiftsHistoryAsync(int limit, int offset)
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
            FROM public.shifts 
            ORDER BY shift_number DESC 
            LIMIT @limit OFFSET @offset";

        return await conn.QueryAsync<Shift>(sql, new { limit, offset });
    }

    public async Task InsertAsync(Shift shift)
    {
        using var conn = CreateConnection();
        const string sql = @"
            INSERT INTO public.shifts (
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
            UPDATE public.shifts SET
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
        const string sql = "SELECT COUNT(1) FROM \"TableSessions\" WHERE shift_id = @shiftId AND end_time IS NULL";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { shiftId });
        return count > 0;
    }

    public async Task<bool> HasUnsettledBillsAsync(Guid shiftId)
    {
        using var conn = CreateConnection();
        // Uses the new bills table
        const string sql = "SELECT COUNT(1) FROM billing.bills WHERE shift_id = @shiftId AND status = 'AwaitingPayment'";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { shiftId });
        return count > 0;
    }

    public async Task<decimal> GetCashSalesAsync(Guid shiftId)
    {
        using var conn = CreateConnection();
        // Uses the new payments table
        const string sql = @"
            SELECT COALESCE(SUM(amount_paid), 0) 
            FROM pay.payments 
            WHERE shift_id = @shiftId 
              AND method = 'Cash'::pay.payment_method 
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
