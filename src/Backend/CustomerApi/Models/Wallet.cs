using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Models;

[Table("wallets", Schema = "customers")]
public class Wallet
{
    [Key]
    [Column("wallet_id")]
    public Guid WalletId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("balance")]
    [Precision(18, 2)]
    public decimal Balance { get; set; } = 0;

    [Column("total_loaded")]
    [Precision(18, 2)]
    public decimal TotalLoaded { get; set; } = 0;

    [Column("total_spent")]
    [Precision(18, 2)]
    public decimal TotalSpent { get; set; } = 0;

    [Column("last_transaction_date")]
    public DateTime? LastTransactionDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();

    // Methods
    public bool CanDeduct(decimal amount)
    {
        return IsActive && Balance >= amount && amount > 0;
    }

    public void AddFunds(decimal amount, string description, string? referenceId = null)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        
        Balance += amount;
        TotalLoaded += amount;
        LastTransactionDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Transactions.Add(new WalletTransaction
        {
            WalletId = WalletId,
            TransactionType = WalletTransactionType.Credit,
            Amount = amount,
            BalanceAfter = Balance,
            Description = description,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public void DeductFunds(decimal amount, string description, string? referenceId = null)
    {
        if (!CanDeduct(amount))
            throw new InvalidOperationException($"Cannot deduct {amount:C}. Current balance: {Balance:C}");

        Balance -= amount;
        TotalSpent += amount;
        LastTransactionDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Transactions.Add(new WalletTransaction
        {
            WalletId = WalletId,
            TransactionType = WalletTransactionType.Debit,
            Amount = amount,
            BalanceAfter = Balance,
            Description = description,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
