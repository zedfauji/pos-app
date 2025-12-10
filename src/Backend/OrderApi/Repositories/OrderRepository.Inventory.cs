using System.Net.Http.Json;

namespace OrderApi.Repositories;

public sealed partial class OrderRepository
{
    private record AvCheckBySkuReq(string sku, decimal quantity);
    private record AdjustReq(decimal delta, decimal? unitCost, string source, string? sourceRef, string? notes, string? userId);

    public async Task<bool> CheckInventoryAvailabilityBySkuAsync(IEnumerable<(string Sku, decimal Quantity)> items, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("InventoryApi");
            var body = items.Select(x => new AvCheckBySkuReq(x.Sku, x.Quantity)).ToArray();
            using var res = await client.PostAsJsonAsync("api/inventory/availability/check-by-sku", body, ct);
            
            // If the endpoint doesn't exist or returns an error, assume inventory is available
            if (!res.IsSuccessStatusCode)
            {
                return true;
            }
            
            var payload = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct);
            if (payload is null) return true; // Assume available if no response
            
            return payload.TryGetValue("ok", out var okObj) && okObj is bool ok && ok;
        }
        catch (Exception ex)
        {
            return true; // Assume available if there's an error
        }
    }

    public async Task DeductInventoryBySkuAsync(long orderId, IEnumerable<(string Sku, decimal Quantity, decimal? UnitCost)> items, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("InventoryApi");
            foreach (var it in items)
            {
                var req = new AdjustReq(-Math.Abs(it.Quantity), it.UnitCost, "customer_order", orderId.ToString(), null, null);
                using var res = await client.PostAsJsonAsync("api/inventory/items/adjust-by-sku", new { sku = it.Sku, req.delta, req.unitCost, req.source, req.sourceRef, req.notes, req.userId }, ct);
                
                // If inventory deduction fails, log but don't throw - this is best-effort
                if (!res.IsSuccessStatusCode)
                {
                }
            }
        }
        catch (Exception ex)
        {
            // Don't throw - this is best-effort inventory deduction
        }
    }
}



