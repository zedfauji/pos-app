using Microsoft.AspNetCore.Mvc;
using OrderApi.Models;
using OrderApi.Services;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;

namespace OrderApi.Controllers.V2;

/// <summary>
/// Version 2 Orders Controller with RBAC enforcement
/// All endpoints require specific permissions
/// </summary>
[ApiController]
[Route("api/v2/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService service, ILogger<OrdersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Create new order (requires order:create permission)
    /// </summary>
    [HttpPost]
    [RequiresPermission(Permissions.ORDER_CREATE)]
    public async Task<ActionResult<OrderDto>> CreateAsync([FromBody] CreateOrderRequestDto req, CancellationToken ct)
    {
        var order = await _service.CreateOrderAsync(req, ct);
        return Created($"/api/v2/orders/{order.Id}", order);
    }

    /// <summary>
    /// Get order by ID (requires order:view permission)
    /// </summary>
    [HttpGet("{orderId:long}")]
    [RequiresPermission(Permissions.ORDER_VIEW)]
    public async Task<ActionResult<OrderDto>> GetAsync([FromRoute] long orderId, CancellationToken ct)
    {
        var order = await _service.GetOrderAsync(orderId, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Get orders by session ID (requires order:view permission)
    /// </summary>
    [HttpGet("by-session/{sessionId:guid}")]
    [RequiresPermission(Permissions.ORDER_VIEW)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetBySessionAsync([FromRoute] Guid sessionId, [FromQuery] bool includeHistory, CancellationToken ct)
    {
        var list = await _service.GetOrdersBySessionAsync(sessionId, includeHistory, ct);
        return Ok(list);
    }

    /// <summary>
    /// Get orders by billing ID (requires order:view permission)
    /// </summary>
    [HttpGet("by-billing/{billingId:guid}")]
    [RequiresPermission(Permissions.ORDER_VIEW)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetByBillingAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var list = await _service.GetOrdersByBillingIdAsync(billingId, ct);
        return Ok(list);
    }

    /// <summary>
    /// Get order items by billing ID (requires order:view permission)
    /// </summary>
    [HttpGet("by-billing/{billingId:guid}/items")]
    [RequiresPermission(Permissions.ORDER_VIEW)]
    public async Task<ActionResult<IReadOnlyList<OrderItemDto>>> GetItemsByBillingAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var items = await _service.GetOrderItemsByBillingIdAsync(billingId, ct);
        return Ok(items);
    }

    /// <summary>
    /// Add items to order (requires order:update permission)
    /// </summary>
    [HttpPost("{orderId:long}/items")]
    [RequiresPermission(Permissions.ORDER_UPDATE)]
    public async Task<ActionResult<OrderDto>> AddItemsAsync([FromRoute] long orderId, [FromBody] IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct)
    {
        var order = await _service.AddItemsAsync(orderId, items, ct);
        return Ok(order);
    }

    /// <summary>
    /// Update order item (requires order:update permission)
    /// </summary>
    [HttpPut("{orderId:long}/items/{orderItemId:long}")]
    [RequiresPermission(Permissions.ORDER_UPDATE)]
    public async Task<ActionResult<OrderDto>> UpdateItemAsync([FromRoute] long orderId, [FromRoute] long orderItemId, [FromBody] UpdateOrderItemDto item, CancellationToken ct)
    {
        var order = await _service.UpdateItemAsync(orderId, item with { OrderItemId = orderItemId }, ct);
        return Ok(order);
    }

    /// <summary>
    /// Delete order item (requires order:update permission)
    /// </summary>
    [HttpDelete("{orderId:long}/items/{orderItemId:long}")]
    [RequiresPermission(Permissions.ORDER_UPDATE)]
    public async Task<ActionResult<OrderDto>> DeleteItemAsync([FromRoute] long orderId, [FromRoute] long orderItemId, CancellationToken ct)
    {
        var order = await _service.DeleteItemAsync(orderId, orderItemId, ct);
        return Ok(order);
    }

    /// <summary>
    /// Close order (requires order:complete permission)
    /// </summary>
    [HttpPost("{orderId:long}/close")]
    [RequiresPermission(Permissions.ORDER_COMPLETE)]
    public async Task<ActionResult<OrderDto>> CloseAsync([FromRoute] long orderId, CancellationToken ct)
    {
        var order = await _service.CloseOrderAsync(orderId, ct);
        return Ok(order);
    }

    /// <summary>
    /// Mark items as delivered (requires order:update permission)
    /// </summary>
    [HttpPost("{orderId:long}/mark-delivered")]
    [RequiresPermission(Permissions.ORDER_UPDATE)]
    public async Task<ActionResult<OrderDto>> MarkDeliveredAsync([FromRoute] long orderId, [FromBody] MarkDeliveredRequestDto request, CancellationToken ct)
    {
        var order = await _service.MarkItemsDeliveredAsync(orderId, request.ItemDeliveries, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Mark order as waiting (requires order:update permission)
    /// </summary>
    [HttpPost("{orderId:long}/mark-waiting")]
    [RequiresPermission(Permissions.ORDER_UPDATE)]
    public async Task<ActionResult<OrderDto>> MarkWaitingAsync([FromRoute] long orderId, CancellationToken ct)
    {
        var order = await _service.MarkOrderWaitingAsync(orderId, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Get order logs (requires order:view permission)
    /// </summary>
    [HttpGet("{orderId:long}/logs")]
    [RequiresPermission(Permissions.ORDER_VIEW)]
    public async Task<ActionResult<OrderApi.Models.PagedResult<OrderLogDto>>> ListLogsAsync([FromRoute] long orderId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await _service.ListLogsAsync(orderId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get order analytics (requires report:sales permission)
    /// </summary>
    [HttpGet("analytics")]
    [RequiresPermission(Permissions.REPORT_SALES)]
    public async Task<ActionResult<OrderAnalyticsDto>> GetAnalyticsAsync([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string reportType = "daily", CancellationToken ct = default)
    {
        var request = new OrderAnalyticsRequestDto(fromDate, toDate, reportType);
        var analytics = await _service.GetOrderAnalyticsAsync(request, ct);
        return Ok(analytics);
    }

    /// <summary>
    /// Get order status summary (requires report:sales permission)
    /// </summary>
    [HttpGet("analytics/status-summary")]
    [RequiresPermission(Permissions.REPORT_SALES)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusSummaryDto>>> GetStatusSummaryAsync(CancellationToken ct = default)
    {
        var summary = await _service.GetOrderStatusSummaryAsync(ct);
        return Ok(summary);
    }

    /// <summary>
    /// Get order trends (requires report:sales permission)
    /// </summary>
    [HttpGet("analytics/trends")]
    [RequiresPermission(Permissions.REPORT_SALES)]
    public async Task<ActionResult<IReadOnlyList<OrderTrendDto>>> GetTrendsAsync([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, CancellationToken ct = default)
    {
        var trends = await _service.GetOrderTrendsAsync(fromDate, toDate, ct);
        return Ok(trends);
    }
}

