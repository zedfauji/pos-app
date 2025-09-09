using InventoryApi.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _svc;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrdersService svc, ILogger<OrdersController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(CancellationToken ct)
    {
        var list = await _svc.GetOrdersAsync(ct);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Finalize(OrderDto order, CancellationToken ct)
    {
        try
        {
            var jobId = await _svc.FinalizeOrderAsync(order, ct);
            return Accepted(new { success = true, jobId, orderId = order.OrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Finalize order failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("cart-draft")]
    public async Task<ActionResult<CartDraftDto>> SaveDraft(CartDraftDto draft, CancellationToken ct)
    {
        try
        {
            var saved = await _svc.SaveDraftAsync(draft, ct);
            return Ok(saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save draft failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("cart-draft/{draftId}")]
    public async Task<IActionResult> GetDraft(string draftId, CancellationToken ct)
    {
        var d = await _svc.GetDraftAsync(draftId, ct);
        if (d is null) return NotFound(new { error = "Draft not found" });
        return Ok(d);
    }

    [HttpGet("cart-draft")]
    public async Task<ActionResult<List<CartDraftDto>>> GetDrafts([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var list = await _svc.GetDraftsAsync(limit, ct);
        return Ok(list);
    }

    [HttpGet("job-history")]
    public async Task<ActionResult<List<OrdersJobDto>>> Jobs([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var list = await _svc.GetJobsAsync(limit, ct);
        return Ok(list);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<List<OrderNotificationDto>>> Notifications([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var list = await _svc.GetNotificationsAsync(limit, ct);
        return Ok(list);
    }
}
