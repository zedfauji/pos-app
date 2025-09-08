using MagiDesk.Backend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MagiDesk.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _svc;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService svc, ILogger<InventoryController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItem>>> Get(CancellationToken ct)
    {
        var list = await _svc.GetInventoryAsync(ct);
        return Ok(list);
    }

    [HttpPost("syncProductNames/launch")]
    public async Task<IActionResult> Launch(CancellationToken ct)
    {
        try
        {
            var jobId = await _svc.LaunchSyncProductNamesAsync(ct);
            return Accepted(new { success = true, jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch syncProductNames job");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
