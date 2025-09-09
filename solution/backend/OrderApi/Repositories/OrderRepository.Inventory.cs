using System.Net.Http.Json;

namespace OrderApi.Repositories;

public sealed partial class OrderRepository
{
    private record AvCheckReq(string id, decimal quantity);
    private record AvCheckBySkuReq(string sku, decimal quantity);
    private record AdjustReq(decimal delta, decimal? unitCost, string source, string? sourceRef, string? notes, string? userId);

    public async Task<bool> CheckInventoryAvailabilityAsync(IEnumerable<(Guid InventoryItemId, decimal Quantity)> items, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("InventoryApi");
        var body = items.Select(x => new AvCheckReq(x.InventoryItemId.ToString(), x.Quantity)).ToArray();
        using var res = await client.PostAsJsonAsync("api/inventory/availability/check", body, ct);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct);
        if (payload is null) return false;
        return payload.TryGetValue("ok", out var okObj) && okObj is bool ok && ok;
    }

    public async Task DeductInventoryAsync(long orderId, IEnumerable<(Guid InventoryItemId, decimal Quantity, decimal? UnitCost)> items, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("InventoryApi");
        foreach (var it in items)
        {
            var req = new AdjustReq(-Math.Abs(it.Quantity), it.UnitCost, "customer_order", orderId.ToString(), null, null);
            using var res = await client.PostAsJsonAsync($"api/inventory/items/{it.InventoryItemId:D}/adjust", req, ct);
            res.EnsureSuccessStatusCode();
        }
    }

    public async Task<bool> CheckInventoryAvailabilityBySkuAsync(IEnumerable<(string Sku, decimal Quantity)> items, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("InventoryApi");
        var body = items.Select(x => new AvCheckBySkuReq(x.Sku, x.Quantity)).ToArray();
        using var res = await client.PostAsJsonAsync("api/inventory/availability/check-by-sku", body, ct);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct);
        if (payload is null) return false;
        return payload.TryGetValue("ok", out var okObj) && okObj is bool ok && ok;
    }

    public async Task DeductInventoryBySkuAsync(long orderId, IEnumerable<(string Sku, decimal Quantity, decimal? UnitCost)> items, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("InventoryApi");
        foreach (var it in items)
        {
            var req = new AdjustReq(-Math.Abs(it.Quantity), it.UnitCost, "customer_order", orderId.ToString(), null, null);
            using var res = await client.PostAsJsonAsync("api/inventory/items/adjust-by-sku", new { sku = it.Sku, req.delta, req.unitCost, req.source, req.sourceRef, req.notes, req.userId }, ct);
            res.EnsureSuccessStatusCode();
        }
    }
}

