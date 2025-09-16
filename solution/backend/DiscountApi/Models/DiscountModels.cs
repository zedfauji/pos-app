using System.ComponentModel.DataAnnotations;

namespace DiscountApi.Models;

public class Campaign
{
    public Guid CampaignId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CampaignType { get; set; } = string.Empty; // Discount, Loyalty, FreeItem
    public string Status { get; set; } = "Draft"; // Draft, Active, Paused, Completed
    public string Channel { get; set; } = "All"; // All, InStore, Online
    public Guid? TargetSegmentId { get; set; }
    public string? TargetSegmentName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? FreeItemId { get; set; }
    public string? FreeItemName { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public int? TotalUsageLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CustomerSegment
{
    public Guid SegmentId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Criteria { get; set; } = "{}"; // JSON criteria
    public int CustomerCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Voucher
{
    public Guid VoucherId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRedeemed { get; set; } = false;
    public DateTime? RedeemedAt { get; set; }
    public Guid? RedeemedBy { get; set; }
    public string? BillingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ComboOffer
{
    public Guid ComboId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Items { get; set; } = "[]"; // JSON array of items
    public decimal OriginalPrice { get; set; }
    public decimal ComboPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class AppliedDiscount
{
    public Guid AppliedDiscountId { get; set; } = Guid.NewGuid();
    public string BillingId { get; set; } = string.Empty;
    public string DiscountId { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string? AppliedBy { get; set; }
}

public class CustomerHistory
{
    public Guid CustomerId { get; set; }
    public int TotalVisits { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public string FavoriteItems { get; set; } = "[]"; // JSON array
    public string? PreferredTimeSlot { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// DTOs
public class DiscountApplicationRequest
{
    public string BillingId { get; set; } = string.Empty;
    public string DiscountId { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

public class DiscountApplicationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal NewTotal { get; set; }
}
