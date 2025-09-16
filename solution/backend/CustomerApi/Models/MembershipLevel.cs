using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

[Table("membership_levels", Schema = "customers")]
public class MembershipLevel
{
    [Key]
    [Column("membership_level_id")]
    public Guid MembershipLevelId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("discount_percentage")]
    [Precision(5, 2)]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column("loyalty_multiplier")]
    [Precision(3, 2)]
    public decimal LoyaltyMultiplier { get; set; } = 1.0m;

    [Column("minimum_spend_requirement")]
    [Precision(18, 2)]
    public decimal? MinimumSpendRequirement { get; set; }

    [Column("validity_months")]
    public int? ValidityMonths { get; set; }

    [Column("color_hex")]
    [MaxLength(7)]
    public string ColorHex { get; set; } = "#808080";

    [Column("icon")]
    [MaxLength(50)]
    public string? Icon { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    // Benefits as JSON or separate table - for now using simple properties
    [Column("max_wallet_balance")]
    [Precision(18, 2)]
    public decimal? MaxWalletBalance { get; set; }

    [Column("free_delivery")]
    public bool FreeDelivery { get; set; } = false;

    [Column("priority_support")]
    public bool PrioritySupport { get; set; } = false;

    [Column("birthday_bonus_points")]
    public int BirthdayBonusPoints { get; set; } = 0;
}
