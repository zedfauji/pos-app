using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using InventoryApi.Repositories;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CashFlowController : ControllerBase
{
    private readonly ICashFlowRepository _cashFlowRepository;
    private readonly ILogger<CashFlowController> _logger;

    public CashFlowController(ICashFlowRepository cashFlowRepository, ILogger<CashFlowController> logger)
    {
        _cashFlowRepository = cashFlowRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CashFlow>>> GetCashFlowHistory(CancellationToken ct = default)
    {
        try
        {
            var history = await _cashFlowRepository.GetCashFlowHistoryAsync(ct);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cash flow history");
            return StatusCode(500, new { error = "Failed to retrieve cash flow history" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CashFlow>> GetCashFlow(string id, CancellationToken ct = default)
    {
        try
        {
            var entry = await _cashFlowRepository.GetCashFlowByIdAsync(id, ct);
            if (entry == null)
                return NotFound();

            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cash flow entry {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve cash flow entry" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CashFlow>> AddCashFlow([FromBody] CashFlow entry, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("AddCashFlow called with entry: {Entry}", System.Text.Json.JsonSerializer.Serialize(entry));
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }
            
            if (string.IsNullOrWhiteSpace(entry.EmployeeName))
            {
                _logger.LogWarning("Employee name is required");
                return BadRequest(new { error = "Employee name is required" });
            }

            if (entry.CashAmount == 0)
            {
                _logger.LogWarning("Cash amount cannot be zero");
                return BadRequest(new { error = "Cash amount cannot be zero" });
            }

            _logger.LogInformation("Calling repository to add cash flow entry");
            var id = await _cashFlowRepository.AddCashFlowAsync(entry, ct);
            entry.Id = id;

            _logger.LogInformation("Successfully added cash flow entry with ID: {Id}", id);
            return CreatedAtAction(nameof(GetCashFlow), new { id }, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add cash flow entry");
            return StatusCode(500, new { error = "Failed to add cash flow entry" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCashFlow(string id, [FromBody] CashFlow entry, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entry.EmployeeName))
                return BadRequest(new { error = "Employee name is required" });

            if (entry.CashAmount == 0)
                return BadRequest(new { error = "Cash amount cannot be zero" });

            entry.Id = id;
            var success = await _cashFlowRepository.UpdateCashFlowAsync(entry, ct);
            
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cash flow entry {Id}", id);
            return StatusCode(500, new { error = "Failed to update cash flow entry" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCashFlow(string id, CancellationToken ct = default)
    {
        try
        {
            var success = await _cashFlowRepository.DeleteCashFlowAsync(id, ct);
            
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cash flow entry {Id}", id);
            return StatusCode(500, new { error = "Failed to delete cash flow entry" });
        }
    }
}
