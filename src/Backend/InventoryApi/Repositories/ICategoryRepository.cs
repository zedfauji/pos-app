namespace InventoryApi.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<object>> ListAsync(CancellationToken ct);
}

