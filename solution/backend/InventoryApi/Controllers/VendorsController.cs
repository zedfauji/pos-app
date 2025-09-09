using InventoryApi.Repositories;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IVendorRepository _vendors;
    private readonly IInventoryRepository _items;
    private readonly ILogger<VendorsController> _logger;

    public VendorsController(IVendorRepository vendors, IInventoryRepository items, ILogger<VendorsController> logger)
    {
        _vendors = vendors;
        _items = items;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendorDto>>> Get(CancellationToken ct)
    {
        try
        {
            var vendors = await _vendors.ListAsync(ct);
            return Ok(vendors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET /api/vendors failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VendorDto>> GetById(string id, CancellationToken ct)
    {
        var vendor = await _vendors.GetAsync(id, ct);
        if (vendor is null) return NotFound();
        return Ok(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create([FromBody] VendorDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        var created = await _vendors.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VendorDto dto, CancellationToken ct)
    {
        var ok = await _vendors.UpdateAsync(id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _vendors.DeleteAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpGet("{vendorId}/export")]
    public async Task<IActionResult> Export(string vendorId, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        try
        {
            return StatusCode(410, new { error = "Export removed. Use Inventory SQL exports." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed for {VendorId}", vendorId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{vendorId}/import")]
    public async Task<IActionResult> Import(string vendorId, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        try
        {
            return StatusCode(410, new { error = "Import removed. Use admin ingestion pipeline." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed for {VendorId}", vendorId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Nested items
    [HttpGet("{vendorId}/items")]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems(string vendorId, CancellationToken ct)
    {
        var items = await _items.ListItemsAsync(vendorId, null, null, null, ct);
        return Ok(items);
    }

    [HttpGet("{vendorId}/items/{itemId}")]
    public async Task<ActionResult<ItemDto>> GetItem(string vendorId, string itemId, CancellationToken ct)
    {
        var item = await _items.GetItemAsync(itemId, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{vendorId}/items")]
    public async Task<ActionResult<ItemDto>> CreateItem(string vendorId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var created = await _items.CreateItemAsync(dto, ct);
        return CreatedAtAction(nameof(GetItem), new { vendorId, itemId = created.Id }, created);
    }

    [HttpPut("{vendorId}/items/{itemId}")]
    public async Task<IActionResult> UpdateItem(string vendorId, string itemId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var ok = await _items.UpdateItemAsync(itemId, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{vendorId}/items/{itemId}")]
    public async Task<IActionResult> DeleteItem(string vendorId, string itemId, CancellationToken ct)
    {
        var ok = await _items.DeleteItemAsync(itemId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
