using Microsoft.AspNetCore.Mvc;
using OrderApi.Models;
using OrderApi.Services;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/orders/{orderId:long}/logs")]
public class OrderLogsController : ControllerBase
{
    private readonly IOrderService _service;

    public OrderLogsController(IOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderLogDto>>> ListAsync([FromRoute] long orderId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        var result = await _service.ListLogsAsync(orderId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("recalculate")]
    public async Task<IActionResult> RecalculateAsync([FromRoute] long orderId, CancellationToken ct)
    {
        await _service.RecalculateTotalsAsync(orderId, ct);
        return NoContent();
    }
}
