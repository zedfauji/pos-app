using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        ICampaignService campaignService,
        ILogger<CampaignsController> logger)
    {
        _campaignService = campaignService;
        _logger = logger;
    }

    /// <summary>
    /// Get all campaigns
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CampaignDto>>> GetCampaigns(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var campaigns = await _campaignService.GetCampaignsAsync(includeInactive, page, pageSize);
            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns");
            return StatusCode(500, "Internal server error while retrieving campaigns");
        }
    }

    /// <summary>
    /// Get a specific campaign by ID
    /// </summary>
    [HttpGet("{campaignId:guid}")]
    public async Task<ActionResult<CampaignDto>> GetCampaign(Guid campaignId)
    {
        try
        {
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
            {
                return NotFound($"Campaign with ID {campaignId} not found");
            }

            return Ok(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaign {CampaignId}", campaignId);
            return StatusCode(500, "Internal server error while retrieving campaign");
        }
    }

    /// <summary>
    /// Create a new campaign
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CampaignDto>> CreateCampaign([FromBody] CampaignDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var campaign = await _campaignService.CreateCampaignAsync(request);
            return CreatedAtAction(nameof(GetCampaign), new { campaignId = campaign.CampaignId }, campaign);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid campaign creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return StatusCode(500, "Internal server error while creating campaign");
        }
    }

    /// <summary>
    /// Update an existing campaign
    /// </summary>
    [HttpPut("{campaignId:guid}")]
    public async Task<ActionResult<CampaignDto>> UpdateCampaign(Guid campaignId, [FromBody] CampaignDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var campaign = await _campaignService.UpdateCampaignAsync(campaignId, request);
            if (campaign == null)
            {
                return NotFound($"Campaign with ID {campaignId} not found");
            }

            return Ok(campaign);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid campaign update request for {CampaignId}", campaignId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign {CampaignId}", campaignId);
            return StatusCode(500, "Internal server error while updating campaign");
        }
    }

    /// <summary>
    /// Delete a campaign
    /// </summary>
    [HttpDelete("{campaignId:guid}")]
    public async Task<IActionResult> DeleteCampaign(Guid campaignId)
    {
        try
        {
            var success = await _campaignService.DeleteCampaignAsync(campaignId);
            if (!success)
            {
                return NotFound($"Campaign with ID {campaignId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting campaign {CampaignId}", campaignId);
            return StatusCode(500, "Internal server error while deleting campaign");
        }
    }

    /// <summary>
    /// Execute a campaign for a specific customer
    /// </summary>
    [HttpPost("{campaignId:guid}/execute/{customerId:guid}")]
    public async Task<ActionResult<CampaignExecutionDto>> ExecuteCampaignForCustomer(Guid campaignId, Guid customerId)
    {
        try
        {
            var execution = await _campaignService.ExecuteCampaignForCustomerAsync(campaignId, customerId);
            if (execution == null)
            {
                return BadRequest("Campaign execution failed - customer may not be eligible or campaign may be inactive");
            }

            return Ok(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing campaign {CampaignId} for customer {CustomerId}", campaignId, customerId);
            return StatusCode(500, "Internal server error while executing campaign");
        }
    }

    /// <summary>
    /// Execute a campaign for all customers in a segment
    /// </summary>
    [HttpPost("{campaignId:guid}/execute/segment/{segmentId:guid}")]
    public async Task<ActionResult<Dictionary<string, object>>> ExecuteCampaignForSegment(Guid campaignId, Guid segmentId)
    {
        try
        {
            var result = await _campaignService.ExecuteCampaignForSegmentAsync(campaignId, segmentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing campaign {CampaignId} for segment {SegmentId}", campaignId, segmentId);
            return StatusCode(500, "Internal server error while executing campaign for segment");
        }
    }

    /// <summary>
    /// Redeem a campaign offer for a customer
    /// </summary>
    [HttpPost("{campaignId:guid}/redeem/{customerId:guid}")]
    public async Task<ActionResult<CampaignRedemptionDto>> RedeemCampaign(
        Guid campaignId, 
        Guid customerId,
        [FromBody] RedeemCampaignRequest request)
    {
        try
        {
            var redemption = await _campaignService.RedeemCampaignAsync(campaignId, customerId, request.OrderId, request.UsedAmount);
            if (redemption == null)
            {
                return BadRequest("Campaign redemption failed - customer may not be eligible or campaign may be expired");
            }

            return Ok(redemption);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming campaign {CampaignId} for customer {CustomerId}", campaignId, customerId);
            return StatusCode(500, "Internal server error while redeeming campaign");
        }
    }

    /// <summary>
    /// Check if a customer is eligible for a campaign
    /// </summary>
    [HttpGet("{campaignId:guid}/eligibility/{customerId:guid}")]
    public async Task<ActionResult<CampaignEligibilityResponse>> CheckCampaignEligibility(Guid campaignId, Guid customerId)
    {
        try
        {
            var isEligible = await _campaignService.IsCustomerEligibleForCampaignAsync(campaignId, customerId);
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            
            if (campaign == null)
            {
                return NotFound($"Campaign with ID {campaignId} not found");
            }

            return Ok(new CampaignEligibilityResponse
            {
                CampaignId = campaignId,
                CustomerId = customerId,
                IsEligible = isEligible,
                CampaignName = campaign.Name,
                CampaignType = campaign.CampaignType.ToString(),
                ValidFrom = campaign.StartDate,
                ValidTo = campaign.EndDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking eligibility for campaign {CampaignId} and customer {CustomerId}", campaignId, customerId);
            return StatusCode(500, "Internal server error while checking campaign eligibility");
        }
    }

    /// <summary>
    /// Get campaign executions
    /// </summary>
    [HttpGet("{campaignId:guid}/executions")]
    public async Task<ActionResult<List<CampaignExecutionDto>>> GetCampaignExecutions(
        Guid campaignId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var executions = await _campaignService.GetCampaignExecutionsAsync(campaignId, page, pageSize);
            return Ok(executions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving executions for campaign {CampaignId}", campaignId);
            return StatusCode(500, "Internal server error while retrieving campaign executions");
        }
    }

    /// <summary>
    /// Get campaign redemptions
    /// </summary>
    [HttpGet("{campaignId:guid}/redemptions")]
    public async Task<ActionResult<List<CampaignRedemptionDto>>> GetCampaignRedemptions(
        Guid campaignId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var redemptions = await _campaignService.GetCampaignRedemptionsAsync(campaignId, page, pageSize);
            return Ok(redemptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving redemptions for campaign {CampaignId}", campaignId);
            return StatusCode(500, "Internal server error while retrieving campaign redemptions");
        }
    }

    /// <summary>
    /// Get campaign analytics and performance metrics
    /// </summary>
    [HttpGet("{campaignId:guid}/analytics")]
    public async Task<ActionResult<Dictionary<string, object>>> GetCampaignAnalytics(Guid campaignId)
    {
        try
        {
            var analytics = await _campaignService.GetCampaignAnalyticsAsync(campaignId);
            if (analytics == null)
            {
                return NotFound($"Campaign with ID {campaignId} not found");
            }

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics for campaign {CampaignId}", campaignId);
            return StatusCode(500, "Internal server error while retrieving campaign analytics");
        }
    }

    /// <summary>
    /// Get available campaign options and configurations
    /// </summary>
    [HttpGet("options")]
    public ActionResult<Dictionary<string, object>> GetCampaignOptions()
    {
        try
        {
            var options = new Dictionary<string, object>
            {
                ["campaign_types"] = new[]
                {
                    new { value = "PercentageDiscount", description = "Percentage discount on order total" },
                    new { value = "FixedAmountDiscount", description = "Fixed amount discount" },
                    new { value = "FreeItem", description = "Free item with purchase" },
                    new { value = "LoyaltyBonus", description = "Bonus loyalty points" },
                    new { value = "WalletCredit", description = "Wallet credit addition" },
                    new { value = "BuyOneGetOne", description = "Buy one get one free" },
                    new { value = "MinimumSpendDiscount", description = "Discount with minimum spend requirement" }
                },
                ["campaign_statuses"] = new[]
                {
                    new { value = "Draft", description = "Campaign being prepared" },
                    new { value = "Scheduled", description = "Campaign scheduled to start" },
                    new { value = "Active", description = "Campaign currently running" },
                    new { value = "Paused", description = "Campaign temporarily paused" },
                    new { value = "Completed", description = "Campaign ended successfully" },
                    new { value = "Cancelled", description = "Campaign cancelled before completion" }
                },
                ["communication_channels"] = new[]
                {
                    new { value = "Email", description = "Email notifications" },
                    new { value = "SMS", description = "SMS text messages" },
                    new { value = "WhatsApp", description = "WhatsApp messages" },
                    new { value = "InApp", description = "In-app notifications" },
                    new { value = "Push", description = "Push notifications" }
                },
                ["targeting_criteria"] = new[]
                {
                    new { field = "segment_id", type = "guid", description = "Target specific segment" },
                    new { field = "membership_level_id", type = "guid", description = "Target membership level" },
                    new { field = "min_total_spent", type = "decimal", description = "Minimum total spent" },
                    new { field = "min_loyalty_points", type = "integer", description = "Minimum loyalty points" },
                    new { field = "days_since_last_visit", type = "integer", description = "Days since last visit" },
                    new { field = "registration_date_range", type = "daterange", description = "Registration date range" }
                }
            };

            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaign options");
            return StatusCode(500, "Internal server error while retrieving campaign options");
        }
    }

    /// <summary>
    /// Get campaigns available for a specific customer
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<List<CampaignDto>>> GetCustomerCampaigns(Guid customerId)
    {
        try
        {
            var campaigns = await _campaignService.GetCustomerCampaignsAsync(customerId);
            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error while retrieving customer campaigns");
        }
    }
}

public class RedeemCampaignRequest
{
    public Guid? OrderId { get; set; }
    public decimal? UsedAmount { get; set; }
}

public class CampaignEligibilityResponse
{
    public Guid CampaignId { get; set; }
    public Guid CustomerId { get; set; }
    public bool IsEligible { get; set; }
    public string CampaignName { get; set; } = "";
    public string CampaignType { get; set; } = "";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
