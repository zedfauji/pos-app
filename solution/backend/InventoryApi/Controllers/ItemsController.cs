using InventoryApi.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IDatabaseService _database;

    public ItemsController(IDatabaseService database)
    {
        _database = database;
    }

    // generic endpoints require vendorId query param
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> Get([FromQuery] string vendorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var items = await _database.GetItemsAsync(vendorId, ct);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetById(string id, [FromQuery] string vendorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var item = await _database.GetItemAsync(vendorId, id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create([FromQuery] string vendorId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var created = await _database.CreateItemAsync(vendorId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id, vendorId }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromQuery] string vendorId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var ok = await _database.UpdateItemAsync(vendorId, id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string vendorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var ok = await _database.DeleteItemAsync(vendorId, id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
