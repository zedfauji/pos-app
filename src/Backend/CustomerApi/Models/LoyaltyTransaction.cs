using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

public enum LoyaltyTransactionType
{
    Earned = 1,
    Redeemed = 2,
    Expired = 3,
    Bonus = 4,
    Adjustment = 5
}

[Table("loyalty_transactions", Schema = "customers")]
public class LoyaltyTransaction
{
    [Key]
    [Column("transaction_id")]
    public Guid TransactionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("transaction_type")]
    public LoyaltyTransactionType TransactionType { get; set; }

    [Column("points")]
    public int Points { get; set; }

    [Column("points_balance_after")]
    public int PointsBalanceAfter { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("order_id")]
    public Guid? OrderId { get; set; }

    [Column("order_amount")]
    [Precision(18, 2)]
    public decimal? OrderAmount { get; set; }

    [Column("expiry_date")]
    public DateTime? ExpiryDate { get; set; }

    [Column("is_expired")]
    public bool IsExpired { get; set; } = false;

    [Column("related_transaction_id")]
    public Guid? RelatedTransactionId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
}
