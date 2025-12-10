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

    // New: get by SKU
    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<MenuItemDetailsDto>> GetBySkuAsync([FromRoute] string sku, CancellationToken ct)
    {
        var item = await _service.GetItemBySkuAsync(sku, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    // New: duplicate SKU check
    [HttpGet("check-duplicate-sku/{sku}")]
    public async Task<ActionResult<object>> CheckDuplicateSkuAsync([FromRoute] string sku, [FromQuery] long? excludeId, CancellationToken ct)
    {
        var exists = await _service.ExistsSkuAsync(sku, excludeId, ct);
        return Ok(new { duplicate = exists });
    }

    // New: availability toggle
    [HttpPut("{id:long}/availability")]
    public async Task<IActionResult> SetAvailabilityAsync([FromRoute] long id, [FromBody] AvailabilityUpdateDto dto, CancellationToken ct)
    {
        await _service.SetItemAvailabilityAsync(id, dto.IsAvailable, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }

    // New: rollback to version
    [HttpPost("{id:long}/rollback")]
    public async Task<ActionResult<object>> RollbackAsync([FromRoute] long id, [FromQuery] int toVersion, CancellationToken ct)
    {
        await _service.RollbackItemAsync(id, toVersion, User.Identity?.Name ?? "system", ct);
        return Accepted(new { success = true, id, version = toVersion });
    }

    // New: picture endpoint - redirect/fallback
    [HttpGet("{id:long}/picture")]
    public async Task<IActionResult> GetPictureAsync([FromRoute] long id, CancellationToken ct)
    {
        var details = await _service.GetItemAsync(id, ct);
        if (details is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(details.Item.PictureUrl))
        {
            return Redirect(details.Item.PictureUrl!);
        }
        return NoContent();
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
