using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(CancellationToken ct)
    {
        return StatusCode(410, new { error = "Moved. Use OrdersApi." });
    }

    [HttpPost]
    public async Task<IActionResult> Finalize(OrderDto order, CancellationToken ct)
    {
        return StatusCode(410, new { error = "Moved. Use OrdersApi." });
    }

    [HttpPost("cart-draft")]
    public async Task<ActionResult<CartDraftDto>> SaveDraft(CartDraftDto draft, CancellationToken ct)
    {
        return StatusCode(410, new { error = "Moved. Use OrdersApi." });
    }

    [HttpGet("cart-draft/{draftId}")]
    public async Task<IActionResult> GetDraft(string draftId, CancellationToken ct)
    {
        return StatusCode(410, new { error = "Moved. Use OrdersApi." });
    }

    [HttpGet("cart-draft")]
    public async Task<ActionResult<List<CartDraftDto>>> GetDrafts([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        return StatusCode(410, new { error = "Moved. Use OrdersApi." });
    }

    [HttpGet("job-history")]
    public async Task<ActionResult<List<OrdersJobDto>>> Jobs([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        return StatusCode(410, new { error = "Moved. Use OrdersApi." });
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<List<OrderNotificationDto>>> Notifications([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        return StatusCode(410, new { error = "Moved. Use OrdersApi." });
    }
}
