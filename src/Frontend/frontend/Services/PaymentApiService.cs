using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public sealed class PaymentApiService
{
    private readonly HttpClient _http;

    public PaymentApiService(HttpClient http)
    {
        _http = http;
    }

    // DTOs (client-side)
    public sealed record RegisterPaymentLineDto(decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta);
    public sealed record RegisterPaymentRequestDto(Guid SessionId, Guid BillingId, decimal? TotalDue, IReadOnlyList<RegisterPaymentLineDto> Lines, string? ServerId);

    public sealed record PaymentDto(Guid PaymentId, Guid SessionId, Guid BillingId, decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta, string? CreatedBy, DateTimeOffset CreatedAt);
    public sealed record BillLedgerDto(Guid BillingId, Guid SessionId, decimal TotalDue, decimal TotalDiscount, decimal TotalPaid, decimal TotalTip, string Status);
    public sealed record PaymentLogDto(long LogId, Guid BillingId, Guid SessionId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

    // Register payments (supports split via multiple lines)
    public async Task<BillLedgerDto?> RegisterPaymentAsync(RegisterPaymentRequestDto req, CancellationToken ct = default)
    {
        
        // Log the full request JSON
        try
        {
            var requestJson = System.Text.Json.JsonSerializer.Serialize(req, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
        }
        
        try
        {
            var res = await _http.PostAsJsonAsync("api/payments", req, ct);
            
            // Log response headers
            foreach (var header in res.Headers)
            {
            }
            
            if (!res.IsSuccessStatusCode) 
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                
                // Try to parse error response as JSON for better debugging
                try
                {
                    var errorJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent);
                }
                catch
                {
                }
                
                return null;
            }
            
            var responseContent = await res.Content.ReadAsStringAsync(ct);
            
            var ledger = await res.Content.ReadFromJsonAsync<BillLedgerDto>(cancellationToken: ct);
            
            return ledger;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Payment API request failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new InvalidOperationException("Payment API request timed out", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Payment processing failed: {ex.Message}", ex);
        }
    }

    // Apply discount (bill-level)
    public async Task<BillLedgerDto?> ApplyDiscountAsync(Guid billingId, Guid sessionId, decimal discountAmount, string? reason = null, string? serverId = null, CancellationToken ct = default)
    {
        using var content = JsonContent.Create(discountAmount);
        var url = $"api/payments/{billingId}/discounts?sessionId={sessionId}";
        if (!string.IsNullOrWhiteSpace(reason)) url += $"&reason={Uri.EscapeDataString(reason)}";
        if (!string.IsNullOrWhiteSpace(serverId)) url += $"&serverId={Uri.EscapeDataString(serverId)}";
        var res = await _http.PostAsync(url, content, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<BillLedgerDto>(cancellationToken: ct);
    }

    public async Task<BillLedgerDto?> GetLedgerAsync(Guid billingId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/payments/{billingId}/ledger", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<BillLedgerDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(Guid billingId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/payments/{billingId}", ct);
        if (!res.IsSuccessStatusCode) return Array.Empty<PaymentDto>();
        return await res.Content.ReadFromJsonAsync<IReadOnlyList<PaymentDto>>(cancellationToken: ct) ?? Array.Empty<PaymentDto>();
    }

    public async Task<PagedResult<PaymentLogDto>> ListLogsAsync(Guid billingId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/payments/{billingId}/logs?page={page}&pageSize={pageSize}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<PagedResult<PaymentLogDto>>(cancellationToken: ct) ?? new PagedResult<PaymentLogDto>(Array.Empty<PaymentLogDto>(), 0);
    }

    // Get all payments across all bills
    public async Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(int limit = 10000, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/payments/all?limit={limit}", ct);
        
        if (!res.IsSuccessStatusCode) 
        {
            return Array.Empty<PaymentDto>();
        }
        
        var content = await res.Content.ReadAsStringAsync(ct);
        
        var payments = await res.Content.ReadFromJsonAsync<IReadOnlyList<PaymentDto>>(cancellationToken: ct) ?? Array.Empty<PaymentDto>();
        
        return payments;
    }
}
