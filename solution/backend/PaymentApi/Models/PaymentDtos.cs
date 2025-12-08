namespace PaymentApi.Models;

public sealed record RegisterPaymentLineDto(decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta);
public sealed record RegisterPaymentRequestDto(Guid SessionId, Guid BillingId, decimal? TotalDue, IReadOnlyList<RegisterPaymentLineDto> Lines, string? ServerId);

public sealed record PaymentDto(Guid PaymentId, Guid SessionId, Guid BillingId, decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta, string? CreatedBy, DateTimeOffset CreatedAt);
public sealed record BillLedgerDto(Guid BillingId, Guid SessionId, decimal TotalDue, decimal TotalDiscount, decimal TotalPaid, decimal TotalTip, string Status);

public sealed record PaymentLogDto(long LogId, Guid BillingId, Guid SessionId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

// Refund DTOs
public sealed record ProcessRefundRequestDto(decimal RefundAmount, string? RefundReason, string RefundMethod = "original", string? ExternalRef = null, object? Meta = null);
public sealed record RefundDto(Guid RefundId, Guid PaymentId, Guid BillingId, Guid SessionId, decimal RefundAmount, string? RefundReason, string RefundMethod, string? ExternalRef, object? Meta, string? CreatedBy, DateTimeOffset CreatedAt);