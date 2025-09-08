using Microsoft.AspNetCore.Mvc;
using MenuApi.Models;
using MenuApi.Repositories;

namespace MenuApi.Controllers;

[ApiController]
[Route("api/menu/modifiers")]
public class ModifiersController : ControllerBase
{
    private readonly IMenuRepository _repo;

    public ModifiersController(IMenuRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ModifierDto>>> ListAsync([FromQuery] ModifierQueryDto query, CancellationToken ct)
    {
        var (items, total) = await _repo.ListModifiersAsync(query, ct);
        return Ok(new PagedResult<ModifierDto>(items, total));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ModifierDto>> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var item = await _repo.GetModifierAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ModifierDto>> CreateAsync([FromBody] CreateModifierDto dto, CancellationToken ct)
    {
        var created = await _repo.CreateModifierAsync(dto, User.Identity?.Name ?? "system", ct);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ModifierDto>> UpdateAsync([FromRoute] long id, [FromBody] UpdateModifierDto dto, CancellationToken ct)
    {
        var updated = await _repo.UpdateModifierAsync(id, dto, User.Identity?.Name ?? "system", ct);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] long id, CancellationToken ct)
    {
        await _repo.DeleteModifierAsync(id, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }
}
