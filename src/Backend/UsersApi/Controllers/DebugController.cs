using Microsoft.AspNetCore.Mvc;
using UsersApi.Services;

namespace UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IRbacService _rbacService;
    private readonly ILogger<DebugController> _logger;

    public DebugController(IRbacService rbacService, ILogger<DebugController> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    [HttpGet("test-db")]
    public async Task<IActionResult> TestDatabase(CancellationToken ct = default)
    {
        try
        {
            var stats = await _rbacService.GetRoleStatsAsync(ct);
            return Ok(new { 
                Message = "Database connection successful",
                Stats = stats,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database test failed");
            return StatusCode(500, new { 
                Error = ex.Message,
                StackTrace = ex.StackTrace,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("test-roles")]
    public async Task<IActionResult> TestRoles(CancellationToken ct = default)
    {
        try
        {
            var request = new MagiDesk.Shared.DTOs.Users.RoleSearchRequest 
            { 
                PageSize = 10,
                Page = 1
            };
            var roles = await _rbacService.GetRolesAsync(request, ct);
            return Ok(new { 
                Message = "Roles retrieved successfully",
                TotalCount = roles.TotalCount,
                ItemsCount = roles.Items.Count(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Roles test failed");
            return StatusCode(500, new { 
                Error = ex.Message,
                StackTrace = ex.StackTrace,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
