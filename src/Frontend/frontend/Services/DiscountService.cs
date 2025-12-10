using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace MagiDesk.Frontend.Services;

public class DiscountService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscountService> _logger;
    private readonly CustomerApiService _customerService;
    private readonly string _baseUrl;

    public DiscountService(HttpClient httpClient, ILogger<DiscountService> logger, CustomerApiService customerService, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _customerService = customerService;
        _baseUrl = configuration["DiscountApi:BaseUrl"] ?? "https://magidesk-discount-904541739138.northamerica-south1.run.app";
    }

    // Get all available discounts for a customer based on their history
    public async Task<List<AvailableDiscountDto>> GetAvailableDiscountsAsync(Guid customerId, string billingId)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer == null) return new List<AvailableDiscountDto>();

            var customerHistory = await GetCustomerHistoryAsync(customerId);
            var availableDiscounts = new List<AvailableDiscountDto>();

            // New customer discount
            if (customerHistory.TotalVisits <= 1)
            {
                availableDiscounts.Add(new AvailableDiscountDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = DiscountType.NewCustomer,
                    Name = "Welcome New Customer",
                    Description = "15% off your first order",
                    DiscountPercentage = 15m,
                    IsAutoApply = true,
                    Priority = 1
                });
            }

            // Returning customer discount
            if (customerHistory.TotalVisits > 1 && customerHistory.DaysSinceLastVisit > 30)
            {
                availableDiscounts.Add(new AvailableDiscountDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = DiscountType.ReturningCustomer,
                    Name = "Welcome Back",
                    Description = "10% off - We missed you!",
                    DiscountPercentage = 10m,
                    IsAutoApply = true,
                    Priority = 2
                });
            }

            // Loyalty tier discounts
            var loyaltyDiscount = GetLoyaltyDiscount(customer, customerHistory);
            if (loyaltyDiscount != null)
            {
                availableDiscounts.Add(loyaltyDiscount);
            }

            // Birthday discount
            if (IsBirthdayMonth(customer.DateOfBirth))
            {
                availableDiscounts.Add(new AvailableDiscountDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = DiscountType.Birthday,
                    Name = "Birthday Special",
                    Description = "Happy Birthday! 20% off your order",
                    DiscountPercentage = 20m,
                    IsAutoApply = false,
                    Priority = 1
                });
            }

            // Get active vouchers
            var vouchers = await GetActiveVouchersAsync(customerId);
            availableDiscounts.AddRange(vouchers);

            // Get combo deals
            var combos = await GetAvailableCombosAsync(billingId);
            availableDiscounts.AddRange(combos);

            return availableDiscounts.OrderBy(d => d.Priority).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available discounts for customer {CustomerId}", customerId);
            return new List<AvailableDiscountDto>();
        }
    }

    // Apply discount to billing
    public async Task<DiscountApplicationResultDto> ApplyDiscountAsync(string billingId, string discountId, DiscountType discountType)
    {
        try
        {
            var request = new ApplyDiscountRequestDto
            {
                BillingId = billingId,
                DiscountId = discountId,
                DiscountType = discountType,
                AppliedAt = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/discounts/apply", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DiscountApplicationResultDto>();
                return result ?? new DiscountApplicationResultDto { Success = false, Message = "Failed to parse response" };
            }

            return new DiscountApplicationResultDto { Success = false, Message = "API call failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount {DiscountId} to billing {BillingId}", discountId, billingId);
            return new DiscountApplicationResultDto { Success = false, Message = ex.Message };
        }
    }

    // Remove discount from billing
    public async Task<bool> RemoveDiscountAsync(string billingId, string discountId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/discounts/{billingId}/{discountId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing discount {DiscountId} from billing {BillingId}", discountId, billingId);
            return false;
        }
    }

    // Get applied discounts for a billing
    public async Task<List<AppliedDiscountDto>> GetAppliedDiscountsAsync(string billingId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/discounts/billing/{billingId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<AppliedDiscountDto>>() ?? new List<AppliedDiscountDto>();
            }
            return new List<AppliedDiscountDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applied discounts for billing {BillingId}", billingId);
            return new List<AppliedDiscountDto>();
        }
    }

    // Voucher Management
    public async Task<List<VoucherDto>> GetCustomerVouchersAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/vouchers/customer/{customerId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<VoucherDto>>() ?? new List<VoucherDto>();
            }
            return new List<VoucherDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vouchers for customer {CustomerId}", customerId);
            return new List<VoucherDto>();
        }
    }

    public async Task<VoucherDto?> RedeemVoucherAsync(string voucherCode, Guid customerId, string billingId)
    {
        try
        {
            var request = new RedeemVoucherRequestDto
            {
                VoucherCode = voucherCode,
                CustomerId = customerId,
                BillingId = billingId,
                RedeemedAt = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/vouchers/redeem", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<VoucherDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming voucher {VoucherCode}", voucherCode);
            return null;
        }
    }

    // Combo Deals
    public async Task<List<ComboDto>> GetAvailableCombosAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/combos/active");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ComboDto>>() ?? new List<ComboDto>();
            }
            return new List<ComboDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available combos");
            return new List<ComboDto>();
        }
    }

    public async Task<ComboApplicationResultDto> ApplyComboAsync(string billingId, string comboId, List<string> selectedItemIds)
    {
        try
        {
            var request = new ApplyComboRequestDto
            {
                BillingId = billingId,
                ComboId = comboId,
                SelectedItemIds = selectedItemIds,
                AppliedAt = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/combos/apply", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ComboApplicationResultDto>();
                return result ?? new ComboApplicationResultDto { Success = false, Message = "Failed to parse response" };
            }

            return new ComboApplicationResultDto { Success = false, Message = "API call failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying combo {ComboId} to billing {BillingId}", comboId, billingId);
            return new ComboApplicationResultDto { Success = false, Message = ex.Message };
        }
    }

    // Private helper methods
    private async Task<CustomerHistoryDto> GetCustomerHistoryAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/customers/{customerId}/history");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CustomerHistoryDto>() ?? new CustomerHistoryDto();
            }
            return new CustomerHistoryDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer history for {CustomerId}", customerId);
            return new CustomerHistoryDto();
        }
    }

    private AvailableDiscountDto? GetLoyaltyDiscount(CustomerDto customer, CustomerHistoryDto history)
    {
        // VIP customers (high spenders)
        if (history.TotalSpent > 1000m)
        {
            return new AvailableDiscountDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = DiscountType.VIP,
                Name = "VIP Member",
                Description = "25% off as our valued VIP customer",
                DiscountPercentage = 25m,
                IsAutoApply = false,
                Priority = 1
            };
        }

        // Frequent customers
        if (history.TotalVisits >= 10)
        {
            return new AvailableDiscountDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = DiscountType.Frequent,
                Name = "Frequent Customer",
                Description = "15% off for our loyal customers",
                DiscountPercentage = 15m,
                IsAutoApply = false,
                Priority = 2
            };
        }

        // Regular customers
        if (history.TotalVisits >= 5)
        {
            return new AvailableDiscountDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = DiscountType.Regular,
                Name = "Regular Customer",
                Description = "10% off for regular customers",
                DiscountPercentage = 10m,
                IsAutoApply = false,
                Priority = 3
            };
        }

        return null;
    }

    private bool IsBirthdayMonth(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return false;
        return dateOfBirth.Value.Month == DateTime.Now.Month;
    }

    private async Task<List<AvailableDiscountDto>> GetActiveVouchersAsync(Guid customerId)
    {
        var vouchers = await GetCustomerVouchersAsync(customerId);
        return vouchers.Where(v => v.IsActive && v.ExpiryDate > DateTime.UtcNow)
                      .Select(v => new AvailableDiscountDto
                      {
                          Id = v.Id,
                          Type = DiscountType.Voucher,
                          Name = v.Name,
                          Description = v.Description,
                          DiscountPercentage = v.DiscountPercentage,
                          DiscountAmount = v.DiscountAmount,
                          IsAutoApply = false,
                          Priority = 3,
                          VoucherCode = v.Code
                      }).ToList();
    }

    private async Task<List<AvailableDiscountDto>> GetAvailableCombosAsync(string billingId)
    {
        var combos = await GetAvailableCombosAsync();
        return combos.Where(c => c.IsActive && c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow)
                    .Select(c => new AvailableDiscountDto
                    {
                        Id = c.Id,
                        Type = DiscountType.Combo,
                        Name = c.Name,
                        Description = c.Description,
                        DiscountAmount = c.DiscountAmount,
                        IsAutoApply = false,
                        Priority = 4,
                        ComboItems = c.Items
                    }).ToList();
    }
}

