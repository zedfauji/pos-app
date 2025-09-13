using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public sealed class OrderApiService
{
    private readonly HttpClient _http;

    public OrderApiService(HttpClient http)
    {
        _http = http;
    }

    // DTOs minimal mirror of backend
    public sealed record ModifierSelectionDto(long ModifierId, long OptionId);
    public sealed record CreateOrderItemDto(long? MenuItemId, long? ComboId, int Quantity, IReadOnlyList<ModifierSelectionDto> Modifiers);
    public sealed record CreateOrderRequestDto(Guid SessionId, Guid? BillingId, string TableId, string ServerId, string? ServerName, IReadOnlyList<CreateOrderItemDto> Items);
    public sealed record UpdateOrderItemDto(long OrderItemId, int? Quantity, IReadOnlyList<ModifierSelectionDto>? Modifiers);

    public sealed record OrderItemDto(long Id, long? MenuItemId, long? ComboId, int Quantity, int DeliveredQuantity, decimal BasePrice, decimal PriceDelta, decimal LineTotal, decimal Profit);
    public sealed record OrderDto(long Id, Guid SessionId, string TableId, string Status, string DeliveryStatus, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total, decimal ProfitTotal, IReadOnlyList<OrderItemDto> Items);

    public sealed record ItemDeliveryDto(long OrderItemId, int DeliveredQuantity);
    public sealed record MarkDeliveredRequestDto(IReadOnlyList<ItemDeliveryDto> ItemDeliveries);

    public sealed record OrderLogDto(long Id, long OrderId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

    // Analytics DTOs
    public sealed record OrderAnalyticsDto(
        int OrdersToday,
        decimal RevenueToday,
        decimal AverageOrderValue,
        decimal CompletionRate,
        int PendingOrders,
        int InProgressOrders,
        int ReadyForDeliveryOrders,
        int CompletedTodayOrders,
        int AveragePrepTimeMinutes,
        string PeakHour,
        decimal EfficiencyScore,
        int TotalOrders,
        decimal TotalRevenue,
        int AverageOrderTimeMinutes,
        decimal CustomerSatisfactionRate,
        decimal ReturnRate,
        int AlertCount,
        string AlertMessage,
        IReadOnlyList<RecentActivityDto> RecentActivities
    );

    public sealed record RecentActivityDto(string Title, string Description, string Timestamp, string ActivityType);
    public sealed record OrderStatusSummaryDto(string Status, int Count, decimal Percentage);
    public sealed record OrderTrendDto(DateTime Date, int OrderCount, decimal Revenue, decimal AverageOrderValue);
    public sealed record OrderAnalyticsRequestDto(DateTime? FromDate, DateTime? ToDate, string ReportType);

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequestDto req, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/orders", req, ct);
            if (!res.IsSuccessStatusCode)
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                System.Diagnostics.Debug.WriteLine($"CreateOrderAsync failed: HTTP {(int)res.StatusCode} - {errorContent}");
                return null;
            }
            return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateOrderAsync exception: {ex.Message}");
            return null;
        }
    }

    public async Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}", ct);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(Guid sessionId, bool includeHistory = false, CancellationToken ct = default)
    {
        var url = $"api/orders/by-session/{sessionId}?includeHistory={(includeHistory ? "true" : "false")}";
        var list = await _http.GetFromJsonAsync<IReadOnlyList<OrderDto>>(url, ct);
        return list ?? Array.Empty<OrderDto>();
    }

    public async Task<OrderDto?> AddItemsAsync(long orderId, IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync($"api/orders/{orderId}/items", items, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<OrderDto?> UpdateItemAsync(long orderId, UpdateOrderItemDto item, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync($"api/orders/{orderId}/items/{item.OrderItemId}", item, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<OrderDto?> DeleteItemAsync(long orderId, long orderItemId, CancellationToken ct = default)
    {
        var res = await _http.DeleteAsync($"api/orders/{orderId}/items/{orderItemId}", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<OrderDto?> CloseOrderAsync(long orderId, CancellationToken ct = default)
    {
        var res = await _http.PostAsync($"api/orders/{orderId}/close", content: new StringContent(""), ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<OrderDto?> MarkItemsDeliveredAsync(long orderId, IReadOnlyList<ItemDeliveryDto> itemDeliveries, CancellationToken ct = default)
    {
        var request = new MarkDeliveredRequestDto(itemDeliveries);
        var res = await _http.PostAsJsonAsync($"api/orders/{orderId}/mark-delivered", request, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<OrderDto?> MarkOrderWaitingAsync(long orderId, CancellationToken ct = default)
    {
        var res = await _http.PostAsync($"api/orders/{orderId}/mark-waiting", content: new StringContent(""), ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<PagedResult<OrderLogDto>> ListLogsAsync(long orderId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/orders/{orderId}/logs?page={page}&pageSize={pageSize}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<PagedResult<OrderLogDto>>(cancellationToken: ct) ?? new PagedResult<OrderLogDto>(Array.Empty<OrderLogDto>(), 0);
    }

    // Analytics methods
    public async Task<OrderAnalyticsDto?> GetOrderAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, string reportType = "daily", CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            queryParams.Add($"reportType={reportType}");
            
            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var res = await _http.GetAsync($"api/orders/analytics{queryString}", ct);
            
            if (!res.IsSuccessStatusCode)
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                System.Diagnostics.Debug.WriteLine($"GetOrderAnalyticsAsync failed: HTTP {(int)res.StatusCode} - {errorContent}");
                return null;
            }
            
            return await res.Content.ReadFromJsonAsync<OrderAnalyticsDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetOrderAnalyticsAsync exception: {ex.Message}");
            return null;
        }
    }

    public async Task<IReadOnlyList<OrderStatusSummaryDto>> GetOrderStatusSummaryAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/orders/analytics/status-summary", ct);
            if (!res.IsSuccessStatusCode) return Array.Empty<OrderStatusSummaryDto>();
            return await res.Content.ReadFromJsonAsync<IReadOnlyList<OrderStatusSummaryDto>>(cancellationToken: ct) ?? Array.Empty<OrderStatusSummaryDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetOrderStatusSummaryAsync exception: {ex.Message}");
            return Array.Empty<OrderStatusSummaryDto>();
        }
    }

    public async Task<IReadOnlyList<OrderTrendDto>> GetOrderTrendsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        try
        {
            var queryString = $"?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
            var res = await _http.GetAsync($"api/orders/analytics/trends{queryString}", ct);
            if (!res.IsSuccessStatusCode) return Array.Empty<OrderTrendDto>();
            return await res.Content.ReadFromJsonAsync<IReadOnlyList<OrderTrendDto>>(cancellationToken: ct) ?? Array.Empty<OrderTrendDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetOrderTrendsAsync exception: {ex.Message}");
            return Array.Empty<OrderTrendDto>();
        }
    }
}
