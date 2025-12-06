using Microsoft.AspNetCore.Mvc;
using SettingsApi.Services;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;

namespace SettingsApi.Controllers.V2;

/// <summary>
/// Version 2 Settings Controller with RBAC enforcement
/// All endpoints require specific permissions
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISettingsService settings, ILogger<SettingsController> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Get frontend default settings (public - no permission required)
    /// </summary>
    [HttpGet("frontend/defaults")]
    public ActionResult<FrontendSettings> GetFrontendDefaults()
        => Ok(SettingsService.DefaultFrontend());

    /// <summary>
    /// Get frontend settings (requires settings:view permission)
    /// </summary>
    [HttpGet("frontend")]
    [RequiresPermission(MagiDesk.Shared.DTOs.Users.Permissions.SETTINGS_VIEW)]
    public async Task<ActionResult<FrontendSettings>> GetFrontend([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var res = await _settings.GetFrontendAsync(host, ct);
        if (res == null) return NotFound();
        return Ok(res);
    }

    /// <summary>
    /// Update frontend settings (requires settings:update permission)
    /// </summary>
    [HttpPut("frontend")]
    [RequiresPermission(MagiDesk.Shared.DTOs.Users.Permissions.SETTINGS_UPDATE)]
    public async Task<IActionResult> PutFrontend([FromBody] FrontendSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveFrontendAsync(body, host, ct);
        return NoContent();
    }

    /// <summary>
    /// Get backend default settings (public - no permission required)
    /// </summary>
    [HttpGet("backend/defaults")]
    public ActionResult<BackendSettings> GetBackendDefaults()
        => Ok(SettingsService.DefaultBackend());

    /// <summary>
    /// Get backend settings (requires settings:view permission)
    /// </summary>
    [HttpGet("backend")]
    [RequiresPermission(MagiDesk.Shared.DTOs.Users.Permissions.SETTINGS_VIEW)]
    public async Task<ActionResult<BackendSettings>> GetBackend([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var res = await _settings.GetBackendAsync(host, ct);
        if (res == null) return NotFound();
        return Ok(res);
    }

    /// <summary>
    /// Update backend settings (requires settings:update permission)
    /// </summary>
    [HttpPut("backend")]
    [RequiresPermission(MagiDesk.Shared.DTOs.Users.Permissions.SETTINGS_UPDATE)]
    public async Task<IActionResult> PutBackend([FromBody] BackendSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveBackendAsync(body, host, ct);
        return NoContent();
    }

    /// <summary>
    /// Get app default settings (public - no permission required)
    /// </summary>
    [HttpGet("app/defaults")]
    public ActionResult<AppSettings> GetAppDefaults()
        => Ok(SettingsService.DefaultApp());

    /// <summary>
    /// Get app settings (requires settings:view permission)
    /// </summary>
    [HttpGet("app")]
    [RequiresPermission(MagiDesk.Shared.DTOs.Users.Permissions.SETTINGS_VIEW)]
    public async Task<ActionResult<AppSettings>> GetApp([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var result = await _settings.GetAppAsync(host, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Update app settings (requires settings:update permission)
    /// </summary>
    [HttpPut("app")]
    [RequiresPermission(MagiDesk.Shared.DTOs.Users.Permissions.SETTINGS_UPDATE)]
    public async Task<IActionResult> PutApp([FromBody] AppSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveAppAsync(body, host, ct);
        return NoContent();
    }
}

