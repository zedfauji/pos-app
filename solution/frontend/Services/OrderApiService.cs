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
    public sealed record CreateOrderRequestDto(string SessionId, string? BillingId, string TableId, string ServerId, string? ServerName, IReadOnlyList<CreateOrderItemDto> Items);
    public sealed record UpdateOrderItemDto(long OrderItemId, int? Quantity, IReadOnlyList<ModifierSelectionDto>? Modifiers);

    public sealed record OrderItemDto(long Id, long? MenuItemId, long? ComboId, int Quantity, decimal BasePrice, decimal PriceDelta, decimal LineTotal, decimal Profit);
    public sealed record OrderDto(long Id, string SessionId, string TableId, string Status, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total, decimal ProfitTotal, IReadOnlyList<OrderItemDto> Items);

    public sealed record OrderLogDto(long Id, long OrderId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequestDto req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/orders", req, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<OrderDto?> GetOrderAsync(long orderId, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}", ct);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersBySessionAsync(string sessionId, bool includeHistory = false, CancellationToken ct = default)
    {
        var url = $"api/orders/by-session/{Uri.EscapeDataString(sessionId)}?includeHistory={(includeHistory ? "true" : "false")}";
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
        var res = await _http.PostAsync($"api/orders/{orderId}/close", content: null, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }

    public async Task<PagedResult<OrderLogDto>> ListLogsAsync(long orderId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/orders/{orderId}/logs?page={page}&pageSize={pageSize}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<PagedResult<OrderLogDto>>(cancellationToken: ct) ?? new PagedResult<OrderLogDto>(Array.Empty<OrderLogDto>(), 0);
    }
}
