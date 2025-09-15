using Microsoft.AspNetCore.Mvc;
using MenuApi.Models;
using MenuApi.Services;

namespace MenuApi.Controllers;

[ApiController]
[Route("api/menu/versioning")]
public sealed class MenuVersioningController : ControllerBase
{
    private readonly IMenuVersioningService _versioningService;
    private readonly ILogger<MenuVersioningController> _logger;

    public MenuVersioningController(IMenuVersioningService versioningService, ILogger<MenuVersioningController> logger)
    {
        _versioningService = versioningService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new menu version
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MenuVersionHistoryDto>> CreateVersion([FromBody] CreateMenuVersionHistoryDto request, CancellationToken ct)
    {
        try
        {
            var version = await _versioningService.CreateVersionAsync(request, ct);
            return CreatedAtAction(nameof(GetVersion), new { versionId = version.Id }, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu version");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get version history with pagination
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<MenuVersionHistoryResponseDto>> GetVersionHistory([FromQuery] MenuVersionHistoryQueryDto query, CancellationToken ct)
    {
        try
        {
            var history = await _versioningService.GetVersionHistoryAsync(query, ct);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get change summaries for menu items
    /// </summary>
    [HttpGet("summaries")]
    public async Task<ActionResult<IReadOnlyList<MenuChangeSummaryDto>>> GetChangeSummaries([FromQuery] long? menuItemId, CancellationToken ct)
    {
        try
        {
            var summaries = await _versioningService.GetChangeSummariesAsync(menuItemId, ct);
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting change summaries");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific version by ID
    /// </summary>
    [HttpGet("{versionId}")]
    public async Task<ActionResult<MenuVersionHistoryDto>> GetVersion(long versionId, CancellationToken ct)
    {
        try
        {
            var version = await _versioningService.GetVersionAsync(versionId, ct);
            if (version == null)
            {
                return NotFound();
            }
            return Ok(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version {VersionId}", versionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    [HttpPost("rollback")]
    public async Task<ActionResult> RollbackToVersion([FromBody] MenuRollbackDto request, CancellationToken ct)
    {
        try
        {
            var success = await _versioningService.RollbackToVersionAsync(request, ct);
            if (!success)
            {
                return NotFound("Version not found");
            }
            return Ok(new { message = "Successfully rolled back to version", versionId = request.VersionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back to version {VersionId}", request.VersionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get recent changes across all menu items
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<MenuVersionHistoryDto>>> GetRecentChanges([FromQuery] int count = 10, CancellationToken ct = default)
    {
        try
        {
            var recentChanges = await _versioningService.GetRecentChangesAsync(count, ct);
            return Ok(recentChanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent changes");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get version history for a specific menu item
    /// </summary>
    [HttpGet("items/{menuItemId}/history")]
    public async Task<ActionResult<MenuVersionHistoryResponseDto>> GetItemVersionHistory(long menuItemId, [FromQuery] MenuVersionHistoryQueryDto query, CancellationToken ct)
    {
        try
        {
            var itemQuery = query with { MenuItemId = menuItemId };
            var history = await _versioningService.GetVersionHistoryAsync(itemQuery, ct);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history for item {MenuItemId}", menuItemId);
            return StatusCode(500, "Internal server error");
        }
    }
}
