using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public sealed class MenuAnalyticsService
{
    private readonly HttpClient _http;

    public MenuAnalyticsService(HttpClient http)
    {
        _http = http;
    }

    // Analytics DTOs
    public sealed record MenuAnalyticsDto(
        long MenuItemId,
        string Name,
        string Category,
        decimal TotalRevenue,
        int TotalOrders,
        int TotalQuantitySold,
        decimal AverageOrderValue,
        decimal ProfitMargin,
        decimal CostOfGoodsSold,
        DateTime LastSold,
        DateTime FirstSold,
        bool IsTopSeller,
        int Ranking,
        decimal ConversionRate,
        TimeSpan AveragePreparationTime,
        int CustomerRating,
        int ReviewCount
    );

    public sealed record MenuPerformanceDto(
        DateTime Date,
        string Category,
        decimal Revenue,
        int Orders,
        int QuantitySold,
        decimal AverageOrderValue,
        decimal ProfitMargin,
        int UniqueCustomers,
        decimal CustomerSatisfactionScore
    );

    public sealed record MenuTrendDto(
        string Category,
        string Trend,
        decimal ChangePercentage,
        int PeriodDays,
        decimal RevenueChange,
        int OrderChange,
        string Recommendation
    );

    public sealed record MenuInsightDto(
        string Type,
        string Title,
        string Description,
        string Priority,
        string ActionRequired,
        DateTime GeneratedAt,
        Dictionary<string, object> Data
    );

    public sealed record MenuAnalyticsQueryDto(
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        string? Category = null,
        List<string> Categories = null,
        string? SortBy = null,
        string? SortDirection = null,
        int? TopCount = null,
        bool IncludeInactive = false,
        string? GroupBy = null
    );

    public sealed record MenuPerformanceQueryDto(
        DateTime FromDate,
        DateTime ToDate,
        string? Category = null,
        List<string> Categories = null,
        string? GroupBy = null,
        bool IncludeComparisons = false,
        int? ComparisonPeriods = null
    );

    public sealed record MenuTrendQueryDto(
        DateTime FromDate,
        DateTime ToDate,
        string? Category = null,
        List<string> Categories = null,
        int MinPeriodDays = 7,
        decimal MinChangeThreshold = 0.05m,
        bool IncludeRecommendations = false
    );

    public sealed record MenuInsightQueryDto(
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        List<string> InsightTypes = null,
        List<string> Priorities = null,
        bool IncludeResolved = false,
        int? Limit = null
    );

    // Analytics Methods
    public async Task<IReadOnlyList<MenuAnalyticsDto>> GetMenuAnalyticsAsync(MenuAnalyticsQueryDto query, CancellationToken ct = default)
    {
        var qp = new List<string>();
        if (query.FromDate.HasValue) qp.Add($"fromDate={query.FromDate.Value:yyyy-MM-dd}");
        if (query.ToDate.HasValue) qp.Add($"toDate={query.ToDate.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(query.Category)) qp.Add($"category={Uri.EscapeDataString(query.Category)}");
        if (query.Categories?.Any() == true) qp.Add($"categories={string.Join(",", query.Categories.Select(Uri.EscapeDataString))}");
        if (!string.IsNullOrWhiteSpace(query.SortBy)) qp.Add($"sortBy={Uri.EscapeDataString(query.SortBy)}");
        if (!string.IsNullOrWhiteSpace(query.SortDirection)) qp.Add($"sortDirection={Uri.EscapeDataString(query.SortDirection)}");
        if (query.TopCount.HasValue) qp.Add($"topCount={query.TopCount.Value}");
        if (query.IncludeInactive) qp.Add("includeInactive=true");
        if (!string.IsNullOrWhiteSpace(query.GroupBy)) qp.Add($"groupBy={Uri.EscapeDataString(query.GroupBy)}");

        var url = "api/menu/analytics" + (qp.Count > 0 ? ("?" + string.Join("&", qp)) : string.Empty);
        var result = await _http.GetFromJsonAsync<IReadOnlyList<MenuAnalyticsDto>>(url, ct);
        return result ?? Array.Empty<MenuAnalyticsDto>();
    }

    public async Task<IReadOnlyList<MenuPerformanceDto>> GetMenuPerformanceAsync(MenuPerformanceQueryDto query, CancellationToken ct = default)
    {
        var qp = new List<string>
        {
            $"fromDate={query.FromDate:yyyy-MM-dd}",
            $"toDate={query.ToDate:yyyy-MM-dd}"
        };
        if (!string.IsNullOrWhiteSpace(query.Category)) qp.Add($"category={Uri.EscapeDataString(query.Category)}");
        if (query.Categories?.Any() == true) qp.Add($"categories={string.Join(",", query.Categories.Select(Uri.EscapeDataString))}");
        if (!string.IsNullOrWhiteSpace(query.GroupBy)) qp.Add($"groupBy={Uri.EscapeDataString(query.GroupBy)}");
        if (query.IncludeComparisons) qp.Add("includeComparisons=true");
        if (query.ComparisonPeriods.HasValue) qp.Add($"comparisonPeriods={query.ComparisonPeriods.Value}");

        var url = "api/menu/analytics/performance?" + string.Join("&", qp);
        var result = await _http.GetFromJsonAsync<IReadOnlyList<MenuPerformanceDto>>(url, ct);
        return result ?? Array.Empty<MenuPerformanceDto>();
    }

    public async Task<IReadOnlyList<MenuTrendDto>> GetMenuTrendsAsync(MenuTrendQueryDto query, CancellationToken ct = default)
    {
        var qp = new List<string>
        {
            $"fromDate={query.FromDate:yyyy-MM-dd}",
            $"toDate={query.ToDate:yyyy-MM-dd}",
            $"minPeriodDays={query.MinPeriodDays}",
            $"minChangeThreshold={query.MinChangeThreshold}"
        };
        if (!string.IsNullOrWhiteSpace(query.Category)) qp.Add($"category={Uri.EscapeDataString(query.Category)}");
        if (query.Categories?.Any() == true) qp.Add($"categories={string.Join(",", query.Categories.Select(Uri.EscapeDataString))}");
        if (query.IncludeRecommendations) qp.Add("includeRecommendations=true");

        var url = "api/menu/analytics/trends?" + string.Join("&", qp);
        var result = await _http.GetFromJsonAsync<IReadOnlyList<MenuTrendDto>>(url, ct);
        return result ?? Array.Empty<MenuTrendDto>();
    }

    public async Task<IReadOnlyList<MenuInsightDto>> GetMenuInsightsAsync(MenuInsightQueryDto query, CancellationToken ct = default)
    {
        var qp = new List<string>();
        if (query.FromDate.HasValue) qp.Add($"fromDate={query.FromDate.Value:yyyy-MM-dd}");
        if (query.ToDate.HasValue) qp.Add($"toDate={query.ToDate.Value:yyyy-MM-dd}");
        if (query.InsightTypes?.Any() == true) qp.Add($"insightTypes={string.Join(",", query.InsightTypes.Select(Uri.EscapeDataString))}");
        if (query.Priorities?.Any() == true) qp.Add($"priorities={string.Join(",", query.Priorities.Select(Uri.EscapeDataString))}");
        if (query.IncludeResolved) qp.Add("includeResolved=true");
        if (query.Limit.HasValue) qp.Add($"limit={query.Limit.Value}");

        var url = "api/menu/analytics/insights" + (qp.Count > 0 ? ("?" + string.Join("&", qp)) : string.Empty);
        var result = await _http.GetFromJsonAsync<IReadOnlyList<MenuInsightDto>>(url, ct);
        return result ?? Array.Empty<MenuInsightDto>();
    }

    public async Task<MenuAnalyticsDto> GetItemAnalyticsAsync(long menuItemId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var qp = new List<string>();
        if (fromDate.HasValue) qp.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) qp.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = $"api/menu/analytics/items/{menuItemId}" + (qp.Count > 0 ? ("?" + string.Join("&", qp)) : string.Empty);
        var result = await _http.GetFromJsonAsync<MenuAnalyticsDto>(url, ct);
        return result ?? throw new InvalidOperationException("Failed to get item analytics");
    }

    public async Task<Dictionary<string, object>> GetDashboardDataAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var qp = new List<string>();
        if (fromDate.HasValue) qp.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) qp.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = "api/menu/analytics/dashboard" + (qp.Count > 0 ? ("?" + string.Join("&", qp)) : string.Empty);
        var result = await _http.GetFromJsonAsync<Dictionary<string, object>>(url, ct);
        return result ?? new Dictionary<string, object>();
    }
}
