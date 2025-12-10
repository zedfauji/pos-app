using Microsoft.EntityFrameworkCore;
using DiscountApi.Data;
using DiscountApi.Models;

namespace DiscountApi.Services;

public interface IDiscountService
{
    Task<List<object>> GetAvailableDiscountsAsync(Guid customerId, string billingId);
    Task<DiscountApplicationResult> ApplyDiscountAsync(DiscountApplicationRequest request);
    Task<bool> RemoveDiscountAsync(string billingId, string discountId);
    Task<List<AppliedDiscount>> GetAppliedDiscountsAsync(string billingId);
    Task<object> AutoApplyDiscountsAsync(string billingId, Guid customerId);
}

public class DiscountService : IDiscountService
{
    private readonly DiscountDbContext _context;
    private readonly ICustomerAnalysisService _customerAnalysisService;

    public DiscountService(DiscountDbContext context, ICustomerAnalysisService customerAnalysisService)
    {
        _context = context;
        _customerAnalysisService = customerAnalysisService;
    }

    public async Task<List<object>> GetAvailableDiscountsAsync(Guid customerId, string billingId)
    {
        var discounts = new List<object>();
        var customerHistory = await _customerAnalysisService.GetCustomerHistoryAsync(customerId);

        // New customer discount
        if (customerHistory.TotalVisits <= 1)
        {
            discounts.Add(new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "NewCustomer",
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
            discounts.Add(new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "ReturningCustomer",
                Name = "Welcome Back",
                Description = "10% off - We missed you!",
                DiscountPercentage = 10m,
                IsAutoApply = true,
                Priority = 2
            });
        }

        // VIP customer discount
        if (customerHistory.TotalSpent > 1000m)
        {
            discounts.Add(new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "VIP",
                Name = "VIP Member",
                Description = "25% off as our valued VIP customer",
                DiscountPercentage = 25m,
                IsAutoApply = false,
                Priority = 1
            });
        }

        // Frequent customer discount
        if (customerHistory.TotalVisits >= 10)
        {
            discounts.Add(new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "Frequent",
                Name = "Frequent Customer",
                Description = "15% off for our loyal customers",
                DiscountPercentage = 15m,
                IsAutoApply = false,
                Priority = 2
            });
        }

        // Birthday discount (mock - would need actual birth date)
        if (DateTime.Now.Day % 10 == customerId.GetHashCode() % 10) // Mock birthday logic
        {
            discounts.Add(new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "Birthday",
                Name = "Birthday Special",
                Description = "Happy Birthday! 20% off your order",
                DiscountPercentage = 20m,
                IsAutoApply = false,
                Priority = 1
            });
        }

        // Get active vouchers
        var vouchers = await _context.Vouchers
            .Where(v => v.IsActive && !v.IsRedeemed && v.ExpiryDate > DateTime.UtcNow)
            .Take(5)
            .ToListAsync();

        foreach (var voucher in vouchers)
        {
            discounts.Add(new
            {
                Id = voucher.VoucherId.ToString(),
                Type = "Voucher",
                Name = voucher.Name,
                Description = voucher.Description,
                DiscountPercentage = voucher.DiscountPercentage,
                DiscountAmount = voucher.DiscountAmount,
                IsAutoApply = false,
                Priority = 3,
                VoucherCode = voucher.Code
            });
        }

        return discounts.OrderBy(d => ((dynamic)d).Priority).ToList();
    }

    public async Task<DiscountApplicationResult> ApplyDiscountAsync(DiscountApplicationRequest request)
    {
        try
        {
            // Mock calculation - in real implementation, would calculate based on order total
            var discountAmount = 10.00m; // Mock discount amount
            var newTotal = 90.00m; // Mock new total

            var appliedDiscount = new AppliedDiscount
            {
                BillingId = request.BillingId,
                DiscountId = request.DiscountId,
                DiscountType = request.DiscountType,
                Name = GetDiscountName(request.DiscountType),
                DiscountAmount = discountAmount,
                DiscountPercentage = 10m,
                AppliedAt = request.AppliedAt
            };

            _context.AppliedDiscounts.Add(appliedDiscount);
            await _context.SaveChangesAsync();

            return new DiscountApplicationResult
            {
                Success = true,
                Message = "Discount applied successfully",
                DiscountAmount = discountAmount,
                NewTotal = newTotal
            };
        }
        catch (Exception ex)
        {
            return new DiscountApplicationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> RemoveDiscountAsync(string billingId, string discountId)
    {
        try
        {
            var appliedDiscount = await _context.AppliedDiscounts
                .FirstOrDefaultAsync(ad => ad.BillingId == billingId && ad.DiscountId == discountId);

            if (appliedDiscount != null)
            {
                _context.AppliedDiscounts.Remove(appliedDiscount);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<AppliedDiscount>> GetAppliedDiscountsAsync(string billingId)
    {
        return await _context.AppliedDiscounts
            .Where(ad => ad.BillingId == billingId)
            .OrderBy(ad => ad.AppliedAt)
            .ToListAsync();
    }

    public async Task<object> AutoApplyDiscountsAsync(string billingId, Guid customerId)
    {
        var availableDiscounts = await GetAvailableDiscountsAsync(customerId, billingId);
        var autoDiscounts = availableDiscounts.Where(d => ((dynamic)d).IsAutoApply).ToList();

        var results = new List<DiscountApplicationResult>();
        decimal totalSavings = 0;

        foreach (var discount in autoDiscounts)
        {
            var request = new DiscountApplicationRequest
            {
                BillingId = billingId,
                DiscountId = ((dynamic)discount).Id,
                DiscountType = ((dynamic)discount).Type,
                AppliedAt = DateTime.UtcNow
            };

            var result = await ApplyDiscountAsync(request);
            results.Add(result);

            if (result.Success)
            {
                totalSavings += result.DiscountAmount;
            }
        }

        return new
        {
            Success = results.Any(r => r.Success),
            AppliedDiscounts = results.Count(r => r.Success),
            TotalSavings = totalSavings,
            Results = results
        };
    }

    private string GetDiscountName(string discountType)
    {
        return discountType switch
        {
            "NewCustomer" => "Welcome New Customer",
            "ReturningCustomer" => "Welcome Back",
            "VIP" => "VIP Member",
            "Frequent" => "Frequent Customer",
            "Birthday" => "Birthday Special",
            _ => "Discount"
        };
    }
}

public interface ICustomerAnalysisService
{
    Task<CustomerHistory> GetCustomerHistoryAsync(Guid customerId);
}

public class CustomerAnalysisService : ICustomerAnalysisService
{
    private readonly DiscountDbContext _context;

    public CustomerAnalysisService(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerHistory> GetCustomerHistoryAsync(Guid customerId)
    {
        var history = await _context.CustomerHistories.FindAsync(customerId);
        
        if (history == null)
        {
            // Create mock history for new customers
            history = new CustomerHistory
            {
                CustomerId = customerId,
                TotalVisits = 1,
                TotalSpent = 0,
                AverageOrderValue = 0,
                DaysSinceLastVisit = 0,
                LastVisitDate = DateTime.UtcNow,
                FavoriteItems = "[]",
                PreferredTimeSlot = "Evening"
            };

            _context.CustomerHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        return history;
    }
}

public interface IVoucherService { }
public class VoucherService : IVoucherService { }

public interface IComboService { }
public class ComboService : IComboService { }
