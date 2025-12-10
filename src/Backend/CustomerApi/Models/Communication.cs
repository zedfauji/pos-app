using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

public enum CommunicationProvider
{
    Email = 1,
    SMS = 2,
    WhatsApp = 3,
    Push = 4
}

public enum MessageStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Opened = 4,
    Clicked = 5,
    Failed = 6,
    Bounced = 7
}

[Table("communication_providers", Schema = "customers")]
public class CommunicationProviderConfig
{
    [Key]
    [Column("provider_id")]
    public Guid ProviderId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("provider_name")]
    public string ProviderName { get; set; } = string.Empty;

    [Column("provider_type")]
    public CommunicationProvider ProviderType { get; set; }

    [Column("configuration_json")]
    public string ConfigurationJson { get; set; } = "{}";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("priority")]
    public int Priority { get; set; } = 0;

    [Column("rate_limit_per_minute")]
    public int? RateLimitPerMinute { get; set; }

    [Column("rate_limit_per_hour")]
    public int? RateLimitPerHour { get; set; }

    [Column("rate_limit_per_day")]
    public int? RateLimitPerDay { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CommunicationLog> Messages { get; set; } = new List<CommunicationLog>();
}

[Table("communication_templates", Schema = "customers")]
public class CommunicationTemplate
{
    [Key]
    [Column("template_id")]
    public Guid TemplateId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("provider_type")]
    public CommunicationProvider ProviderType { get; set; }

    [Column("subject_template")]
    [MaxLength(200)]
    public string? SubjectTemplate { get; set; }

    [Column("body_template")]
    public string BodyTemplate { get; set; } = string.Empty;

    [Column("variables_json")]
    public string? VariablesJson { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("category")]
    [MaxLength(50)]
    public string? Category { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<CommunicationLog> CommunicationLogs { get; set; } = new List<CommunicationLog>();
}

[Table("communication_log", Schema = "customers")]
public class CommunicationLog
{
    [Key]
    [Column("log_id")]
    public Guid LogId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("provider_id")]
    public Guid? ProviderId { get; set; }

    [Column("template_id")]
    public Guid? TemplateId { get; set; }

    [Column("campaign_id")]
    public Guid? CampaignId { get; set; }

    [Column("trigger_id")]
    public Guid? TriggerId { get; set; }

    [Column("provider_type")]
    public CommunicationProvider ProviderType { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [Column("subject")]
    [MaxLength(200)]
    public string? Subject { get; set; }

    [Column("message_body")]
    public string MessageBody { get; set; } = string.Empty;

    [Column("status")]
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    [Column("external_message_id")]
    public string? ExternalMessageId { get; set; }

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("delivered_at")]
    public DateTime? DeliveredAt { get; set; }

    [Column("opened_at")]
    public DateTime? OpenedAt { get; set; }

    [Column("clicked_at")]
    public DateTime? ClickedAt { get; set; }

    [Column("failed_at")]
    public DateTime? FailedAt { get; set; }

    [Column("failure_reason")]
    public string? FailureReason { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    [Column("max_retries")]
    public int MaxRetries { get; set; } = 3;

    [Column("metadata_json")]
    public string? MetadataJson { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual CommunicationProviderConfig? Provider { get; set; }
    public virtual CommunicationTemplate? Template { get; set; }
    public virtual Campaign? Campaign { get; set; }
    public virtual BehavioralTrigger? Trigger { get; set; }

    // Computed properties
    [NotMapped]
    public bool CanRetry => RetryCount < MaxRetries && 
                           Status == MessageStatus.Failed && 
                           FailedAt.HasValue &&
                           FailedAt.Value.AddMinutes(RetryCount * 5) < DateTime.UtcNow;

    [NotMapped]
    public TimeSpan? DeliveryTime => SentAt.HasValue && DeliveredAt.HasValue ? 
        DeliveredAt.Value - SentAt.Value : null;
}
