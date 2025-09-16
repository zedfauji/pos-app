using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<MembershipLevel> MembershipLevels { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }

    // Customer Intelligence entities
    public DbSet<CustomerSegment> CustomerSegments { get; set; }
    public DbSet<CustomerSegmentMembership> CustomerSegmentMemberships { get; set; }
    public DbSet<BehavioralTrigger> BehavioralTriggers { get; set; }
    public DbSet<TriggerExecution> TriggerExecutions { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CampaignExecution> CampaignExecutions { get; set; }
    public DbSet<CampaignRedemption> CampaignRedemptions { get; set; }
    public DbSet<CommunicationProviderConfig> CommunicationProviderConfigs { get; set; }
    public DbSet<CommunicationTemplate> CommunicationTemplates { get; set; }
    public DbSet<CommunicationLog> CommunicationLogs { get; set; }
    public DbSet<BehavioralTriggerExecution> BehavioralTriggerExecutions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("customers");

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.HasIndex(e => e.Phone).IsUnique().HasFilter("phone IS NOT NULL");
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("email IS NOT NULL");
            entity.HasIndex(e => new { e.FirstName, e.LastName });
            entity.HasIndex(e => e.MembershipLevelId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.MembershipLevel)
                  .WithMany(m => m.Customers)
                  .HasForeignKey(e => e.MembershipLevelId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Wallet)
                  .WithOne(w => w.Customer)
                  .HasForeignKey<Wallet>(w => w.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MembershipLevel configuration
        modelBuilder.Entity<MembershipLevel>(entity =>
        {
            entity.HasKey(e => e.MembershipLevelId);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDefault);
        });

        // Wallet configuration
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId);
            entity.HasIndex(e => e.CustomerId).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.HasMany(e => e.Transactions)
                  .WithOne(t => t.Wallet)
                  .HasForeignKey(t => t.WalletId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // WalletTransaction configuration
        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.HasIndex(e => e.WalletId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ReferenceId);

            entity.Property(e => e.TransactionType)
                  .HasConversion<int>();
        });

        // LoyaltyTransaction configuration
        modelBuilder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ExpiryDate);

            entity.Property(e => e.TransactionType)
                  .HasConversion<int>();

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.LoyaltyTransactions)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Customer Intelligence entities configuration
        ConfigureCustomerIntelligenceEntities(modelBuilder);

        // Seed default membership levels
        SeedMembershipLevels(modelBuilder);
    }

    private static void ConfigureCustomerIntelligenceEntities(ModelBuilder modelBuilder)
    {
        // CustomerSegment configuration
        modelBuilder.Entity<CustomerSegment>(entity =>
        {
            entity.HasKey(e => e.SegmentId);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasMany(e => e.Memberships)
                  .WithOne(m => m.Segment)
                  .HasForeignKey(m => m.SegmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.SegmentType)
                  .HasConversion<int>();
        });

        // CustomerSegmentMembership configuration
        modelBuilder.Entity<CustomerSegmentMembership>(entity =>
        {
            entity.HasKey(e => e.MembershipId);
            entity.HasIndex(e => new { e.SegmentId, e.CustomerId }).IsUnique();
            entity.HasIndex(e => e.AddedAt);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BehavioralTrigger configuration
        modelBuilder.Entity<BehavioralTrigger>(entity =>
        {
            entity.HasKey(e => e.TriggerId);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.ConditionType)
                .HasConversion<string>();

            entity.Property(e => e.ActionType)
                .HasConversion<string>();

            entity.HasMany(e => e.Executions)
                  .WithOne(ex => ex.Trigger)
                  .HasForeignKey(ex => ex.TriggerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TriggerExecution configuration
        modelBuilder.Entity<TriggerExecution>(entity =>
        {
            entity.HasKey(e => e.ExecutionId);
            entity.HasIndex(e => e.TriggerId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => e.Success);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Campaign configuration
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.CampaignId);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.CampaignType)
                  .HasConversion<int>();
            entity.Property(e => e.Status)
                  .HasConversion<int>();
            entity.Property(e => e.CommunicationChannel)
                  .HasConversion<int>();

            entity.HasMany(e => e.Executions)
                  .WithOne(ex => ex.Campaign)
                  .HasForeignKey(ex => ex.CampaignId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CampaignExecution configuration
        modelBuilder.Entity<CampaignExecution>(entity =>
        {
            entity.HasKey(e => e.ExecutionId);
            entity.HasIndex(e => e.CampaignId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.Status);


            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CampaignRedemption configuration
        modelBuilder.Entity<CampaignRedemption>(entity =>
        {
            entity.HasKey(e => e.RedemptionId);
            entity.HasIndex(e => e.CampaignId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ExecutionId);
            entity.HasIndex(e => e.RedeemedAt);

            entity.HasOne(e => e.Execution)
                  .WithMany()
                  .HasForeignKey(e => e.ExecutionId)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // CommunicationProviderConfig configuration
        modelBuilder.Entity<CommunicationProviderConfig>(entity =>
        {
            entity.HasKey(e => e.ProviderId);
            entity.HasIndex(e => new { e.ProviderType, e.ProviderName }).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDefault);
            entity.HasIndex(e => e.Priority);

            entity.Property(e => e.ProviderType)
                  .HasConversion<int>();
        });
    }

    private static void SeedMembershipLevels(ModelBuilder modelBuilder)
    {
        var membershipLevels = new[]
        {
            new MembershipLevel
            {
                MembershipLevelId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Regular",
                Description = "Standard membership with basic benefits",
                DiscountPercentage = 0,
                LoyaltyMultiplier = 1.0m,
                ColorHex = "#808080",
                Icon = "Person",
                SortOrder = 1,
                IsDefault = true,
                MaxWalletBalance = 1000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new MembershipLevel
            {
                MembershipLevelId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Silver",
                Description = "Silver membership with 5% discount and enhanced benefits",
                DiscountPercentage = 5,
                LoyaltyMultiplier = 1.25m,
                MinimumSpendRequirement = 500,
                ValidityMonths = 12,
                ColorHex = "#C0C0C0",
                Icon = "Star",
                SortOrder = 2,
                MaxWalletBalance = 2500,
                BirthdayBonusPoints = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new MembershipLevel
            {
                MembershipLevelId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Gold",
                Description = "Gold membership with 10% discount and premium benefits",
                DiscountPercentage = 10,
                LoyaltyMultiplier = 1.5m,
                MinimumSpendRequirement = 1500,
                ValidityMonths = 12,
                ColorHex = "#FFD700",
                Icon = "Crown",
                SortOrder = 3,
                MaxWalletBalance = 5000,
                FreeDelivery = true,
                BirthdayBonusPoints = 250,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new MembershipLevel
            {
                MembershipLevelId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "VIP",
                Description = "VIP membership with 15% discount and exclusive benefits",
                DiscountPercentage = 15,
                LoyaltyMultiplier = 2.0m,
                MinimumSpendRequirement = 5000,
                ValidityMonths = 24,
                ColorHex = "#8B008B",
                Icon = "Diamond",
                SortOrder = 4,
                MaxWalletBalance = 10000,
                FreeDelivery = true,
                PrioritySupport = true,
                BirthdayBonusPoints = 500,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<MembershipLevel>().HasData(membershipLevels);
    }
}
