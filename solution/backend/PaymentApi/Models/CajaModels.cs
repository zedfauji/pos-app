namespace PaymentApi.Models;

public sealed class CajaSession
{
    public Guid Id { get; set; }
    public string OpenedByUserId { get; set; } = string.Empty;
    public string? ClosedByUserId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal OpeningAmount { get; set; }
    public decimal? ClosingAmount { get; set; }
    public decimal? SystemCalculatedTotal { get; set; }
    public decimal? Difference { get; set; }
    public string Status { get; set; } = "open";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CajaTransaction
{
    public Guid Id { get; set; }
    public Guid CajaSessionId { get; set; }
    public Guid TransactionId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
