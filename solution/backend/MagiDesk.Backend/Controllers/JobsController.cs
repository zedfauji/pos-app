using MagiDesk.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MagiDesk.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public JobsController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(string jobId, CancellationToken ct)
    {
        var job = await _inventory.GetJobAsync(jobId, ct);
        if (job is null) return NotFound(new { error = "Job not found" });
        return Ok(job);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var list = await _inventory.GetRecentJobsAsync(limit, ct);
        return Ok(list);
    }
}
