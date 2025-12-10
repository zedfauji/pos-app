using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Services;

public class MembershipService : IMembershipService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(CustomerDbContext context, ILogger<MembershipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MembershipLevelDto>> GetAllMembershipLevelsAsync()
    {
        var levels = await _context.MembershipLevels
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .ToListAsync();

        return levels.Select(MapToDto).ToList();
    }

    public async Task<MembershipLevelDto?> GetMembershipLevelByIdAsync(Guid membershipLevelId)
    {
        var level = await _context.MembershipLevels
            .FirstOrDefaultAsync(m => m.MembershipLevelId == membershipLevelId);

        return level == null ? null : MapToDto(level);
    }

    public async Task<MembershipLevelDto> GetDefaultMembershipLevelAsync()
    {
        var defaultLevel = await _context.MembershipLevels
            .FirstOrDefaultAsync(m => m.IsDefault && m.IsActive);

        if (defaultLevel == null)
        {
            // Fallback to first active level if no default is set
            defaultLevel = await _context.MembershipLevels
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .FirstOrDefaultAsync();
        }

        if (defaultLevel == null)
        {
            throw new InvalidOperationException("No active membership levels found");
        }

        return MapToDto(defaultLevel);
    }

    public async Task<bool> CanUpgradeMembershipAsync(Guid customerId, Guid targetMembershipLevelId)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return false;

        var targetLevel = await _context.MembershipLevels
            .FirstOrDefaultAsync(m => m.MembershipLevelId == targetMembershipLevelId);

        if (targetLevel == null || !targetLevel.IsActive) return false;

        // Check if customer meets minimum spend requirement
        if (targetLevel.MinimumSpendRequirement.HasValue &&
            customer.TotalSpent < targetLevel.MinimumSpendRequirement.Value)
        {
            return false;
        }

        // Check if target level is higher than current level
        return targetLevel.SortOrder > customer.MembershipLevel.SortOrder;
    }

    public async Task<MembershipLevelDto?> GetRecommendedMembershipAsync(decimal totalSpent)
    {
        var eligibleLevels = await _context.MembershipLevels
            .Where(m => m.IsActive && 
                       (!m.MinimumSpendRequirement.HasValue || 
                        m.MinimumSpendRequirement.Value <= totalSpent))
            .OrderByDescending(m => m.SortOrder)
            .FirstOrDefaultAsync();

        return eligibleLevels == null ? null : MapToDto(eligibleLevels);
    }

    private static MembershipLevelDto MapToDto(MembershipLevel level)
    {
        return new MembershipLevelDto
        {
            MembershipLevelId = level.MembershipLevelId,
            Name = level.Name,
            Description = level.Description,
            DiscountPercentage = level.DiscountPercentage,
            LoyaltyMultiplier = level.LoyaltyMultiplier,
            MinimumSpendRequirement = level.MinimumSpendRequirement,
            ValidityMonths = level.ValidityMonths,
            ColorHex = level.ColorHex,
            Icon = level.Icon,
            SortOrder = level.SortOrder,
            IsActive = level.IsActive,
            IsDefault = level.IsDefault,
            MaxWalletBalance = level.MaxWalletBalance,
            FreeDelivery = level.FreeDelivery,
            PrioritySupport = level.PrioritySupport,
            BirthdayBonusPoints = level.BirthdayBonusPoints,
            CreatedAt = level.CreatedAt,
            UpdatedAt = level.UpdatedAt
        };
    }
}
