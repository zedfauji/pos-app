using Npgsql;
using System.Reflection;

namespace DiscountApi.Services;

public interface IMigrationService
{
    Task RunMigrationsAsync();
}

public class MigrationService : IMigrationService
{
    private readonly string _connectionString;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(IConfiguration configuration, ILogger<MigrationService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Port=5432;Username=posapp;Password=Campus_66;Database=postgres;SSL Mode=Require;Trust Server Certificate=true";
        _logger = logger;
    }

    public async Task RunMigrationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting database migrations...");

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get all migration files
            var assembly = Assembly.GetExecutingAssembly();
            var migrationFiles = assembly.GetManifestResourceNames()
                .Where(name => name.Contains("Migrations") && name.EndsWith(".sql"))
                .OrderBy(name => name)
                .ToList();

            // If no embedded resources, try to read from file system
            if (!migrationFiles.Any())
            {
                var migrationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");
                if (Directory.Exists(migrationsPath))
                {
                    var sqlFiles = Directory.GetFiles(migrationsPath, "*.sql")
                        .OrderBy(f => f)
                        .ToList();

                    foreach (var file in sqlFiles)
                    {
                        var sql = await File.ReadAllTextAsync(file);
                        await ExecuteMigrationAsync(connection, Path.GetFileName(file), sql);
                    }
                }
                else
                {
                    // Fallback: run the initial migration directly
                    await RunInitialMigrationAsync(connection);
                }
            }
            else
            {
                // Run embedded migrations
                foreach (var migrationFile in migrationFiles)
                {
                    using var stream = assembly.GetManifestResourceStream(migrationFile);
                    using var reader = new StreamReader(stream!);
                    var sql = await reader.ReadToEndAsync();
                    await ExecuteMigrationAsync(connection, migrationFile, sql);
                }
            }

