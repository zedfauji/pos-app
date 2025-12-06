using Microsoft.AspNetCore.Mvc;
using MenuApi.Models;
using MenuApi.Services;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;

namespace MenuApi.Controllers.V2;

/// <summary>
/// Version 2 Menu Items Controller with RBAC enforcement
/// All endpoints require specific permissions
/// </summary>
[ApiController]
[Route("api/v2/menu/items")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuService _service;
    private readonly ILogger<MenuItemsController> _logger;

    public MenuItemsController(IMenuService service, ILogger<MenuItemsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// List menu items (requires menu:view permission)
    /// </summary>
    [HttpGet]
    [RequiresPermission(Permissions.MENU_VIEW)]
    public async Task<ActionResult<MenuApi.Models.PagedResult<MenuItemDto>>> ListAsync([FromQuery] MenuItemQueryDto query, CancellationToken ct)
    {
        var result = await _service.ListItemsAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get menu item by ID (requires menu:view permission)
    /// </summary>
    [HttpGet("{id:long}")]
    [RequiresPermission(Permissions.MENU_VIEW)]
    public async Task<ActionResult<MenuItemDetailsDto>> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var item = await _service.GetItemAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// Get menu item by SKU (requires menu:view permission)
    /// </summary>
    [HttpGet("sku/{sku}")]
    [RequiresPermission(Permissions.MENU_VIEW)]
    public async Task<ActionResult<MenuItemDetailsDto>> GetBySkuAsync([FromRoute] string sku, CancellationToken ct)
    {
        var item = await _service.GetItemBySkuAsync(sku, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// Check for duplicate SKU (requires menu:view permission)
    /// </summary>
    [HttpGet("check-duplicate-sku/{sku}")]
    [RequiresPermission(Permissions.MENU_VIEW)]
    public async Task<ActionResult<object>> CheckDuplicateSkuAsync([FromRoute] string sku, [FromQuery] long? excludeId, CancellationToken ct)
    {
        var exists = await _service.ExistsSkuAsync(sku, excludeId, ct);
        return Ok(new { duplicate = exists });
    }

    /// <summary>
    /// Set item availability (requires menu:update permission)
    /// </summary>
    [HttpPut("{id:long}/availability")]
    [RequiresPermission(Permissions.MENU_UPDATE)]
    public async Task<IActionResult> SetAvailabilityAsync([FromRoute] long id, [FromBody] AvailabilityUpdateDto dto, CancellationToken ct)
    {
        await _service.SetItemAvailabilityAsync(id, dto.IsAvailable, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }

    /// <summary>
    /// Rollback item to previous version (requires menu:update permission)
    /// </summary>
    [HttpPost("{id:long}/rollback")]
    [RequiresPermission(Permissions.MENU_UPDATE)]
    public async Task<ActionResult<object>> RollbackAsync([FromRoute] long id, [FromQuery] int toVersion, CancellationToken ct)
    {
        await _service.RollbackItemAsync(id, toVersion, User.Identity?.Name ?? "system", ct);
        return Accepted(new { success = true, id, version = toVersion });
    }

    /// <summary>
    /// Get item picture (requires menu:view permission)
    /// </summary>
    [HttpGet("{id:long}/picture")]
    [RequiresPermission(Permissions.MENU_VIEW)]
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

    /// <summary>
    /// Create new menu item (requires menu:create permission)
    /// </summary>
    [HttpPost]
    [RequiresPermission(Permissions.MENU_CREATE)]
    public async Task<ActionResult<MenuItemDto>> CreateAsync([FromBody] CreateMenuItemDto dto, CancellationToken ct)
    {
        var created = await _service.CreateItemAsync(dto, User.Identity?.Name ?? "system", ct);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update menu item (requires menu:update permission)
    /// </summary>
    [HttpPut("{id:long}")]
    [RequiresPermission(Permissions.MENU_UPDATE)]
    public async Task<ActionResult<MenuItemDto>> UpdateAsync([FromRoute] long id, [FromBody] UpdateMenuItemDto dto, CancellationToken ct)
    {
        var updated = await _service.UpdateItemAsync(id, dto, User.Identity?.Name ?? "system", ct);
        return Ok(updated);
    }

    /// <summary>
    /// Delete menu item (requires menu:delete permission)
    /// </summary>
    [HttpDelete("{id:long}")]
    [RequiresPermission(Permissions.MENU_DELETE)]
    public async Task<IActionResult> DeleteAsync([FromRoute] long id, CancellationToken ct)
    {
        await _service.DeleteItemAsync(id, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }

    /// <summary>
    /// Restore deleted menu item (requires menu:update permission)
    /// </summary>
    [HttpPost("{id:long}/restore")]
    [RequiresPermission(Permissions.MENU_UPDATE)]
    public async Task<IActionResult> RestoreAsync([FromRoute] long id, CancellationToken ct)
    {
        await _service.RestoreItemAsync(id, User.Identity?.Name ?? "system", ct);
        return NoContent();
    }
}

