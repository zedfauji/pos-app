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
    public sealed record RegisterPaymentRequestDto(string SessionId, string BillingId, decimal? TotalDue, IReadOnlyList<RegisterPaymentLineDto> Lines, string? ServerId);

    public sealed record PaymentDto(long PaymentId, string SessionId, string BillingId, decimal AmountPaid, string PaymentMethod, decimal DiscountAmount, string? DiscountReason, decimal TipAmount, string? ExternalRef, object? Meta, string? CreatedBy, DateTimeOffset CreatedAt);
    public sealed record BillLedgerDto(string BillingId, string SessionId, decimal TotalDue, decimal TotalDiscount, decimal TotalPaid, decimal TotalTip, string Status);
    public sealed record PaymentLogDto(long LogId, string BillingId, string SessionId, string Action, object? OldValue, object? NewValue, string? ServerId, DateTimeOffset CreatedAt);
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

    // Register payments (supports split via multiple lines)
    public async Task<BillLedgerDto?> RegisterPaymentAsync(RegisterPaymentRequestDto req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/payments", req, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<BillLedgerDto>(cancellationToken: ct);
    }

    // Apply discount (bill-level)
    public async Task<BillLedgerDto?> ApplyDiscountAsync(string billingId, string sessionId, decimal discountAmount, string? reason = null, string? serverId = null, CancellationToken ct = default)
    {
        using var content = JsonContent.Create(discountAmount);
        var url = $"api/payments/{Uri.EscapeDataString(billingId)}/discounts?sessionId={Uri.EscapeDataString(sessionId)}";
        if (!string.IsNullOrWhiteSpace(reason)) url += $"&reason={Uri.EscapeDataString(reason)}";
        if (!string.IsNullOrWhiteSpace(serverId)) url += $"&serverId={Uri.EscapeDataString(serverId)}";
        var res = await _http.PostAsync(url, content, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<BillLedgerDto>(cancellationToken: ct);
    }

    public async Task<BillLedgerDto?> GetLedgerAsync(string billingId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/payments/{Uri.EscapeDataString(billingId)}/ledger", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<BillLedgerDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(string billingId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/payments/{Uri.EscapeDataString(billingId)}", ct);
        if (!res.IsSuccessStatusCode) return Array.Empty<PaymentDto>();
        return await res.Content.ReadFromJsonAsync<IReadOnlyList<PaymentDto>>(cancellationToken: ct) ?? Array.Empty<PaymentDto>();
    }

    public async Task<PagedResult<PaymentLogDto>> ListLogsAsync(string billingId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var res = await _http.GetAsync($"api/payments/{Uri.EscapeDataString(billingId)}/logs?page={page}&pageSize={pageSize}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<PagedResult<PaymentLogDto>>(cancellationToken: ct) ?? new PagedResult<PaymentLogDto>(Array.Empty<PaymentLogDto>(), 0);
    }
}
