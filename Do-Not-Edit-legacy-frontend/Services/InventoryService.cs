using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

public interface IInventoryService
{
    Task<IEnumerable<ItemDto>> GetItemsAsync(string? vendorId = null, string? categoryId = null, bool? isMenuAvailable = null, string? search = null, CancellationToken ct = default);
    Task<ItemDto?> GetItemAsync(string id, CancellationToken ct = default);
    Task<ItemDto> CreateItemAsync(ItemDto item, CancellationToken ct = default);
    Task<bool> UpdateItemAsync(string id, ItemDto item, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(string id, CancellationToken ct = default);
    Task<bool> AdjustStockAsync(string itemId, decimal delta, decimal? unitCost = null, string? source = null, string? sourceRef = null, string? notes = null, string? userId = null, CancellationToken ct = default);
    Task<IEnumerable<LowStockItemDto>> GetLowStockItemsAsync(decimal? threshold = null, CancellationToken ct = default);
    Task<StockValueDto> GetStockValueAsync(CancellationToken ct = default);
    Task<IEnumerable<InventoryTransactionServiceDto>> GetRecentTransactionsAsync(int limit = 50, CancellationToken ct = default);
}

public class InventoryService : IInventoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(HttpClient httpClient, ILogger<InventoryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ItemDto>> GetItemsAsync(string? vendorId = null, string? categoryId = null, bool? isMenuAvailable = null, string? search = null, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(vendorId)) queryParams.Add($"vendorId={Uri.EscapeDataString(vendorId)}");
            if (!string.IsNullOrEmpty(categoryId)) queryParams.Add($"categoryId={Uri.EscapeDataString(categoryId)}");
            if (isMenuAvailable.HasValue) queryParams.Add($"isMenuAvailable={isMenuAvailable.Value}");
            if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"api/items{queryString}", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<IEnumerable<ItemDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get items");
            return new List<ItemDto>();
        }
    }

    public async Task<ItemDto?> GetItemAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/items/{Uri.EscapeDataString(id)}", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ItemDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get item {ItemId}", id);
            return null;
        }
    }

    public async Task<ItemDto> CreateItemAsync(ItemDto item, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(item);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/items", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ItemDto>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create item");
            throw;
        }
    }

    public async Task<bool> UpdateItemAsync(string id, ItemDto item, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(item);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/items/{Uri.EscapeDataString(id)}", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update item {ItemId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteItemAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/items/{Uri.EscapeDataString(id)}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete item {ItemId}", id);
            return false;
        }
    }

    public async Task<bool> AdjustStockAsync(string itemId, decimal delta, decimal? unitCost = null, string? source = null, string? sourceRef = null, string? notes = null, string? userId = null, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                delta,
                unitCost,
                source,
                sourceRef,
                notes,
                userId
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"api/inventory/items/{Uri.EscapeDataString(itemId)}/adjust", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adjust stock for item {ItemId}", itemId);
            return false;
        }
    }

    public async Task<IEnumerable<LowStockItemDto>> GetLowStockItemsAsync(decimal? threshold = null, CancellationToken ct = default)
    {
        try
        {
            var queryString = threshold.HasValue ? $"?threshold={threshold.Value}" : "";
            var response = await _httpClient.GetAsync($"api/reports/low-stock{queryString}", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<IEnumerable<LowStockItemDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<LowStockItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get low stock items");
            return new List<LowStockItemDto>();
        }
    }

    public async Task<StockValueDto> GetStockValueAsync(CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual stock value calculation endpoint
            // For now, return sample data
            return new StockValueDto
            {
                TotalValue = 125000.50m,
                ValueChange = 5.2m,
                ItemCount = 150
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stock value");
            return new StockValueDto { TotalValue = 0, ValueChange = 0, ItemCount = 0 };
        }
    }

    public async Task<IEnumerable<InventoryTransactionServiceDto>> GetRecentTransactionsAsync(int limit = 50, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual transactions endpoint
            // For now, return sample data
            return new List<InventoryTransactionServiceDto>
            {
                new InventoryTransactionServiceDto { Id = "1", ItemName = "Coffee Beans", Type = "Sale", Quantity = -2, Timestamp = DateTime.Now.AddHours(-1) },
                new InventoryTransactionServiceDto { Id = "2", ItemName = "Burger Patties", Type = "Restock", Quantity = 50, Timestamp = DateTime.Now.AddHours(-2) },
                new InventoryTransactionServiceDto { Id = "3", ItemName = "Cooking Oil", Type = "Sale", Quantity = -1, Timestamp = DateTime.Now.AddHours(-3) }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent transactions");
            return new List<InventoryTransactionServiceDto>();
        }
    }
}

// DTOs for inventory service
public class LowStockItemDto
{
    public string ItemId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal? ReorderThreshold { get; set; }
}

public class StockValueDto
{
    public decimal TotalValue { get; set; }
    public decimal ValueChange { get; set; }
    public int ItemCount { get; set; }
}

public class InventoryTransactionServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Sale, Restock, Adjustment, etc.
    public decimal Quantity { get; set; }
    public DateTime Timestamp { get; set; }
}
