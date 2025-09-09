using InventoryApi.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IDatabaseService _database;
    private readonly ILogger<VendorsController> _logger;

    public VendorsController(IDatabaseService database, ILogger<VendorsController> logger)
    {
        _database = database;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendorDto>>> Get(CancellationToken ct)
    {
        try
        {
            var vendors = await _database.GetVendorsAsync(ct);
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
        var vendor = await _database.GetVendorAsync(id, ct);
        if (vendor is null) return NotFound();
        return Ok(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create([FromBody] VendorDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        var created = await _database.CreateVendorAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VendorDto dto, CancellationToken ct)
    {
        var ok = await _database.UpdateVendorAsync(id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _database.DeleteVendorAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpGet("{vendorId}/export")]
    public async Task<IActionResult> Export(string vendorId, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        try
        {
            var content = await _database.ExportVendorItemsAsync(vendorId, format, ct);
            var fileName = $"{vendorId}-items.{(format.ToLowerInvariant() == "yaml" ? "yml" : format.ToLowerInvariant())}";
            var mime = format.ToLowerInvariant() switch
            {
                "json" => "application/json",
                "yaml" => "application/x-yaml",
                "csv" => "text/csv",
                _ => "text/plain"
            };
            return File(System.Text.Encoding.UTF8.GetBytes(content), mime, fileName);
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
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms, ct);
            ms.Position = 0;
            await _database.ImportVendorItemsAsync(vendorId, format, ms, ct);
            return NoContent();
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
        var items = await _database.GetItemsAsync(vendorId, ct);
        return Ok(items);
    }

    [HttpGet("{vendorId}/items/{itemId}")]
    public async Task<ActionResult<ItemDto>> GetItem(string vendorId, string itemId, CancellationToken ct)
    {
        var item = await _database.GetItemAsync(vendorId, itemId, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{vendorId}/items")]
    public async Task<ActionResult<ItemDto>> CreateItem(string vendorId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var created = await _database.CreateItemAsync(vendorId, dto, ct);
        return CreatedAtAction(nameof(GetItem), new { vendorId, itemId = created.Id }, created);
    }

    [HttpPut("{vendorId}/items/{itemId}")]
    public async Task<IActionResult> UpdateItem(string vendorId, string itemId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var ok = await _database.UpdateItemAsync(vendorId, itemId, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{vendorId}/items/{itemId}")]
    public async Task<IActionResult> DeleteItem(string vendorId, string itemId, CancellationToken ct)
    {
        var ok = await _database.DeleteItemAsync(vendorId, itemId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
