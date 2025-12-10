using Microsoft.AspNetCore.Mvc;
using DiscountApi.Services;
using DiscountApi.Models;

namespace DiscountApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;
    private readonly ICustomerAnalysisService _customerAnalysisService;

    public DiscountsController(IDiscountService discountService, ICustomerAnalysisService customerAnalysisService)
    {
        _discountService = discountService;
        _customerAnalysisService = customerAnalysisService;
    }

    [HttpGet("available/{customerId}/{billingId}")]
    public async Task<ActionResult<List<object>>> GetAvailableDiscounts(Guid customerId, string billingId)
    {
        try
        {
            var discounts = await _discountService.GetAvailableDiscountsAsync(customerId, billingId);
            return Ok(discounts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("apply")]
    public async Task<ActionResult<DiscountApplicationResult>> ApplyDiscount([FromBody] DiscountApplicationRequest request)
    {
        try
        {
            var result = await _discountService.ApplyDiscountAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpDelete("{billingId}/{discountId}")]
    public async Task<ActionResult<bool>> RemoveDiscount(string billingId, string discountId)
    {
        try
        {
            var result = await _discountService.RemoveDiscountAsync(billingId, discountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("billing/{billingId}")]
    public async Task<ActionResult<List<AppliedDiscount>>> GetAppliedDiscounts(string billingId)
    {
        try
        {
            var discounts = await _discountService.GetAppliedDiscountsAsync(billingId);
            return Ok(discounts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("auto-apply")]
    public async Task<ActionResult<object>> AutoApplyDiscounts([FromBody] AutoApplyRequest request)
    {
        try
        {
            var result = await _discountService.AutoApplyDiscountsAsync(request.BillingId, request.CustomerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

public class AutoApplyRequest
{
    public string BillingId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
}
