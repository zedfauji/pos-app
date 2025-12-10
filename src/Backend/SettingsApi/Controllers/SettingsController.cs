using Microsoft.AspNetCore.Mvc;
using SettingsApi.Services;

namespace SettingsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISettingsService settings, ILogger<SettingsController> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    [HttpGet("frontend/defaults")]
    public ActionResult<FrontendSettings> GetFrontendDefaults()
        => Ok(SettingsService.DefaultFrontend());

    [HttpGet("frontend")]
    public async Task<ActionResult<FrontendSettings>> GetFrontend([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var res = await _settings.GetFrontendAsync(host, ct);
        if (res == null) return NotFound();
        return Ok(res);
    }

    [HttpPut("frontend")]
    public async Task<IActionResult> PutFrontend([FromBody] FrontendSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveFrontendAsync(body, host, ct);
        return NoContent();
    }

    [HttpGet("backend/defaults")]
    public ActionResult<BackendSettings> GetBackendDefaults()
        => Ok(SettingsService.DefaultBackend());

    [HttpGet("backend")]
    public async Task<ActionResult<BackendSettings>> GetBackend([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var res = await _settings.GetBackendAsync(host, ct);
        if (res == null) return NotFound();
        return Ok(res);
    }

    [HttpPut("backend")]
    public async Task<IActionResult> PutBackend([FromBody] BackendSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveBackendAsync(body, host, ct);
        return NoContent();
    }

    // GET api/settings/app?host=HOSTNAME
    [HttpGet("app/defaults")]
    public ActionResult<AppSettings> GetAppDefaults()
        => Ok(SettingsService.DefaultApp());

    [HttpGet("app")]
    public async Task<ActionResult<AppSettings>> GetApp([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var result = await _settings.GetAppAsync(host, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // PUT api/settings/app
    [HttpPut("app")]
    public async Task<IActionResult> PutApp([FromBody] AppSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveAppAsync(body, host, ct);
        return NoContent();
    }

    [HttpGet("audit")]
    public async Task<ActionResult<List<Dictionary<string, object?>>>> GetAudit([FromQuery] string? host = null, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var list = await _settings.GetAuditAsync(host, limit, ct);
        return Ok(list);
    }
}
