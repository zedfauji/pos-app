using System.Net.Http.Json;
using System.Text.Json;

namespace MagiDesk.Frontend.Services;

public sealed class MenuBulkOperationService
{
    private readonly HttpClient _http;

    public MenuBulkOperationService(HttpClient http)
    {
        _http = http;
    }

    // Bulk Operation DTOs
    public sealed record BulkOperationDto(
        string Operation,
        List<long> MenuItemIds,
        Dictionary<string, object> Parameters,
        string User
    );

    public sealed record BulkOperationResultDto(
        string Operation,
        int TotalItems,
        int SuccessCount,
        int FailureCount,
        List<string> Errors,
        List<string> Warnings,
        DateTime CompletedAt,
        TimeSpan Duration
    );

    public sealed record UpdatePricesRequest(
        List<long> MenuItemIds,
        decimal PriceChange,
        string ChangeType
    );

    public sealed record ChangeCategoryRequest(
        List<long> MenuItemIds,
        string NewCategory
    );

    public sealed record ToggleAvailabilityRequest(
        List<long> MenuItemIds,
        bool IsAvailable
    );

    public sealed record UpdateImagesRequest(
        List<long> MenuItemIds,
        string ImageUrl
    );

    public sealed record ApplyDiscountRequest(
        List<long> MenuItemIds,
        decimal DiscountPercentage
    );

    // Bulk Operation Methods
    public async Task<BulkOperationResultDto> ExecuteBulkOperationAsync(BulkOperationDto operation, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/menu/bulk/execute", operation, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BulkOperationResultDto>(ct) 
               ?? throw new InvalidOperationException("Failed to execute bulk operation");
    }

    public async Task<BulkOperationResultDto> UpdatePricesAsync(List<long> menuItemIds, decimal priceChange, string changeType, CancellationToken ct = default)
    {
        var request = new UpdatePricesRequest(menuItemIds, priceChange, changeType);
        var response = await _http.PostAsJsonAsync("api/menu/bulk/update-prices", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BulkOperationResultDto>(ct) 
               ?? throw new InvalidOperationException("Failed to update prices");
    }

    public async Task<BulkOperationResultDto> ChangeCategoryAsync(List<long> menuItemIds, string newCategory, CancellationToken ct = default)
    {
        var request = new ChangeCategoryRequest(menuItemIds, newCategory);
        var response = await _http.PostAsJsonAsync("api/menu/bulk/change-category", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BulkOperationResultDto>(ct) 
               ?? throw new InvalidOperationException("Failed to change category");
    }

    public async Task<BulkOperationResultDto> ToggleAvailabilityAsync(List<long> menuItemIds, bool isAvailable, CancellationToken ct = default)
    {
        var request = new ToggleAvailabilityRequest(menuItemIds, isAvailable);
        var response = await _http.PostAsJsonAsync("api/menu/bulk/toggle-availability", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BulkOperationResultDto>(ct) 
               ?? throw new InvalidOperationException("Failed to toggle availability");
    }

    public async Task<BulkOperationResultDto> UpdateImagesAsync(List<long> menuItemIds, string imageUrl, CancellationToken ct = default)
    {
        var request = new UpdateImagesRequest(menuItemIds, imageUrl);
        var response = await _http.PostAsJsonAsync("api/menu/bulk/update-images", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BulkOperationResultDto>(ct) 
               ?? throw new InvalidOperationException("Failed to update images");
    }

    public async Task<BulkOperationResultDto> ApplyDiscountAsync(List<long> menuItemIds, decimal discountPercentage, CancellationToken ct = default)
    {
        var request = new ApplyDiscountRequest(menuItemIds, discountPercentage);
        var response = await _http.PostAsJsonAsync("api/menu/bulk/apply-discount", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BulkOperationResultDto>(ct) 
               ?? throw new InvalidOperationException("Failed to apply discount");
    }

    // Convenience methods for common operations
    public async Task<BulkOperationResultDto> AddToPricesAsync(List<long> menuItemIds, decimal amount, CancellationToken ct = default)
    {
        return await UpdatePricesAsync(menuItemIds, amount, "Add", ct);
    }

    public async Task<BulkOperationResultDto> SubtractFromPricesAsync(List<long> menuItemIds, decimal amount, CancellationToken ct = default)
    {
        return await UpdatePricesAsync(menuItemIds, amount, "Subtract", ct);
    }

    public async Task<BulkOperationResultDto> MultiplyPricesAsync(List<long> menuItemIds, decimal multiplier, CancellationToken ct = default)
    {
        return await UpdatePricesAsync(menuItemIds, multiplier, "Multiply", ct);
    }

    public async Task<BulkOperationResultDto> SetPricesAsync(List<long> menuItemIds, decimal price, CancellationToken ct = default)
    {
        return await UpdatePricesAsync(menuItemIds, price, "Set", ct);
    }

    public async Task<BulkOperationResultDto> MakeAvailableAsync(List<long> menuItemIds, CancellationToken ct = default)
    {
        return await ToggleAvailabilityAsync(menuItemIds, true, ct);
    }

    public async Task<BulkOperationResultDto> MakeUnavailableAsync(List<long> menuItemIds, CancellationToken ct = default)
    {
        return await ToggleAvailabilityAsync(menuItemIds, false, ct);
    }
}
