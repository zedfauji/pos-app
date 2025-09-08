using MagiDesk.Backend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MagiDesk.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IFirestoreService _firestore;
    private readonly ILogger<VendorsController> _logger;

    public VendorsController(IFirestoreService firestore, ILogger<VendorsController> logger)
    {
        _firestore = firestore;
        _logger = logger;
    }

    // Dump all vendors and their subcollections/documents
    [HttpGet("probe/dump/all")]
    public async Task<IActionResult> DumpAll(CancellationToken ct)
    {
        try
        {
            var data = await _firestore.GetAllVendorsDumpAsync(ct);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DumpAll failed");
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }

    // Detailed probe for a specific vendor document: fields, subcollections, and sample docs
    [HttpGet("probe/{vendorId}/details")]
    public async Task<IActionResult> ProbeVendorDetails(string vendorId, CancellationToken ct)
    {
        try
        {
            var info = await _firestore.GetVendorDebugAsync(vendorId, ct);
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProbeVendorDetails failed for {VendorId}", vendorId);
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendorDto>>> Get(CancellationToken ct)
    {
        try
        {
            var vendors = await _firestore.GetVendorsAsync(ct);
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
        var vendor = await _firestore.GetVendorAsync(id, ct);
        if (vendor is null) return NotFound();
        return Ok(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create([FromBody] VendorDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        var created = await _firestore.CreateVendorAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VendorDto dto, CancellationToken ct)
    {
        var ok = await _firestore.UpdateVendorAsync(id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _firestore.DeleteVendorAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    // Export vendor items
    [HttpGet("{vendorId}/export")]
    public async Task<IActionResult> Export(string vendorId, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        try
        {
            var content = await _firestore.ExportVendorItemsAsync(vendorId, format, ct);
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

    // Import vendor items (raw body as file content)
    [HttpPost("{vendorId}/import")]
    public async Task<IActionResult> Import(string vendorId, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        try
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms, ct);
            ms.Position = 0;
            await _firestore.ImportVendorItemsAsync(vendorId, format, ms, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed for {VendorId}", vendorId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Probe endpoint to help debug Firestore mapping/connectivity
    [HttpGet("probe/heineken")]
    public async Task<IActionResult> ProbeHeineken(CancellationToken ct)
    {
        try
        {
            var vendors = await _firestore.GetVendorsAsync(ct);
            var match = vendors.FirstOrDefault(v => string.Equals(v.Name, "Heineken", StringComparison.OrdinalIgnoreCase));
            return Ok(new
            {
                count = vendors.Count,
                found = match is not null,
                vendor = match
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProbeHeineken failed");
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }

    // Nested Items routes
    [HttpGet("{vendorId}/items")]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems(string vendorId, CancellationToken ct)
    {
        var items = await _firestore.GetItemsAsync(vendorId, ct);
        return Ok(items);
    }

    [HttpGet("{vendorId}/items/{itemId}")]
    public async Task<ActionResult<ItemDto>> GetItem(string vendorId, string itemId, CancellationToken ct)
    {
        var item = await _firestore.GetItemAsync(vendorId, itemId, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{vendorId}/items")]
    public async Task<ActionResult<ItemDto>> CreateItem(string vendorId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var created = await _firestore.CreateItemAsync(vendorId, dto, ct);
        return CreatedAtAction(nameof(GetItem), new { vendorId, itemId = created.Id }, created);
    }

    [HttpPut("{vendorId}/items/{itemId}")]
    public async Task<IActionResult> UpdateItem(string vendorId, string itemId, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var ok = await _firestore.UpdateItemAsync(vendorId, itemId, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{vendorId}/items/{itemId}")]
    public async Task<IActionResult> DeleteItem(string vendorId, string itemId, CancellationToken ct)
    {
        var ok = await _firestore.DeleteItemAsync(vendorId, itemId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
