using System.ComponentModel.DataAnnotations;

namespace CustomerApi.DTOs;

public record CustomerDto
{
    public Guid CustomerId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public Guid MembershipLevelId { get; init; }
    public string? MembershipLevel { get; init; }
    public DateTime MembershipStartDate { get; init; }
    public DateTime? MembershipExpiryDate { get; init; }
    public DateTime RegistrationDate { get; init; }
    public DateTime? LastVisit { get; init; }
    public bool IsMembershipExpired { get; init; }
    public int DaysUntilExpiry { get; init; }
    public decimal TotalSpent { get; init; }
    public int TotalVisits { get; init; }
    public int LoyaltyPoints { get; init; }
    public WalletDto? Wallet { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? Notes { get; init; }
    public string? PreferredLanguage { get; init; }
    public bool MarketingOptIn { get; init; }
}

public record CreateCustomerRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Phone { get; init; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; init; }

    public DateTime? DateOfBirth { get; init; }

    public string? PhotoUrl { get; init; }

    public Guid? MembershipLevelId { get; init; }

    public DateTime? MembershipExpiryDate { get; init; }

    public string? Notes { get; init; }
}

public record UpdateCustomerRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? FirstName { get; init; }

    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; init; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; init; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; init; }

    public DateTime? DateOfBirth { get; init; }

    public string? PhotoUrl { get; init; }

    public Guid? MembershipLevelId { get; init; }

    public DateTime? MembershipExpiryDate { get; init; }

    public bool? IsActive { get; init; }

    public string? Notes { get; init; }
}

public record CustomerSearchRequest
{
    public string? SearchTerm { get; init; }
    public Guid? MembershipLevelId { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsMembershipExpired { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public decimal? MinTotalSpent { get; init; }
    public decimal? MaxTotalSpent { get; init; }
    public int? MinLoyaltyPoints { get; init; }
    public string SortBy { get; init; } = "FullName";
    public bool SortDescending { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record CustomerStatsDto
{
    public int TotalCustomers { get; init; }
    public int ActiveCustomers { get; init; }
    public int ExpiredMemberships { get; init; }
    public decimal TotalCustomerValue { get; init; }
    public decimal AverageCustomerValue { get; init; }
    public int TotalLoyaltyPoints { get; init; }
    public Dictionary<string, int> CustomersByMembershipLevel { get; init; } = new();
    public Dictionary<string, decimal> RevenueByMembershipLevel { get; init; } = new();
}

public record MembershipLevelDto
{
    public Guid MembershipLevelId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal DiscountPercentage { get; init; }
    public decimal LoyaltyMultiplier { get; init; }
    public decimal? MinimumSpendRequirement { get; init; }
    public int? ValidityMonths { get; init; }
    public string ColorHex { get; init; } = "#808080";
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public decimal? MaxWalletBalance { get; init; }
    public bool FreeDelivery { get; init; }
    public bool PrioritySupport { get; init; }
    public int BirthdayBonusPoints { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record WalletDto
{
    public Guid WalletId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Balance { get; init; }
    public decimal TotalLoaded { get; init; }
    public decimal TotalSpent { get; init; }
    public DateTime? LastTransactionDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record WalletTransactionDto
{
    public Guid TransactionId { get; init; }
    public Guid WalletId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal BalanceAfter { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? ReferenceId { get; init; }
    public Guid? OrderId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record LoyaltyTransactionDto
{
    public Guid TransactionId { get; init; }
    public Guid CustomerId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public int Points { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public decimal? OrderAmount { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public bool IsExpired { get; init; }
    public Guid? RelatedTransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record AddWalletFundsRequest
{
    [Required]
    [Range(0.01, 10000)]
    public decimal Amount { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;

    public string? ReferenceId { get; init; }
}

public record ProcessOrderRequest
{
    [Required]
    public Guid OrderId { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal OrderAmount { get; init; }

    public decimal? WalletAmountUsed { get; init; }

    public int? LoyaltyPointsRedeemed { get; init; }

    public string? CreatedBy { get; init; }
}

// Additional request/response models for API endpoints
public class CustomerSearchResponse
{
    public List<CustomerDto> Customers { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class UpdateMembershipRequest
{
    [Required]
    public Guid MembershipLevelId { get; set; }
}

public class DeductWalletFundsRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ReferenceId { get; set; }

    public Guid? OrderId { get; set; }
}

public class WalletTransactionResponse
{
    public List<WalletTransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AddLoyaltyPointsRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than 0")]
    public int Points { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public Guid? OrderId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? OrderAmount { get; set; }
}

public class RedeemLoyaltyPointsRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than 0")]
    public int Points { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public Guid? OrderId { get; set; }
}

public class LoyaltyTransactionResponse
{
    public List<LoyaltyTransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}


public class LoyaltyStatsDto
{
    public int CurrentBalance { get; set; }
    public int TotalEarned { get; set; }
    public int TotalRedeemed { get; set; }
    public int TotalExpired { get; set; }
    public int ExpiringIn30Days { get; set; }
}
