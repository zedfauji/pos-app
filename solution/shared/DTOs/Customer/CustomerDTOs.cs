using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MagiDesk.Shared.DTOs.Customer
{
    // Customer DTOs
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int MembershipLevelId { get; set; }
        public DateTime MembershipExpiryDate { get; set; }
        public decimal TotalSpent { get; set; }
        public int LoyaltyPoints { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public MembershipLevelDto? MembershipLevel { get; set; }
        public WalletDto? Wallet { get; set; }
    }

    public class CustomerCreateRequestDto
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }
        
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        public int? MembershipLevelId { get; set; }
    }

    public class CustomerUpdateRequestDto
    {
        [StringLength(100)]
        public string? FirstName { get; set; }
        
        [StringLength(100)]
        public string? LastName { get; set; }
        
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }
        
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        public int? MembershipLevelId { get; set; }
        
        public bool? IsActive { get; set; }
    }

    public class CustomerSearchRequestDto
    {
        public string? Query { get; set; }
        public int? MembershipLevelId { get; set; }
        public bool? IsActive { get; set; }
        public string SortBy { get; set; } = "firstname_asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CustomerSearchResponseDto
    {
        public List<CustomerDto> Customers { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class CustomerStatsDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ExpiredMemberships { get; set; }
        public Dictionary<string, int> CustomersByMembershipLevel { get; set; } = new();
        public Dictionary<string, decimal> RevenueByMembershipLevel { get; set; } = new();
    }

    public class CustomerOrderProcessRequestDto
    {
        public decimal OrderAmount { get; set; }
        public int ItemCount { get; set; }
        public bool UseWallet { get; set; } = false;
        public decimal WalletAmount { get; set; } = 0;
    }

    public class CustomerOrderProcessResponseDto
    {
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public int LoyaltyPointsEarned { get; set; }
        public decimal WalletAmountUsed { get; set; }
        public decimal WalletRemainingBalance { get; set; }
        public bool MembershipUpgradeEligible { get; set; }
        public MembershipLevelDto? RecommendedMembership { get; set; }
    }

    public class MembershipUpdateRequestDto
    {
        public int MembershipLevelId { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    // Membership Level DTOs
    public class MembershipLevelDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MinimumSpend { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal LoyaltyMultiplier { get; set; }
        public decimal MaxWalletBalance { get; set; }
        public int? BirthdayBonusPoints { get; set; }
        public int ValidityDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MembershipUpgradeEligibilityDto
    {
        public bool IsEligible { get; set; }
        public MembershipLevelDto? RecommendedLevel { get; set; }
        public decimal CurrentSpend { get; set; }
        public decimal RequiredSpend { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Wallet DTOs
    public class WalletDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Balance { get; set; }
        public decimal MaxBalance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletFundRequestDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [StringLength(255)]
        public string? Description { get; set; }
    }

    public class WalletTransactionDto
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public string TransactionType { get; set; } = string.Empty; // credit, debit
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Loyalty DTOs
    public class LoyaltyTransactionDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string TransactionType { get; set; } = string.Empty; // earned, redeemed, expired, bonus
        public int Points { get; set; }
        public int BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
        public int? RelatedTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LoyaltyPointsCalculationRequestDto
    {
        public decimal OrderAmount { get; set; }
        public int ItemCount { get; set; }
    }

    public class LoyaltyPointsCalculationResponseDto
    {
        public int PointsEarned { get; set; }
        public decimal Multiplier { get; set; }
        public string Calculation { get; set; } = string.Empty;
    }

    public class LoyaltyPointsAddRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than 0")]
        public int Points { get; set; }
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
    }

    public class LoyaltyPointsRedeemRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than 0")]
        public int Points { get; set; }
        
        [StringLength(255)]
        public string? Description { get; set; }
    }

    public class LoyaltyPointsRedemptionEligibilityDto
    {
        public bool IsEligible { get; set; }
        public int AvailablePoints { get; set; }
        public int RequestedPoints { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LoyaltyStatsDto
    {
        public int TotalPointsEarned { get; set; }
        public int TotalPointsRedeemed { get; set; }
        public int TotalPointsExpired { get; set; }
        public int CurrentBalance { get; set; }
        public int PointsExpiringIn30Days { get; set; }
        public DateTime? NextExpiryDate { get; set; }
    }
}
