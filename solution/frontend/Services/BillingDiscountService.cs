using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public class BillingDiscountService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BillingDiscountService> _logger;
    private readonly DiscountService _discountService;
    private readonly string _baseUrl;

    public BillingDiscountService(
        HttpClient httpClient, 
        ILogger<BillingDiscountService> logger,
        DiscountService discountService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _discountService = discountService;
        _baseUrl = configuration["OrderApi:BaseUrl"] ?? "https://magidesk-order-904541739138.northamerica-south1.run.app";
    }

    // Apply discount directly to billing
    public async Task<BillingDiscountResultDto> ApplyDiscountToBillingAsync(string billingId, string discountId, DiscountType discountType)
    {
        try
        {
            // First apply the discount through discount service
            var discountResult = await _discountService.ApplyDiscountAsync(billingId, discountId, discountType);
            
            if (!discountResult.Success)
            {
                return new BillingDiscountResultDto
                {
                    Success = false,
                    Message = discountResult.Message
                };
            }

            // Then update the billing record
            var billingUpdate = new BillingDiscountUpdateDto
            {
                BillingId = billingId,
                DiscountId = discountId,
                DiscountType = discountType.ToString(),
                DiscountAmount = discountResult.DiscountAmount,
                NewSubtotal = discountResult.NewTotal,
                AppliedAt = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/billing/{billingId}/discount", billingUpdate);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BillingDiscountResultDto>();
                return result ?? new BillingDiscountResultDto { Success = false, Message = "Failed to parse response" };
            }

            return new BillingDiscountResultDto
            {
                Success = false,
                Message = $"Failed to update billing: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount {DiscountId} to billing {BillingId}", discountId, billingId);
            return new BillingDiscountResultDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // Remove discount from billing
    public async Task<bool> RemoveDiscountFromBillingAsync(string billingId, string discountId)
    {
        try
        {
            // Remove from discount service
            var discountRemoved = await _discountService.RemoveDiscountAsync(billingId, discountId);
            
            // Remove from billing record
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/billing/{billingId}/discount/{discountId}");
            
            return response.IsSuccessStatusCode && discountRemoved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing discount {DiscountId} from billing {BillingId}", discountId, billingId);
            return false;
        }
    }

    // Get billing summary with discounts
    public async Task<BillingSummaryDto> GetBillingSummaryAsync(string billingId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/billing/{billingId}/summary");
            
            if (response.IsSuccessStatusCode)
            {
                var summary = await response.Content.ReadFromJsonAsync<BillingSummaryDto>();
                if (summary != null)
                {
                    // Enrich with discount details
                    var appliedDiscounts = await _discountService.GetAppliedDiscountsAsync(billingId);
                    summary.AppliedDiscounts = appliedDiscounts;
                    return summary;
                }
            }

            return new BillingSummaryDto { BillingId = billingId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing summary for {BillingId}", billingId);
            return new BillingSummaryDto { BillingId = billingId };
        }
    }

    // Calculate discount impact on order
    public async Task<DiscountImpactDto> CalculateDiscountImpactAsync(string billingId, List<string> discountIds)
    {
        try
        {
            var request = new DiscountImpactRequestDto
            {
                BillingId = billingId,
                DiscountIds = discountIds
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/billing/discount-impact", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DiscountImpactDto>() ?? new DiscountImpactDto();
            }

            return new DiscountImpactDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating discount impact for billing {BillingId}", billingId);
            return new DiscountImpactDto();
        }
    }

    // Auto-apply eligible discounts for customer
    public async Task<AutoDiscountResultDto> AutoApplyDiscountsAsync(string billingId, Guid customerId)
    {
        try
        {
            var availableDiscounts = await _discountService.GetAvailableDiscountsAsync(customerId, billingId);
            var autoDiscounts = availableDiscounts.Where(d => d.IsAutoApply).ToList();
            
            var results = new List<BillingDiscountResultDto>();
            decimal totalSavings = 0;

            foreach (var discount in autoDiscounts)
            {
                var result = await ApplyDiscountToBillingAsync(billingId, discount.Id, discount.Type);
                results.Add(result);
                
                if (result.Success)
                {
                    totalSavings += result.DiscountAmount;
                }
            }

            return new AutoDiscountResultDto
            {
                Success = results.Any(r => r.Success),
                AppliedDiscounts = results.Where(r => r.Success).Count(),
                TotalSavings = totalSavings,
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-applying discounts for billing {BillingId}", billingId);
            return new AutoDiscountResultDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // Validate discount eligibility
    public async Task<DiscountEligibilityDto> ValidateDiscountEligibilityAsync(string billingId, Guid customerId, string discountId)
    {
        try
        {
            var request = new DiscountEligibilityRequestDto
            {
                BillingId = billingId,
                CustomerId = customerId,
                DiscountId = discountId
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/billing/validate-discount", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DiscountEligibilityDto>() ?? new DiscountEligibilityDto { IsEligible = false };
            }

            return new DiscountEligibilityDto { IsEligible = false, Reason = "API call failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating discount eligibility");
            return new DiscountEligibilityDto { IsEligible = false, Reason = ex.Message };
        }
    }
}

// DTOs for billing integration
public record BillingDiscountUpdateDto
{
    public string BillingId { get; init; } = string.Empty;
    public string DiscountId { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public decimal NewSubtotal { get; init; }
    public DateTime AppliedAt { get; init; }
}

public record BillingDiscountResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public decimal NewSubtotal { get; init; }
    public decimal NewTax { get; init; }
    public decimal NewTotal { get; init; }
}

public record BillingSummaryDto
{
    public string BillingId { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalDiscounts { get; init; }
    public decimal FinalTotal { get; init; }
    public List<BillingItemDto> Items { get; init; } = new();
    public List<AppliedDiscountDto> AppliedDiscounts { get; set; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record BillingItemDto
{
    public string ItemId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public List<string> Modifiers { get; init; } = new();
}

public record DiscountImpactDto
{
    public decimal OriginalTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal NewTotal { get; init; }
    public decimal TaxImpact { get; init; }
    public List<DiscountBreakdownDto> Breakdown { get; init; } = new();
}

public record DiscountBreakdownDto
{
    public string DiscountId { get; init; } = string.Empty;
    public string DiscountName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
}

public record DiscountImpactRequestDto
{
    public string BillingId { get; init; } = string.Empty;
    public List<string> DiscountIds { get; init; } = new();
}

public record AutoDiscountResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int AppliedDiscounts { get; init; }
    public decimal TotalSavings { get; init; }
    public List<BillingDiscountResultDto> Results { get; init; } = new();
}

public record DiscountEligibilityDto
{
    public bool IsEligible { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<string> Requirements { get; init; } = new();
    public decimal MinimumOrderValue { get; init; }
    public DateTime? ValidUntil { get; init; }
}

public record DiscountEligibilityRequestDto
{
    public string BillingId { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string DiscountId { get; init; } = string.Empty;
}
