using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<LoyaltyService> _logger;

    public LoyaltyService(CustomerDbContext context, ILogger<LoyaltyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> GetLoyaltyPointsAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        return customer?.LoyaltyPoints ?? 0;
    }

    public async Task<int> CalculatePointsForOrderAsync(Guid customerId, decimal orderAmount)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer?.MembershipLevel == null)
        {
            return 0;
        }

        // Base calculation: 1 point per dollar spent, multiplied by membership level multiplier
        var basePoints = (int)Math.Floor(orderAmount);
        var multipliedPoints = (int)Math.Floor(basePoints * customer.MembershipLevel.LoyaltyMultiplier);

        return multipliedPoints;
    }

    public async Task<bool> AddLoyaltyPointsAsync(Guid customerId, int points, string description, Guid? orderId = null, decimal? orderAmount = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null || !customer.IsActive)
            {
                return false;
            }

            // Create loyalty transaction
            var loyaltyTransaction = new LoyaltyTransaction
            {
                CustomerId = customerId,
                TransactionType = LoyaltyTransactionType.Earned,
                Points = points,
                Description = description,
                OrderId = orderId,
                OrderAmount = orderAmount,
                ExpiryDate = DateTime.UtcNow.AddYears(2) // Points expire after 2 years
            };

            _context.LoyaltyTransactions.Add(loyaltyTransaction);

            // Update customer's loyalty points
            customer.LoyaltyPoints += points;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Added {Points} loyalty points to customer {CustomerId}", points, customerId);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RedeemLoyaltyPointsAsync(Guid customerId, int points, string description, Guid? orderId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null || !customer.IsActive || customer.LoyaltyPoints < points)
            {
                return false;
            }

            // Create loyalty transaction
            var loyaltyTransaction = new LoyaltyTransaction
            {
                CustomerId = customerId,
                TransactionType = LoyaltyTransactionType.Redeemed,
                Points = points,
                Description = description,
                OrderId = orderId
            };

            _context.LoyaltyTransactions.Add(loyaltyTransaction);

            // Update customer's loyalty points
            customer.LoyaltyPoints -= points;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Redeemed {Points} loyalty points from customer {CustomerId}", points, customerId);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CanRedeemPointsAsync(Guid customerId, int points)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        return customer?.IsActive == true && customer.LoyaltyPoints >= points;
    }

    public async Task<(List<LoyaltyTransactionDto> Transactions, int TotalCount)> GetLoyaltyTransactionsAsync(Guid customerId, int page = 1, int pageSize = 20)
    {
        var query = _context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (transactions.Select(MapToDto).ToList(), totalCount);
    }

    public async Task<int> ProcessExpiredPointsAsync()
    {
        var expiredTransactions = await _context.LoyaltyTransactions
            .Where(t => t.TransactionType == LoyaltyTransactionType.Earned &&
                       t.ExpiryDate.HasValue &&
                       t.ExpiryDate.Value < DateTime.UtcNow &&
                       !t.IsExpired)
            .Include(t => t.Customer)
            .ToListAsync();

        if (!expiredTransactions.Any())
        {
            return 0;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customerPointsToDeduct = new Dictionary<Guid, int>();

            foreach (var expiredTransaction in expiredTransactions)
            {
                // Mark transaction as expired
                expiredTransaction.IsExpired = true;
                expiredTransaction.UpdatedAt = DateTime.UtcNow;

                // Track points to deduct per customer
                if (!customerPointsToDeduct.ContainsKey(expiredTransaction.CustomerId))
                {
                    customerPointsToDeduct[expiredTransaction.CustomerId] = 0;
                }
                customerPointsToDeduct[expiredTransaction.CustomerId] += expiredTransaction.Points;

                // Create expiry transaction record
                var expiryTransaction = new LoyaltyTransaction
                {
                    CustomerId = expiredTransaction.CustomerId,
                    TransactionType = LoyaltyTransactionType.Expired,
                    Points = expiredTransaction.Points,
                    Description = $"Points expired from transaction {expiredTransaction.TransactionId}",
                    RelatedTransactionId = expiredTransaction.TransactionId
                };

                _context.LoyaltyTransactions.Add(expiryTransaction);
            }

            // Update customer loyalty points
            foreach (var kvp in customerPointsToDeduct)
            {
                var customer = await _context.Customers.FindAsync(kvp.Key);
                if (customer != null)
                {
                    customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints - kvp.Value);
                    customer.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Processed {Count} expired loyalty point transactions", expiredTransactions.Count);

            return expiredTransactions.Count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<LoyaltyTransactionDto>> GetExpiringPointsAsync(Guid customerId, int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        var expiringTransactions = await _context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId &&
                       t.TransactionType == LoyaltyTransactionType.Earned &&
                       !t.IsExpired &&
                       t.ExpiryDate.HasValue &&
                       t.ExpiryDate.Value <= cutoffDate &&
                       t.ExpiryDate.Value > DateTime.UtcNow)
            .OrderBy(t => t.ExpiryDate)
            .ToListAsync();

        return expiringTransactions.Select(MapToDto).ToList();
    }

    public async Task<LoyaltyStatsDto> GetLoyaltyStatsAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null)
        {
            return new LoyaltyStatsDto();
        }

        var totalEarned = await _context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId && t.TransactionType == LoyaltyTransactionType.Earned)
            .SumAsync(t => t.Points);

        var totalRedeemed = await _context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId && t.TransactionType == LoyaltyTransactionType.Redeemed)
            .SumAsync(t => t.Points);

        var totalExpired = await _context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId && t.TransactionType == LoyaltyTransactionType.Expired)
            .SumAsync(t => t.Points);

        var expiringIn30Days = await _context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId &&
                       t.TransactionType == LoyaltyTransactionType.Earned &&
                       !t.IsExpired &&
                       t.ExpiryDate.HasValue &&
                       t.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30) &&
                       t.ExpiryDate.Value > DateTime.UtcNow)
            .SumAsync(t => t.Points);

        return new LoyaltyStatsDto
        {
            CurrentBalance = customer.LoyaltyPoints,
            TotalEarned = totalEarned,
            TotalRedeemed = totalRedeemed,
            TotalExpired = totalExpired,
            ExpiringIn30Days = expiringIn30Days
        };
    }

    public async Task<int> ProcessBirthdayBonusAsync()
    {
        var today = DateTime.UtcNow.Date;
        var birthdayCustomers = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Where(c => c.IsActive &&
                       c.DateOfBirth.HasValue &&
                       c.DateOfBirth.Value.Month == today.Month &&
                       c.DateOfBirth.Value.Day == today.Day &&
                       c.MembershipLevel != null &&
                       c.MembershipLevel.BirthdayBonusPoints > 0)
            .ToListAsync();

        if (!birthdayCustomers.Any())
        {
            return 0;
        }

        var processedCount = 0;

        foreach (var customer in birthdayCustomers)
        {
            // Check if birthday bonus was already given this year
            var thisYear = today.Year;
            var existingBonus = await _context.LoyaltyTransactions
                .AnyAsync(t => t.CustomerId == customer.CustomerId &&
                              t.TransactionType == LoyaltyTransactionType.Earned &&
                              t.Description.Contains("Birthday bonus") &&
                              t.CreatedAt.Year == thisYear);

            if (!existingBonus)
            {
                var bonusPoints = customer.MembershipLevel.BirthdayBonusPoints;
                await AddLoyaltyPointsAsync(
                    customer.CustomerId,
                    bonusPoints,
                    $"Birthday bonus for {customer.FullName}");

                processedCount++;
            }
        }

        _logger.LogInformation("Processed birthday bonuses for {Count} customers", processedCount);

        return processedCount;
    }

    private static LoyaltyTransactionDto MapToDto(LoyaltyTransaction transaction)
    {
        return new LoyaltyTransactionDto
        {
            TransactionId = transaction.TransactionId,
            CustomerId = transaction.CustomerId,
            TransactionType = transaction.TransactionType.ToString(),
            Points = transaction.Points,
            Description = transaction.Description,
            OrderId = transaction.OrderId,
            OrderAmount = transaction.OrderAmount,
            ExpiryDate = transaction.ExpiryDate,
            IsExpired = transaction.IsExpired,
            RelatedTransactionId = transaction.RelatedTransactionId,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            CreatedBy = transaction.CreatedBy
        };
    }
}
