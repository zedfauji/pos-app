using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerSegmentsController : ControllerBase
{
    private readonly ISegmentationService _segmentationService;
    private readonly ILogger<CustomerSegmentsController> _logger;

    public CustomerSegmentsController(
        ISegmentationService segmentationService,
        ILogger<CustomerSegmentsController> logger)
    {
        _segmentationService = segmentationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all customer segments
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CustomerSegmentDto>>> GetSegments(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var segments = await _segmentationService.GetSegmentsAsync(includeInactive, page, pageSize);
            return Ok(segments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer segments");
            return StatusCode(500, "Internal server error while retrieving segments");
        }
    }

    /// <summary>
    /// Get a specific customer segment by ID
    /// </summary>
    [HttpGet("{segmentId:guid}")]
    public async Task<ActionResult<CustomerSegmentDto>> GetSegment(Guid segmentId)
    {
        try
        {
            var segment = await _segmentationService.GetSegmentAsync(segmentId);
            if (segment == null)
            {
                return NotFound($"Segment with ID {segmentId} not found");
            }

            return Ok(segment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving segment {SegmentId}", segmentId);
            return StatusCode(500, "Internal server error while retrieving segment");
        }
    }

    /// <summary>
    /// Create a new customer segment
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CustomerSegmentDto>> CreateSegment([FromBody] CustomerSegmentDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var segment = await _segmentationService.CreateSegmentAsync(request);
            return CreatedAtAction(nameof(GetSegment), new { segmentId = segment.SegmentId }, segment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid segment creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer segment");
            return StatusCode(500, "Internal server error while creating segment");
        }
    }

    /// <summary>
    /// Update an existing customer segment
    /// </summary>
    [HttpPut("{segmentId:guid}")]
    public async Task<ActionResult<CustomerSegmentDto>> UpdateSegment(Guid segmentId, [FromBody] CustomerSegmentDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var segment = await _segmentationService.UpdateSegmentAsync(segmentId, request);
            if (segment == null)
            {
                return NotFound($"Segment with ID {segmentId} not found");
            }

            return Ok(segment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid segment update request for {SegmentId}", segmentId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating segment {SegmentId}", segmentId);
            return StatusCode(500, "Internal server error while updating segment");
        }
    }

    /// <summary>
    /// Delete a customer segment
    /// </summary>
    [HttpDelete("{segmentId:guid}")]
    public async Task<IActionResult> DeleteSegment(Guid segmentId)
    {
        try
        {
            var success = await _segmentationService.DeleteSegmentAsync(segmentId);
            if (!success)
            {
                return NotFound($"Segment with ID {segmentId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting segment {SegmentId}", segmentId);
            return StatusCode(500, "Internal server error while deleting segment");
        }
    }

    /// <summary>
    /// Refresh segment membership for all customers
    /// </summary>
    [HttpPost("{segmentId:guid}/refresh")]
    public async Task<IActionResult> RefreshSegmentMembership(Guid segmentId)
    {
        try
        {
            var success = await _segmentationService.RefreshSegmentMembershipAsync(segmentId);
            if (!success)
            {
                return NotFound($"Segment with ID {segmentId} not found");
            }

            return Ok(new { message = "Segment membership refreshed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing segment membership for {SegmentId}", segmentId);
            return StatusCode(500, "Internal server error while refreshing segment membership");
        }
    }

    /// <summary>
    /// Get customers in a specific segment
    /// </summary>
    [HttpGet("{segmentId:guid}/customers")]
    public async Task<ActionResult<List<CustomerDto>>> GetSegmentCustomers(
        Guid segmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var customers = await _segmentationService.GetSegmentCustomersAsync(segmentId, page, pageSize);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers for segment {SegmentId}", segmentId);
            return StatusCode(500, "Internal server error while retrieving segment customers");
        }
    }

    /// <summary>
    /// Get segment analytics and statistics
    /// </summary>
    [HttpGet("{segmentId:guid}/analytics")]
    public async Task<ActionResult<Dictionary<string, object>>> GetSegmentAnalytics(Guid segmentId)
    {
        try
        {
            var analytics = await _segmentationService.GetSegmentAnalyticsAsync(segmentId);
            if (analytics == null)
            {
                return NotFound($"Segment with ID {segmentId} not found");
            }

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics for segment {SegmentId}", segmentId);
            return StatusCode(500, "Internal server error while retrieving segment analytics");
        }
    }

    /// <summary>
    /// Test segment criteria without creating a segment
    /// </summary>
    [HttpPost("test-criteria")]
    public async Task<ActionResult<List<CustomerDto>>> TestSegmentCriteria([FromBody] CustomerSegmentDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CriteriaJson))
            {
                return BadRequest("Criteria JSON is required for testing");
            }

            // Create a temporary segment for testing purposes
            var testSegment = new CustomerSegmentDto
            {
                Name = "Test Segment",
                Description = "Temporary segment for criteria testing",
                CriteriaJson = request.CriteriaJson,
                IsActive = true
            };

            var customers = await _segmentationService.GetCustomersByCriteriaAsync(request.CriteriaJson, 1, 100);
            return Ok(customers);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid criteria for segment testing");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing segment criteria");
            return StatusCode(500, "Internal server error while testing segment criteria");
        }
    }

    /// <summary>
    /// Get available segment criteria options
    /// </summary>
    [HttpGet("criteria-options")]
    public ActionResult<Dictionary<string, object>> GetCriteriaOptions()
    {
        try
        {
            var options = new Dictionary<string, object>
            {
                ["comparison_operators"] = new[] { "equals", "not_equals", "greater_than", "less_than", "greater_than_or_equal", "less_than_or_equal", "contains", "starts_with", "ends_with", "in", "not_in" },
                ["logical_operators"] = new[] { "and", "or" },
                ["customer_fields"] = new[]
                {
                    new { field = "total_spent", type = "decimal", description = "Total amount spent by customer" },
                    new { field = "loyalty_points", type = "integer", description = "Current loyalty points balance" },
                    new { field = "membership_level_id", type = "guid", description = "Membership level ID" },
                    new { field = "registration_date", type = "datetime", description = "Customer registration date" },
                    new { field = "last_visit_date", type = "datetime", description = "Last visit date" },
                    new { field = "visit_count", type = "integer", description = "Total number of visits" },
                    new { field = "average_order_value", type = "decimal", description = "Average order value" },
                    new { field = "days_since_last_visit", type = "integer", description = "Days since last visit" },
                    new { field = "city", type = "string", description = "Customer city" },
                    new { field = "province", type = "string", description = "Customer province/state" },
                    new { field = "age_group", type = "string", description = "Customer age group" }
                }
            };

            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving criteria options");
            return StatusCode(500, "Internal server error while retrieving criteria options");
        }
    }
}
