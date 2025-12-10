using System.Net.Http.Json;

namespace OrderApi.Services;

public sealed class KitchenService : IKitchenService
{
    private readonly IHttpClientFactory _httpFactory;

    public KitchenService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<bool> ServeDrinksAsync(long orderId, IReadOnlyList<(string Sku, decimal Quantity)> drinks, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("InventoryApi");
            foreach (var drink in drinks)
            {
                var request = new { sku = drink.Sku, delta = -Math.Abs(drink.Quantity), unitCost = (decimal?)null, source = "kitchen_serve", sourceRef = orderId.ToString(), notes = "Drink served to customer", userId = (string?)null };
                using var res = await client.PostAsJsonAsync("api/inventory/items/adjust-by-sku", request, ct);
                
                if (!res.IsSuccessStatusCode)
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> PrepareFoodAsync(long orderId, IReadOnlyList<(string Sku, decimal Quantity)> rawMaterials, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("InventoryApi");
            foreach (var material in rawMaterials)
            {
                var request = new { sku = material.Sku, delta = -Math.Abs(material.Quantity), unitCost = (decimal?)null, source = "kitchen_prepare", sourceRef = orderId.ToString(), notes = "Raw material used in food preparation", userId = (string?)null };
                using var res = await client.PostAsJsonAsync("api/inventory/items/adjust-by-sku", request, ct);
                
                if (!res.IsSuccessStatusCode)
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}



