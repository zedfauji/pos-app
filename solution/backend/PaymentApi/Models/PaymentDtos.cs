namespace PaymentApi.Models;

public sealed record RegisterPaymentLineDto(decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta);
public sealed record RegisterPaymentRequestDto(string SessionId, string BillingId, decimal? TotalDue, IReadOnlyList<RegisterPaymentLineDto> Lines, string? ServerId);

public sealed record PaymentDto(long PaymentId, string SessionId, string BillingId, decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta, string? CreatedBy, DateTimeOffset CreatedAt);
public sealed record BillLedgerDto(string BillingId, string SessionId, decimal TotalDue, decimal TotalDiscount, decimal TotalPaid, decimal TotalTip, string Status);

public sealed record PaymentLogDto(long LogId, string BillingId, string SessionId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
