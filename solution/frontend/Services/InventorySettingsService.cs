using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

public sealed class InventorySettingsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventorySettingsService> _logger;

    public InventorySettingsService(HttpClient httpClient, ILogger<InventorySettingsService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InventorySettings?> GetInventorySettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/inventory/settings");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<InventorySettings>();
            }
            
            _logger.LogWarning("Failed to get inventory settings. Status: {StatusCode}", response.StatusCode);
            return GetDefaultSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory settings");
            return GetDefaultSettings();
        }
    }

    public async Task<bool> SaveInventorySettingsAsync(InventorySettings settings)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("/api/inventory/settings", settings);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving inventory settings");
            return false;
        }
    }

    public async Task<List<VendorInfo>> GetVendorsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/inventory/vendors");
            if (response.IsSuccessStatusCode)
            {
                var vendors = await response.Content.ReadFromJsonAsync<List<VendorInfo>>();
                return vendors ?? new List<VendorInfo>();
            }
            
            _logger.LogWarning("Failed to get vendors. Status: {StatusCode}", response.StatusCode);
            return new List<VendorInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendors");
            return new List<VendorInfo>();
        }
    }

    private static InventorySettings GetDefaultSettings()
    {
        return new InventorySettings
        {
            // General Settings
            DefaultUnit = "each",
            DefaultTaxRate = 16.0m, // 16% default tax rate
            AutoSyncMenuItems = true,
            RequireApproval = false,
            TrackingMethod = "perpetual",

            // Stock Management Settings
            LowStockThreshold = 10,
            CriticalStockThreshold = 5,
            AutoReorder = false,
            ReorderMultiplier = 2,
            AlertFrequency = "daily",

            // Vendor Management Settings
            PaymentTerms = "30",
            DefaultCurrency = "MXN",
            AutoGeneratePO = false,
            RequireVendorApproval = false,
            ContactMethod = "email",

            // Reporting Settings
            ReportFormat = "pdf",
            IncludeImages = true,
            AutoGenerateReports = false,
            ReportRetentionDays = 90,

            // Integration Settings
            SyncFrequency = "15min",
            ApiLogging = false,
            BackupSettings = "weekly"
        };
    }
}

public sealed class InventorySettings
{
    // General Settings
    public string? DefaultUnit { get; set; }
    public decimal? DefaultTaxRate { get; set; }
    public bool? AutoSyncMenuItems { get; set; }
    public bool? RequireApproval { get; set; }
    public string? DefaultVendorId { get; set; }
    public string? TrackingMethod { get; set; }

    // Stock Management Settings
    public int? LowStockThreshold { get; set; }
    public int? CriticalStockThreshold { get; set; }
    public bool? AutoReorder { get; set; }
    public int? ReorderMultiplier { get; set; }
    public string? StockAlertEmail { get; set; }
    public string? AlertFrequency { get; set; }

    // Vendor Management Settings
    public string? PaymentTerms { get; set; }
    public string? DefaultCurrency { get; set; }
    public bool? AutoGeneratePO { get; set; }
    public bool? RequireVendorApproval { get; set; }
    public string? ContactMethod { get; set; }

    // Reporting Settings
    public string? ReportFormat { get; set; }
    public bool? IncludeImages { get; set; }
    public bool? AutoGenerateReports { get; set; }
    public int? ReportRetentionDays { get; set; }
    public string? ReportEmail { get; set; }

    // Integration Settings
    public string? InventoryApiUrl { get; set; }
    public string? SyncFrequency { get; set; }
    public bool? ApiLogging { get; set; }
    public string? BackupSettings { get; set; }
}

public sealed class VendorInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ContactInfo { get; set; }
    public string Status { get; set; } = "active";
}
