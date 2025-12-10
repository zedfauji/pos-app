using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BehavioralTriggersController : ControllerBase
{
    private readonly IBehavioralTriggerService _triggerService;
    private readonly ILogger<BehavioralTriggersController> _logger;

    public BehavioralTriggersController(
        IBehavioralTriggerService triggerService,
        ILogger<BehavioralTriggersController> logger)
    {
        _triggerService = triggerService;
        _logger = logger;
    }

    /// <summary>
    /// Get all behavioral triggers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BehavioralTriggerDto>>> GetTriggers(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var triggers = await _triggerService.GetTriggersAsync(includeInactive, page, pageSize);
            return Ok(triggers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving behavioral triggers");
            return StatusCode(500, "Internal server error while retrieving triggers");
        }
    }

    /// <summary>
    /// Get a specific behavioral trigger by ID
    /// </summary>
    [HttpGet("{triggerId:guid}")]
    public async Task<ActionResult<BehavioralTriggerDto>> GetTrigger(Guid triggerId)
    {
        try
        {
            var trigger = await _triggerService.GetTriggerAsync(triggerId);
            if (trigger == null)
            {
                return NotFound($"Trigger with ID {triggerId} not found");
            }

            return Ok(trigger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trigger {TriggerId}", triggerId);
            return StatusCode(500, "Internal server error while retrieving trigger");
        }
    }

    /// <summary>
    /// Create a new behavioral trigger
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BehavioralTriggerDto>> CreateTrigger([FromBody] BehavioralTriggerDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var trigger = await _triggerService.CreateTriggerAsync(request);
            return CreatedAtAction(nameof(GetTrigger), new { triggerId = trigger.TriggerId }, trigger);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid trigger creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating behavioral trigger");
            return StatusCode(500, "Internal server error while creating trigger");
        }
    }

    /// <summary>
    /// Update an existing behavioral trigger
    /// </summary>
    [HttpPut("{triggerId:guid}")]
    public async Task<ActionResult<BehavioralTriggerDto>> UpdateTrigger(Guid triggerId, [FromBody] BehavioralTriggerDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var trigger = await _triggerService.UpdateTriggerAsync(triggerId, request);
            if (trigger == null)
            {
                return NotFound($"Trigger with ID {triggerId} not found");
            }

            return Ok(trigger);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid trigger update request for {TriggerId}", triggerId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trigger {TriggerId}", triggerId);
            return StatusCode(500, "Internal server error while updating trigger");
        }
    }

    /// <summary>
    /// Delete a behavioral trigger
    /// </summary>
    [HttpDelete("{triggerId:guid}")]
    public async Task<IActionResult> DeleteTrigger(Guid triggerId)
    {
        try
        {
            var success = await _triggerService.DeleteTriggerAsync(triggerId);
            if (!success)
            {
                return NotFound($"Trigger with ID {triggerId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting trigger {TriggerId}", triggerId);
            return StatusCode(500, "Internal server error while deleting trigger");
        }
    }

    /// <summary>
    /// Process triggers for a specific customer
    /// </summary>
    [HttpPost("process/{customerId:guid}")]
    public async Task<ActionResult<List<TriggerExecutionDto>>> ProcessCustomerTriggers(Guid customerId)
    {
        try
        {
            var executions = await _triggerService.ProcessCustomerTriggersAsync(customerId);
            return Ok(executions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing triggers for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error while processing customer triggers");
        }
    }

    /// <summary>
    /// Process all active triggers for all eligible customers
    /// </summary>
    [HttpPost("process-all")]
    public async Task<ActionResult<Dictionary<string, object>>> ProcessAllTriggers()
    {
        try
        {
            var result = await _triggerService.ProcessAllTriggersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing all triggers");
            return StatusCode(500, "Internal server error while processing all triggers");
        }
    }

    /// <summary>
    /// Get trigger execution history
    /// </summary>
    [HttpGet("{triggerId:guid}/executions")]
    public async Task<ActionResult<List<TriggerExecutionDto>>> GetTriggerExecutions(
        Guid triggerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var executions = await _triggerService.GetTriggerExecutionsAsync(triggerId, page, pageSize);
            return Ok(executions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving executions for trigger {TriggerId}", triggerId);
            return StatusCode(500, "Internal server error while retrieving trigger executions");
        }
    }

    /// <summary>
    /// Get trigger analytics and performance metrics
    /// </summary>
    [HttpGet("{triggerId:guid}/analytics")]
    public async Task<ActionResult<Dictionary<string, object>>> GetTriggerAnalytics(Guid triggerId)
    {
        try
        {
            var analytics = await _triggerService.GetTriggerAnalyticsAsync(triggerId);
            if (analytics == null)
            {
                return NotFound($"Trigger with ID {triggerId} not found");
            }

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics for trigger {TriggerId}", triggerId);
            return StatusCode(500, "Internal server error while retrieving trigger analytics");
        }
    }

    /// <summary>
    /// Test trigger conditions without executing actions
    /// </summary>
    [HttpPost("test-conditions")]
    public async Task<ActionResult<List<CustomerDto>>> TestTriggerConditions([FromBody] BehavioralTriggerDto request)
    {
        try
        {
            // Convert trigger DTO to conditions JSON for testing
            var conditionsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ConditionType = request.ConditionType,
                ConditionValue = request.ConditionValue,
                ConditionDays = request.ConditionDays
            });

            var customers = await _triggerService.GetCustomersMatchingConditionsAsync(conditionsJson, 1, 100);
            return Ok(customers);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid conditions for trigger testing");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing trigger conditions");
            return StatusCode(500, "Internal server error while testing trigger conditions");
        }
    }

    /// <summary>
    /// Get available trigger condition and action options
    /// </summary>
    [HttpGet("options")]
    public ActionResult<Dictionary<string, object>> GetTriggerOptions()
    {
        try
        {
            var options = new Dictionary<string, object>
            {
                ["trigger_types"] = new[]
                {
                    new { value = "CustomerEvent", description = "Triggered by customer actions (order, payment, registration)" },
                    new { value = "ScheduledCheck", description = "Triggered by scheduled evaluation of customer data" },
                    new { value = "WalletThreshold", description = "Triggered when wallet balance crosses threshold" },
                    new { value = "LoyaltyMilestone", description = "Triggered when loyalty points reach milestone" },
                    new { value = "MembershipChange", description = "Triggered when membership level changes" }
                },
                ["condition_operators"] = new[] { "equals", "not_equals", "greater_than", "less_than", "greater_than_or_equal", "less_than_or_equal", "contains", "in", "not_in" },
                ["logical_operators"] = new[] { "and", "or" },
                ["action_types"] = new[]
                {
                    new { value = "SendNotification", description = "Send notification via email, SMS, or WhatsApp" },
                    new { value = "AddToSegment", description = "Add customer to a specific segment" },
                    new { value = "RemoveFromSegment", description = "Remove customer from a specific segment" },
                    new { value = "GrantLoyaltyPoints", description = "Award loyalty points to customer" },
                    new { value = "AddWalletFunds", description = "Add funds to customer wallet" },
                    new { value = "CreateCampaign", description = "Create and execute a campaign for customer" },
                    new { value = "UpdateMembership", description = "Change customer membership level" }
                },
                ["customer_data_fields"] = new[]
                {
                    new { field = "total_spent", type = "decimal", description = "Total amount spent by customer" },
                    new { field = "loyalty_points", type = "integer", description = "Current loyalty points balance" },
                    new { field = "wallet_balance", type = "decimal", description = "Current wallet balance" },
                    new { field = "membership_level_id", type = "guid", description = "Membership level ID" },
                    new { field = "visit_count", type = "integer", description = "Total number of visits" },
                    new { field = "days_since_last_visit", type = "integer", description = "Days since last visit" },
                    new { field = "average_order_value", type = "decimal", description = "Average order value" },
                    new { field = "last_order_date", type = "datetime", description = "Date of last order" },
                    new { field = "registration_date", type = "datetime", description = "Customer registration date" }
                }
            };

            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trigger options");
            return StatusCode(500, "Internal server error while retrieving trigger options");
        }
    }

    /// <summary>
    /// Enable or disable a trigger
    /// </summary>
    [HttpPatch("{triggerId:guid}/status")]
    public async Task<ActionResult<BehavioralTriggerDto>> UpdateTriggerStatus(
        Guid triggerId, 
        [FromBody] UpdateTriggerStatusRequest request)
    {
        try
        {
            var trigger = await _triggerService.GetTriggerAsync(triggerId);
            if (trigger == null)
            {
                return NotFound($"Trigger with ID {triggerId} not found");
            }

            var result = trigger;
            if (request.IsActive != result.IsActive)
            {
                result = result with { IsActive = request.IsActive };
            }
            var updatedTrigger = await _triggerService.UpdateTriggerAsync(triggerId, result);

            return Ok(updatedTrigger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trigger status for {TriggerId}", triggerId);
            return StatusCode(500, "Internal server error while updating trigger status");
        }
    }
}

public class UpdateTriggerStatusRequest
{
    public bool IsActive { get; set; }
}
