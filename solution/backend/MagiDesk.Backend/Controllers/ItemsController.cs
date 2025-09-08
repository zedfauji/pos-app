using MagiDesk.Backend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MagiDesk.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IFirestoreService _firestore;

    public ItemsController(IFirestoreService firestore)
    {
        _firestore = firestore;
    }

    // For convenience, vendorId provided via query
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> Get([FromQuery] string vendorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var items = await _firestore.GetItemsAsync(vendorId, ct);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetById(string id, [FromQuery] string vendorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var item = await _firestore.GetItemAsync(vendorId, id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create([FromQuery] string vendorId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var created = await _firestore.CreateItemAsync(vendorId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id, vendorId }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromQuery] string vendorId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var ok = await _firestore.UpdateItemAsync(vendorId, id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string vendorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return BadRequest("vendorId is required");
        var ok = await _firestore.DeleteItemAsync(vendorId, id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
