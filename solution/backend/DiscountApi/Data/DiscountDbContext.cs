using Microsoft.EntityFrameworkCore;
using DiscountApi.Models;

namespace DiscountApi.Data;

public class DiscountDbContext : DbContext
{
    public DiscountDbContext(DbContextOptions<DiscountDbContext> options) : base(options) { }

    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CustomerSegment> CustomerSegments { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<ComboOffer> ComboOffers { get; set; }
    public DbSet<AppliedDiscount> AppliedDiscounts { get; set; }
    public DbSet<CustomerHistory> CustomerHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("discounts");

        // Campaign configuration
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.CampaignId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });

        // Customer Segment configuration
        modelBuilder.Entity<CustomerSegment>(entity =>
        {
            entity.HasKey(e => e.SegmentId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Criteria).HasColumnType("jsonb");
            entity.HasIndex(e => e.IsActive);
        });

        // Voucher configuration
        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.MinimumOrderValue).HasPrecision(10, 2);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiryDate);
        });

        // Combo Offer configuration
        modelBuilder.Entity<ComboOffer>(entity =>
        {
            entity.HasKey(e => e.ComboId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Items).HasColumnType("jsonb");
            entity.Property(e => e.OriginalPrice).HasPrecision(10, 2);
            entity.Property(e => e.ComboPrice).HasPrecision(10, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });

        // Applied Discount configuration
        modelBuilder.Entity<AppliedDiscount>(entity =>
        {
            entity.HasKey(e => e.AppliedDiscountId);
            entity.Property(e => e.BillingId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DiscountId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DiscountType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.AppliedBy).HasMaxLength(100);
            entity.HasIndex(e => e.BillingId);
            entity.HasIndex(e => e.AppliedAt);
        });

        // Customer History configuration
        modelBuilder.Entity<CustomerHistory>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.TotalSpent).HasPrecision(12, 2);
            entity.Property(e => e.AverageOrderValue).HasPrecision(10, 2);
            entity.Property(e => e.FavoriteItems).HasColumnType("jsonb");
            entity.Property(e => e.PreferredTimeSlot).HasMaxLength(50);
            entity.HasIndex(e => e.TotalVisits);
            entity.HasIndex(e => e.TotalSpent);
            entity.HasIndex(e => e.LastVisitDate);
        });
    }
}
