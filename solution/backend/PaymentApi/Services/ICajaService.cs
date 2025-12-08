using PaymentApi.Models;

namespace PaymentApi.Services;

public interface ICajaService
{
    Task<CajaSessionDto> OpenCajaAsync(OpenCajaRequestDto request, string userId, CancellationToken ct = default);
    Task<CajaSessionDto> CloseCajaAsync(CloseCajaRequestDto request, string userId, CancellationToken ct = default);
    Task<CajaSessionDto?> GetActiveSessionAsync(CancellationToken ct = default);
    Task<CajaSessionDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<CajaReportDto> GetSessionReportAsync(Guid sessionId, CancellationToken ct = default);
    Task<(IReadOnlyList<CajaSessionSummaryDto> Items, int Total)> GetSessionsHistoryAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, int page, int pageSize, CancellationToken ct = default);
    Task<bool> ValidateActiveSessionAsync(CancellationToken ct = default);
}
