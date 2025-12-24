using Refit;
using MagiDesk.Shared.DTOs.Payments;
using MagiDesk.Client.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services;

public interface IPaymentApi
{
    [Post("/api/payments")]
    Task<ApiResponse<PaymentTransactionResult>> RegisterPaymentAsync([Body] RegisterPaymentRequestDto request, CancellationToken ct = default);

    [Get("/api/payments/{billingId}/ledger")]
    Task<BillLedgerDto> GetLedgerAsync(Guid billingId, CancellationToken ct = default);

    [Get("/api/payments/{billingId}")]
    Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(Guid billingId, CancellationToken ct = default);

    [Post("/api/payments/void")]
    Task<ApiResponse<PaymentTransactionResult>> VoidPaymentAsync([Body] VoidPaymentRequestDto req, CancellationToken ct = default);

    [Post("/api/payments/{billingId}/close")]
    Task<ApiResponse<BillLedgerDto>> CloseBillAsync(Guid billingId, [Query] string? serverId = null, CancellationToken ct = default);
}
