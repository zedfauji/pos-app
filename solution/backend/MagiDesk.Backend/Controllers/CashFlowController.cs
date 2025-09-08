using MagiDesk.Backend.Services;
using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MagiDesk.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CashFlowController : ControllerBase
{
    private readonly ICashFlowService _cashFlow;
    private readonly ILogger<CashFlowController> _logger;

    public CashFlowController(ICashFlowService cashFlow, ILogger<CashFlowController> logger)
    {
        _cashFlow = cashFlow;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CashFlow>>> Get(CancellationToken ct)
    {
        var list = await _cashFlow.GetCashFlowHistoryAsync(ct);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CashFlow entry, CancellationToken ct)
    {
        try
        {
            await _cashFlow.AddCashFlowAsync(entry, ct);
            return Created($"api/cashflow/{entry.Id}", entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add cash flow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] CashFlow entry, CancellationToken ct)
    {
        try
        {
            await _cashFlow.UpdateCashFlowAsync(entry, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cash flow");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
