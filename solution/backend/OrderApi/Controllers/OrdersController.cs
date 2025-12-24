using Microsoft.AspNetCore.Mvc;
using OrderApi.Models;
using OrderApi.Models;
using OrderApi.Services;
using OrderApi.Filters;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly MagiDesk.Core.Interfaces.IShiftService _shiftService;

    public OrdersController(IOrderService service, MagiDesk.Core.Interfaces.IShiftService shiftService)
    {
        _service = service;
        _shiftService = shiftService;
    }

    [HttpPost]
    [RequireOpenShift]
    public async Task<ActionResult<OrderDto>> CreateAsync([FromBody] CreateOrderRequestDto req, CancellationToken ct)
    {
        var shiftId = HttpContext.Items["CurrentShiftId"] as Guid?;
        
        // Fallback if attribute didn't populate (redundancy for robustness)
        if (shiftId == null)
        {
             var shift = await _shiftService.GetCurrentOpenShiftAsync();
             shiftId = shift?.ShiftId;
        }

        if (shiftId == null) 
            return StatusCode(500, new { error = "SHIFT_REQUIRED", message = "No open shift found." });

        var order = await _service.CreateOrderAsync(req, shiftId, ct);
        // Use explicit URI to avoid link generation issues in some hosting setups
        return Created($"/api/orders/{order.Id}", order);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderDto>> GetAsync([FromRoute] Guid orderId, CancellationToken ct)
    {
        var order = await _service.GetOrderAsync(orderId, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpGet("by-session/{sessionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetBySessionAsync([FromRoute] Guid sessionId, [FromQuery] bool includeHistory, CancellationToken ct)
    {
        var list = await _service.GetOrdersBySessionAsync(sessionId, includeHistory, ct);
        return Ok(list);
    }

    [HttpGet("by-billing/{billingId:guid}")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetByBillingAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var list = await _service.GetOrdersByBillingIdAsync(billingId, ct);
        return Ok(list);
    }

    [HttpGet("by-billing/{billingId:guid}/items")]
    public async Task<ActionResult<IReadOnlyList<OrderItemDto>>> GetItemsByBillingAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var items = await _service.GetOrderItemsByBillingIdAsync(billingId, ct);
        return Ok(items);
    }

    [HttpPost("{orderId:guid}/items")]
    [RequireOpenShift]
    public async Task<ActionResult<OrderDto>> AddItemsAsync([FromRoute] Guid orderId, [FromBody] IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct)
    {
        var order = await _service.AddItemsAsync(orderId, items, ct);
        return Ok(order);
    }

    [HttpPut("{orderId:guid}/items/{orderItemId:guid}")]
    [RequireOpenShift]
    public async Task<ActionResult<OrderDto>> UpdateItemAsync([FromRoute] Guid orderId, [FromRoute] Guid orderItemId, [FromBody] UpdateOrderItemDto item, CancellationToken ct)
    {
        var order = await _service.UpdateItemAsync(orderId, item with { OrderItemId = orderItemId }, ct);
        return Ok(order);
    }

    [HttpDelete("{orderId:guid}/items/{orderItemId:guid}")]
    [RequireOpenShift]
    public async Task<ActionResult<OrderDto>> DeleteItemAsync([FromRoute] Guid orderId, [FromRoute] Guid orderItemId, CancellationToken ct)
    {
        var order = await _service.DeleteItemAsync(orderId, orderItemId, ct);
        return Ok(order);
    }

    [HttpPost("{orderId:guid}/close")]
    [RequireOpenShift]
    public async Task<ActionResult<OrderDto>> CloseAsync([FromRoute] Guid orderId, CancellationToken ct)
    {
        var order = await _service.CloseOrderAsync(orderId, ct);
        return Ok(order);
    }

    [HttpPost("{orderId:guid}/mark-delivered")]
    public async Task<ActionResult<OrderDto>> MarkDeliveredAsync([FromRoute] Guid orderId, [FromBody] MarkDeliveredRequestDto request, CancellationToken ct)
    {
        var order = await _service.MarkItemsDeliveredAsync(orderId, request.ItemDeliveries, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpPost("{orderId:guid}/mark-waiting")]
    public async Task<ActionResult<OrderDto>> MarkWaitingAsync([FromRoute] Guid orderId, CancellationToken ct)
    {
        var order = await _service.MarkOrderWaitingAsync(orderId, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpGet("{orderId:guid}/logs")]
    public async Task<ActionResult<PagedResult<OrderLogDto>>> ListLogsAsync([FromRoute] Guid orderId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await _service.ListLogsAsync(orderId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<OrderAnalyticsDto>> GetAnalyticsAsync([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string reportType = "daily", CancellationToken ct = default)
    {
        var request = new OrderAnalyticsRequestDto(fromDate, toDate, reportType);
        var analytics = await _service.GetOrderAnalyticsAsync(request, ct);
        return Ok(analytics);
    }

    [HttpGet("analytics/status-summary")]
    public async Task<ActionResult<IReadOnlyList<OrderStatusSummaryDto>>> GetStatusSummaryAsync(CancellationToken ct = default)
    {
        var summary = await _service.GetOrderStatusSummaryAsync(ct);
        return Ok(summary);
    }

    [HttpGet("analytics/trends")]
    public async Task<ActionResult<IReadOnlyList<OrderTrendDto>>> GetTrendsAsync([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, CancellationToken ct = default)
    {
        var trends = await _service.GetOrderTrendsAsync(fromDate, toDate, ct);
        return Ok(trends);
    }
}
