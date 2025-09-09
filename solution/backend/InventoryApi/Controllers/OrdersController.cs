using InventoryApi.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryOrdersController : ControllerBase
{
    // Temporarily commented out due to missing IOrdersService
    // private readonly IOrdersService _svc;
    private readonly ILogger<InventoryOrdersController> _logger;

    public InventoryOrdersController(/*IOrdersService svc,*/ ILogger<InventoryOrdersController> logger)
    {
        // _svc = svc;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(CancellationToken ct)
    {
        // var list = await _svc.GetOrdersAsync(ct);
        // return Ok(list);
        _logger.LogWarning("GetOrders endpoint is temporarily disabled due to service migration.");
        return Ok(new List<OrderDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Finalize(OrderDto order, CancellationToken ct)
    {
        try
        {
            // var jobId = await _svc.FinalizeOrderAsync(order, ct);
            // return Accepted(new { success = true, jobId, orderId = order.OrderId });
            _logger.LogWarning("Finalize endpoint is temporarily disabled due to service migration.");
            return Accepted(new { success = false, jobId = "", orderId = order.OrderId, message = "Endpoint temporarily disabled" });
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
            // var saved = await _svc.SaveDraftAsync(draft, ct);
            // return Ok(saved);
            _logger.LogWarning("SaveDraft endpoint is temporarily disabled due to service migration.");
            return Ok(draft);
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
        // var d = await _svc.GetDraftAsync(draftId, ct);
        // if (d is null) return NotFound(new { error = "Draft not found" });
        // return Ok(d);
        _logger.LogWarning("GetDraft endpoint is temporarily disabled due to service migration.");
        return NotFound(new { error = "Endpoint temporarily disabled" });
    }

    [HttpGet("cart-draft")]
    public async Task<ActionResult<List<CartDraftDto>>> GetDrafts([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        // var list = await _svc.GetDraftsAsync(limit, ct);
        // return Ok(list);
        _logger.LogWarning("GetDrafts endpoint is temporarily disabled due to service migration.");
        return Ok(new List<CartDraftDto>());
    }

    [HttpGet("job-history")]
    public async Task<ActionResult<List<OrdersJobDto>>> Jobs([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        // var list = await _svc.GetJobsAsync(limit, ct);
        // return Ok(list);
        _logger.LogWarning("Jobs endpoint is temporarily disabled due to service migration.");
        return Ok(new List<OrdersJobDto>());
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<List<OrderNotificationDto>>> Notifications([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        // var list = await _svc.GetNotificationsAsync(limit, ct);
        // return Ok(list);
        _logger.LogWarning("Notifications endpoint is temporarily disabled due to service migration.");
        return Ok(new List<OrderNotificationDto>());
    }
}
