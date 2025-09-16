using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

public enum WalletTransactionType
{
    Credit = 1,
    Debit = 2
}

[Table("wallet_transactions", Schema = "customers")]
public class WalletTransaction
{
    [Key]
    [Column("transaction_id")]
    public Guid TransactionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("wallet_id")]
    public Guid WalletId { get; set; }

    [Column("transaction_type")]
    public WalletTransactionType TransactionType { get; set; }

    [Column("amount")]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Column("balance_after")]
    [Precision(18, 2)]
    public decimal BalanceAfter { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("reference_id")]
    public string? ReferenceId { get; set; }

    [Column("order_id")]
    public Guid? OrderId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public virtual Wallet Wallet { get; set; } = null!;
}
