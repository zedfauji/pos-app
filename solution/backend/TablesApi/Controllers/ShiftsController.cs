using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Shifts;
using Microsoft.AspNetCore.Mvc;

namespace TablesApi.Controllers;

[ApiController]
[Route("shifts")]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _service;

    public ShiftsController(IShiftService service)
    {
        _service = service;
    }

    [HttpGet("validate")]
    public async Task<ActionResult<ShiftValidationDto>> Validate()
    {
        var shift = await _service.GetCurrentOpenShiftAsync();
        return Ok(new ShiftValidationDto 
        {
            CanOperate = shift != null,
            CurrentShiftId = shift?.ShiftId,
            ShiftNumber = shift?.ShiftNumber,
            Status = shift?.Status ?? "closed"
        });
    }

    [HttpGet("current")]
    public async Task<ActionResult<ShiftDto>> GetCurrentShift()
    {
        var shift = await _service.GetCurrentOpenShiftAsync();
        if (shift == null) return NoContent();
        
        return Ok(new ShiftDto
        {
            ShiftId = shift.ShiftId,
            ShiftNumber = shift.ShiftNumber,
            OpenedByUserId = shift.OpenedByUserId,
            OpenedByName = shift.OpenedByName,
            OpenedAt = shift.OpenedAt,
            StartingCash = shift.StartingCash,
            Status = shift.Status
        });
    }

    [HttpPost("open")]
    public async Task<ActionResult<ShiftDto>> OpenShift([FromBody] MagiDesk.Shared.DTOs.Shifts.OpenShiftRequest request)
    {
        try
        {
            // TODO: Get real User ID from Context/Auth
            int userId = 1; 
            string userName = "Admin"; 

            var shift = await _service.OpenShiftAsync(userId, userName, request.StartingCash, request.IdempotencyKey ?? Guid.NewGuid());
            
            return Ok(new ShiftDto
            {
                ShiftId = shift.ShiftId,
                ShiftNumber = shift.ShiftNumber,
                OpenedByUserId = shift.OpenedByUserId,
                OpenedByName = shift.OpenedByName,
                OpenedAt = shift.OpenedAt,
                StartingCash = shift.StartingCash,
                Status = shift.Status
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<ShiftDto>> CloseShift(Guid id, [FromBody] MagiDesk.Shared.DTOs.Shifts.CloseShiftRequest request)
    {
        try
        {
            int userId = 1;
            string userName = "Admin"; // Placeholder

            var result = await _service.CloseShiftAsync(id, userId, userName, request.DeclaredCash, request.IdempotencyKey ?? Guid.NewGuid(), request.Note);
            
            if (!result.Success)
            {
                return Conflict(new { message = "Cannot close shift", errors = result.Errors });
            }

            var s = result.Shift;
            return Ok(new ShiftDto
            {
                ShiftId = s.ShiftId,
                ShiftNumber = s.ShiftNumber,
                OpenedByUserId = s.OpenedByUserId,
                OpenedByName = s.OpenedByName,
                OpenedAt = s.OpenedAt,
                StartingCash = s.StartingCash,
                Status = s.Status
            });
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
    
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        var shifts = await _service.GetHistoryAsync(limit, offset);
        // Mapping list
        var dtos = shifts.Select(s => new ShiftDto
        {
            ShiftId = s.ShiftId,
            ShiftNumber = s.ShiftNumber,
            OpenedByUserId = s.OpenedByUserId,
            OpenedByName = s.OpenedByName,
            OpenedAt = s.OpenedAt,
            StartingCash = s.StartingCash,
            Status = s.Status
        });
        return Ok(dtos);
    }
}
