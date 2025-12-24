using System;
using System.Collections.Generic;
using MagiDesk.Shared.Enums;

namespace MagiDesk.Shared.DTOs.Payments;

public sealed record RegisterPaymentLineDto
{
    public decimal AmountPaid { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? DiscountReason { get; init; }
    public decimal TipAmount { get; init; }
    public string? ExternalRef { get; init; }
    public object? Meta { get; init; }
    public string? Notes { get; init; }
}

public sealed record RegisterPaymentRequestDto(
    Guid SessionId, 
    Guid BillingId, 
    decimal? TotalDue, 
    IReadOnlyList<RegisterPaymentLineDto> Lines, 
    string? ServerId, 
    decimal? AmountTendered = null);

public sealed record BillLedgerDto(
    Guid BillingId, 
    Guid SessionId, 
    decimal TotalDue, 
    decimal TotalDiscount, 
    decimal TotalPaid, 
    decimal TotalTip, 
    string Status);

/// <summary>
/// Transaction result with contextual information for the UI.
/// Includes change due calculation for cash transactions.
/// </summary>
public sealed record PaymentTransactionResult
{
    public BillLedgerDto? Ledger { get; init; }
    public decimal ChangeDue { get; init; }
    public decimal RemainingBalance { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed record PaymentDto(
    Guid PaymentId, 
    Guid SessionId, 
    Guid BillingId, 
    decimal AmountPaid, 
    string PaymentMethod, 
    decimal DiscountAmount, 
    string? DiscountReason, 
    decimal TipAmount, 
    string? ExternalRef, 
    object? Meta, 
    string? CreatedBy, 
    DateTimeOffset CreatedAt,
    string? Notes = null
);
