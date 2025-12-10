using Microsoft.AspNetCore.Mvc;
using MenuApi.Models;
using MenuApi.Repositories;

namespace MenuApi.Controllers;

[ApiController]
[Route("api/menu/history")]
public class HistoryController : ControllerBase
{
    private readonly IMenuRepository _repo;

    public HistoryController(IMenuRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<HistoryDto>>> ListAsync([FromQuery] HistoryQueryDto query, CancellationToken ct)
    {
        var (items, total) = await _repo.ListHistoryAsync(query, ct);
        return Ok(new PagedResult<HistoryDto>(items, total));
    }
}
