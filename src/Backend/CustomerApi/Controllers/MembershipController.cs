using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembershipController : ControllerBase
{
    private readonly IMembershipService _membershipService;
    private readonly ILogger<MembershipController> _logger;

    public MembershipController(IMembershipService membershipService, ILogger<MembershipController> logger)
    {
        _membershipService = membershipService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active membership levels
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MembershipLevelDto>>> GetAllMembershipLevels()
    {
        try
        {
            var levels = await _membershipService.GetAllMembershipLevelsAsync();
            return Ok(levels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving membership levels");
            return StatusCode(500, "An error occurred while retrieving membership levels");
        }
    }

    /// <summary>
    /// Get membership level by ID
    /// </summary>
    [HttpGet("{membershipLevelId:guid}")]
    public async Task<ActionResult<MembershipLevelDto>> GetMembershipLevel(Guid membershipLevelId)
    {
        try
        {
            var level = await _membershipService.GetMembershipLevelByIdAsync(membershipLevelId);
            if (level == null)
            {
                return NotFound($"Membership level with ID {membershipLevelId} not found");
            }

            return Ok(level);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving membership level {MembershipLevelId}", membershipLevelId);
            return StatusCode(500, "An error occurred while retrieving the membership level");
        }
    }

    /// <summary>
    /// Get default membership level
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<MembershipLevelDto>> GetDefaultMembershipLevel()
    {
        try
        {
            var level = await _membershipService.GetDefaultMembershipLevelAsync();
            return Ok(level);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default membership level");
            return StatusCode(500, "An error occurred while retrieving the default membership level");
        }
    }

    /// <summary>
    /// Check if customer can upgrade to a specific membership level
    /// </summary>
    [HttpGet("can-upgrade/{customerId:guid}/{targetMembershipLevelId:guid}")]
    public async Task<ActionResult<bool>> CanUpgradeMembership(Guid customerId, Guid targetMembershipLevelId)
    {
        try
        {
            var canUpgrade = await _membershipService.CanUpgradeMembershipAsync(customerId, targetMembershipLevelId);
            return Ok(canUpgrade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking membership upgrade eligibility for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while checking upgrade eligibility");
        }
    }

    /// <summary>
    /// Get recommended membership level based on total spending
    /// </summary>
    [HttpGet("recommended")]
    public async Task<ActionResult<MembershipLevelDto>> GetRecommendedMembership([FromQuery] decimal totalSpent)
    {
        try
        {
            var level = await _membershipService.GetRecommendedMembershipAsync(totalSpent);
            if (level == null)
            {
                return NotFound("No membership level found for the specified spending amount");
            }

            return Ok(level);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recommended membership level for spending {TotalSpent}", totalSpent);
            return StatusCode(500, "An error occurred while retrieving the recommended membership level");
        }
    }
}
