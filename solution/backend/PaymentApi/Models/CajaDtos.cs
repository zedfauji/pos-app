namespace PaymentApi.Models;

public sealed record OpenCajaRequestDto(
    decimal OpeningAmount,
    string? Notes = null
);

public sealed record CloseCajaRequestDto(
    decimal ClosingAmount,
    string? Notes = null,
    bool ManagerOverride = false
);

public sealed record CajaSessionDto(
    Guid Id,
    string OpenedByUserId,
    string? ClosedByUserId,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal OpeningAmount,
    decimal? ClosingAmount,
    decimal? SystemCalculatedTotal,
    decimal? Difference,
    string Status,
    string? Notes
);

public sealed record CajaTransactionDto(
    Guid Id,
    Guid CajaSessionId,
    Guid TransactionId,
    string TransactionType,
    decimal Amount,
    DateTimeOffset Timestamp
);

public sealed record CajaReportDto(
    Guid SessionId,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal OpeningAmount,
    decimal? ClosingAmount,
    decimal SystemCalculatedTotal,
    decimal? Difference,
    int TransactionCount,
    decimal SalesTotal,
    decimal RefundsTotal,
    decimal TipsTotal,
    decimal DepositsTotal,
    decimal WithdrawalsTotal,
    string OpenedByUserId,
    string? ClosedByUserId,
    string? Notes
);

public sealed record CajaSessionSummaryDto(
    Guid Id,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal OpeningAmount,
    decimal? ClosingAmount,
    decimal? SystemCalculatedTotal,
    decimal? Difference,
    string Status,
    string OpenedByUserId,
    string? ClosedByUserId
);
