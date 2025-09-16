using System.ComponentModel.DataAnnotations;
using CustomerApi.Models;

namespace CustomerApi.DTOs;

// Customer Segmentation DTOs
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
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Required]
    public string CriteriaJson { get; init; } = "{}";

    public bool AutoRefresh { get; init; } = true;
}

public record UpdateSegmentRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public string? CriteriaJson { get; init; }

    public bool? IsActive { get; init; }

    public bool? AutoRefresh { get; init; }
}

public record SegmentCriteria
{
    public decimal? MinTotalSpent { get; init; }
    public decimal? MaxTotalSpent { get; init; }
    public int? MinTotalVisits { get; init; }
    public int? MaxTotalVisits { get; init; }
    public int? MinLoyaltyPoints { get; init; }
    public int? MaxLoyaltyPoints { get; init; }
    public decimal? MinWalletBalance { get; init; }
    public decimal? MaxWalletBalance { get; init; }
    public List<Guid>? MembershipLevelIds { get; init; }
    public int? LastVisitDaysAgo { get; init; }
    public int? MembershipExpiringInDays { get; init; }
    public bool? BirthdayThisMonth { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public decimal? MinAverageOrderValue { get; init; }
    public decimal? MaxAverageOrderValue { get; init; }
}

// Behavioral Trigger DTOs
public record BehavioralTriggerDto
{
    public Guid TriggerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TriggerCondition ConditionType { get; init; }
    public decimal? ConditionValue { get; init; }
    public int? ConditionDays { get; init; }
    public TriggerAction ActionType { get; init; }
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
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Required]
    public TriggerCondition ConditionType { get; init; }

    public decimal? ConditionValue { get; init; }

    public int? ConditionDays { get; init; }

    [Required]
    public TriggerAction ActionType { get; init; }

    public string? ActionParametersJson { get; init; }

    public Guid? TargetSegmentId { get; init; }

    public bool IsRecurring { get; init; } = false;

    [Range(1, 168)]
    public int CooldownHours { get; init; } = 24;

    public int? MaxExecutions { get; init; }
}

public record UpdateTriggerRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public TriggerCondition? ConditionType { get; init; }

    public decimal? ConditionValue { get; init; }

    public int? ConditionDays { get; init; }

    public TriggerAction? ActionType { get; init; }

    public string? ActionParametersJson { get; init; }

    public Guid? TargetSegmentId { get; init; }

    public bool? IsActive { get; init; }

    public bool? IsRecurring { get; init; }

    public int? CooldownHours { get; init; }

    public int? MaxExecutions { get; init; }
}

public record TriggerExecutionDto
{
    public Guid ExecutionId { get; init; }
    public Guid TriggerId { get; init; }
    public string TriggerName { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; }
    public bool Success { get; init; }
    public string? ResultMessage { get; init; }
    public string? ResultDataJson { get; init; }
}

// Campaign DTOs
public record CampaignDto
{
    public Guid CampaignId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public CampaignType CampaignType { get; init; }
    public CampaignStatus Status { get; init; }
    public CampaignChannel Channel { get; init; }
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
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Required]
    public CampaignType CampaignType { get; init; }

    [Required]
    public CampaignChannel Channel { get; init; }

    public Guid? TargetSegmentId { get; init; }

    [Required]
    public DateTime StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    [Range(0, 100)]
    public decimal? DiscountPercentage { get; init; }

    [Range(0, 10000)]
    public decimal? DiscountAmount { get; init; }

    [Range(0, 10000)]
    public decimal? MinimumSpend { get; init; }

    [Range(0, 10000)]
    public decimal? MaximumDiscount { get; init; }

    [Range(0, 10000)]
    public int? LoyaltyPointsBonus { get; init; }

    [Range(0, 10000)]
    public decimal? WalletCreditAmount { get; init; }

    public Guid? FreeItemId { get; init; }

    public Guid? UpgradeMembershipLevelId { get; init; }

    [Range(1, 1000)]
    public int? UsageLimitPerCustomer { get; init; } = 1;

    public int? TotalUsageLimit { get; init; }

    public string? MessageTemplate { get; init; }

    [StringLength(200)]
    public string? SubjectLine { get; init; }

    [StringLength(50)]
    public string? CallToAction { get; init; }

    public bool IsPersonalized { get; init; } = false;

    public string? PersonalizationRulesJson { get; init; }
}

