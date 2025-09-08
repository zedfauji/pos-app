using Microsoft.AspNetCore.Mvc;
using MenuApi.Models;
using MenuApi.Services;

namespace MenuApi.Controllers;

[ApiController]
[Route("api/menu/items")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuService _service;

    public MenuItemsController(IMenuService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MenuItemDto>>> ListAsync([FromQuery] MenuItemQueryDto query, CancellationToken ct)
    {
        var result = await _service.ListItemsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MenuItemDetailsDto>> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var item = await _service.GetItemAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<MenuItemDto>> CreateAsync([FromBody] CreateMenuItemDto dto, CancellationToken ct)
    {
        var created = await _service.CreateItemAsync(dto, User.Identity?.Name ?? "system", ct);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<MenuItemDto>> UpdateAsync([FromRoute] long id, [FromBody] UpdateMenuItemDto dto, CancellationToken ct)
    {
        var updated = await _service.UpdateItemAsync(id, dto, User.Identity?.Name ?? "system", ct);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] long id, CancellationToken ct)
    {
        await _service.DeleteItemAsync(id, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }

    [HttpPost("{id:long}/restore")]
    public async Task<IActionResult> RestoreAsync([FromRoute] long id, CancellationToken ct)
    {
        await _service.RestoreItemAsync(id, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }
}
