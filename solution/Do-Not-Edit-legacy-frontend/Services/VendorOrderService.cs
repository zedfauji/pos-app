using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

public interface IVendorOrderService
{
    Task<IEnumerable<VendorOrderDto>> GetVendorOrdersAsync(CancellationToken ct = default);
    Task<IEnumerable<VendorOrderDto>> GetVendorOrdersByVendorIdAsync(string vendorId, CancellationToken ct = default);
    Task<VendorOrderDto?> GetVendorOrderAsync(string id, CancellationToken ct = default);
    Task<VendorOrderDto> CreateVendorOrderAsync(VendorOrderDto order, CancellationToken ct = default);
    Task<bool> UpdateVendorOrderAsync(string id, VendorOrderDto order, CancellationToken ct = default);
    Task<bool> DeleteVendorOrderAsync(string id, CancellationToken ct = default);
    Task<bool> SubmitVendorOrderAsync(string id, CancellationToken ct = default);
    Task<bool> ReceiveVendorOrderAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<VendorOrderDto>> GetPendingOrdersAsync(CancellationToken ct = default);
}

public class VendorOrderService : IVendorOrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VendorOrderService> _logger;

    public VendorOrderService(HttpClient httpClient, ILogger<VendorOrderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<VendorOrderDto>> GetVendorOrdersAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/vendor-orders", ct);
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<IEnumerable<VendorOrderDto>>(ct);
                return orders ?? new List<VendorOrderDto>();
            }
            _logger.LogWarning("Failed to get vendor orders: {StatusCode}", response.StatusCode);
            return new List<VendorOrderDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor orders");
            return new List<VendorOrderDto>();
        }
    }

    public async Task<IEnumerable<VendorOrderDto>> GetVendorOrdersByVendorIdAsync(string vendorId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return new List<VendorOrderDto>();

            var response = await _httpClient.GetAsync($"api/vendor-orders?vendorId={Uri.EscapeDataString(vendorId)}", ct);
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<IEnumerable<VendorOrderDto>>(ct);
                return orders ?? new List<VendorOrderDto>();
            }
            _logger.LogWarning("Failed to get vendor orders for {VendorId}: {StatusCode}", vendorId, response.StatusCode);
            return new List<VendorOrderDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor orders for {VendorId}", vendorId);
            return new List<VendorOrderDto>();
        }
    }

    public async Task<VendorOrderDto?> GetVendorOrderAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var orders = await GetVendorOrdersAsync(ct);
            return orders.FirstOrDefault(o => o.OrderId == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor order {OrderId}", id);
            return null;
        }
    }

    public async Task<VendorOrderDto> CreateVendorOrderAsync(VendorOrderDto order, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual endpoint
            order.OrderId = Guid.NewGuid().ToString();
            order.OrderDate = DateTime.Now;
            order.Status = "Draft";
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vendor order");
            throw;
        }
    }

    public async Task<bool> UpdateVendorOrderAsync(string id, VendorOrderDto order, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual endpoint
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vendor order {OrderId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteVendorOrderAsync(string id, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual endpoint
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vendor order {OrderId}", id);
            return false;
        }
    }

    public async Task<bool> SubmitVendorOrderAsync(string id, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual endpoint
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit vendor order {OrderId}", id);
            return false;
        }
    }

    public async Task<bool> ReceiveVendorOrderAsync(string id, CancellationToken ct = default)
    {
        try
        {
            // TODO: Implement actual endpoint
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive vendor order {OrderId}", id);
            return false;
        }
    }

    public async Task<IEnumerable<VendorOrderDto>> GetPendingOrdersAsync(CancellationToken ct = default)
    {
        try
        {
            var orders = await GetVendorOrdersAsync(ct);
            return orders.Where(o => o.Status == "Submitted" || o.Status == "In Transit");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending vendor orders");
            return new List<VendorOrderDto>();
        }
    }
}
