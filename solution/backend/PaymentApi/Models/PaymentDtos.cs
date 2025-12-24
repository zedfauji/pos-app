namespace PaymentApi.Models;

// DTOs moved to Shared
// RegisterPaymentLineDto
// RegisterPaymentRequestDto
// BillLedgerDto
// PaymentTransactionResult

public sealed record PaymentLogDto(long LogId, Guid BillingId, Guid SessionId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
