using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Services;

public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<CustomerService> _logger;
    private readonly IMembershipService _membershipService;
    private readonly IWalletService _walletService;
    private readonly ILoyaltyService _loyaltyService;

    public CustomerService(
        CustomerDbContext context,
        ILogger<CustomerService> logger,
        IMembershipService membershipService,
        IWalletService walletService,
        ILoyaltyService loyaltyService)
    {
        _context = context;
        _logger = logger;
        _membershipService = membershipService;
        _walletService = walletService;
        _loyaltyService = loyaltyService;
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phone)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.Phone == phone);

        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto?> GetCustomerByEmailAsync(string email)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.Email == email);

        return customer == null ? null : MapToDto(customer);
    }

    public async Task<(List<CustomerDto> Customers, int TotalCount)> SearchCustomersAsync(CustomerSearchRequest request)
    {
        var query = _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(searchTerm) ||
                c.LastName.ToLower().Contains(searchTerm) ||
                (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                (c.Email != null && c.Email.ToLower().Contains(searchTerm)));
        }

        if (request.MembershipLevelId.HasValue)
            query = query.Where(c => c.MembershipLevelId == request.MembershipLevelId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        if (request.IsMembershipExpired.HasValue)
        {
            var now = DateTime.UtcNow;
            if (request.IsMembershipExpired.Value)
                query = query.Where(c => c.MembershipExpiryDate.HasValue && c.MembershipExpiryDate.Value < now);
            else
                query = query.Where(c => !c.MembershipExpiryDate.HasValue || c.MembershipExpiryDate.Value >= now);
        }

        if (request.CreatedAfter.HasValue)
            query = query.Where(c => c.CreatedAt >= request.CreatedAfter.Value);

        if (request.CreatedBefore.HasValue)
            query = query.Where(c => c.CreatedAt <= request.CreatedBefore.Value);

        if (request.MinTotalSpent.HasValue)
            query = query.Where(c => c.TotalSpent >= request.MinTotalSpent.Value);

        if (request.MaxTotalSpent.HasValue)
            query = query.Where(c => c.TotalSpent <= request.MaxTotalSpent.Value);

        if (request.MinLoyaltyPoints.HasValue)
            query = query.Where(c => c.LoyaltyPoints >= request.MinLoyaltyPoints.Value);

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "firstname" => request.SortDescending ? query.OrderByDescending(c => c.FirstName) : query.OrderBy(c => c.FirstName),
            "lastname" => request.SortDescending ? query.OrderByDescending(c => c.LastName) : query.OrderBy(c => c.LastName),
            "email" => request.SortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "phone" => request.SortDescending ? query.OrderByDescending(c => c.Phone) : query.OrderBy(c => c.Phone),
            "totalspent" => request.SortDescending ? query.OrderByDescending(c => c.TotalSpent) : query.OrderBy(c => c.TotalSpent),
            "loyaltypoints" => request.SortDescending ? query.OrderByDescending(c => c.LoyaltyPoints) : query.OrderBy(c => c.LoyaltyPoints),
            "createdat" => request.SortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _ => request.SortDescending ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName) : query.OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
        };

        // Apply pagination
        var customers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (customers.Select(MapToDto).ToList(), totalCount);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Get default membership level if not specified
            var membershipLevelId = request.MembershipLevelId ?? 
                (await _membershipService.GetDefaultMembershipLevelAsync()).MembershipLevelId;

            var customer = new Customer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Email = request.Email,
                DateOfBirth = request.DateOfBirth,
                PhotoUrl = request.PhotoUrl,
                MembershipLevelId = membershipLevelId,
                MembershipExpiryDate = request.MembershipExpiryDate,
                Notes = request.Notes
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Create wallet for the customer
            await _walletService.CreateWalletAsync(customer.CustomerId);

            await transaction.CommitAsync();

            _logger.LogInformation("Created customer {CustomerId} - {FullName}", customer.CustomerId, customer.FullName);

            // Reload with includes
            var createdCustomer = await _context.Customers
                .Include(c => c.MembershipLevel)
                .Include(c => c.Wallet)
                .FirstAsync(c => c.CustomerId == customer.CustomerId);

            return MapToDto(createdCustomer);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return null;

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            customer.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName))
            customer.LastName = request.LastName;

        if (request.Phone != null)
            customer.Phone = request.Phone;

        if (request.Email != null)
            customer.Email = request.Email;

        if (request.DateOfBirth.HasValue)
            customer.DateOfBirth = request.DateOfBirth;

        if (request.PhotoUrl != null)
            customer.PhotoUrl = request.PhotoUrl;

        if (request.MembershipLevelId.HasValue)
            customer.MembershipLevelId = request.MembershipLevelId.Value;

        if (request.MembershipExpiryDate.HasValue)
            customer.MembershipExpiryDate = request.MembershipExpiryDate;

        if (request.IsActive.HasValue)
            customer.IsActive = request.IsActive.Value;

        if (request.Notes != null)
            customer.Notes = request.Notes;

        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated customer {CustomerId} - {FullName}", customer.CustomerId, customer.FullName);

        return MapToDto(customer);
    }

    public async Task<bool> DeleteCustomerAsync(Guid customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return false;

        // Soft delete
        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted customer {CustomerId} - {FullName}", customer.CustomerId, customer.FullName);

        return true;
    }

    public async Task<CustomerStatsDto> GetCustomerStatsAsync()
    {
        var totalCustomers = await _context.Customers.CountAsync();
        var activeCustomers = await _context.Customers.CountAsync(c => c.IsActive);
        var now = DateTime.UtcNow;
        var expiredMemberships = await _context.Customers
            .CountAsync(c => c.MembershipExpiryDate.HasValue && c.MembershipExpiryDate.Value < now);

        var totalCustomerValue = await _context.Customers.SumAsync(c => c.TotalSpent);
        var averageCustomerValue = totalCustomers > 0 ? totalCustomerValue / totalCustomers : 0;
        var totalLoyaltyPoints = await _context.Customers.SumAsync(c => c.LoyaltyPoints);

        var customersByLevel = await _context.Customers
            .Include(c => c.MembershipLevel)
            .GroupBy(c => c.MembershipLevel.Name)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Level, x => x.Count);

        var revenueByLevel = await _context.Customers
            .Include(c => c.MembershipLevel)
            .GroupBy(c => c.MembershipLevel.Name)
            .Select(g => new { Level = g.Key, Revenue = g.Sum(c => c.TotalSpent) })
            .ToDictionaryAsync(x => x.Level, x => x.Revenue);

        return new CustomerStatsDto
        {
            TotalCustomers = totalCustomers,
            ActiveCustomers = activeCustomers,
            ExpiredMemberships = expiredMemberships,
            TotalCustomerValue = totalCustomerValue,
            AverageCustomerValue = averageCustomerValue,
            TotalLoyaltyPoints = totalLoyaltyPoints,
            CustomersByMembershipLevel = customersByLevel,
            RevenueByMembershipLevel = revenueByLevel
        };
    }

    public async Task<bool> ProcessOrderAsync(Guid customerId, ProcessOrderRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers
                .Include(c => c.MembershipLevel)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null) return false;

            // Update customer stats
            customer.TotalSpent += request.OrderAmount;
            customer.TotalVisits++;
            customer.UpdatedAt = DateTime.UtcNow;

            // Process wallet deduction if specified
            if (request.WalletAmountUsed.HasValue && request.WalletAmountUsed.Value > 0)
            {
                await _walletService.DeductFundsAsync(
                    customerId, 
                    request.WalletAmountUsed.Value, 
                    $"Payment for order {request.OrderId}",
                    request.OrderId.ToString(),
                    request.OrderId);
            }

            // Process loyalty points redemption if specified
            if (request.LoyaltyPointsRedeemed.HasValue && request.LoyaltyPointsRedeemed.Value > 0)
            {
                await _loyaltyService.RedeemLoyaltyPointsAsync(
                    customerId,
                    request.LoyaltyPointsRedeemed.Value,
                    $"Redeemed for order {request.OrderId}",
                    request.OrderId);
            }

            // Calculate and add loyalty points for the order
            var pointsEarned = await _loyaltyService.CalculatePointsForOrderAsync(customerId, request.OrderAmount);
            if (pointsEarned > 0)
            {
                await _loyaltyService.AddLoyaltyPointsAsync(
                    customerId,
                    pointsEarned,
                    $"Earned from order {request.OrderId}",
                    request.OrderId,
                    request.OrderAmount);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Processed order {OrderId} for customer {CustomerId}, amount: {Amount}, points earned: {Points}",
                request.OrderId, customerId, request.OrderAmount, pointsEarned);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateMembershipLevelAsync(Guid customerId, Guid membershipLevelId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return false;

        var membershipLevel = await _context.MembershipLevels.FindAsync(membershipLevelId);
        if (membershipLevel == null || !membershipLevel.IsActive) return false;

        customer.MembershipLevelId = membershipLevelId;
        
        // Set expiry date if the membership level has validity
        if (membershipLevel.ValidityMonths.HasValue)
        {
            customer.MembershipExpiryDate = DateTime.UtcNow.AddMonths(membershipLevel.ValidityMonths.Value);
        }

        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated membership level for customer {CustomerId} to {MembershipLevel}",
            customerId, membershipLevel.Name);

        return true;
    }

    public async Task<List<CustomerDto>> GetExpiringMembershipsAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        var customers = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .Where(c => c.IsActive && 
                       c.MembershipExpiryDate.HasValue && 
                       c.MembershipExpiryDate.Value <= cutoffDate &&
                       c.MembershipExpiryDate.Value > DateTime.UtcNow)
            .OrderBy(c => c.MembershipExpiryDate)
            .ToListAsync();

        return customers.Select(MapToDto).ToList();
    }

    public async Task<int> ProcessMembershipExpiriesAsync()
    {
        var expiredCustomers = await _context.Customers
            .Where(c => c.IsActive && 
                       c.MembershipExpiryDate.HasValue && 
                       c.MembershipExpiryDate.Value < DateTime.UtcNow)
            .ToListAsync();

        if (!expiredCustomers.Any()) return 0;

        var defaultMembership = await _membershipService.GetDefaultMembershipLevelAsync();

        foreach (var customer in expiredCustomers)
        {
            customer.MembershipLevelId = defaultMembership.MembershipLevelId;
            customer.MembershipExpiryDate = null;
            customer.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed {Count} expired memberships", expiredCustomers.Count);

        return expiredCustomers.Count;
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Email,
            DateOfBirth = customer.DateOfBirth,
            PhotoUrl = customer.PhotoUrl,
            MembershipLevelId = customer.MembershipLevelId,
            MembershipLevel = customer.MembershipLevel?.Name,
            MembershipStartDate = customer.MembershipStartDate,
            MembershipExpiryDate = customer.MembershipExpiryDate,
            IsMembershipExpired = customer.IsMembershipExpired,
            DaysUntilExpiry = customer.DaysUntilExpiry,
            TotalSpent = customer.TotalSpent,
            TotalVisits = customer.TotalVisits,
            LoyaltyPoints = customer.LoyaltyPoints,
            Wallet = customer.Wallet == null ? null : new WalletDto
            {
                WalletId = customer.Wallet.WalletId,
                CustomerId = customer.Wallet.CustomerId,
                Balance = customer.Wallet.Balance,
                TotalLoaded = customer.Wallet.TotalLoaded,
                TotalSpent = customer.Wallet.TotalSpent,
                LastTransactionDate = customer.Wallet.LastTransactionDate,
                IsActive = customer.Wallet.IsActive,
                CreatedAt = customer.Wallet.CreatedAt,
                UpdatedAt = customer.Wallet.UpdatedAt
            },
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            Notes = customer.Notes
        };
    }
}