            _logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run database migrations");
            throw;
        }
    }

    private async Task ExecuteMigrationAsync(NpgsqlConnection connection, string migrationName, string sql)
    {
        try
        {
            _logger.LogInformation("Running migration: {MigrationName}", migrationName);
            
            using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Migration completed: {MigrationName}", migrationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run migration: {MigrationName}", migrationName);
            throw;
        }
    }

    private async Task RunInitialMigrationAsync(NpgsqlConnection connection)
    {
        var sql = @"
-- Create discounts schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS discounts;

-- Set search path
SET search_path TO discounts;

-- Create campaigns table
CREATE TABLE IF NOT EXISTS campaigns (
    campaign_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    campaign_type VARCHAR(50) NOT NULL DEFAULT 'Discount',
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    channel VARCHAR(50) NOT NULL DEFAULT 'All',
    target_segment_id UUID,
    target_segment_name VARCHAR(200),
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    end_date TIMESTAMP WITH TIME ZONE NOT NULL,
    discount_percentage DECIMAL(5,2),
    discount_amount DECIMAL(10,2),
    free_item_id VARCHAR(100),
    free_item_name VARCHAR(200),
    minimum_order_value DECIMAL(10,2),
    max_usage_per_customer INTEGER,
    total_usage_limit INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create customer segments table
CREATE TABLE IF NOT EXISTS customer_segments (
    segment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    criteria JSONB NOT NULL DEFAULT '{}',
    customer_count INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create vouchers table
CREATE TABLE IF NOT EXISTS vouchers (
    voucher_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    discount_percentage DECIMAL(5,2),
    discount_amount DECIMAL(10,2),
    minimum_order_value DECIMAL(10,2),
    expiry_date TIMESTAMP WITH TIME ZONE NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_redeemed BOOLEAN NOT NULL DEFAULT false,
    redeemed_at TIMESTAMP WITH TIME ZONE,
    redeemed_by UUID,
    billing_id VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create combo offers table
CREATE TABLE IF NOT EXISTS combo_offers (
    combo_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    items JSONB NOT NULL DEFAULT '[]',
    original_price DECIMAL(10,2) NOT NULL,
    combo_price DECIMAL(10,2) NOT NULL,
    discount_amount DECIMAL(10,2) NOT NULL,
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    end_date TIMESTAMP WITH TIME ZONE NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create applied discounts table
CREATE TABLE IF NOT EXISTS applied_discounts (
    applied_discount_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    billing_id VARCHAR(100) NOT NULL,
    discount_id VARCHAR(100) NOT NULL,
    discount_type VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    discount_amount DECIMAL(10,2) NOT NULL,
    discount_percentage DECIMAL(10,2) NOT NULL,
    applied_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    applied_by VARCHAR(100)
);

-- Create customer histories table
CREATE TABLE IF NOT EXISTS customer_histories (
    customer_id UUID PRIMARY KEY,
    total_visits INTEGER NOT NULL DEFAULT 0,
    total_spent DECIMAL(12,2) NOT NULL DEFAULT 0,
    average_order_value DECIMAL(10,2) NOT NULL DEFAULT 0,
    days_since_last_visit INTEGER NOT NULL DEFAULT 0,
    last_visit_date TIMESTAMP WITH TIME ZONE,
    favorite_items JSONB NOT NULL DEFAULT '[]',
    preferred_time_slot VARCHAR(50),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_campaigns_active ON campaigns(is_active);
CREATE INDEX IF NOT EXISTS idx_campaigns_dates ON campaigns(start_date, end_date);
CREATE INDEX IF NOT EXISTS idx_customer_segments_active ON customer_segments(is_active);
CREATE INDEX IF NOT EXISTS idx_vouchers_code ON vouchers(code);
CREATE INDEX IF NOT EXISTS idx_vouchers_active ON vouchers(is_active);
CREATE INDEX IF NOT EXISTS idx_vouchers_expiry ON vouchers(expiry_date);
CREATE INDEX IF NOT EXISTS idx_combo_offers_active ON combo_offers(is_active);
CREATE INDEX IF NOT EXISTS idx_combo_offers_dates ON combo_offers(start_date, end_date);
CREATE INDEX IF NOT EXISTS idx_applied_discounts_billing ON applied_discounts(billing_id);
CREATE INDEX IF NOT EXISTS idx_applied_discounts_applied_at ON applied_discounts(applied_at);
CREATE INDEX IF NOT EXISTS idx_customer_histories_visits ON customer_histories(total_visits);
CREATE INDEX IF NOT EXISTS idx_customer_histories_spent ON customer_histories(total_spent);
CREATE INDEX IF NOT EXISTS idx_customer_histories_last_visit ON customer_histories(last_visit_date);

-- Insert some sample data for testing
INSERT INTO customer_histories (customer_id, total_visits, total_spent, average_order_value, days_since_last_visit, last_visit_date, favorite_items, preferred_time_slot)
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', 15, 1250.00, 83.33, 2, NOW() - INTERVAL '2 days', '[""Burger Deluxe"", ""French Fries"", ""Soft Drink""]', 'Evening'),
    ('550e8400-e29b-41d4-a716-446655440001', 1, 0, 0, 0, NOW(), '[]', 'Lunch'),
    ('550e8400-e29b-41d4-a716-446655440002', 8, 450.00, 56.25, 35, NOW() - INTERVAL '35 days', '[""Pizza"", ""Salad""]', 'Dinner')
ON CONFLICT (customer_id) DO NOTHING;

-- Insert sample vouchers
INSERT INTO vouchers (code, name, description, discount_percentage, expiry_date)
VALUES 
    ('VIP25', 'VIP Member Discount', '25% off for VIP members', 25.00, NOW() + INTERVAL '30 days'),
    ('WELCOME15', 'Welcome New Customer', '15% off first order', 15.00, NOW() + INTERVAL '60 days'),
    ('SAVE20', 'Save 20%', '20% off any order', 20.00, NOW() + INTERVAL '14 days')
ON CONFLICT (code) DO NOTHING;
";

        await ExecuteMigrationAsync(connection, "InitialCreate", sql);
    }
}
