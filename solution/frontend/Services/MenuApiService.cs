using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public sealed class MenuApiService
{
    private readonly HttpClient _http;

    public MenuApiService(HttpClient http)
    {
        _http = http;
    }

    // DTOs (client-side lightweight)
    public sealed record AvailabilityUpdateDto(bool IsAvailable);
    public sealed record MenuItemDto(long Id, string Sku, string Name, string? Description, string Category, string? GroupName, decimal SellingPrice, decimal? Price, string? PictureUrl, bool IsDiscountable, bool IsPartOfCombo, bool IsAvailable, int Version);
    public sealed record ModifierOptionDto(long Id, string Name, decimal PriceDelta, bool IsAvailable, int SortOrder);
    public sealed record ModifierDto(long Id, string Name, bool IsRequired, bool AllowMultiple, int? MaxSelections, IReadOnlyList<ModifierOptionDto> Options);
    public sealed record MenuItemDetailsDto(MenuItemDto Item, IReadOnlyList<ModifierDto> Modifiers);
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

    public sealed record ComboItemPriceLineDto(long MenuItemId, int Quantity, decimal UnitPrice);
    public sealed record ComboPriceResponseDto(decimal ComputedPrice, IReadOnlyList<ComboItemPriceLineDto> Items);

    // Query DTOs
    public sealed record ItemsQuery(string? Q, string? Category, string? GroupName, bool? AvailableOnly);
    public sealed record ModifierQuery(string? Q, int Page = 1, int PageSize = 50);

    // Modifier DTOs
    public sealed record CreateModifierOptionDto(string Name, decimal PriceDelta, bool IsAvailable, int SortOrder);
    public sealed record UpdateModifierOptionDto(string Name, decimal PriceDelta, bool IsAvailable, int SortOrder);
    public sealed record CreateModifierDto(string Name, string? Description, bool IsRequired, bool AllowMultiple, int? MinSelections, int? MaxSelections, IReadOnlyList<CreateModifierOptionDto> Options);
    public sealed record UpdateModifierDto(string Name, string? Description, bool IsRequired, bool AllowMultiple, int? MinSelections, int? MaxSelections, IReadOnlyList<UpdateModifierOptionDto> Options);

    // List/Search Items
    public async Task<IReadOnlyList<MenuItemDto>> ListItemsAsync(ItemsQuery query, CancellationToken ct = default)
    {
        var qp = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Q)) qp.Add($"q={Uri.EscapeDataString(query.Q)}");
        if (!string.IsNullOrWhiteSpace(query.Category)) qp.Add($"category={Uri.EscapeDataString(query.Category)}");
        if (!string.IsNullOrWhiteSpace(query.GroupName)) qp.Add($"group={Uri.EscapeDataString(query.GroupName)}");
        if (query.AvailableOnly.HasValue) qp.Add($"availableOnly={(query.AvailableOnly.Value ? "true" : "false")}");
        var url = "api/menu/items" + (qp.Count > 0 ? ("?" + string.Join("&", qp)) : string.Empty);
        var page = await _http.GetFromJsonAsync<PagedResult<MenuItemDto>>(url, ct);
        return page?.Items ?? Array.Empty<MenuItemDto>();
    }

    // Items
    public async Task<MenuItemDto?> GetItemByIdAsync(long id, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/menu/items/{id}", ct);
        if (!res.IsSuccessStatusCode) return null;
        var details = await res.Content.ReadFromJsonAsync<MenuItemDetailsDto>(cancellationToken: ct);
        return details?.Item;
    }

    public async Task<MenuItemDetailsDto?> GetItemBySkuAsync(string sku, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/menu/items/sku/{Uri.EscapeDataString(sku)}", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<MenuItemDetailsDto>(cancellationToken: ct);
    }

    public async Task<bool> CheckDuplicateSkuAsync(string sku, long? excludeId = null, CancellationToken ct = default)
    {
        var url = $"api/menu/items/check-duplicate-sku/{Uri.EscapeDataString(sku)}";
        if (excludeId.HasValue) url += $"?excludeId={excludeId.Value}";
        var res = await _http.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode) return false;
        var doc = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct) ?? new();
        return doc.TryGetValue("duplicate", out var d) && d is bool b && b;
    }

    public async Task<bool> SetItemAvailabilityAsync(long id, bool isAvailable, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync($"api/menu/items/{id}/availability", new AvailabilityUpdateDto(isAvailable), ct);
        return res.IsSuccessStatusCode;
    }

    // Combos
    public async Task<ComboPriceResponseDto?> GetComboComputedPriceAsync(long comboId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/menu/combos/{comboId}/price", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ComboPriceResponseDto>(cancellationToken: ct);
    }

    public async Task<bool> SetComboAvailabilityAsync(long id, bool isAvailable, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync($"api/menu/combos/{id}/availability", new AvailabilityUpdateDto(isAvailable), ct);
        return res.IsSuccessStatusCode;
    }

    // Rollbacks
    public async Task<bool> RollbackItemAsync(long id, int toVersion, CancellationToken ct = default)
    {
        var res = await _http.PostAsync($"api/menu/items/{id}/rollback?toVersion={toVersion}", content: new StringContent(""), ct);
        return res.IsSuccessStatusCode || (int)res.StatusCode == 202;
    }

    public async Task<bool> RollbackComboAsync(long id, int toVersion, CancellationToken ct = default)
    {
        var res = await _http.PostAsync($"api/menu/combos/{id}/rollback?toVersion={toVersion}", content: new StringContent(""), ct);
        return res.IsSuccessStatusCode || (int)res.StatusCode == 202;
    }

    // Modifiers CRUD
    public async Task<IReadOnlyList<ModifierDto>> ListModifiersAsync(ModifierQuery query, CancellationToken ct = default)
    {
        var qp = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Q)) qp.Add($"q={Uri.EscapeDataString(query.Q)}");
        qp.Add($"page={query.Page}");
        qp.Add($"pageSize={query.PageSize}");
        
        var url = "api/menu/modifiers?" + string.Join("&", qp);
        var page = await _http.GetFromJsonAsync<PagedResult<ModifierDto>>(url, ct);
        return page?.Items ?? Array.Empty<ModifierDto>();
    }

    public async Task<ModifierDto?> GetModifierAsync(long id, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/menu/modifiers/{id}", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ModifierDto>(cancellationToken: ct);
    }

    public async Task<ModifierDto?> CreateModifierAsync(CreateModifierDto dto, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/menu/modifiers", dto, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ModifierDto>(cancellationToken: ct);
    }

    public async Task<ModifierDto?> UpdateModifierAsync(long id, UpdateModifierDto dto, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync($"api/menu/modifiers/{id}", dto, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ModifierDto>(cancellationToken: ct);
    }

    public async Task<bool> DeleteModifierAsync(long id, CancellationToken ct = default)
    {
        var res = await _http.DeleteAsync($"api/menu/modifiers/{id}", ct);
        return res.IsSuccessStatusCode;
    }
}
