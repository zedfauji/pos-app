using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MagiDesk.Frontend.Services;

public class CustomerIntelligenceService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public CustomerIntelligenceService()
    {
        _httpClient = new HttpClient();
        _baseUrl = "https://magidesk-customer-904541739138.northamerica-south1.run.app";
    }

    public CustomerIntelligenceService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["CustomerApi:BaseUrl"] ?? "https://magidesk-customer-904541739138.northamerica-south1.run.app";
    }

    // Campaign Management
    public async Task<List<CampaignDto>> GetCampaignsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/campaigns");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<CampaignDto>>() ?? new List<CampaignDto>();
            }
            // Return empty list if API is not available
            return new List<CampaignDto>();
        }
        catch (Exception)
        {
            // Return empty list if API call fails (e.g., no backend running)
            return new List<CampaignDto>();
        }
    }

    public async Task<CampaignDto?> GetCampaignAsync(Guid campaignId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/campaigns/{campaignId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CampaignDto>();
        }
        return null;
    }

    public async Task<CampaignDto> CreateCampaignAsync(UICreateCampaignRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/campaigns", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CampaignDto>() ?? throw new InvalidOperationException("Failed to create campaign");
    }

    public async Task<CampaignDto> UpdateCampaignAsync(Guid campaignId, UIUpdateCampaignRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/campaigns/{campaignId}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CampaignDto>() ?? throw new InvalidOperationException("Failed to update campaign");
    }

    public async Task<bool> DeleteCampaignAsync(Guid campaignId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/campaigns/{campaignId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<CampaignExecutionResultDto> ExecuteCampaignAsync(string campaignId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/campaigns/{campaignId}/execute", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CampaignExecutionResultDto>() ?? new CampaignExecutionResultDto();
    }

    // Customer Segmentation
    public async Task<List<CustomerSegmentDto>> GetSegmentsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/segments");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<CustomerSegmentDto>>() ?? new List<CustomerSegmentDto>();
            }
            // Return empty list if API is not available
            return new List<CustomerSegmentDto>();
        }
        catch (Exception)
        {
            // Return empty list if API call fails (e.g., no backend running)
            return new List<CustomerSegmentDto>();
        }
    }

    public async Task<CustomerSegmentDto?> GetSegmentAsync(Guid segmentId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/segments/{segmentId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CustomerSegmentDto>();
        }
        return null;
    }

    public async Task<CustomerSegmentDto> CreateSegmentAsync(UICreateSegmentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/segments", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerSegmentDto>() ?? throw new InvalidOperationException("Failed to create segment");
    }

    public async Task<CustomerSegmentDto> UpdateSegmentAsync(Guid segmentId, UIUpdateSegmentRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/segments/{segmentId}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerSegmentDto>() ?? throw new InvalidOperationException("Failed to update segment");
    }

    public async Task<bool> DeleteSegmentAsync(Guid segmentId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/segments/{segmentId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CustomerDto>> GetSegmentCustomersAsync(string segmentId, int page = 1, int pageSize = 50)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/segments/{segmentId}/customers?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CustomerDto>>() ?? new List<CustomerDto>();
    }

    public async Task<List<CampaignActivityDto>> GetCampaignActivitiesAsync(string campaignId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/campaigns/{campaignId}/activities");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CampaignActivityDto>>() ?? new List<CampaignActivityDto>();
    }

    public async Task<bool> RefreshSegmentMembershipAsync(string segmentId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/segments/{segmentId}/refresh", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<int> PreviewSegmentSizeAsync(UICreateSegmentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/segments/preview", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SegmentPreviewResult>();
        return result?.EstimatedCount ?? 0;
    }

    // Behavioral Triggers
    public async Task<List<BehavioralTriggerDto>> GetTriggersAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/triggers");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<BehavioralTriggerDto>>() ?? new List<BehavioralTriggerDto>();
    }

    public async Task<BehavioralTriggerDto?> GetTriggerAsync(Guid triggerId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/triggers/{triggerId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BehavioralTriggerDto>();
        }
        return null;
    }

    public async Task<BehavioralTriggerDto> CreateTriggerAsync(CreateTriggerRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/triggers", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BehavioralTriggerDto>() ?? throw new InvalidOperationException("Failed to create trigger");
    }

    public async Task<BehavioralTriggerDto> UpdateTriggerAsync(Guid triggerId, UpdateTriggerRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/triggers/{triggerId}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BehavioralTriggerDto>() ?? throw new InvalidOperationException("Failed to update trigger");
    }

    public async Task<bool> DeleteTriggerAsync(Guid triggerId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/triggers/{triggerId}");
        return response.IsSuccessStatusCode;
    }

    // Analytics
    public async Task<CampaignAnalyticsDto?> GetCampaignAnalyticsAsync(string campaignId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/campaigns/{campaignId}/analytics");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CampaignAnalyticsDto>();
        }
        return null;
    }

    public async Task<SegmentAnalyticsDto?> GetSegmentAnalyticsAsync(Guid segmentId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/segments/{segmentId}/analytics");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<SegmentAnalyticsDto>();
        }
        return null;
    }

    // Customer Management
    public async Task<List<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 50)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/customers?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CustomerDto>>() ?? new List<CustomerDto>();
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/customers/{customerId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CustomerDto>();
        }
        return null;
    }
}

