using Microsoft.AspNetCore.Mvc;
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
}
