namespace PaymentApi.Models;

public sealed record RegisterPaymentLineDto(decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta);
public sealed record RegisterPaymentRequestDto(Guid SessionId, Guid BillingId, decimal? TotalDue, IReadOnlyList<RegisterPaymentLineDto> Lines, string? ServerId, decimal? AmountTendered = null);

public sealed record PaymentDto(Guid PaymentId, Guid SessionId, Guid BillingId, decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta, string? CreatedBy, DateTimeOffset CreatedAt);
public sealed record BillLedgerDto(Guid BillingId, Guid SessionId, decimal TotalDue, decimal TotalDiscount, decimal TotalPaid, decimal TotalTip, string Status);

/// <summary>
/// Transaction result with contextual information for the UI.
/// Includes change due calculation for cash transactions.
/// </summary>
public sealed record PaymentTransactionResult
{
    public BillLedgerDto Ledger { get; init; } = null!;
    public decimal ChangeDue { get; init; }
    public decimal RemainingBalance { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed record PaymentLogDto(long LogId, Guid BillingId, Guid SessionId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
