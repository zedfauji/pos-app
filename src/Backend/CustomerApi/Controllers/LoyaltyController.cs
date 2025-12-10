using CustomerApi.DTOs;
using CustomerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<LoyaltyController> _logger;

    public LoyaltyController(ILoyaltyService loyaltyService, ILogger<LoyaltyController> logger)
    {
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate loyalty points for an order amount
    /// </summary>
    [HttpGet("calculate-points/{customerId:guid}")]
    public async Task<ActionResult<int>> CalculatePointsForOrder(Guid customerId, [FromQuery] decimal orderAmount)
    {
        try
        {
            var points = await _loyaltyService.CalculatePointsForOrderAsync(customerId, orderAmount);
            return Ok(points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating loyalty points for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while calculating loyalty points");
        }
    }

    /// <summary>
    /// Add loyalty points to customer
    /// </summary>
    [HttpPost("customer/{customerId:guid}/add-points")]
    public async Task<ActionResult> AddLoyaltyPoints(Guid customerId, [FromBody] AddLoyaltyPointsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _loyaltyService.AddLoyaltyPointsAsync(
                customerId, 
                request.Points, 
                request.Description, 
                request.OrderId, 
                request.OrderAmount);

            if (!success)
            {
                return BadRequest("Unable to add loyalty points. Customer may not exist or be inactive.");
            }

            return Ok(new { Message = "Loyalty points added successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding loyalty points for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while adding loyalty points");
        }
    }

    /// <summary>
    /// Redeem loyalty points for customer
    /// </summary>
    [HttpPost("customer/{customerId:guid}/redeem-points")]
    public async Task<ActionResult> RedeemLoyaltyPoints(Guid customerId, [FromBody] RedeemLoyaltyPointsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _loyaltyService.RedeemLoyaltyPointsAsync(
                customerId, 
                request.Points, 
                request.Description, 
                request.OrderId);

            if (!success)
            {
                return BadRequest("Unable to redeem loyalty points. Insufficient points or customer may not exist.");
            }

            return Ok(new { Message = "Loyalty points redeemed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming loyalty points for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while redeeming loyalty points");
        }
    }

    /// <summary>
    /// Check if customer can redeem specified points
    /// </summary>
    [HttpGet("customer/{customerId:guid}/can-redeem/{points:int}")]
    public async Task<ActionResult<bool>> CanRedeemPoints(Guid customerId, int points)
    {
        try
        {
            var canRedeem = await _loyaltyService.CanRedeemPointsAsync(customerId, points);
            return Ok(canRedeem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking loyalty points balance for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while checking loyalty points balance");
        }
    }

    /// <summary>
    /// Get loyalty transactions for customer with pagination
    /// </summary>
    [HttpGet("customer/{customerId:guid}/transactions")]
    public async Task<ActionResult<LoyaltyTransactionResponse>> GetLoyaltyTransactions(
        Guid customerId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (transactions, totalCount) = await _loyaltyService.GetLoyaltyTransactionsAsync(customerId, page, pageSize);

            var response = new LoyaltyTransactionResponse
            {
                Transactions = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loyalty transactions for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving loyalty transactions");
        }
    }

    /// <summary>
    /// Get loyalty statistics for customer
    /// </summary>
    [HttpGet("customer/{customerId:guid}/stats")]
    public async Task<ActionResult<LoyaltyStatsDto>> GetLoyaltyStats(Guid customerId)
    {
        try
        {
            var stats = await _loyaltyService.GetLoyaltyStatsAsync(customerId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loyalty statistics for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving loyalty statistics");
        }
    }

    /// <summary>
    /// Get expiring loyalty points for customer
    /// </summary>
    [HttpGet("customer/{customerId:guid}/expiring-points")]
    public async Task<ActionResult<List<LoyaltyTransactionDto>>> GetExpiringPoints(
        Guid customerId, 
        [FromQuery] int daysAhead = 30)
    {
        try
        {
            var expiringPoints = await _loyaltyService.GetExpiringPointsAsync(customerId, daysAhead);
            return Ok(expiringPoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring points for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving expiring points");
        }
    }

    /// <summary>
    /// Process expired loyalty points (background job endpoint)
    /// </summary>
    [HttpPost("process-expired-points")]
    public async Task<ActionResult> ProcessExpiredPoints()
    {
        try
        {
            var processedCount = await _loyaltyService.ProcessExpiredPointsAsync();
            return Ok(new { ProcessedCount = processedCount, Message = $"Processed {processedCount} expired loyalty point transactions" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired loyalty points");
            return StatusCode(500, "An error occurred while processing expired loyalty points");
        }
    }

    /// <summary>
    /// Process birthday bonuses (background job endpoint)
    /// </summary>
    [HttpPost("process-birthday-bonuses")]
    public async Task<ActionResult> ProcessBirthdayBonuses()
    {
        try
        {
            var processedCount = await _loyaltyService.ProcessBirthdayBonusAsync();
            return Ok(new { ProcessedCount = processedCount, Message = $"Processed birthday bonuses for {processedCount} customers" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing birthday bonuses");
            return StatusCode(500, "An error occurred while processing birthday bonuses");
        }
    }
}
