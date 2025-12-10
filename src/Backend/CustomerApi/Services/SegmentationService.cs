using System.Text.Json;
using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Services;

public class SegmentationService : ISegmentationService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<SegmentationService> _logger;

    public SegmentationService(CustomerDbContext context, ILogger<SegmentationService> logger)
    {
        _context = context;
        _logger = logger;
    }


    public async Task<List<CustomerSegmentDto>> GetSegmentsAsync(bool? activeOnly = null)
    {
        var query = _context.CustomerSegments
            .Include(s => s.Memberships)
            .AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(s => s.IsActive);
        }

        var segments = await query
            .OrderBy(s => s.Name)
            .ToListAsync();

        return segments.Select(MapToDto).ToList();
    }

    public async Task<List<CustomerSegmentDto>> GetSegmentsAsync(bool activeOnly, int page, int pageSize)
    {
        var query = _context.CustomerSegments.AsQueryable();
        
        if (activeOnly)
            query = query.Where(s => s.IsActive);

        var totalCount = await query.CountAsync();
        
        var segments = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return segments.Select(MapToDto).ToList();
    }

    public async Task<CustomerSegmentDto?> GetSegmentAsync(Guid segmentId)
    {
        var segment = await _context.CustomerSegments
            .Include(s => s.Memberships)
            .FirstOrDefaultAsync(s => s.SegmentId == segmentId);

        return segment != null ? MapToDto(segment) : null;
    }

    public async Task<bool> RefreshSegmentMembershipAsync(Guid segmentId)
    {
        await RefreshSegmentAsync(segmentId);
        return true;
    }

    public async Task<List<CustomerDto>> GetCustomersByCriteriaAsync(string criteriaJson, int page, int pageSize)
    {
        var criteria = JsonSerializer.Deserialize<SegmentCriteria>(criteriaJson);
        if (criteria == null) return new List<CustomerDto>();

        var customers = await GetCustomersMatchingCriteriaAsync(criteria);
        
        return customers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapCustomerToDto)
            .ToList();
    }

    public async Task<bool> RefreshAllSegmentMembershipsAsync()
    {
        var segments = await _context.CustomerSegments.Where(s => s.IsActive).ToListAsync();
        foreach (var segment in segments)
        {
            await RefreshSegmentAsync(segment.SegmentId);
        }
        return true;
    }

    public async Task<bool> IsCustomerInSegmentAsync(Guid customerId, Guid segmentId)
    {
        return await _context.CustomerSegmentMemberships
            .AnyAsync(m => m.CustomerId == customerId && m.SegmentId == segmentId && m.IsActive);
    }

    public async Task<CustomerSegmentDto> CreateSegmentAsync(CustomerSegmentDto request)
    {
        // Validate criteria JSON
        try
        {
            JsonSerializer.Deserialize<SegmentCriteria>(request.CriteriaJson);
        }
        catch (JsonException)
        {
            throw new ArgumentException("Invalid criteria JSON format", nameof(request.CriteriaJson));
        }

        var segment = new CustomerSegment
        {
            Name = request.Name,
            Description = request.Description,
            CriteriaJson = request.CriteriaJson,
            AutoRefresh = request.AutoRefresh,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CustomerSegments.Add(segment);
        await _context.SaveChangesAsync();

        // Calculate initial segment membership
        await RefreshSegmentAsync(segment.SegmentId);

        _logger.LogInformation("Created customer segment {SegmentName} with ID {SegmentId}", 
            segment.Name, segment.SegmentId);

        return MapToDto(segment);
    }

    public async Task<CustomerSegmentDto?> UpdateSegmentAsync(Guid segmentId, CustomerSegmentDto request)
    {
        var segment = await _context.CustomerSegments.FindAsync(segmentId);
        if (segment == null) return null;

        if (!string.IsNullOrEmpty(request.Name))
            segment.Name = request.Name;

        if (request.Description != null)
            segment.Description = request.Description;

        if (!string.IsNullOrEmpty(request.CriteriaJson))
        {
            // Validate criteria JSON
            try
            {
                JsonSerializer.Deserialize<SegmentCriteria>(request.CriteriaJson);
                segment.CriteriaJson = request.CriteriaJson;
            }
            catch (JsonException)
            {
                throw new ArgumentException("Invalid criteria JSON format", nameof(request.CriteriaJson));
            }
        }

        segment.IsActive = request.IsActive;

        segment.AutoRefresh = request.AutoRefresh;

        segment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Refresh segment if criteria changed
        if (!string.IsNullOrEmpty(request.CriteriaJson))
        {
            await RefreshSegmentAsync(segmentId);
        }

        _logger.LogInformation("Updated customer segment {SegmentName} with ID {SegmentId}", 
            segment.Name, segment.SegmentId);

        return MapToDto(segment);
    }

    public async Task<bool> DeleteSegmentAsync(Guid segmentId)
    {
        var segment = await _context.CustomerSegments
            .FirstOrDefaultAsync(s => s.SegmentId == segmentId);

        if (segment == null) return false;

        // Remove all customer memberships
        var memberships = await _context.CustomerSegmentMemberships
            .Where(m => m.SegmentId == segmentId)
            .ToListAsync();
        _context.CustomerSegmentMemberships.RemoveRange(memberships);
        _context.CustomerSegments.Remove(segment);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted customer segment {SegmentName} with ID {SegmentId}", 
            segment.Name, segment.SegmentId);

        return true;
    }

    public async Task<bool> RefreshSegmentAsync(Guid segmentId)
    {
        var segment = await _context.CustomerSegments
            .FirstOrDefaultAsync(s => s.SegmentId == segmentId);

        if (segment == null) return false;

        try
        {
            var criteria = JsonSerializer.Deserialize<SegmentCriteria>(segment.CriteriaJson);
            if (criteria == null) return false;

            // Get customers matching the criteria
            var matchingCustomers = await GetCustomersMatchingCriteriaAsync(criteria);
            var matchingCustomerIds = matchingCustomers.Select(c => c.CustomerId).ToHashSet();

            // Get current segment members
            var currentMemberIds = await _context.CustomerSegmentMemberships
                .Where(m => m.SegmentId == segmentId && m.IsActive)
                .Select(m => m.CustomerId)
                .ToListAsync();
            var currentMemberIdsSet = currentMemberIds.ToHashSet();

            // Add new members
            var newMemberIds = matchingCustomerIds.Except(currentMemberIdsSet);
            foreach (var customerId in newMemberIds)
            {
                _context.CustomerSegmentMemberships.Add(new CustomerSegmentMembership
                {
                    SegmentId = segmentId,
                    CustomerId = customerId,
                    AddedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            // Deactivate members who no longer match
            var removedMemberIds = currentMemberIds.Except(matchingCustomerIds);
            var membershipsToDeactivate = await _context.CustomerSegmentMemberships
                .Where(m => m.SegmentId == segmentId && m.IsActive && removedMemberIds.Contains(m.CustomerId))
                .ToListAsync();
            
            foreach (var membership in membershipsToDeactivate)
            {
                membership.IsActive = false;
            }

            // Update segment statistics
            segment.CustomerCount = matchingCustomerIds.Count;
            segment.LastCalculatedAt = DateTime.UtcNow;
            segment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Refreshed segment {SegmentName}: {CustomerCount} customers", 
                segment.Name, segment.CustomerCount);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing segment {SegmentId}", segmentId);
            return false;
        }
    }

    public async Task<List<CustomerDto>> GetSegmentCustomersAsync(Guid segmentId, int page = 1, int pageSize = 50)
    {
        var skip = (page - 1) * pageSize;
        var take = pageSize;

        var customers = await _context.CustomerSegmentMemberships
            .Where(m => m.SegmentId == segmentId && m.IsActive)
            .Include(m => m.Customer)
                .ThenInclude(c => c.MembershipLevel)
            .Include(m => m.Customer.Wallet)
            .Select(m => m.Customer)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return customers.Select(MapCustomerToDto).ToList();
    }

    public async Task<int> GetSegmentCustomerCountAsync(Guid segmentId)
    {
        return await _context.CustomerSegmentMemberships
            .CountAsync(m => m.SegmentId == segmentId && m.IsActive);
    }

    public async Task<Dictionary<string, object>?> GetSegmentAnalyticsAsync(Guid segmentId)
    {
        var segment = await _context.CustomerSegments.FindAsync(segmentId);
        if (segment == null)
            throw new ArgumentException("Segment not found", nameof(segmentId));

        var customerIds = await _context.CustomerSegmentMemberships
            .Where(m => m.SegmentId == segmentId && m.IsActive)
            .Include(m => m.Customer)
            .Select(m => m.Customer)
            .ToListAsync();

        var totalRevenue = customerIds.Sum(c => c.TotalSpent);
        var activeCampaigns = await _context.Campaigns
            .CountAsync(c => c.TargetSegmentId == segmentId && c.Status == CampaignStatus.Active);

        return new Dictionary<string, object>
        {
            ["segmentId"] = segmentId,
            ["totalCustomers"] = customerIds.Count,
            ["totalRevenue"] = totalRevenue,
            ["averageOrderValue"] = customerIds.Count > 0 ? totalRevenue / customerIds.Count : 0,
            ["activeCampaigns"] = activeCampaigns
        };
    }

    public async Task<List<CustomerSegmentDto>> GetCustomerSegmentsAsync(Guid customerId)
    {
        var segments = await _context.CustomerSegmentMemberships
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Include(m => m.Segment)
            .Select(m => m.Segment)
            .ToListAsync();

        return segments.Select(MapToDto).ToList();
    }

    public async Task<bool> AddCustomerToSegmentAsync(Guid segmentId, Guid customerId)
    {
        var segment = await _context.CustomerSegments.FindAsync(segmentId);
        var customer = await _context.Customers.FindAsync(customerId);

        if (segment == null || customer == null) return false;

        var existingMembership = await _context.CustomerSegmentMemberships
            .FirstOrDefaultAsync(m => m.SegmentId == segmentId && m.CustomerId == customerId);

        if (existingMembership != null)
        {
            existingMembership.IsActive = true;
        }
        else
        {
            _context.CustomerSegmentMemberships.Add(new CustomerSegmentMembership
            {
                SegmentId = segmentId,
                CustomerId = customerId,
                AddedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveCustomerFromSegmentAsync(Guid segmentId, Guid customerId)
    {
        var membership = await _context.CustomerSegmentMemberships
            .FirstOrDefaultAsync(m => m.SegmentId == segmentId && m.CustomerId == customerId && m.IsActive);

        if (membership == null) return false;

        membership.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RefreshAllActiveSegmentsAsync()
    {
        var activeSegments = await _context.CustomerSegments
            .Where(s => s.IsActive && s.AutoRefresh)
            .ToListAsync();

        foreach (var segment in activeSegments)
        {
            await RefreshSegmentAsync(segment.SegmentId);
        }

        _logger.LogInformation("Refreshed {Count} active segments", activeSegments.Count);
    }

    public async Task<List<CustomerDto>> PreviewSegmentCriteriaAsync(SegmentCriteria criteria, int limit = 100)
    {
        var customers = await GetCustomersMatchingCriteriaAsync(criteria, limit);
        return customers.Select(MapCustomerToDto).ToList();
    }

    private async Task<List<Customer>> GetCustomersMatchingCriteriaAsync(SegmentCriteria criteria, int? limit = null, bool includeInactive = true)
    {
        var query = _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .Where(c => c.IsActive);

        // Apply criteria filters
        if (criteria.MinTotalSpent.HasValue)
            query = query.Where(c => c.TotalSpent >= criteria.MinTotalSpent.Value);

        if (criteria.MaxTotalSpent.HasValue)
            query = query.Where(c => c.TotalSpent <= criteria.MaxTotalSpent.Value);

        if (criteria.MinTotalVisits.HasValue)
            query = query.Where(c => c.TotalVisits >= criteria.MinTotalVisits.Value);

        if (criteria.MaxTotalVisits.HasValue)
            query = query.Where(c => c.TotalVisits <= criteria.MaxTotalVisits.Value);

        if (criteria.MinLoyaltyPoints.HasValue)
            query = query.Where(c => c.LoyaltyPoints >= criteria.MinLoyaltyPoints.Value);

        if (criteria.MaxLoyaltyPoints.HasValue)
            query = query.Where(c => c.LoyaltyPoints <= criteria.MaxLoyaltyPoints.Value);

        if (criteria.MinWalletBalance.HasValue)
            query = query.Where(c => c.Wallet != null && c.Wallet.Balance >= criteria.MinWalletBalance.Value);

        if (criteria.MaxWalletBalance.HasValue)
            query = query.Where(c => c.Wallet != null && c.Wallet.Balance <= criteria.MaxWalletBalance.Value);

        if (criteria.MembershipLevelIds?.Any() == true)
            query = query.Where(c => criteria.MembershipLevelIds.Contains(c.MembershipLevelId));

        if (criteria.LastVisitDaysAgo.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-criteria.LastVisitDaysAgo.Value);
            query = query.Where(c => c.Wallet != null && c.Wallet.LastTransactionDate < cutoffDate);
        }

        if (criteria.MembershipExpiringInDays.HasValue)
        {
            var expiryDate = DateTime.UtcNow.AddDays(criteria.MembershipExpiringInDays.Value);
            query = query.Where(c => c.MembershipExpiryDate.HasValue && 
                                   c.MembershipExpiryDate.Value <= expiryDate && 
                                   c.MembershipExpiryDate.Value > DateTime.UtcNow);
        }

        if (criteria.BirthdayThisMonth == true)
        {
            var currentMonth = DateTime.UtcNow.Month;
            query = query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Month == currentMonth);
        }

        if (criteria.CreatedAfter.HasValue)
            query = query.Where(c => c.CreatedAt >= criteria.CreatedAfter.Value);

        if (criteria.CreatedBefore.HasValue)
            query = query.Where(c => c.CreatedAt <= criteria.CreatedBefore.Value);

        if (criteria.MinAverageOrderValue.HasValue)
            query = query.Where(c => c.TotalVisits > 0 && (c.TotalSpent / c.TotalVisits) >= criteria.MinAverageOrderValue.Value);

        if (criteria.MaxAverageOrderValue.HasValue)
            query = query.Where(c => c.TotalVisits > 0 && (c.TotalSpent / c.TotalVisits) <= criteria.MaxAverageOrderValue.Value);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    private static CustomerSegmentDto MapToDto(CustomerSegment segment)
    {
        return new CustomerSegmentDto
        {
            SegmentId = segment.SegmentId,
            Name = segment.Name,
            Description = segment.Description,
            CriteriaJson = segment.CriteriaJson,
            IsActive = segment.IsActive,
            AutoRefresh = segment.AutoRefresh,
            LastCalculatedAt = segment.LastCalculatedAt,
            CustomerCount = segment.CustomerCount,
            CreatedAt = segment.CreatedAt,
            UpdatedAt = segment.UpdatedAt,
            CreatedBy = segment.CreatedBy
        };
    }

    private static CustomerDto MapCustomerToDto(Customer customer)
    {
        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            DateOfBirth = customer.DateOfBirth,
            MembershipLevelId = customer.MembershipLevelId,
            MembershipLevel = customer.MembershipLevel?.Name,
            MembershipStartDate = customer.MembershipStartDate,
            MembershipExpiryDate = customer.MembershipExpiryDate,
            RegistrationDate = customer.CreatedAt,
            TotalSpent = customer.TotalSpent,
            TotalVisits = customer.TotalVisits,
            LoyaltyPoints = customer.LoyaltyPoints,
            IsActive = customer.IsActive,
            Notes = customer.Notes,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            IsMembershipExpired = customer.IsMembershipExpired,
            DaysUntilExpiry = customer.DaysUntilExpiry
        };
    }
}
