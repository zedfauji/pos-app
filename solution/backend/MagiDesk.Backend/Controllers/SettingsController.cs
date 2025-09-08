using MagiDesk.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MagiDesk.Backend.Controllers;

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

    // GET api/settings/frontend?host=HOSTNAME
    [HttpGet("frontend")]
    public async Task<ActionResult<FrontendSettings>> GetFrontend([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var result = await _settings.GetFrontendAsync(host, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // PUT api/settings/frontend
    [HttpPut("frontend")]
    public async Task<ActionResult> PutFrontend([FromBody] FrontendSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveFrontendAsync(body, host, ct);
        return NoContent();
    }

    // GET api/settings/backend?host=HOSTNAME
    [HttpGet("backend")]
    public async Task<ActionResult<BackendSettings>> GetBackend([FromQuery] string? host = null, CancellationToken ct = default)
    {
        var result = await _settings.GetBackendAsync(host, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // PUT api/settings/backend
    [HttpPut("backend")]
    public async Task<ActionResult> PutBackend([FromBody] BackendSettings body, [FromQuery] string? host = null, CancellationToken ct = default)
    {
        if (body == null) return BadRequest();
        await _settings.SaveBackendAsync(body, host, ct);
        return NoContent();
    }
}
