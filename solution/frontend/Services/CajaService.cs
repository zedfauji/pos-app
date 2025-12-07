using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public sealed class CajaService
{
    private readonly HttpClient _http;

    public CajaService(HttpClient http)
    {
        _http = http;
    }

    // DTOs (client-side)
    public sealed record OpenCajaRequestDto(decimal OpeningAmount, string? Notes = null);
    public sealed record CloseCajaRequestDto(decimal ClosingAmount, string? Notes = null, bool ManagerOverride = false);
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
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

    public async Task<CajaSessionDto?> OpenCajaAsync(OpenCajaRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/caja/open", request, ct);
            if (!res.IsSuccessStatusCode)
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                System.Diagnostics.Debug.WriteLine($"CajaService: Open caja failed - {res.StatusCode}: {errorContent}");
                return null;
            }
            return await res.Content.ReadFromJsonAsync<CajaSessionDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CajaService: Error opening caja: {ex.Message}");
            throw;
        }
    }

    public async Task<CajaSessionDto?> CloseCajaAsync(CloseCajaRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/caja/close", request, ct);
            if (!res.IsSuccessStatusCode)
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                System.Diagnostics.Debug.WriteLine($"CajaService: Close caja failed - {res.StatusCode}: {errorContent}");
                return null;
            }
            return await res.Content.ReadFromJsonAsync<CajaSessionDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CajaService: Error closing caja: {ex.Message}");
            throw;
        }
    }

    public async Task<CajaSessionDto?> GetActiveSessionAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/caja/active", ct);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }
            return await res.Content.ReadFromJsonAsync<CajaSessionDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CajaService: Error getting active session: {ex.Message}");
            return null;
        }
    }

    public async Task<CajaSessionDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/caja/{sessionId}", ct);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }
            return await res.Content.ReadFromJsonAsync<CajaSessionDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CajaService: Error getting session: {ex.Message}");
            return null;
        }
    }

    public async Task<PagedResult<CajaSessionSummaryDto>?> GetHistoryAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (startDate.HasValue)
                queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("O"))}");
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");

            var url = $"api/caja/history?{string.Join("&", queryParams)}";
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }
            return await res.Content.ReadFromJsonAsync<PagedResult<CajaSessionSummaryDto>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CajaService: Error getting history: {ex.Message}");
            return null;
        }
    }

    public async Task<CajaReportDto?> GetReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/caja/{sessionId}/report", ct);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }
            return await res.Content.ReadFromJsonAsync<CajaReportDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CajaService: Error getting report: {ex.Message}");
            return null;
        }
    }
}
