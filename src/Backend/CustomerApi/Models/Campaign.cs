using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

public enum CampaignType
{
    PercentageDiscount = 1,
    FixedAmountDiscount = 2,
    FreeItem = 3,
    LoyaltyPointsBonus = 4,
    WalletCredit = 5,
    MembershipUpgrade = 6,
    BuyOneGetOne = 7,
    MinimumSpendDiscount = 8
}

public enum CampaignStatus
{
    Draft = 1,
    Active = 2,
    Paused = 3,
    Completed = 4,
    Cancelled = 5
}

public enum CampaignChannel
{
    InApp = 1,
    Email = 2,
    SMS = 3,
    WhatsApp = 4,
    Push = 5,
    All = 6
}

[Table("campaigns", Schema = "customers")]
public class Campaign
{
    [Key]
    [Column("campaign_id")]
    public Guid CampaignId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("campaign_type")]
    public CampaignType CampaignType { get; set; }

    [Column("status")]
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    [Column("channel")]
    public CampaignChannel Channel { get; set; } = CampaignChannel.InApp;

    [Column("target_segment_id")]
    public Guid? TargetSegmentId { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("valid_from")]
    public DateTime? ValidFrom { get; set; }

    [Column("valid_to")]
    public DateTime? ValidTo { get; set; }

    [Column("communication_channel")]
    public CommunicationProvider CommunicationChannel { get; set; } = CommunicationProvider.Email;

    // Campaign Value Configuration
    [Column("discount_percentage")]
    [Precision(5, 2)]
    public decimal? DiscountPercentage { get; set; }

    [Column("discount_amount")]
    [Precision(18, 2)]
    public decimal? DiscountAmount { get; set; }

    [Column("minimum_spend")]
    [Precision(18, 2)]
    public decimal? MinimumSpend { get; set; }

    [Column("maximum_discount")]
    [Precision(18, 2)]
    public decimal? MaximumDiscount { get; set; }

    [Column("loyalty_points_bonus")]
    public int? LoyaltyPointsBonus { get; set; }

    [Column("wallet_credit_amount")]
    [Precision(18, 2)]
    public decimal? WalletCreditAmount { get; set; }

    [Column("free_item_id")]
    public Guid? FreeItemId { get; set; }

    [Column("upgrade_membership_level_id")]
    public Guid? UpgradeMembershipLevelId { get; set; }

    // Usage Limits
    [Column("usage_limit_per_customer")]
    public int? UsageLimitPerCustomer { get; set; } = 1;

    [Column("total_usage_limit")]
    public int? TotalUsageLimit { get; set; }

    [Column("current_usage_count")]
    public int CurrentUsageCount { get; set; } = 0;

    // Message Configuration
    [Column("message_template")]
    public string? MessageTemplate { get; set; }

    [Column("subject_line")]
    [MaxLength(200)]
    public string? SubjectLine { get; set; }

    [Column("call_to_action")]
    [MaxLength(50)]
    public string? CallToAction { get; set; }

    // Personalization
    [Column("is_personalized")]
    public bool IsPersonalized { get; set; } = false;

    [Column("personalization_rules_json")]
    public string? PersonalizationRulesJson { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("channel_used")]
    [MaxLength(50)]
    public string? ChannelUsed { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public virtual CustomerSegment? TargetSegment { get; set; }
    public virtual MembershipLevel? UpgradeMembershipLevel { get; set; }
    public virtual ICollection<CampaignExecution> Executions { get; set; } = new List<CampaignExecution>();
    public virtual ICollection<CampaignRedemption> Redemptions { get; set; } = new List<CampaignRedemption>();

    // Computed properties
    [NotMapped]
    public bool HasUsageLimit => TotalUsageLimit.HasValue && TotalUsageLimit.Value > 0;

    [NotMapped]
    public bool IsActive => Status == CampaignStatus.Active;

    [NotMapped]
    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;

    [NotMapped]
    public bool IsValid => IsActive && !IsExpired && 
                          StartDate <= DateTime.UtcNow &&
                          (EndDate == null || EndDate >= DateTime.UtcNow);

    [NotMapped]
    public int DaysRemaining => EndDate.HasValue ? 
        Math.Max(0, (int)(EndDate.Value - DateTime.UtcNow).TotalDays) : 0;

    [NotMapped]
    public double RedemptionRate => Executions.Count == 0 ? 0 : 
        (double)Redemptions.Count / Executions.Count * 100;
}

[Table("campaign_executions", Schema = "customers")]
public class CampaignExecution
{
    [Key]
    [Column("execution_id")]
    public Guid ExecutionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("campaign_id")]
    public Guid CampaignId { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("executed_at")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("execution_data_json")]
    public string? ExecutionDataJson { get; set; }

    [Column("message_sent")]
    public string? MessageSent { get; set; }

    [Column("delivery_status")]
    [MaxLength(50)]
    public string? DeliveryStatus { get; set; }

    [Column("status")]
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    [Column("opened_at")]
    public DateTime? OpenedAt { get; set; }

    [Column("clicked_at")]
    public DateTime? ClickedAt { get; set; }

    [Column("external_message_id")]
    public string? ExternalMessageId { get; set; }

    [Column("channel_used")]
    [MaxLength(50)]
    public string? ChannelUsed { get; set; }

    // Navigation properties
    public virtual Campaign Campaign { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
}

[Table("campaign_redemptions", Schema = "customers")]
public class CampaignRedemption
{
    [Key]
    [Column("redemption_id")]
    public Guid RedemptionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("campaign_id")]
    public Guid CampaignId { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("execution_id")]
    public Guid ExecutionId { get; set; }

    [Column("order_id")]
    public Guid? OrderId { get; set; }

    [Column("redeemed_at")]
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

    [Column("discount_applied")]
    [Precision(18, 2)]
    public decimal? DiscountApplied { get; set; }

    [Column("points_awarded")]
    public int? PointsAwarded { get; set; }

    [Column("wallet_credit_applied")]
    [Precision(18, 2)]
    public decimal? WalletCreditApplied { get; set; }

    [Column("order_total")]
    [Precision(18, 2)]
    public decimal? OrderTotal { get; set; }

    // Navigation properties
    public virtual Campaign Campaign { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
    public virtual CampaignExecution Execution { get; set; } = null!;
}
