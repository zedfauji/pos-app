using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

public interface IVendorService
{
    Task<IEnumerable<VendorDto>> GetVendorsAsync(CancellationToken ct = default);
    Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default);
    Task<VendorDto> CreateVendorAsync(VendorDto vendor, CancellationToken ct = default);
    Task<bool> UpdateVendorAsync(string id, VendorDto vendor, CancellationToken ct = default);
    Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<ItemDto>> GetVendorItemsAsync(string vendorId, CancellationToken ct = default);
    Task<ItemDto> CreateVendorItemAsync(string vendorId, ItemDto item, CancellationToken ct = default);
    Task<bool> UpdateVendorItemAsync(string vendorId, string itemId, ItemDto item, CancellationToken ct = default);
    Task<bool> DeleteVendorItemAsync(string vendorId, string itemId, CancellationToken ct = default);
}

public class VendorService : IVendorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VendorService> _logger;

    public VendorService(HttpClient httpClient, ILogger<VendorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<VendorDto>> GetVendorsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/vendors", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<IEnumerable<VendorDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<VendorDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendors");
            return new List<VendorDto>();
        }
    }

    public async Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/vendors/{Uri.EscapeDataString(id)}", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<VendorDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor {VendorId}", id);
            return null;
        }
    }

    public async Task<VendorDto> CreateVendorAsync(VendorDto vendor, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(vendor);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/vendors", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<VendorDto>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? vendor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vendor");
            throw;
        }
    }

    public async Task<bool> UpdateVendorAsync(string id, VendorDto vendor, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(vendor);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/vendors/{Uri.EscapeDataString(id)}", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vendor {VendorId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/vendors/{Uri.EscapeDataString(id)}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vendor {VendorId}", id);
            return false;
        }
    }

    public async Task<IEnumerable<ItemDto>> GetVendorItemsAsync(string vendorId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/vendors/{Uri.EscapeDataString(vendorId)}/items", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<IEnumerable<ItemDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor items for {VendorId}", vendorId);
            return new List<ItemDto>();
        }
    }

    public async Task<ItemDto> CreateVendorItemAsync(string vendorId, ItemDto item, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(item);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"api/vendors/{Uri.EscapeDataString(vendorId)}/items", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ItemDto>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vendor item for {VendorId}", vendorId);
            throw;
        }
    }

    public async Task<bool> UpdateVendorItemAsync(string vendorId, string itemId, ItemDto item, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(item);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/vendors/{Uri.EscapeDataString(vendorId)}/items/{Uri.EscapeDataString(itemId)}", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vendor item {ItemId} for {VendorId}", itemId, vendorId);
            return false;
        }
    }

    public async Task<bool> DeleteVendorItemAsync(string vendorId, string itemId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/vendors/{Uri.EscapeDataString(vendorId)}/items/{Uri.EscapeDataString(itemId)}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vendor item {ItemId} for {VendorId}", itemId, vendorId);
            return false;
        }
    }
}
