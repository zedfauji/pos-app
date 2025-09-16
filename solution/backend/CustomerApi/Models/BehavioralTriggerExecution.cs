using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerApi.Models;

[Table("behavioral_trigger_executions", Schema = "customers")]
public class BehavioralTriggerExecution
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
    public bool Success { get; set; }

    [Column("result_message")]
    [MaxLength(500)]
    public string? ResultMessage { get; set; }

    [Column("result_data_json")]
    public string? ResultDataJson { get; set; }

    [Column("execution_duration_ms")]
    public int? ExecutionDurationMs { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual BehavioralTrigger Trigger { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
}
