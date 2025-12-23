using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MagiDesk.Shared.DTOs;
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
}

public class VendorService : IVendorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VendorService> _logger;

    public VendorService(HttpClient httpClient, ILogger<VendorService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<VendorDto>> GetVendorsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/vendors", ct);
            response.EnsureSuccessStatusCode();
            var vendors = await response.Content.ReadFromJsonAsync<IEnumerable<VendorDto>>(ct);
            return vendors ?? new List<VendorDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendors");
            throw;
        }
    }

    public async Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Vendor ID cannot be null or empty", nameof(id));

            var response = await _httpClient.GetAsync($"api/vendors/{Uri.EscapeDataString(id)}", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VendorDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor {VendorId}", id);
            throw;
        }
    }

    public async Task<VendorDto> CreateVendorAsync(VendorDto vendor, CancellationToken ct = default)
    {
        try
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            if (string.IsNullOrWhiteSpace(vendor.Name))
                throw new ArgumentException("Vendor name is required", nameof(vendor));

            var response = await _httpClient.PostAsJsonAsync("api/vendors", vendor, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VendorDto>(ct) ?? vendor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vendor {VendorName}", vendor?.Name);
            throw;
        }
    }

    public async Task<bool> UpdateVendorAsync(string id, VendorDto vendor, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Vendor ID cannot be null or empty", nameof(id));

            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            var response = await _httpClient.PutAsJsonAsync($"api/vendors/{Uri.EscapeDataString(id)}", vendor, ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vendor {VendorId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Vendor ID cannot be null or empty", nameof(id));

            var response = await _httpClient.DeleteAsync($"api/vendors/{Uri.EscapeDataString(id)}", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vendor {VendorId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ItemDto>> GetVendorItemsAsync(string vendorId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                throw new ArgumentException("Vendor ID cannot be null or empty", nameof(vendorId));

            var response = await _httpClient.GetAsync($"api/vendors/{Uri.EscapeDataString(vendorId)}/items", ct);
            response.EnsureSuccessStatusCode();
            var items = await response.Content.ReadFromJsonAsync<IEnumerable<ItemDto>>(ct);
            return items ?? new List<ItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get items for vendor {VendorId}", vendorId);
            throw;
        }
    }
}

