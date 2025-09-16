using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

[Table("customers", Schema = "customers")]
public class Customer
{
    [Key]
    [Column("customer_id")]
    public Guid CustomerId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [Column("photo_url")]
    public string? PhotoUrl { get; set; }

    [Required]
    [Column("membership_level_id")]
    public Guid MembershipLevelId { get; set; }

    [Column("membership_start_date")]
    public DateTime MembershipStartDate { get; set; } = DateTime.UtcNow;

    [Column("membership_expiry_date")]
    public DateTime? MembershipExpiryDate { get; set; }

    [Column("total_spent")]
    [Precision(18, 2)]
    public decimal TotalSpent { get; set; } = 0;

    [Column("total_visits")]
    public int TotalVisits { get; set; } = 0;

    [Column("loyalty_points")]
    public int LoyaltyPoints { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("notes")]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual MembershipLevel MembershipLevel { get; set; } = null!;
    public virtual Wallet? Wallet { get; set; }
    public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public bool IsMembershipExpired => MembershipExpiryDate.HasValue && MembershipExpiryDate.Value < DateTime.UtcNow;

    [NotMapped]
    public int DaysUntilExpiry => MembershipExpiryDate.HasValue 
        ? Math.Max(0, (int)(MembershipExpiryDate.Value - DateTime.UtcNow).TotalDays)
        : int.MaxValue;
}
