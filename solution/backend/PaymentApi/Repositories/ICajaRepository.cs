using PaymentApi.Models;

namespace PaymentApi.Repositories;

public interface ICajaRepository
{
    Task<CajaSessionDto?> GetActiveSessionAsync(CancellationToken ct = default);
    Task<CajaSessionDto> OpenSessionAsync(CajaSessionDto session, CancellationToken ct = default);
    Task<CajaSessionDto> CloseSessionAsync(Guid sessionId, decimal closingAmount, decimal systemTotal, decimal difference, string? closedByUserId, string? notes, CancellationToken ct = default);
    Task<CajaSessionDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<(IReadOnlyList<CajaSessionSummaryDto> Items, int Total)> GetSessionsAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, int page, int pageSize, CancellationToken ct = default);
    Task AddTransactionAsync(Guid cajaSessionId, Guid transactionId, string transactionType, decimal amount, CancellationToken ct = default);
    Task<IReadOnlyList<CajaTransactionDto>> GetTransactionsBySessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<decimal> CalculateSystemTotalAsync(Guid sessionId, CancellationToken ct = default);
}
