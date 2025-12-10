using InventoryApi.Repositories;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IInventoryRepository _repo;

    public ItemsController(IInventoryRepository repo)
    {
        _repo = repo;
    }

    // generic endpoints; optional filters
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> Get([FromQuery] string? vendorId, [FromQuery] string? categoryId, [FromQuery] bool? isMenuAvailable, [FromQuery] string? search, CancellationToken ct)
    {
        var items = await _repo.ListItemsAsync(vendorId, categoryId, isMenuAvailable, search, ct);
        return Ok(items ?? new List<ItemDto>());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetById(string id, CancellationToken ct)
    {
        var item = await _repo.GetItemAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create([FromBody] ItemDto dto, CancellationToken ct)
    {
        var created = await _repo.CreateItemAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var ok = await _repo.UpdateItemAsync(id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _repo.DeleteItemAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
