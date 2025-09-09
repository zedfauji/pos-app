using Npgsql;
using PaymentApi.Models;

namespace PaymentApi.Repositories;

public interface IPaymentRepository
{
    Task ExecuteInTransactionAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> action, CancellationToken ct);

    Task InsertPaymentsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string sessionId, string billingId, string? serverId, IReadOnlyList<RegisterPaymentLineDto> lines, CancellationToken ct);

    Task<(decimal totalDue, decimal totalDiscount, decimal totalPaid, decimal totalTip, string status)> UpsertLedgerAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string sessionId, string billingId, decimal? providedTotalDue, (decimal addPaid, decimal addDiscount, decimal addTip) deltas, CancellationToken ct);

    Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(string billingId, CancellationToken ct);
    Task<BillLedgerDto?> GetLedgerAsync(string billingId, CancellationToken ct);

    Task AppendLogAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string billingId, string sessionId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct);
    Task<(IReadOnlyList<PaymentLogDto> Items, int Total)> ListLogsAsync(string billingId, int page, int pageSize, CancellationToken ct);
}
