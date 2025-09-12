namespace MenuApi.Services;

public interface IInventoryService
{
    Task<bool> CheckItemAvailabilityAsync(string sku, decimal quantity, CancellationToken ct);
    Task<decimal> GetItemQuantityAsync(string sku, CancellationToken ct);
}