// DTOs for Customer Intelligence
public record CampaignDto
{
    public Guid CampaignId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CampaignType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public Guid? TargetSegmentId { get; init; }
    public string? TargetSegmentName { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? MinimumSpend { get; init; }
    public decimal? MaximumDiscount { get; init; }
    public int? LoyaltyPointsBonus { get; init; }
    public decimal? WalletCreditAmount { get; init; }
    public Guid? FreeItemId { get; init; }
    public Guid? UpgradeMembershipLevelId { get; init; }
    public string? UpgradeMembershipLevelName { get; init; }
    public int? UsageLimitPerCustomer { get; init; }
    public int? TotalUsageLimit { get; init; }
    public int CurrentUsageCount { get; init; }
    public string? MessageTemplate { get; init; }
    public string? SubjectLine { get; init; }
    public string? CallToAction { get; init; }
    public bool IsPersonalized { get; init; }
    public string? PersonalizationRulesJson { get; init; }
    public bool IsActive { get; init; }
    public bool HasUsageLimit { get; init; }
    public double RedemptionRate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record CreateCampaignRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CampaignType { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public Guid? TargetSegmentId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? MinimumSpend { get; init; }
    public decimal? MaximumDiscount { get; init; }
    public int? LoyaltyPointsBonus { get; init; }
    public decimal? WalletCreditAmount { get; init; }
    public Guid? FreeItemId { get; init; }
    public Guid? UpgradeMembershipLevelId { get; init; }
    public int? UsageLimitPerCustomer { get; init; } = 1;
    public int? TotalUsageLimit { get; init; }
    public string? MessageTemplate { get; init; }
    public string? SubjectLine { get; init; }
    public string? CallToAction { get; init; }
    public bool IsPersonalized { get; init; } = false;
    public string? PersonalizationRulesJson { get; init; }
}

public record UpdateCampaignRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? Channel { get; init; }
    public Guid? TargetSegmentId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? MinimumSpend { get; init; }
    public decimal? MaximumDiscount { get; init; }
    public int? LoyaltyPointsBonus { get; init; }
    public decimal? WalletCreditAmount { get; init; }
    public Guid? FreeItemId { get; init; }
    public Guid? UpgradeMembershipLevelId { get; init; }
    public int? UsageLimitPerCustomer { get; init; }
    public int? TotalUsageLimit { get; init; }
    public string? MessageTemplate { get; init; }
    public string? SubjectLine { get; init; }
    public string? CallToAction { get; init; }
    public bool? IsPersonalized { get; init; }
    public string? PersonalizationRulesJson { get; init; }
}

public record CustomerSegmentDto
{
    public Guid SegmentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CriteriaJson { get; init; } = "{}";
    public bool IsActive { get; init; }
    public bool AutoRefresh { get; init; }
    public DateTime? LastCalculatedAt { get; init; }
    public int CustomerCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record CreateSegmentRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CriteriaJson { get; init; } = "{}";
    public bool AutoRefresh { get; init; } = true;
}

public record UpdateSegmentRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CriteriaJson { get; init; }
    public bool? IsActive { get; init; }
    public bool? AutoRefresh { get; init; }
}