public record UpdateCampaignRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public CampaignStatus? Status { get; init; }

    public CampaignChannel? Channel { get; init; }

    public Guid? TargetSegmentId { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    [Range(0, 100)]
    public decimal? DiscountPercentage { get; init; }

    [Range(0, 10000)]
    public decimal? DiscountAmount { get; init; }

    [Range(0, 10000)]
    public decimal? MinimumSpend { get; init; }

    [Range(0, 10000)]
    public decimal? MaximumDiscount { get; init; }

    [Range(0, 10000)]
    public int? LoyaltyPointsBonus { get; init; }

    [Range(0, 10000)]
    public decimal? WalletCreditAmount { get; init; }

    public Guid? FreeItemId { get; init; }

    public Guid? UpgradeMembershipLevelId { get; init; }

    [Range(1, 1000)]
    public int? UsageLimitPerCustomer { get; init; }

    public int? TotalUsageLimit { get; init; }

    public string? MessageTemplate { get; init; }

    [StringLength(200)]
    public string? SubjectLine { get; init; }

    [StringLength(50)]
    public string? CallToAction { get; init; }

    public bool? IsPersonalized { get; init; }

    public string? PersonalizationRulesJson { get; init; }
}

public record CampaignExecutionDto
{
    public Guid ExecutionId { get; init; }
    public Guid CampaignId { get; init; }
    public string CampaignName { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; }
    public CampaignChannel ChannelUsed { get; init; }
    public string? MessageSent { get; init; }
    public string? DeliveryStatus { get; init; }
    public DateTime? OpenedAt { get; init; }
    public DateTime? ClickedAt { get; init; }
    public string? ExternalMessageId { get; init; }
}

public record CampaignRedemptionDto
{
    public Guid RedemptionId { get; init; }
    public Guid CampaignId { get; init; }
    public string CampaignName { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public DateTime RedeemedAt { get; init; }
    public decimal? DiscountApplied { get; init; }
    public int? PointsAwarded { get; init; }
    public decimal? WalletCreditApplied { get; init; }
    public decimal? OrderTotal { get; init; }
}

// Communication DTOs
public record CommunicationProviderDto
{
    public Guid ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public CommunicationProvider ProviderType { get; init; }
    public string ConfigurationJson { get; init; } = "{}";
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public int Priority { get; init; }
    public int? RateLimitPerMinute { get; init; }
    public int? RateLimitPerHour { get; init; }
    public int? RateLimitPerDay { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CommunicationTemplateDto
{
    public Guid TemplateId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public CommunicationProvider ProviderType { get; init; }
    public string? SubjectTemplate { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
    public string? VariablesJson { get; init; }
    public bool IsActive { get; init; }
    public string? Category { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record CommunicationLogDto
{
    public Guid LogId { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public Guid? ProviderId { get; init; }
    public string? ProviderName { get; init; }
    public Guid? TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public Guid? CampaignId { get; init; }
    public string? CampaignName { get; init; }
    public Guid? TriggerId { get; init; }
    public string? TriggerName { get; init; }
    public CommunicationProvider ProviderType { get; init; }
    public string Recipient { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string MessageBody { get; init; } = string.Empty;
    public MessageStatus Status { get; init; }
    public string? ExternalMessageId { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? OpenedAt { get; init; }
    public DateTime? ClickedAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public string? FailureReason { get; init; }
    public int RetryCount { get; init; }
    public int MaxRetries { get; init; }
    public bool CanRetry { get; init; }
    public TimeSpan? DeliveryTime { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

// Request/Response Models
public record SendMessageRequest
{
    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    public CommunicationProvider ProviderType { get; init; }

    public Guid? TemplateId { get; init; }

    public string? Subject { get; init; }

    [Required]
    public string MessageBody { get; init; } = string.Empty;

    public Dictionary<string, object>? Variables { get; init; }

    public Guid? CampaignId { get; init; }

    public Guid? TriggerId { get; init; }
}

public record CampaignAnalyticsDto
{
    public Guid CampaignId { get; init; }
    public string CampaignName { get; init; } = string.Empty;
    public int TotalExecutions { get; init; }
    public int TotalRedemptions { get; init; }
    public double RedemptionRate { get; init; }
    public int MessagesOpened { get; init; }
    public int MessagesClicked { get; init; }
    public double OpenRate { get; init; }
    public double ClickRate { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalDiscountGiven { get; init; }
    public decimal AverageOrderValue { get; init; }
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

public record CampaignUsageDto
{
    public Guid CampaignId { get; init; }
    public Guid CustomerId { get; init; }
    public int TotalExecutions { get; init; }
    public int TotalRedemptions { get; init; }
    public DateTime? LastExecutedAt { get; init; }
    public DateTime? LastRedeemedAt { get; init; }
}
