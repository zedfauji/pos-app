using System.Net.Http.Json;

namespace MenuApi.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IHttpClientFactory _httpFactory;

    public InventoryService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<bool> CheckItemAvailabilityAsync(string sku, decimal quantity, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("InventoryApi");
            var request = new { sku, quantity };
            using var res = await client.PostAsJsonAsync("api/inventory/availability/check-by-sku", new[] { request }, ct);
            
            if (!res.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Inventory check failed for SKU {sku} with status {res.StatusCode}");
                return true; // Assume available if inventory service is down
            }
            
            var payload = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct);
            if (payload is null) return true; // Assume available if no response
            
            return payload.TryGetValue("ok", out var okObj) && okObj is bool ok && ok;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Inventory check exception for SKU {sku}: {ex.Message}");
            return true; // Assume available if there's an error
        }
    }

    public async Task<decimal> GetItemQuantityAsync(string sku, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("InventoryApi");
            using var res = await client.GetAsync($"api/inventory/items/sku/{sku}", ct);
            
            if (!res.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Inventory quantity check failed for SKU {sku} with status {res.StatusCode}");
                return 100m; // Return high quantity if service is down
            }
            
            var item = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct);
            if (item is null) return 100m; // Return high quantity if no response
            
            if (item.TryGetValue("quantity", out var qtyObj) && qtyObj is decimal qty)
            {
                return qty;
            }
            
            return 100m; // Return high quantity if quantity not found
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Inventory quantity check exception for SKU {sku}: {ex.Message}");
            return 100m; // Return high quantity if there's an error
        }
    }
}
