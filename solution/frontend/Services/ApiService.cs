using MagiDesk.Shared.DTOs;
using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public class ApiService
{
    private readonly HttpClient _http;
    public Uri BaseAddress => _http.BaseAddress!;

    public ApiService(string baseUrl)
    {
        var handler = new HttpClientHandler();
        // Allow dev certificates for local HTTPS during development only
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(20) };
        Log.Info($"ApiService BaseAddress: {_http.BaseAddress}");
    }

    // Vendors
    public async Task<List<VendorDto>> GetVendorsAsync(CancellationToken ct = default)
    {
        try
        {
            Log.Info("GET api/vendors");
            var result = await _http.GetFromJsonAsync<List<VendorDto>>("api/vendors", ct);
            return result ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GET vendors failed", ex);
            return new();
        }
    }

    public async Task<VendorDto?> CreateVendorAsync(VendorDto dto, CancellationToken ct = default)
    {
        try
        {
            Log.Info("POST api/vendors");
            var res = await _http.PostAsJsonAsync("api/vendors", dto, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"POST vendors HTTP {(int)res.StatusCode}: {msg}");
                return null;
            }
            return await res.Content.ReadFromJsonAsync<VendorDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("POST vendors failed", ex);
            return null;
        }
    }

    public async Task<VendorDto?> UpdateVendorAsync(VendorDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Id)) return null;
        try
        {
            Log.Info($"PUT api/vendors/{dto.Id}");
            var res = await _http.PutAsJsonAsync($"api/vendors/{dto.Id}", dto, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"PUT vendor HTTP {(int)res.StatusCode}: {msg}");
                return null;
            }
            // Many backends return no content; attempt to get latest list entry after update
            return dto;
        }
        catch (Exception ex)
        {
            Log.Error("PUT vendor failed", ex);
            return null;
        }
    }

    public async Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default)
    {
        try
        {
            Log.Info($"DELETE api/vendors/{id}");
            var res = await _http.DeleteAsync($"api/vendors/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"DELETE vendor HTTP {(int)res.StatusCode}: {msg}");
            }
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("DELETE vendor failed", ex);
            return false;
        }
    }

    // Items
    public async Task<List<ItemDto>> GetItemsAsync(string vendorId, CancellationToken ct = default)
    {
        try
        {
            Log.Info($"GET api/vendors/{vendorId}/items");
            var result = await _http.GetFromJsonAsync<List<ItemDto>>($"api/vendors/{vendorId}/items", ct);
            return result ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GET items failed", ex);
            return new();
        }
    }

    public async Task<ItemDto?> CreateItemAsync(string vendorId, ItemDto dto, CancellationToken ct = default)
    {
        try
        {
            Log.Info($"POST api/vendors/{vendorId}/items");
            var res = await _http.PostAsJsonAsync($"api/vendors/{vendorId}/items", dto, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"POST item HTTP {(int)res.StatusCode}: {msg}");
                return null;
            }
            return await res.Content.ReadFromJsonAsync<ItemDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("POST item failed", ex);
            return null;
        }
    }

    public async Task<ItemDto?> UpdateItemAsync(string vendorId, ItemDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Id)) return null;
        try
        {
            Log.Info($"PUT api/vendors/{vendorId}/items/{dto.Id}");
            var res = await _http.PutAsJsonAsync($"api/vendors/{vendorId}/items/{dto.Id}", dto, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"PUT item HTTP {(int)res.StatusCode}: {msg}");
                return null;
            }
            return dto;
        }
        catch (Exception ex)
        {
            Log.Error("PUT item failed", ex);
            return null;
        }
    }

    public async Task<bool> DeleteItemAsync(string vendorId, string itemId, CancellationToken ct = default)
    {
        try
        {
            Log.Info($"DELETE api/vendors/{vendorId}/items/{itemId}");
            var res = await _http.DeleteAsync($"api/vendors/{vendorId}/items/{itemId}", ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"DELETE item HTTP {(int)res.StatusCode}: {msg}");
            }
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("DELETE item failed", ex);
            return false;
        }
    }

    // Counts for dashboard
    public async Task<(int vendors, int items)> GetCountsAsync(CancellationToken ct = default)
    {
        try
        {
            var vendors = await GetVendorsAsync(ct);
            int itemCount = 0;
            foreach (var v in vendors)
            {
                if (!string.IsNullOrWhiteSpace(v.Id))
                {
                    var items = await GetItemsAsync(v.Id!, ct);
                    itemCount += items.Count;
                }
            }
            return (vendors.Count, itemCount);
        }
        catch (Exception ex)
        {
            Log.Error("GetCounts failed", ex);
            return (0, 0);
        }
    }

    // Import vendor items
    public async Task<bool> ImportVendorItemsAsync(string vendorId, string format, Stream content, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/vendors/{vendorId}/import?format={format}";
            using var sc = new StreamContent(content);
            var res = await _http.PostAsync(url, sc, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"Import items HTTP {(int)res.StatusCode}: {msg}");
            }
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Error("ImportVendorItems failed", ex);
            return false;
        }
    }

    // Export vendor items
    public async Task<string?> ExportVendorItemsAsync(string vendorId, string format, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/vendors/{vendorId}/export?format={format}";
            Log.Info($"GET {url}");
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"Export items HTTP {(int)res.StatusCode}: {msg}");
                return null;
            }
            return await res.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            Log.Error("ExportVendorItems failed", ex);
            return null;
        }
    }

    // Cash Flow
    public async Task<List<CashFlow>> GetCashFlowHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/cashflow", ct);
            if (!res.IsSuccessStatusCode) return new();
            var list = await res.Content.ReadFromJsonAsync<List<CashFlow>>(cancellationToken: ct) ?? new();
            return list.OrderByDescending(c => c.Date).ToList();
        }
        catch (Exception ex)
        {
            Log.Error("GET cashflow failed", ex);
            return new();
        }
    }

    public async Task AddCashFlowAsync(CashFlow entry, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/cashflow", entry, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"AddCashFlow failed {(int)res.StatusCode}: {msg}");
            }
        }
        catch (Exception ex)
        {
            Log.Error("AddCashFlow exception", ex);
        }
    }

    public async Task UpdateCashFlowAsync(CashFlow entry, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PutAsJsonAsync("api/cashflow", entry, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync(ct);
                Log.Error($"UpdateCashFlow failed {(int)res.StatusCode}: {msg}");
            }
        }
        catch (Exception ex)
        {
            Log.Error("UpdateCashFlow exception", ex);
        }
    }

    // Inventory
    public async Task<List<InventoryItem>> GetInventoryAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/inventory", ct);
            if (!res.IsSuccessStatusCode) return new();
            return await res.Content.ReadFromJsonAsync<List<InventoryItem>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GET inventory failed", ex);
            return new();
        }
    }

    public async Task<(bool success, string jobId)> LaunchSyncProductNamesAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsync("api/inventory/syncProductNames/launch", content: null, ct);
            if (!res.IsSuccessStatusCode) return (false, string.Empty);
            var doc = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct) ?? new();
            var success = doc.TryGetValue("success", out var s) && s is bool b && b;
            var jobId = doc.TryGetValue("jobId", out var j) ? j?.ToString() ?? string.Empty : string.Empty;
            return (success, jobId);
        }
        catch (Exception ex)
        {
            Log.Error("LaunchSyncProductNames failed", ex);
            return (false, string.Empty);
        }
    }

    public async Task<JobStatusDto?> GetJobAsync(string jobId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/jobs/{jobId}", ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<JobStatusDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("GetJob failed", ex);
            return null;
        }
    }

    public async Task<List<JobStatusDto>> GetRecentJobsAsync(int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/jobs?limit={limit}", ct);
            if (!res.IsSuccessStatusCode) return new();
            return await res.Content.ReadFromJsonAsync<List<JobStatusDto>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GetRecentJobs failed", ex);
            return new();
        }
    }

    // Orders
    public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/orders", ct);
            if (!res.IsSuccessStatusCode) return new();
            return await res.Content.ReadFromJsonAsync<List<OrderDto>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GetOrders failed", ex);
            return new();
        }
    }

    public async Task<(bool success, string jobId, string orderId)> FinalizeOrderAsync(OrderDto order, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/orders", order, ct);
            if (!res.IsSuccessStatusCode) return (false, string.Empty, string.Empty);
            // Try to parse JSON first
            Dictionary<string, object?> doc = new();
            try { doc = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct) ?? new(); }
            catch
            {
                // Fall back to raw string logging if not JSON
                var raw = await res.Content.ReadAsStringAsync(ct);
                Log.Info($"FinalizeOrderAsync: Non-JSON response: {raw}");
            }
            var success = doc.TryGetValue("success", out var s) && s is bool b && b;
            var jobId = doc.TryGetValue("jobId", out var j) ? j?.ToString() ?? string.Empty : string.Empty;
            var orderId = doc.TryGetValue("orderId", out var o) ? o?.ToString() ?? string.Empty : string.Empty;
            // If backend accepted (2xx) but payload missing flags, assume success and fall back to provided order id
            if (!success && string.IsNullOrEmpty(orderId))
            {
                success = true;
                orderId = string.IsNullOrWhiteSpace(order.OrderId) ? Guid.NewGuid().ToString("N") : order.OrderId;
            }
            return (success, jobId, orderId);
        }
        catch (Exception ex)
        {
            Log.Error("FinalizeOrder failed", ex);
            return (false, string.Empty, string.Empty);
        }
    }

    public async Task<CartDraftDto?> SaveCartDraftAsync(CartDraftDto draft, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/orders/cart-draft", draft, ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<CartDraftDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("SaveCartDraft failed", ex);
            return null;
        }
    }

    public async Task<CartDraftDto?> GetCartDraftAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/orders/cart-draft/{id}", ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<CartDraftDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Log.Error("GetCartDraft failed", ex);
            return null;
        }
    }

    public async Task<List<OrdersJobDto>> GetOrdersJobsAsync(int limit = 10, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/orders/job-history?limit={limit}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<List<OrdersJobDto>>(cancellationToken: ct) ?? new();
    }

    public async Task<List<OrderNotificationDto>> GetOrderNotificationsAsync(int limit = 50, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/orders/notifications?limit={limit}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<List<OrderNotificationDto>>(cancellationToken: ct) ?? new();
    }

    // Vendors & Items (for Order Builder)
    public async Task<List<ItemDto>> GetItemsByVendorAsync(string vendorId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/items?vendorId={Uri.EscapeDataString(vendorId)}", ct);
            if (!res.IsSuccessStatusCode) return new();
            return await res.Content.ReadFromJsonAsync<List<ItemDto>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GetItemsByVendor failed", ex);
            return new();
        }
    }

    // Drafts
    public async Task<List<CartDraftDto>> GetDraftsAsync(int limit = 20, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"api/orders/cart-draft?limit={limit}", ct);
            if (!res.IsSuccessStatusCode) return new();
            return await res.Content.ReadFromJsonAsync<List<CartDraftDto>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GetDrafts failed", ex);
            return new();
        }
    }
}

