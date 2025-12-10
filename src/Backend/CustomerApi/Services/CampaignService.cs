using System.Text.Json;
using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Services;

public class CampaignService : ICampaignService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<CampaignService> _logger;
    private readonly ICommunicationService _communicationService;
    private readonly ISegmentationService _segmentationService;

    public CampaignService(
        CustomerDbContext context,
        ILogger<CampaignService> logger,
        ICommunicationService communicationService,
        ISegmentationService segmentationService)
    {
        _context = context;
        _logger = logger;
        _communicationService = communicationService;
        _segmentationService = segmentationService;
    }

    public async Task<CampaignDto?> GetCampaignByIdAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        return campaign == null ? null : MapToDto(campaign);
    }

    public async Task<List<CampaignDto>> GetAllCampaignsAsync(CampaignStatus? status = null)
    {
        var query = _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(MapToDto).ToList();
    }

    public async Task<CampaignDto> CreateCampaignAsync(CampaignDto request)
    {
        var campaign = new Campaign
        {
            Name = request.Name,
            Description = request.Description,
            CampaignType = request.CampaignType,
            Channel = request.Channel,
            TargetSegmentId = request.TargetSegmentId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = request.DiscountAmount,
            MinimumSpend = request.MinimumSpend,
            MaximumDiscount = request.MaximumDiscount,
            LoyaltyPointsBonus = request.LoyaltyPointsBonus,
            WalletCreditAmount = request.WalletCreditAmount,
            FreeItemId = request.FreeItemId,
            UpgradeMembershipLevelId = request.UpgradeMembershipLevelId,
            UsageLimitPerCustomer = request.UsageLimitPerCustomer,
            TotalUsageLimit = request.TotalUsageLimit,
            MessageTemplate = request.MessageTemplate,
            SubjectLine = request.SubjectLine,
            CallToAction = request.CallToAction,
            IsPersonalized = request.IsPersonalized,
            PersonalizationRulesJson = request.PersonalizationRulesJson,
            Status = CampaignStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created campaign {CampaignName} with ID {CampaignId}",
            campaign.Name, campaign.CampaignId);

        // Load the campaign with navigation properties for DTO mapping
        var createdCampaign = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .FirstAsync(c => c.CampaignId == campaign.CampaignId);

        return MapToDto(createdCampaign);
    }

    public async Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignDto request)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null) return null;

        // Don't allow updating active campaigns with certain changes
        if (campaign.Status == CampaignStatus.Active)
        {
            _logger.LogWarning("Attempted to update active campaign {CampaignId}", campaignId);
            // Allow only status and end date changes for active campaigns
            campaign.Status = request.Status;
            if (request.EndDate.HasValue)
                campaign.EndDate = request.EndDate.Value;
        }
        else
        {
            if (!string.IsNullOrEmpty(request.Name))
                campaign.Name = request.Name;


            if (request.Description != null)
                campaign.Description = request.Description;

            campaign.Status = request.Status;

            campaign.Channel = request.Channel;

            if (request.TargetSegmentId.HasValue)
                campaign.TargetSegmentId = request.TargetSegmentId.Value;

            campaign.StartDate = request.StartDate;

            if (request.EndDate.HasValue)
                campaign.EndDate = request.EndDate.Value;

            if (request.DiscountPercentage.HasValue)
                campaign.DiscountPercentage = request.DiscountPercentage.Value;

            if (request.DiscountAmount.HasValue)
                campaign.DiscountAmount = request.DiscountAmount.Value;

            if (request.MinimumSpend.HasValue)
                campaign.MinimumSpend = request.MinimumSpend.Value;

            if (request.MaximumDiscount.HasValue)
                campaign.MaximumDiscount = request.MaximumDiscount.Value;

            if (request.LoyaltyPointsBonus.HasValue)
                campaign.LoyaltyPointsBonus = request.LoyaltyPointsBonus.Value;

            if (request.WalletCreditAmount.HasValue)
                campaign.WalletCreditAmount = request.WalletCreditAmount.Value;

            if (request.FreeItemId.HasValue)
                campaign.FreeItemId = request.FreeItemId.Value;

            if (request.UpgradeMembershipLevelId.HasValue)
                campaign.UpgradeMembershipLevelId = request.UpgradeMembershipLevelId.Value;

            if (request.UsageLimitPerCustomer.HasValue)
                campaign.UsageLimitPerCustomer = request.UsageLimitPerCustomer.Value;

            if (request.TotalUsageLimit.HasValue)
                campaign.TotalUsageLimit = request.TotalUsageLimit.Value;

            if (request.MessageTemplate != null)
                campaign.MessageTemplate = request.MessageTemplate;

            if (request.SubjectLine != null)
                campaign.SubjectLine = request.SubjectLine;

            if (request.CallToAction != null)
                campaign.CallToAction = request.CallToAction;

            campaign.IsPersonalized = request.IsPersonalized;

            if (request.PersonalizationRulesJson != null)
                campaign.PersonalizationRulesJson = request.PersonalizationRulesJson;
        }

        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated campaign {CampaignName} with ID {CampaignId}",
            campaign.Name, campaign.CampaignId);

        return MapToDto(campaign);
    }

    public async Task<bool> DeleteCampaignAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Executions)
            .Include(c => c.Redemptions)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null) return false;

        // Don't allow deleting campaigns that have been executed
        if (campaign.Executions.Any() || campaign.Redemptions.Any())
        {
            _logger.LogWarning("Attempted to delete campaign {CampaignId} with existing executions/redemptions", campaignId);
            return false;
        }

        _context.Campaigns.Remove(campaign);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted campaign {CampaignName} with ID {CampaignId}",
            campaign.Name, campaign.CampaignId);

        return true;
    }

    public async Task<bool> ActivateCampaignAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.Status != CampaignStatus.Draft) return false;

        campaign.Status = CampaignStatus.Active;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated campaign {CampaignId}", campaignId);

        // Execute campaign for target segment if specified
        if (campaign.TargetSegmentId.HasValue)
        {
            await ExecuteCampaignForSegmentAsync(campaignId);
        }

        return true;
    }

    public async Task<bool> PauseCampaignAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.Status != CampaignStatus.Active) return false;

        campaign.Status = CampaignStatus.Paused;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Paused campaign {CampaignId}", campaignId);
        return true;
    }

    public async Task<bool> CompleteCampaignAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) return false;

        campaign.Status = CampaignStatus.Completed;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Completed campaign {CampaignId}", campaignId);
        return true;
    }

    public async Task<List<CampaignDto>> GetCustomerEligibleCampaignsAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return new List<CampaignDto>();

        // Get customer's segments
        var customerSegmentIds = await _context.CustomerSegmentMemberships
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.SegmentId)
            .ToListAsync();

        // Get active campaigns
        var activeCampaigns = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .Where(c => c.IsActive)
            .ToListAsync();

        var eligibleCampaigns = new List<Campaign>();

        foreach (var campaign in activeCampaigns)
        {
            // Check if customer can use this campaign
            if (await CanCustomerUseCampaignAsync(campaign.CampaignId, customerId))
            {
                // Check segment targeting
                if (campaign.TargetSegmentId == null || customerSegmentIds.Contains(campaign.TargetSegmentId.Value))
                {
                    eligibleCampaigns.Add(campaign);
                }
            }
        }

        return eligibleCampaigns.Select(MapToDto).ToList();
    }

    public async Task<bool> ExecuteCampaignForCustomerAsync(Guid campaignId, Guid customerId, CampaignChannel? overrideChannel = null)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (campaign == null || customer == null || !campaign.IsActive)
            return false;

        if (!await CanCustomerUseCampaignAsync(campaignId, customerId))
            return false;

        try
        {
            var channel = overrideChannel ?? campaign.Channel;
            var message = PersonalizeMessageForCustomer(campaign, customer);

            // Create execution record
            var execution = new CampaignExecution
            {
                CampaignId = campaignId,
                CustomerId = customerId,
                ChannelUsed = channel.ToString(),
                MessageSent = message,
                ExecutedAt = DateTime.UtcNow
            };

            _context.CampaignExecutions.Add(execution);

            // Send message if applicable
            if (channel != CampaignChannel.InApp)
            {
                var request = new SendMessageRequest
                {
                    CustomerId = customerId,
                    ProviderType = MapChannelToProvider(channel),
                    Subject = campaign.SubjectLine,
                    MessageBody = message,
                    CampaignId = campaignId
                };

                var sent = await _communicationService.SendMessageAsync(request);
                execution.DeliveryStatus = sent ? "Sent" : "Failed";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Executed campaign {CampaignId} for customer {CustomerId} via {Channel}",
                campaignId, customerId, channel);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing campaign {CampaignId} for customer {CustomerId}", campaignId, customerId);
            return false;
        }
    }

    public async Task<bool> ExecuteCampaignForSegmentAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null || !campaign.IsActive || campaign.TargetSegmentId == null)
            return false;

        var segmentCustomers = await _segmentationService.GetSegmentCustomersAsync(campaign.TargetSegmentId.Value);
        var executedCount = 0;

        foreach (var customer in segmentCustomers)
        {
            var execution = await ExecuteCampaignForCustomerAsync(campaignId, customer.CustomerId);
            if (execution != null)
            {
                executedCount++;
            }
        }

        _logger.LogInformation("Executed campaign {CampaignId} for {Count} customers in segment",
            campaignId, executedCount);

        return true;
    }


    public async Task<List<CampaignExecutionDto>> GetCampaignExecutionsAsync(Guid campaignId, int page = 1, int pageSize = 50)
    {
        var executions = await _context.CampaignExecutions
            .Where(e => e.CampaignId == campaignId)
            .Include(e => e.Campaign)
            .Include(e => e.Customer)
            .OrderByDescending(e => e.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return executions.Select(MapExecutionToDto).ToList();
    }

    public async Task<List<CampaignRedemptionDto>> GetCampaignRedemptionsAsync(Guid campaignId, int page = 1, int pageSize = 50)
    {
        var redemptions = await _context.CampaignRedemptions
            .Where(r => r.CampaignId == campaignId)
            .Include(r => r.Campaign)
            .Include(r => r.Customer)
            .OrderByDescending(r => r.RedeemedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return redemptions.Select(MapRedemptionToDto).ToList();
    }

    public async Task<Dictionary<string, object>?> GetCampaignAnalyticsAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
            throw new ArgumentException("Campaign not found", nameof(campaignId));

        var executions = await _context.CampaignExecutions
            .Where(e => e.CampaignId == campaignId)
            .ToListAsync();

        var redemptions = await _context.CampaignRedemptions
            .Where(r => r.CampaignId == campaignId)
            .ToListAsync();

        var totalExecutions = executions.Count;
        var totalRedemptions = redemptions.Count;
        var messagesOpened = executions.Count(e => e.OpenedAt.HasValue);
        var messagesClicked = executions.Count(e => e.ClickedAt.HasValue);

        var totalRevenue = redemptions.Where(r => r.OrderTotal.HasValue).Sum(r => r.OrderTotal.Value);

        return new Dictionary<string, object>
        {
            ["campaignId"] = campaignId,
            ["totalExecutions"] = executions.Count,
            ["totalRedemptions"] = redemptions.Count,
            ["conversionRate"] = executions.Count > 0 ? (decimal)redemptions.Count / executions.Count * 100 : 0,
            ["totalRevenue"] = totalRevenue,
            ["averageOrderValue"] = redemptions.Count > 0 ? totalRevenue / redemptions.Count : 0
        };
    }

    public async Task<List<CampaignDto>> GetCampaignsAsync(bool activeOnly, int page, int pageSize)
    {
        var query = _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(c => c.Status == CampaignStatus.Active);

        var totalCount = await query.CountAsync();
        
        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return campaigns.Select(MapToDto).ToList();
    }

    public async Task<CampaignDto?> GetCampaignAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        return campaign != null ? MapToDto(campaign) : null;
    }

    public async Task<CampaignExecutionDto?> ExecuteCampaignForCustomerAsync(Guid campaignId, Guid customerId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) return null;

        var execution = new CampaignExecution
        {
            CampaignId = campaignId,
            CustomerId = customerId,
            Status = ExecutionStatus.Sent
        };

        _context.CampaignExecutions.Add(execution);
        await _context.SaveChangesAsync();

        return MapExecutionToDto(execution);
    }

    public async Task<Dictionary<string, object>> ExecuteCampaignForSegmentAsync(Guid campaignId, Guid segmentId)
    {
        var customers = await _context.CustomerSegmentMemberships
            .Where(m => m.SegmentId == segmentId && m.IsActive)
            .Select(m => m.CustomerId)
            .ToListAsync();

        var executions = new List<CampaignExecution>();
        foreach (var customerId in customers)
        {
            executions.Add(new CampaignExecution
            {
                CampaignId = campaignId,
                CustomerId = customerId,
                Status = ExecutionStatus.Sent
            });
        }

        _context.CampaignExecutions.AddRange(executions);
        await _context.SaveChangesAsync();

        return new Dictionary<string, object>
        {
            ["campaignId"] = campaignId,
            ["segmentId"] = segmentId,
            ["totalExecutions"] = executions.Count,
            ["executedAt"] = DateTime.UtcNow
        };
    }

    public async Task<CampaignRedemptionDto?> RedeemCampaignAsync(Guid campaignId, Guid customerId, Guid? orderId, decimal? orderTotal)
    {
        var redemption = new CampaignRedemption
        {
            CampaignId = campaignId,
            CustomerId = customerId,
            OrderId = orderId,
            OrderTotal = orderTotal
        };

        _context.CampaignRedemptions.Add(redemption);
        await _context.SaveChangesAsync();

        return MapRedemptionToDto(redemption);
    }

    public async Task<bool> IsCustomerEligibleForCampaignAsync(Guid customerId, Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null || campaign.Status != CampaignStatus.Active)
            return false;

        if (campaign.TargetSegmentId.HasValue)
        {
            return await _context.CustomerSegmentMemberships
                .AnyAsync(m => m.CustomerId == customerId && 
                              m.SegmentId == campaign.TargetSegmentId.Value && 
                              m.IsActive);
        }

        return true;
    }

    public async Task<List<CampaignDto>> GetCustomerCampaignsAsync(Guid customerId)
    {
        var customerSegments = await _context.CustomerSegmentMemberships
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.SegmentId)
            .ToListAsync();

        var campaigns = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .Where(c => c.Status == CampaignStatus.Active &&
                       (c.TargetSegmentId == null || customerSegments.Contains(c.TargetSegmentId.Value)))
            .ToListAsync();

        return campaigns.Select(MapToDto).ToList();
    }

    public async Task<decimal> GetCampaignUsageForCustomerAsync(Guid campaignId, Guid customerId)
    {
        var executions = await _context.CampaignExecutions
            .Where(e => e.CampaignId == campaignId && e.CustomerId == customerId)
            .ToListAsync();

        var redemptions = await _context.CampaignRedemptions
            .Where(r => r.CampaignId == campaignId && r.CustomerId == customerId)
            .ToListAsync();

        return redemptions.Where(r => r.OrderTotal.HasValue).Sum(r => r.OrderTotal.Value);
    }

    public async Task<int> GetCampaignRedemptionCountAsync(Guid campaignId)
    {
        return await _context.CampaignRedemptions
            .CountAsync(r => r.CampaignId == campaignId);
    }

    public async Task<List<CampaignDto>> GetActiveCampaignsAsync()
    {
        var campaigns = await _context.Campaigns
            .Include(c => c.TargetSegment)
            .Include(c => c.UpgradeMembershipLevel)
            .Where(c => c.Status == CampaignStatus.Active)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return campaigns.Select(MapToDto).ToList();
    }

    public async Task ProcessExpiredCampaignsAsync()
    {
        var expiredCampaigns = await _context.Campaigns
            .Where(c => c.Status == CampaignStatus.Active &&
                       c.EndDate.HasValue &&
                       c.EndDate.Value < DateTime.UtcNow)
            .ToListAsync();

        foreach (var campaign in expiredCampaigns)
        {
            campaign.Status = CampaignStatus.Completed;
            campaign.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed {Count} expired campaigns", expiredCampaigns.Count);
    }

    public async Task<bool> CanCustomerUseCampaignAsync(Guid campaignId, Guid customerId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null || !campaign.IsActive) return false;

        // Check if campaign has reached total usage limit
        if (campaign.HasUsageLimit) return false;

        // Check customer usage limit
        if (campaign.UsageLimitPerCustomer.HasValue)
        {
            var customerUsageCount = await _context.CampaignRedemptions
                .CountAsync(r => r.CampaignId == campaignId && r.CustomerId == customerId);

            if (customerUsageCount >= campaign.UsageLimitPerCustomer.Value) return false;
        }

        return true;
    }

    public async Task<decimal> CalculateCampaignDiscountAsync(Guid campaignId, decimal orderAmount)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) return 0;

        // Check minimum spend requirement
        if (campaign.MinimumSpend.HasValue && orderAmount < campaign.MinimumSpend.Value)
            return 0;

        decimal discount = 0;

        switch (campaign.CampaignType)
        {
            case CampaignType.PercentageDiscount:
                if (campaign.DiscountPercentage.HasValue)
                {
                    discount = orderAmount * (campaign.DiscountPercentage.Value / 100);
                }
                break;
            case CampaignType.FixedAmountDiscount:
                discount = campaign.DiscountAmount ?? 0;
                break;
            case CampaignType.MinimumSpendDiscount:
                if (campaign.MinimumSpend.HasValue && orderAmount >= campaign.MinimumSpend.Value)
                {
                    if (campaign.DiscountPercentage.HasValue)
                    {
                        discount = orderAmount * (campaign.DiscountPercentage.Value / 100);
                    }
                    else
                    {
                        discount = campaign.DiscountAmount ?? 0;
                    }
                }
                break;
        }

        // Apply maximum discount limit
        if (campaign.MaximumDiscount.HasValue && discount > campaign.MaximumDiscount.Value)
        {
            discount = campaign.MaximumDiscount.Value;
        }

        // Ensure discount doesn't exceed order amount
        return Math.Min(discount, orderAmount);
    }

    private string PersonalizeMessageForCustomer(Campaign campaign, Customer customer)
    {
        var message = campaign.MessageTemplate ?? "Special offer just for you!";

        // Replace common placeholders
        message = message.Replace("{CustomerName}", customer.FirstName);
        message = message.Replace("{FullName}", customer.FullName);
        message = message.Replace("{MembershipLevel}", customer.MembershipLevel?.Name ?? "");
        message = message.Replace("{LoyaltyPoints}", customer.LoyaltyPoints.ToString());

        if (campaign.DiscountPercentage.HasValue)
        {
            message = message.Replace("{DiscountPercentage}", campaign.DiscountPercentage.Value.ToString("0.##"));
        }

        if (campaign.DiscountAmount.HasValue)
        {
            message = message.Replace("{DiscountAmount}", campaign.DiscountAmount.Value.ToString("C"));
        }

        return message;
    }

    private static CommunicationProvider MapChannelToProvider(CampaignChannel channel)
    {
        return channel switch
        {
            CampaignChannel.Email => CommunicationProvider.Email,
            CampaignChannel.SMS => CommunicationProvider.SMS,
            CampaignChannel.WhatsApp => CommunicationProvider.WhatsApp,
            CampaignChannel.Push => CommunicationProvider.Push,
            _ => CommunicationProvider.Email
        };
    }

    private static CampaignDto MapToDto(Campaign campaign)
    {
        return new CampaignDto
        {
            CampaignId = campaign.CampaignId,
            Name = campaign.Name,
            Description = campaign.Description,
            CampaignType = campaign.CampaignType,
            Status = campaign.Status,
            Channel = campaign.Channel,
            TargetSegmentId = campaign.TargetSegmentId,
            TargetSegmentName = campaign.TargetSegment?.Name,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            DiscountPercentage = campaign.DiscountPercentage,
            DiscountAmount = campaign.DiscountAmount,
            MinimumSpend = campaign.MinimumSpend,
            MaximumDiscount = campaign.MaximumDiscount,
            LoyaltyPointsBonus = campaign.LoyaltyPointsBonus,
            WalletCreditAmount = campaign.WalletCreditAmount,
            FreeItemId = campaign.FreeItemId,
            UpgradeMembershipLevelId = campaign.UpgradeMembershipLevelId,
            UpgradeMembershipLevelName = campaign.UpgradeMembershipLevel?.Name,
            UsageLimitPerCustomer = campaign.UsageLimitPerCustomer,
            TotalUsageLimit = campaign.TotalUsageLimit,
            CurrentUsageCount = campaign.CurrentUsageCount,
            MessageTemplate = campaign.MessageTemplate,
            SubjectLine = campaign.SubjectLine,
            CallToAction = campaign.CallToAction,
            IsPersonalized = campaign.IsPersonalized,
            PersonalizationRulesJson = campaign.PersonalizationRulesJson,
            IsActive = campaign.IsActive,
            HasUsageLimit = campaign.HasUsageLimit,
            RedemptionRate = campaign.RedemptionRate,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            CreatedBy = campaign.CreatedBy
        };
    }

    private static CampaignExecutionDto MapExecutionToDto(CampaignExecution execution)
    {
        return new CampaignExecutionDto
        {
            ExecutionId = execution.ExecutionId,
            CampaignId = execution.CampaignId,
            CampaignName = execution.Campaign.Name,
            CustomerId = execution.CustomerId,
            CustomerName = execution.Customer.FullName,
            ExecutedAt = execution.ExecutedAt,
            ChannelUsed = Enum.Parse<CampaignChannel>(execution.ChannelUsed),
            MessageSent = execution.MessageSent,
            DeliveryStatus = execution.DeliveryStatus,
            OpenedAt = execution.OpenedAt,
            ClickedAt = execution.ClickedAt,
            ExternalMessageId = execution.ExternalMessageId
        };
    }

    private static CampaignRedemptionDto MapRedemptionToDto(CampaignRedemption redemption)
    {
        return new CampaignRedemptionDto
        {
            RedemptionId = redemption.RedemptionId,
            CampaignId = redemption.CampaignId,
            CampaignName = redemption.Campaign.Name,
            CustomerId = redemption.CustomerId,
            CustomerName = redemption.Customer.FullName,
            OrderId = redemption.OrderId,
            RedeemedAt = redemption.RedeemedAt,
            DiscountApplied = redemption.DiscountApplied,
            PointsAwarded = redemption.PointsAwarded,
            WalletCreditApplied = redemption.WalletCreditApplied,
            OrderTotal = redemption.OrderTotal
        };
    }
}
