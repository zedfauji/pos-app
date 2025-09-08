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
        return CreatedAtAction(nameof(GetAsync), new { orderId = order.Id }, order);
    }

    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<OrderDto>> GetAsync([FromRoute] long orderId, CancellationToken ct)
    {
        var order = await _service.GetOrderAsync(orderId, ct);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpGet("by-session/{sessionId}")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetBySessionAsync([FromRoute] string sessionId, [FromQuery] bool includeHistory, CancellationToken ct)
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
}
