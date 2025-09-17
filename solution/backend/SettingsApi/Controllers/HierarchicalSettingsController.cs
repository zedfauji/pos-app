using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Settings;
using SettingsApi.Services;

namespace SettingsApi.Controllers;

[ApiController]
[Route("api/v2/settings")]
public class HierarchicalSettingsController : ControllerBase
{
    private readonly IHierarchicalSettingsService _settingsService;
    private readonly ILogger<HierarchicalSettingsController> _logger;

    public HierarchicalSettingsController(
        IHierarchicalSettingsService settingsService,
        ILogger<HierarchicalSettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all hierarchical settings for a host
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HierarchicalSettings>> GetSettings(
        [FromQuery] string? host = null, 
        CancellationToken ct = default)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync(host, ct);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hierarchical settings for host {Host}", host);
            return StatusCode(500, new { error = "Failed to retrieve settings", details = ex.Message });
        }
    }

    /// <summary>
    /// Get settings for a specific category
    /// </summary>
    [HttpGet("{category}")]
    public async Task<ActionResult> GetSettingsCategory(
        string category,
        [FromQuery] string? host = null,
        CancellationToken ct = default)
    {
        try
        {
            object? result = category.ToLowerInvariant() switch
            {
                "general" => await _settingsService.GetSettingsCategoryAsync<GeneralSettings>(category, host, ct),
                "pos" => await _settingsService.GetSettingsCategoryAsync<PosSettings>(category, host, ct),
                "inventory" => await _settingsService.GetSettingsCategoryAsync<InventorySettings>(category, host, ct),
                "customers" => await _settingsService.GetSettingsCategoryAsync<CustomerSettings>(category, host, ct),
                "payments" => await _settingsService.GetSettingsCategoryAsync<PaymentSettings>(category, host, ct),
                "printers" => await _settingsService.GetSettingsCategoryAsync<PrinterSettings>(category, host, ct),
                "notifications" => await _settingsService.GetSettingsCategoryAsync<NotificationSettings>(category, host, ct),
                "security" => await _settingsService.GetSettingsCategoryAsync<SecuritySettings>(category, host, ct),
                "integrations" => await _settingsService.GetSettingsCategoryAsync<IntegrationSettings>(category, host, ct),
                "system" => await _settingsService.GetSettingsCategoryAsync<SystemSettings>(category, host, ct),
                _ => null
            };

            if (result == null)
            {
                return NotFound(new { error = $"Settings category '{category}' not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings category {Category} for host {Host}", category, host);
            return StatusCode(500, new { error = "Failed to retrieve settings category", details = ex.Message });
        }
    }

    /// <summary>
    /// Save all hierarchical settings
    /// </summary>
    [HttpPut]
    public async Task<ActionResult> SaveSettings(
        [FromBody] HierarchicalSettings settings,
        [FromQuery] string? host = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("SaveSettings called with host: {Host}", host ?? "null");
            
            // Check if settings is null
            if (settings == null)
            {
                _logger.LogWarning("Settings object is null in request body");
                return BadRequest(new { error = "Settings object is required" });
            }

            _logger.LogInformation("Settings object received successfully");

            // Validate settings
            var isValid = await _settingsService.ValidateSettingsAsync(settings, ct);
            if (!isValid)
            {
                _logger.LogWarning("Settings validation failed");
                return BadRequest(new { error = "Settings validation failed" });
            }

            _logger.LogInformation("Settings validation passed");

            var success = await _settingsService.SaveSettingsAsync(settings, host, ct);
            if (!success)
            {
                _logger.LogError("Failed to save settings to database");
                return StatusCode(500, new { error = "Failed to save settings" });
            }

            _logger.LogInformation("Settings saved successfully");
            return Ok(new { message = "Settings saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save hierarchical settings for host {Host}", host);
            return StatusCode(500, new { error = "Failed to save settings", details = ex.Message });
        }
    }

    /// <summary>
    /// Save settings for a specific category
    /// </summary>
    [HttpPut("{category}")]
    public async Task<ActionResult> SaveSettingsCategory(
        string category,
        [FromBody] object settings,
        [FromQuery] string? host = null,
        CancellationToken ct = default)
    {
        try
        {
            var success = category.ToLowerInvariant() switch
            {
                "general" => await SaveCategoryTyped<GeneralSettings>(category, settings, host, ct),
                "pos" => await SaveCategoryTyped<PosSettings>(category, settings, host, ct),
                "inventory" => await SaveCategoryTyped<InventorySettings>(category, settings, host, ct),
                "customers" => await SaveCategoryTyped<CustomerSettings>(category, settings, host, ct),
                "payments" => await SaveCategoryTyped<PaymentSettings>(category, settings, host, ct),
                "printers" => await SaveCategoryTyped<PrinterSettings>(category, settings, host, ct),
                "notifications" => await SaveCategoryTyped<NotificationSettings>(category, settings, host, ct),
                "security" => await SaveCategoryTyped<SecuritySettings>(category, settings, host, ct),
                "integrations" => await SaveCategoryTyped<IntegrationSettings>(category, settings, host, ct),
                "system" => await SaveCategoryTyped<SystemSettings>(category, settings, host, ct),
                _ => false
            };

            if (!success)
            {
                return BadRequest(new { error = $"Invalid category '{category}' or failed to save" });
            }

            return Ok(new { message = $"Settings category '{category}' saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings category {Category} for host {Host}", category, host);
            return StatusCode(500, new { error = "Failed to save settings category", details = ex.Message });
        }
    }

    /// <summary>
    /// Get settings metadata for UI generation
    /// </summary>
    [HttpGet("metadata")]
    public async Task<ActionResult<List<SettingMetadata>>> GetSettingsMetadata(CancellationToken ct = default)
    {
        try
        {
            var metadata = await _settingsService.GetSettingsMetadataAsync(ct);
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings metadata");
            return StatusCode(500, new { error = "Failed to retrieve settings metadata", details = ex.Message });
        }
    }

    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    [HttpPost("reset")]
    public async Task<ActionResult> ResetToDefaults(
        [FromQuery] string? host = null,
        CancellationToken ct = default)
    {
        try
        {
            var success = await _settingsService.ResetToDefaultsAsync(host, ct);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to reset settings to defaults" });
            }

            return Ok(new { message = "Settings reset to defaults successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings to defaults for host {Host}", host);
            return StatusCode(500, new { error = "Failed to reset settings", details = ex.Message });
        }
    }

    /// <summary>
    /// Get settings audit log
    /// </summary>
    [HttpGet("audit")]
    public async Task<ActionResult<List<SettingsAuditEntry>>> GetAuditLog(
        [FromQuery] string? host = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        try
        {
            var auditLog = await _settingsService.GetAuditLogAsync(host, limit, ct);
            return Ok(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings audit log for host {Host}", host);
            return StatusCode(500, new { error = "Failed to retrieve audit log", details = ex.Message });
        }
    }

    /// <summary>
    /// Test connections for various settings
    /// </summary>
    [HttpPost("test-connections")]
    public async Task<ActionResult<Dictionary<string, bool>>> TestConnections(
        [FromBody] HierarchicalSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            var results = await _settingsService.TestConnectionsAsync(settings, ct);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connections");
            return StatusCode(500, new { error = "Failed to test connections", details = ex.Message });
        }
    }

    /// <summary>
    /// Validate settings without saving
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<bool>> ValidateSettings(
        [FromBody] HierarchicalSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            var isValid = await _settingsService.ValidateSettingsAsync(settings, ct);
            return Ok(new { isValid, message = isValid ? "Settings are valid" : "Settings validation failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate settings");
            return StatusCode(500, new { error = "Failed to validate settings", details = ex.Message });
        }
    }

    /// <summary>
    /// Get available printers for printer settings
    /// </summary>
    [HttpGet("printers/available")]
    public async Task<ActionResult<List<PrinterDevice>>> GetAvailablePrinters(CancellationToken ct = default)
    {
        try
        {
            // This would typically query the system for available printers
            var printers = await GetSystemPrintersAsync(ct);
            return Ok(printers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available printers");
            return StatusCode(500, new { error = "Failed to retrieve available printers", details = ex.Message });
        }
    }

    /// <summary>
    /// Test print functionality
    /// </summary>
    [HttpPost("printers/test-print")]
    public async Task<ActionResult> TestPrint(
        [FromBody] TestPrintRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Implement test print functionality
            var success = await PerformTestPrintAsync(request, ct);
            
            if (!success)
            {
                return BadRequest(new { error = "Test print failed" });
            }

            return Ok(new { message = "Test print completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test print failed for printer {PrinterName}", request.PrinterName);
            return StatusCode(500, new { error = "Test print failed", details = ex.Message });
        }
    }

    #region Private Helper Methods

    private async Task<bool> SaveCategoryTyped<T>(string category, object settings, string? host, CancellationToken ct) where T : class
    {
        try
        {
            var typedSettings = System.Text.Json.JsonSerializer.Deserialize<T>(
                System.Text.Json.JsonSerializer.Serialize(settings));
            
            if (typedSettings == null) return false;
            
            return await _settingsService.SaveSettingsCategoryAsync(category, typedSettings, host, ct);
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<PrinterDevice>> GetSystemPrintersAsync(CancellationToken ct)
    {
        // This would typically use System.Drawing.Printing.PrinterSettings.InstalledPrinters
        // or Windows API calls to enumerate available printers
        await Task.Delay(100, ct); // Simulate async operation
        
        return new List<PrinterDevice>
        {
            new() { Name = "Default Printer", Type = "USB", IsOnline = true },
            new() { Name = "Receipt Printer", Type = "Serial", IsOnline = false },
            new() { Name = "Kitchen Printer", Type = "Network", IsOnline = true }
        };
    }

    private async Task<bool> PerformTestPrintAsync(TestPrintRequest request, CancellationToken ct)
    {
        // Implement actual test print functionality
        await Task.Delay(1000, ct); // Simulate print operation
        
        // This would typically:
        // 1. Create a test receipt
        // 2. Send it to the specified printer
        // 3. Return success/failure status
        
        return true; // Placeholder
    }

    #endregion
}

public class TestPrintRequest
{
    public string PrinterName { get; set; } = "";
    public string PaperSize { get; set; } = "80mm";
    public bool IncludeLogo { get; set; } = true;
    public string TestMessage { get; set; } = "Test Print - MagiDesk POS";
}
