using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

public enum SegmentType
{
    Manual,
    Automatic,
    Dynamic
}

[Table("customer_segments", Schema = "customers")]
public class CustomerSegment
{
    [Key]
    [Column("segment_id")]
    public Guid SegmentId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("criteria_json")]
    public string CriteriaJson { get; set; } = "{}";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("auto_refresh")]
    public bool AutoRefresh { get; set; } = true;

    [Column("last_calculated_at")]
    public DateTime? LastCalculatedAt { get; set; }

    [Column("customer_count")]
    public int CustomerCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [Column("segment_type")]
    public SegmentType SegmentType { get; set; } = SegmentType.Manual;

    // Navigation properties
    public virtual ICollection<CustomerSegmentMembership> Memberships { get; set; } = new List<CustomerSegmentMembership>();

    [NotMapped]
    public bool HasCriteria => !string.IsNullOrEmpty(CriteriaJson) && CriteriaJson != "{}";
}

[Table("customer_segment_memberships", Schema = "customers")]
public class CustomerSegmentMembership
{
    [Key]
    [Column("membership_id")]
    public Guid MembershipId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("segment_id")]
    public Guid SegmentId { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual CustomerSegment Segment { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
}
