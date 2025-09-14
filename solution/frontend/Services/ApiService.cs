using MagiDesk.Shared.DTOs;
using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public class ApiService
{
    private readonly HttpClient _backendHttp;
    private readonly HttpClient _inventoryHttp;
    public Uri BackendBase => _backendHttp.BaseAddress!;
    public Uri InventoryBase => _inventoryHttp.BaseAddress!;

    public ApiService(string backendBaseUrl, string inventoryBaseUrl)
    {
        var backendHandler = new HttpClientHandler();
        backendHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        _backendHttp = new HttpClient(backendHandler)
        {
            BaseAddress = new Uri(backendBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };

        var invHandler = new HttpClientHandler();
        invHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        _inventoryHttp = new HttpClient(invHandler)
        {
            BaseAddress = new Uri(inventoryBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };

        Log.Info($"ApiService Backend: {_backendHttp.BaseAddress} | Inventory: {_inventoryHttp.BaseAddress}");
    }

    // Legacy vendor endpoints removed - using new inventory system

    // Cash Flow - now using InventoryApi
    public async Task<List<CashFlow>> GetCashFlowHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _inventoryHttp.GetAsync("api/cashflow", ct);
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
            var res = await _inventoryHttp.PostAsJsonAsync("api/cashflow", entry, ct);
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
            var res = await _inventoryHttp.PutAsJsonAsync($"api/cashflow/{entry.Id}", entry, ct);
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
            var res = await _backendHttp.GetAsync("api/inventory", ct);
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
            var res = await _backendHttp.PostAsync("api/inventory/syncProductNames/launch", content: new StringContent(""), ct);
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
            var res = await _backendHttp.GetAsync($"api/jobs/{jobId}", ct);
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
            var res = await _backendHttp.GetAsync($"api/jobs?limit={limit}", ct);
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
            var res = await _inventoryHttp.GetAsync("api/orders", ct);
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
            var res = await _inventoryHttp.PostAsJsonAsync("api/orders", order, ct);
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
            var res = await _inventoryHttp.PostAsJsonAsync("api/orders/cart-draft", draft, ct);
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
            var res = await _inventoryHttp.GetAsync($"api/orders/cart-draft/{id}", ct);
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
        var res = await _inventoryHttp.GetAsync($"api/orders/job-history?limit={limit}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<List<OrdersJobDto>>(cancellationToken: ct) ?? new();
    }

    public async Task<List<OrderNotificationDto>> GetOrderNotificationsAsync(int limit = 50, CancellationToken ct = default)
    {
        var res = await _inventoryHttp.GetAsync($"api/orders/notifications?limit={limit}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<List<OrderNotificationDto>>(cancellationToken: ct) ?? new();
    }

    // Legacy vendor items endpoint removed - using new inventory system

    // Drafts
    public async Task<List<CartDraftDto>> GetDraftsAsync(int limit = 20, CancellationToken ct = default)
    {
        try
        {
            var res = await _inventoryHttp.GetAsync($"api/orders/cart-draft?limit={limit}", ct);
            if (!res.IsSuccessStatusCode) return new();
            return await res.Content.ReadFromJsonAsync<List<CartDraftDto>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GetDrafts failed", ex);
            return new();
        }
    }

    // Session Recovery - Get all active sessions from TablesApi
    public async Task<List<MagiDesk.Shared.DTOs.Tables.SessionOverview>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        try
        {
            // Get TablesApi base URL from configuration
            var tablesApiUrl = Environment.GetEnvironmentVariable("TABLESAPI_BASEURL") ?? "https://magidesk-tables-904541739138.northamerica-south1.run.app";
            
            using var tablesHttp = new HttpClient(new HttpClientHandler 
            { 
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
            })
            {
                BaseAddress = new Uri(tablesApiUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(10)
            };

            Log.Info("[TablesApi] GET sessions/active");
            var result = await tablesHttp.GetFromJsonAsync<List<MagiDesk.Shared.DTOs.Tables.SessionOverview>>("sessions/active", ct);
            return result ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("GET active sessions failed", ex);
            return new();
        }
    }
}

