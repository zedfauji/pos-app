using Npgsql;
using PaymentApi.Models;

namespace PaymentApi.Repositories;

public interface IPaymentRepository
{
    Task ExecuteInTransactionAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> action, CancellationToken ct);

    Task InsertPaymentsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid sessionId, Guid billingId, string? serverId, IReadOnlyList<RegisterPaymentLineDto> lines, CancellationToken ct);

    Task<(decimal totalDue, decimal totalDiscount, decimal totalPaid, decimal totalTip, string status)> UpsertLedgerAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid sessionId, Guid billingId, decimal? providedTotalDue, (decimal addPaid, decimal addDiscount, decimal addTip) deltas, CancellationToken ct);

    Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(Guid billingId, CancellationToken ct);
    Task<BillLedgerDto?> GetLedgerAsync(Guid billingId, CancellationToken ct);

    Task AppendLogAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid billingId, Guid sessionId, string action, object? oldValue, object? newValue, string? serverId, CancellationToken ct);
    Task<(IReadOnlyList<PaymentLogDto> Items, int Total)> ListLogsAsync(Guid billingId, int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(int limit, CancellationToken ct);

    // Refund methods
    Task<RefundDto> InsertRefundAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid paymentId, Guid billingId, Guid sessionId, decimal refundAmount, string? refundReason, string refundMethod, string? externalRef, object? meta, string? createdBy, CancellationToken ct);
    Task<IReadOnlyList<RefundDto>> GetRefundsByPaymentIdAsync(Guid paymentId, CancellationToken ct);
    Task<IReadOnlyList<RefundDto>> GetRefundsByBillingIdAsync(Guid billingId, CancellationToken ct);
    Task<decimal> GetTotalRefundedAmountAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid paymentId, CancellationToken ct);
    Task<decimal> GetTotalRefundedAmountByBillingIdAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid billingId, CancellationToken ct);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct);
}