// DTOs for discount system
public class AvailableDiscountDto
{
    public string Id { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public bool IsAutoApply { get; set; }
    public int Priority { get; set; }
    public string? VoucherCode { get; set; }
    public List<ComboItemDto>? ComboItems { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public record AppliedDiscountDto
{
    public string Id { get; init; } = string.Empty;
    public string BillingId { get; init; } = string.Empty;
    public DiscountType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public decimal DiscountPercentage { get; init; }
    public DateTime AppliedAt { get; init; }
    public string AppliedBy { get; init; } = string.Empty;
}

public record CustomerHistoryDto
{
    public int TotalVisits { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int DaysSinceLastVisit { get; init; }
    public DateTime? LastVisitDate { get; init; }
    public List<string> FavoriteItems { get; init; } = new();
    public string PreferredTimeSlot { get; init; } = string.Empty;
}

public record VoucherDto
{
    public string Id { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal? DiscountPercentage { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? MinimumOrderValue { get; init; }
    public DateTime ExpiryDate { get; init; }
    public bool IsActive { get; init; }
    public bool IsRedeemed { get; init; }
    public DateTime? RedeemedAt { get; init; }
}

public record ComboDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<ComboItemDto> Items { get; init; } = new();
    public decimal OriginalPrice { get; init; }
    public decimal ComboPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; }
}

public class ComboItemDto
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public bool IsRequired { get; set; }
    public List<string> Alternatives { get; set; } = new();
}

public record ApplyDiscountRequestDto
{
    public string BillingId { get; init; } = string.Empty;
    public string DiscountId { get; init; } = string.Empty;
    public DiscountType DiscountType { get; init; }
    public DateTime AppliedAt { get; init; }
}

public record DiscountApplicationResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public decimal NewTotal { get; init; }
}

public record RedeemVoucherRequestDto
{
    public string VoucherCode { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string BillingId { get; init; } = string.Empty;
    public DateTime RedeemedAt { get; init; }
}

public record ApplyComboRequestDto
{
    public string BillingId { get; init; } = string.Empty;
    public string ComboId { get; init; } = string.Empty;
    public List<string> SelectedItemIds { get; init; } = new();
    public DateTime AppliedAt { get; init; }
}

public record ComboApplicationResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public decimal NewTotal { get; init; }
    public List<string> AppliedItems { get; init; } = new();
}

public enum DiscountType
{
    NewCustomer,
    ReturningCustomer,
    Birthday,
    VIP,
    Frequent,
    Regular,
    Voucher,
    Combo,
    Seasonal,
    HappyHour,
    GroupDiscount,
    StudentDiscount,
    SeniorDiscount,
    EmployeeDiscount
}
