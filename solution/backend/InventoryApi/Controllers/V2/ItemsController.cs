using InventoryApi.Repositories;
using MagiDesk.Shared.DTOs;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers.V2;

/// <summary>
/// Version 2 Inventory Items Controller with RBAC enforcement
/// All endpoints require specific permissions
/// </summary>
[ApiController]
[Route("api/v2/inventory/items")]
public class ItemsController : ControllerBase
{
    private readonly IInventoryRepository _repo;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(IInventoryRepository repo, ILogger<ItemsController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// List inventory items (requires inventory:view permission)
    /// </summary>
    [HttpGet]
    [RequiresPermission(Permissions.INVENTORY_VIEW)]
    public async Task<ActionResult<IEnumerable<ItemDto>>> Get([FromQuery] string? vendorId, [FromQuery] string? categoryId, [FromQuery] bool? isMenuAvailable, [FromQuery] string? search, CancellationToken ct)
    {
        var items = await _repo.ListItemsAsync(vendorId, categoryId, isMenuAvailable, search, ct);
        return Ok(items ?? new List<ItemDto>());
    }

    /// <summary>
    /// Get inventory item by ID (requires inventory:view permission)
    /// </summary>
    [HttpGet("{id}")]
    [RequiresPermission(Permissions.INVENTORY_VIEW)]
    public async Task<ActionResult<ItemDto>> GetById(string id, CancellationToken ct)
    {
        var item = await _repo.GetItemAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// Create inventory item (requires inventory:update permission)
    /// </summary>
    [HttpPost]
    [RequiresPermission(Permissions.INVENTORY_UPDATE)]
    public async Task<ActionResult<ItemDto>> Create([FromBody] ItemDto dto, CancellationToken ct)
    {
        var created = await _repo.CreateItemAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update inventory item (requires inventory:update permission)
    /// </summary>
    [HttpPut("{id}")]
    [RequiresPermission(Permissions.INVENTORY_UPDATE)]
    public async Task<IActionResult> Update(string id, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var ok = await _repo.UpdateItemAsync(id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Delete inventory item (requires inventory:update permission)
    /// </summary>
    [HttpDelete("{id}")]
    [RequiresPermission(Permissions.INVENTORY_UPDATE)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _repo.DeleteItemAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}

