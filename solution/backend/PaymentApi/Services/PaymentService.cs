using PaymentApi.Models;
using PaymentApi.Repositories;

namespace PaymentApi.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;

    public PaymentService(IPaymentRepository repo)
    {
        _repo = repo;
    }

    public async Task<BillLedgerDto> RegisterPaymentAsync(RegisterPaymentRequestDto req, CancellationToken ct)
    {
        if (req.Lines is null || req.Lines.Count == 0)
            throw new InvalidOperationException("NO_PAYMENT_LINES");
        if (req.Lines.Any(l => l.AmountPaid < 0 || l.DiscountAmount < 0 || l.TipAmount < 0))
            throw new InvalidOperationException("INVALID_AMOUNTS");

        BillLedgerDto? ledger = null;
        await _repo.ExecuteInTransactionAsync(async (conn, tx, token) =>
        {
            // Current ledger snapshot for logging
            var old = await _repo.GetLedgerAsync(req.BillingId, token);

            // Insert all payment legs
            await _repo.InsertPaymentsAsync(conn, tx, req.SessionId, req.BillingId, req.ServerId, req.Lines, token);

            // Aggregate deltas
            var addPaid = req.Lines.Sum(l => l.AmountPaid);
            var addDisc = req.Lines.Sum(l => l.DiscountAmount);
            var addTip = req.Lines.Sum(l => l.TipAmount);

            // Upsert ledger, compute status
            var (due, disc, paid, tip, status) = await _repo.UpsertLedgerAsync(conn, tx, req.SessionId, req.BillingId, req.TotalDue, (addPaid, addDisc, addTip), token);
            ledger = new BillLedgerDto(req.BillingId, req.SessionId, due, disc, paid, tip, status);

            // Log
            await _repo.AppendLogAsync(conn, tx, req.BillingId, req.SessionId, "register_payment", old, new { lines = req.Lines, ledger }, req.ServerId, token);
        }, ct);

        return ledger!;
    }

    public async Task<BillLedgerDto> ApplyDiscountAsync(string billingId, string sessionId, decimal discountAmount, string? discountReason, string? serverId, CancellationToken ct)
    {
        if (discountAmount <= 0) throw new InvalidOperationException("INVALID_DISCOUNT");
        BillLedgerDto? ledger = null;
        await _repo.ExecuteInTransactionAsync(async (conn, tx, token) =>
        {
            var old = await _repo.GetLedgerAsync(billingId, token);
            var (due, disc, paid, tip, status) = await _repo.UpsertLedgerAsync(conn, tx, sessionId, billingId, old?.TotalDue, (0m, discountAmount, 0m), token);
            ledger = new BillLedgerDto(billingId, sessionId, due, disc, paid, tip, status);
            await _repo.AppendLogAsync(conn, tx, billingId, sessionId, "apply_discount", old, new { amount = discountAmount, reason = discountReason, ledger }, serverId, token);
        }, ct);
        return ledger!;
    }

    public Task<BillLedgerDto?> GetLedgerAsync(string billingId, CancellationToken ct)
        => _repo.GetLedgerAsync(billingId, ct);

    public Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(string billingId, CancellationToken ct)
        => _repo.ListPaymentsAsync(billingId, ct);

    public async Task<PagedResult<PaymentLogDto>> ListLogsAsync(string billingId, int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await _repo.ListLogsAsync(billingId, page, pageSize, ct);
        return new PagedResult<PaymentLogDto>(items, total);
    }
}
