using Microsoft.AspNetCore.Mvc;
using OrderApi.Models;
using OrderApi.Services;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateAsync([FromBody] CreateOrderRequestDto req, CancellationToken ct)
    {
        var order = await _service.CreateOrderAsync(req, ct);
        // Use explicit URI to avoid link generation issues in some hosting setups
        return Created($"/api/orders/{order.Id}", order);
    }

    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<OrderDto>> GetAsync([FromRoute] long orderId, CancellationToken ct)
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

    [HttpPost("{orderId:long}/items")]
    public async Task<ActionResult<OrderDto>> AddItemsAsync([FromRoute] long orderId, [FromBody] IReadOnlyList<CreateOrderItemDto> items, CancellationToken ct)
    {
        var order = await _service.AddItemsAsync(orderId, items, ct);
        return Ok(order);
    }

    [HttpPut("{orderId:long}/items/{orderItemId:long}")]
    public async Task<ActionResult<OrderDto>> UpdateItemAsync([FromRoute] long orderId, [FromRoute] long orderItemId, [FromBody] UpdateOrderItemDto item, CancellationToken ct)
    {
        var order = await _service.UpdateItemAsync(orderId, item with { OrderItemId = orderItemId }, ct);
        return Ok(order);
    }

    [HttpDelete("{orderId:long}/items/{orderItemId:long}")]
    public async Task<ActionResult<OrderDto>> DeleteItemAsync([FromRoute] long orderId, [FromRoute] long orderItemId, CancellationToken ct)
    {
        var order = await _service.DeleteItemAsync(orderId, orderItemId, ct);
        return Ok(order);
    }

    [HttpPost("{orderId:long}/close")]
    public async Task<ActionResult<OrderDto>> CloseAsync([FromRoute] long orderId, CancellationToken ct)
    {
        var order = await _service.CloseOrderAsync(orderId, ct);
        return Ok(order);
    }

    [HttpPost("{orderId:long}/mark-delivered")]
    public async Task<ActionResult<OrderDto>> MarkDeliveredAsync([FromRoute] long orderId, [FromBody] MarkDeliveredRequestDto request, CancellationToken ct)
    {
        var order = await _service.MarkItemsDeliveredAsync(orderId, request.ItemDeliveries, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpPost("{orderId:long}/mark-waiting")]
    public async Task<ActionResult<OrderDto>> MarkWaitingAsync([FromRoute] long orderId, CancellationToken ct)
    {
        var order = await _service.MarkOrderWaitingAsync(orderId, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpGet("{orderId:long}/logs")]
    public async Task<ActionResult<PagedResult<OrderLogDto>>> ListLogsAsync([FromRoute] long orderId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await _service.ListLogsAsync(orderId, page, pageSize, ct);
        return Ok(result);
    }
}
