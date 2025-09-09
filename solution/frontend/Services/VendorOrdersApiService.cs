using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public sealed class VendorOrdersApiService
{
    private readonly HttpClient _http;

    public VendorOrdersApiService(HttpClient http)
    {
        _http = http;
    }

    public sealed record VendorOrderItemDto(long ItemId, string ItemName, int Quantity, decimal Price);
    public sealed record VendorOrderDto(string OrderId, string VendorName, IReadOnlyList<VendorOrderItemDto> Items, decimal TotalAmount, string Status, DateTimeOffset CreatedAt);

    public async Task<IReadOnlyList<VendorOrderDto>> ListOrdersAsync(int limit = 100, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/vendor-orders?limit={limit}";
            var list = await _http.GetFromJsonAsync<IReadOnlyList<VendorOrderDto>>(url, ct);
            return list ?? Array.Empty<VendorOrderDto>();
        }
        catch
        {
            // If endpoint is not available yet, return empty list to keep UI stable
            return Array.Empty<VendorOrderDto>();
        }
    }
}
