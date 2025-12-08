using Microsoft.AspNetCore.Mvc;
using PaymentApi.Models;
using PaymentApi.Services;
using MagiDesk.Shared.Authorization.Attributes;
using MagiDesk.Shared.DTOs.Users;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/caja")]
public sealed class CajaController : ControllerBase
{
    private readonly ICajaService _service;
    private readonly ILogger<CajaController> _logger;

    public CajaController(ICajaService service, ILogger<CajaController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("open")]
    [RequiresPermission(Permissions.CAJA_OPEN)]
    public async Task<ActionResult<CajaSessionDto>> OpenCajaAsync([FromBody] OpenCajaRequestDto request, CancellationToken ct)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { error = "USER_ID_REQUIRED", message = "User ID is required." });
            }

            var session = await _service.OpenCajaAsync(request, userId, ct);
            return Created($"/api/caja/{session.Id}", session);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "CAJA_ALREADY_OPEN", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "INVALID_REQUEST", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening caja");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    [HttpPost("close")]
    [RequiresPermission(Permissions.CAJA_CLOSE)]
    public async Task<ActionResult<CajaSessionDto>> CloseCajaAsync([FromBody] CloseCajaRequestDto request, CancellationToken ct)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { error = "USER_ID_REQUIRED", message = "User ID is required." });
            }

            var session = await _service.CloseCajaAsync(request, userId, ct);
            return Ok(session);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "CAJA_CANNOT_CLOSE", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "INVALID_REQUEST", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing caja");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    [HttpGet("active")]
    [RequiresPermission(Permissions.CAJA_VIEW)]
    public async Task<ActionResult<CajaSessionDto>> GetActiveSessionAsync(CancellationToken ct)
    {
        try
        {
            var session = await _service.GetActiveSessionAsync(ct);
            if (session == null)
            {
                return NotFound(new { error = "NO_ACTIVE_SESSION", message = "No active caja session found." });
            }
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active caja session");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [RequiresPermission(Permissions.CAJA_VIEW)]
    public async Task<ActionResult<CajaSessionDto>> GetSessionByIdAsync([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            var session = await _service.GetSessionByIdAsync(id, ct);
            if (session == null)
            {
                return NotFound(new { error = "SESSION_NOT_FOUND", message = $"Caja session {id} not found." });
            }
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting caja session {SessionId}", id);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    [HttpGet("history")]
    [RequiresPermission(Permissions.CAJA_VIEW_HISTORY)]
    public async Task<ActionResult<PagedResult<CajaSessionSummaryDto>>> GetHistoryAsync(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetSessionsHistoryAsync(startDate, endDate, page, pageSize, ct);
            return Ok(new PagedResult<CajaSessionSummaryDto>(result.Items, result.Total));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting caja history");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    [HttpGet("{id}/report")]
    [RequiresPermission(Permissions.CAJA_VIEW)]
    public async Task<ActionResult<CajaReportDto>> GetReportAsync([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            var report = await _service.GetSessionReportAsync(id, ct);
            return Ok(report);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = "SESSION_NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting caja report for session {SessionId}", id);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }
}
