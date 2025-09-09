using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MenuApi.Models;
using MenuApi.Services;

namespace MenuApi.Controllers;

[ApiController]
[Route("api/menu/combos")]
public class CombosController : ControllerBase
{
    private readonly IMenuService _service;

    public CombosController(IMenuService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ComboDto>>> ListAsync([FromQuery] ComboQueryDto query, CancellationToken ct)
    {
        var result = await _service.ListCombosAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ComboDetailsDto>> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var item = await _service.GetComboAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ComboDto>> CreateAsync([FromBody] CreateComboDto dto, CancellationToken ct)
    {
        var created = await _service.CreateComboAsync(dto, User.Identity?.Name ?? "system", ct);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ComboDto>> UpdateAsync([FromRoute] long id, [FromBody] UpdateComboDto dto, CancellationToken ct)
    {
        var updated = await _service.UpdateComboAsync(id, dto, User.Identity?.Name ?? "system", ct);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] long id, CancellationToken ct)
    {
        await _service.DeleteComboAsync(id, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }

    // New: availability toggle
    [HttpPut("{id:long}/availability")]
    public async Task<IActionResult> SetAvailabilityAsync([FromRoute] long id, [FromBody] AvailabilityUpdateDto dto, CancellationToken ct)
    {
        await _service.SetComboAvailabilityAsync(id, dto.IsAvailable, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }

    // New: compute combo price from item components
    [HttpGet("{id:long}/price")] 
    public async Task<ActionResult<ComboPriceResponseDto>> GetComputedPriceAsync([FromRoute] long id, CancellationToken ct)
    {
        var (computed, lines) = await _service.ComputeComboPriceAsync(id, ct);
        var dto = new ComboPriceResponseDto(computed, lines.Select(l => new ComboItemPriceLineDto(l.MenuItemId, l.Quantity, l.UnitPrice)).ToList());
        return Ok(dto);
    }

    // New: rollback to version
    [HttpPost("{id:long}/rollback")] 
    public async Task<ActionResult<object>> RollbackAsync([FromRoute] long id, [FromQuery] int toVersion, CancellationToken ct)
    {
        await _service.RollbackComboAsync(id, toVersion, User.Identity?.Name ?? "system", ct);
        return Accepted(new { success = true, id, version = toVersion });
    }
}
