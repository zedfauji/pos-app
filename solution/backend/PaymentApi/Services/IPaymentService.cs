using PaymentApi.Models;

namespace PaymentApi.Services;

public interface IPaymentService
{
    Task<BillLedgerDto> RegisterPaymentAsync(RegisterPaymentRequestDto req, CancellationToken ct);
    Task<BillLedgerDto> ApplyDiscountAsync(Guid billingId, Guid sessionId, decimal discountAmount, string? discountReason, string? serverId, CancellationToken ct);
    Task<BillLedgerDto?> GetLedgerAsync(Guid billingId, CancellationToken ct);
    Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(Guid billingId, CancellationToken ct);
    Task<PagedResult<PaymentLogDto>> ListLogsAsync(Guid billingId, int page, int pageSize, CancellationToken ct);

    // Close bill when fully settled (paid or paid+discount >= due)
    Task<BillLedgerDto> CloseBillAsync(Guid billingId, string? serverId, CancellationToken ct);

    // Get all payments across all bills
    Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(int limit, CancellationToken ct);
}
