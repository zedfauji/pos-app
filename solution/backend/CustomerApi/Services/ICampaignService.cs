using CustomerApi.DTOs;
using CustomerApi.Models;

namespace CustomerApi.Services;

public interface ICampaignService
{
    // Campaign management
    Task<CampaignDto> CreateCampaignAsync(CampaignDto request);
    Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignDto request);
    Task<bool> DeleteCampaignAsync(Guid campaignId);
    Task<CampaignDto?> GetCampaignAsync(Guid campaignId);
    Task<List<CampaignDto>> GetCampaignsAsync(bool includeInactive = false, int page = 1, int pageSize = 50);

    // Campaign execution
    Task<CampaignExecutionDto?> ExecuteCampaignForCustomerAsync(Guid campaignId, Guid customerId);
    Task<Dictionary<string, object>> ExecuteCampaignForSegmentAsync(Guid campaignId, Guid segmentId);

    // Campaign redemption
    Task<CampaignRedemptionDto?> RedeemCampaignAsync(Guid campaignId, Guid customerId, Guid? orderId = null, decimal? usedAmount = null);
    Task<List<CampaignRedemptionDto>> GetCampaignRedemptionsAsync(Guid campaignId, int page = 1, int pageSize = 50);

    // Campaign analytics
    Task<Dictionary<string, object>?> GetCampaignAnalyticsAsync(Guid campaignId);
    Task<List<CampaignExecutionDto>> GetCampaignExecutionsAsync(Guid campaignId, int page = 1, int pageSize = 50);

    // Customer eligibility
    Task<bool> IsCustomerEligibleForCampaignAsync(Guid campaignId, Guid customerId);
    Task<List<CampaignDto>> GetCustomerCampaignsAsync(Guid customerId);

    // Utility
    Task<decimal> GetCampaignUsageForCustomerAsync(Guid campaignId, Guid customerId);
    Task<int> GetCampaignRedemptionCountAsync(Guid campaignId);
    Task<List<CampaignDto>> GetActiveCampaignsAsync();
}
