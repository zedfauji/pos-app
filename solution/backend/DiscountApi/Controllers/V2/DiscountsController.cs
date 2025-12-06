using Microsoft.AspNetCore.Mvc;
using DiscountApi.Services;
using DiscountApi.Models;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;

namespace DiscountApi.Controllers.V2;

/// <summary>
/// Version 2 Discounts Controller with RBAC enforcement
/// All endpoints require specific permissions
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;
    private readonly ICustomerAnalysisService _customerAnalysisService;
    private readonly ILogger<DiscountsController> _logger;

    public DiscountsController(
        IDiscountService discountService,
        ICustomerAnalysisService customerAnalysisService,
        ILogger<DiscountsController> logger)
    {
        _discountService = discountService;
        _customerAnalysisService = customerAnalysisService;
        _logger = logger;
    }

    /// <summary>
    /// Get available discounts for customer and billing (requires payment:view permission)
    /// </summary>
    [HttpGet("available/{customerId}/{billingId}")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<List<object>>> GetAvailableDiscounts(Guid customerId, string billingId)
    {
        try
        {
            var discounts = await _discountService.GetAvailableDiscountsAsync(customerId, billingId);
            return Ok(discounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available discounts for customer {CustomerId}, billing {BillingId}", customerId, billingId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Apply discount (requires payment:process permission)
    /// </summary>
    [HttpPost("apply")]
    [RequiresPermission(Permissions.PAYMENT_PROCESS)]
    public async Task<ActionResult<DiscountApplicationResult>> ApplyDiscount([FromBody] DiscountApplicationRequest request)
    {
        try
        {
            var result = await _discountService.ApplyDiscountAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove discount (requires payment:process permission)
    /// </summary>
    [HttpDelete("{billingId}/{discountId}")]
    [RequiresPermission(Permissions.PAYMENT_PROCESS)]
    public async Task<ActionResult<bool>> RemoveDiscount(string billingId, string discountId)
    {
        try
        {
            var result = await _discountService.RemoveDiscountAsync(billingId, discountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing discount {DiscountId} from billing {BillingId}", discountId, billingId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get applied discounts for billing (requires payment:view permission)
    /// </summary>
    [HttpGet("billing/{billingId}")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<List<AppliedDiscount>>> GetAppliedDiscounts(string billingId)
    {
        try
        {
            var discounts = await _discountService.GetAppliedDiscountsAsync(billingId);
            return Ok(discounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applied discounts for billing {BillingId}", billingId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Auto-apply discounts (requires payment:process permission)
    /// </summary>
    [HttpPost("auto-apply")]
    [RequiresPermission(Permissions.PAYMENT_PROCESS)]
    public async Task<ActionResult<object>> AutoApplyDiscounts([FromBody] AutoApplyRequest request)
    {
        try
        {
            var result = await _discountService.AutoApplyDiscountsAsync(request.BillingId, request.CustomerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-applying discounts for customer {CustomerId}, billing {BillingId}", request.CustomerId, request.BillingId);
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

public class AutoApplyRequest
{
    public string BillingId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
}

