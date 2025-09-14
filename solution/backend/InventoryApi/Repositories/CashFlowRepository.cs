using Dapper;
using MagiDesk.Shared.DTOs;
using Npgsql;

namespace InventoryApi.Repositories;

public class CashFlowRepository : ICashFlowRepository
{
    private readonly string _connectionString;

    public CashFlowRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<CashFlow>> GetCashFlowHistoryAsync(CancellationToken ct = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = @"
            SELECT id as Id, 
                   employee_name as EmployeeName, 
                   date as Date, 
                   cash_amount as CashAmount, 
                   notes as Notes
            FROM inventory.cash_flow 
            ORDER BY date DESC";

        var result = await connection.QueryAsync<CashFlow>(sql);
        return result.ToList();
    }

    public async Task<CashFlow?> GetCashFlowByIdAsync(string id, CancellationToken ct = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = @"
            SELECT id as Id, 
                   employee_name as EmployeeName, 
                   date as Date, 
                   cash_amount as CashAmount, 
                   notes as Notes
            FROM inventory.cash_flow 
            WHERE id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<CashFlow>(sql, new { Id = id });
        return result;
    }

    public async Task<string> AddCashFlowAsync(CashFlow entry, CancellationToken ct = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var id = string.IsNullOrWhiteSpace(entry.Id) ? Guid.NewGuid().ToString() : entry.Id;

        var sql = @"
            INSERT INTO inventory.cash_flow (id, employee_name, date, cash_amount, notes)
            VALUES (@Id, @EmployeeName, @Date, @CashAmount, @Notes)";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            EmployeeName = entry.EmployeeName,
            Date = entry.Date,
            CashAmount = entry.CashAmount,
            Notes = entry.Notes
        });

        return id;
    }

    public async Task<bool> UpdateCashFlowAsync(CashFlow entry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
            return false;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = @"
            UPDATE inventory.cash_flow 
            SET employee_name = @EmployeeName, 
                date = @Date, 
                cash_amount = @CashAmount, 
                notes = @Notes,
                updated_at = now()
            WHERE id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = entry.Id,
            EmployeeName = entry.EmployeeName,
            Date = entry.Date,
            CashAmount = entry.CashAmount,
            Notes = entry.Notes
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteCashFlowAsync(string id, CancellationToken ct = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = "DELETE FROM inventory.cash_flow WHERE id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

        return rowsAffected > 0;
    }
}
