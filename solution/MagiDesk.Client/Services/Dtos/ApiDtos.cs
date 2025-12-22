using System;
using System.Collections.Generic;

namespace MagiDesk.Client.Services.Dtos;

public class ValidateCredentialsRequest
{
    public string Pin { get; set; } = string.Empty;
}

public class SessionResult
{
    public Guid SessionId { get; set; }
    public string TableLabel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class MoveResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
}

public class OrderItemRequest
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>
/// DTO for bill data from Payment Hub /bills endpoints.
/// </summary>
public class BillDto
{
    public Guid BillId { get; set; }
    public Guid BillingId { get; set; }
    public Guid SessionId { get; set; }
    public Guid TableId { get; set; }
    public string? TableLabel { get; set; }
    public string? ServerName { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public decimal ItemsTotal { get; set; }
    public decimal TimeTotal { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discounts { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public int TimeMinutes { get; set; }
    public string Status { get; set; } = "AwaitingPayment";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Request to settle an unsettled bill.
/// </summary>
public class SettleBillRequest
{
    public string PaymentMethod { get; set; } = "cash";
    public decimal AmountTendered { get; set; }
    public decimal TipAmount { get; set; }
    public decimal DiscountAmount { get; set; }
}
