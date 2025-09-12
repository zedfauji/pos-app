namespace OrderApi.Services;

public interface IKitchenService
{
    Task<bool> ServeDrinksAsync(long orderId, IReadOnlyList<(string Sku, decimal Quantity)> drinks, CancellationToken ct);
    Task<bool> PrepareFoodAsync(long orderId, IReadOnlyList<(string Sku, decimal Quantity)> rawMaterials, CancellationToken ct);
}