public record BehavioralTriggerDto
{
    public Guid TriggerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ConditionType { get; init; } = string.Empty;
    public decimal? ConditionValue { get; init; }
    public int? ConditionDays { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string? ActionParametersJson { get; init; }
    public Guid? TargetSegmentId { get; init; }
    public string? TargetSegmentName { get; init; }
    public bool IsActive { get; init; }
    public bool IsRecurring { get; init; }
    public int CooldownHours { get; init; }
    public int? MaxExecutions { get; init; }
    public int ExecutionCount { get; init; }
    public DateTime? LastExecutedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record CreateTriggerRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ConditionType { get; init; } = string.Empty;
    public decimal? ConditionValue { get; init; }
    public int? ConditionDays { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string? ActionParametersJson { get; init; }
    public Guid? TargetSegmentId { get; init; }
    public bool IsRecurring { get; init; } = false;
    public int CooldownHours { get; init; } = 24;
    public int? MaxExecutions { get; init; }
}

public record UpdateTriggerRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ConditionType { get; init; }
    public decimal? ConditionValue { get; init; }
    public int? ConditionDays { get; init; }
    public string? ActionType { get; init; }
    public string? ActionParametersJson { get; init; }
    public Guid? TargetSegmentId { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsRecurring { get; init; }
    public int? CooldownHours { get; init; }
    public int? MaxExecutions { get; init; }
}

public record CampaignAnalyticsDto
{
    public int MessagesSent { get; init; }
    public double MessagesSentChange { get; init; }
    public int Redemptions { get; init; }
    public double RedemptionsChange { get; init; }
    public double RedemptionRate { get; init; }
    public double RedemptionRateChange { get; init; }
    public decimal RevenueImpact { get; init; }
    public decimal RevenueImpactChange { get; init; }
    public int UniqueCustomers { get; init; }
    public double UniqueCustomersChange { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal AverageOrderValueChange { get; init; }
    public decimal CostPerAcquisition { get; init; }
    public decimal CostPerAcquisitionChange { get; init; }
    public double ROI { get; init; }
    public double ROIChange { get; init; }
}

public record SegmentAnalyticsDto
{
    public Guid SegmentId { get; init; }
    public string SegmentName { get; init; } = string.Empty;
    public int CustomerCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageCustomerValue { get; init; }
    public int TotalOrders { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int ActiveCampaigns { get; init; }
    public DateTime? LastUpdated { get; init; }
}

// Using existing CustomerDto from CustomerApiService.cs

// Additional DTOs for Customer Intelligence
public record CampaignExecutionResultDto
{
    public int MessagesSent { get; init; }
    public List<string> Errors { get; init; } = new();
    public bool Success { get; init; }
}

public record CampaignActivityDto
{
    public string Id { get; init; } = string.Empty;
    public string ActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
}

public record SegmentPreviewResult
{
    public int EstimatedCount { get; init; }
}

// DTOs for UI components (different from API DTOs to avoid conflicts)
public class SegmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UICreateSegmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<UISegmentCriterion> Criteria { get; set; } = new();
}

public class UIUpdateSegmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<UISegmentCriterion> Criteria { get; set; } = new();
}

public class UICreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? TargetSegmentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int MaxRedemptionsPerCustomer { get; set; }
    public decimal MinimumOrderValue { get; set; }
    public string MessageTemplate { get; set; } = string.Empty;
    public decimal OfferValue { get; set; }
    public Dictionary<string, object> OfferConfiguration { get; set; } = new();
}

public class UIUpdateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? TargetSegmentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int MaxRedemptionsPerCustomer { get; set; }
    public decimal MinimumOrderValue { get; set; }
    public string MessageTemplate { get; set; } = string.Empty;
    public decimal OfferValue { get; set; }
    public Dictionary<string, object> OfferConfiguration { get; set; } = new();
}

public class UISegmentCriterion
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
