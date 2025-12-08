using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Configuration;

namespace MagiDesk.Frontend.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public string BaseUrl { get; private set; }

    public ApiClient(IConfiguration config)
    {
        BaseUrl = config["Backend:BaseUrl"] ?? "https://localhost:5001";
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    public void SetBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http.BaseAddress = new Uri(BaseUrl);
    }

    // Vendors
    public Task<VendorDto[]?> GetVendorsAsync(CancellationToken ct = default)
        => _http.GetFromJsonAsync<VendorDto[]>("api/vendors", cancellationToken: ct);

    public Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<VendorDto>($"api/vendors/{id}", cancellationToken: ct);

    public async Task<VendorDto?> CreateVendorAsync(VendorDto dto, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/vendors", dto, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<VendorDto>(cancellationToken: ct);
    }

    public async Task<bool> UpdateVendorAsync(string id, VendorDto dto, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"api/vendors/{id}", dto, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/vendors/{id}", ct);
        return resp.IsSuccessStatusCode;
    }

    // Items (scoped by vendor)
    public Task<ItemDto[]?> GetItemsAsync(string vendorId, CancellationToken ct = default)
        => _http.GetFromJsonAsync<ItemDto[]>($"api/vendors/{vendorId}/items", cancellationToken: ct);

    public Task<ItemDto?> GetItemAsync(string vendorId, string id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<ItemDto>($"api/vendors/{vendorId}/items/{id}", cancellationToken: ct);

    public async Task<ItemDto?> CreateItemAsync(string vendorId, ItemDto dto, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/vendors/{vendorId}/items", dto, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<ItemDto>(cancellationToken: ct);
    }

    public async Task<bool> UpdateItemAsync(string vendorId, string id, ItemDto dto, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"api/vendors/{vendorId}/items/{id}", dto, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteItemAsync(string vendorId, string id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/vendors/{vendorId}/items/{id}", ct);
        return resp.IsSuccessStatusCode;
    }
}
