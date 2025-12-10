using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

public enum TriggerCondition
{
    WalletBalanceBelow = 1,
    WalletBalanceAbove = 2,
    LoyaltyPointsBelow = 3,
    LoyaltyPointsAbove = 4,
    LastVisitDaysAgo = 5,
    TotalSpentBelow = 6,
    TotalSpentAbove = 7,
    MembershipExpiringSoon = 8,
    BirthdayThisMonth = 9,
    OrderCountBelow = 10,
    OrderCountAbove = 11,
    AverageOrderValueBelow = 12,
    AverageOrderValueAbove = 13,
    DaysSinceLastVisit = 14,
    OrderFrequency = 15
}

public enum TriggerAction
{
    SendNotification = 1,
    AddToSegment = 2,
    GrantLoyaltyPoints = 3,
    ApplyDiscount = 4,
    CreateCampaign = 5,
    UpdateMembershipLevel = 6,
    AddWalletFunds = 7,
    SendPersonalizedOffer = 8,
    SendMessage = 9,
    AddLoyaltyPoints = 10
}

[Table("behavioral_triggers", Schema = "customers")]
public class BehavioralTrigger
{
    [Key]
    [Column("trigger_id")]
    public Guid TriggerId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("condition_type")]
    public TriggerCondition ConditionType { get; set; }

    [Column("condition_value")]
    [Precision(18, 2)]
    public decimal? ConditionValue { get; set; }

    [Column("condition_days")]
    public int? ConditionDays { get; set; }

    [Column("action_type")]
    public TriggerAction ActionType { get; set; }

    [Column("action_parameters_json")]
    public string? ActionParametersJson { get; set; }

    [Column("target_segment_id")]
    public Guid? TargetSegmentId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_recurring")]
    public bool IsRecurring { get; set; } = false;

    [Column("cooldown_hours")]
    public int CooldownHours { get; set; } = 24;

    [Column("cooldown_minutes")]
    public int CooldownMinutes { get; set; } = 1440; // 24 hours in minutes

    [Column("max_executions")]
    public int? MaxExecutions { get; set; }

    [Column("max_executions_per_customer")]
    public int MaxExecutionsPerCustomer { get; set; } = 0;

    [Column("execution_count")]
    public int ExecutionCount { get; set; } = 0;

    [Column("last_executed_at")]
    public DateTime? LastExecutedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public virtual CustomerSegment? TargetSegment { get; set; }
    public virtual ICollection<TriggerExecution> Executions { get; set; } = new List<TriggerExecution>();
}

[Table("trigger_executions", Schema = "customers")]
public class TriggerExecution
{
    [Key]
    [Column("execution_id")]
    public Guid ExecutionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("trigger_id")]
    public Guid TriggerId { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("executed_at")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    [Column("success")]
    public bool Success { get; set; } = true;

    [Column("result_message")]
    public string? ResultMessage { get; set; }

    [Column("result_data_json")]
    public string? ResultDataJson { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public virtual BehavioralTrigger Trigger { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
}
