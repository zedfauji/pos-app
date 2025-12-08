using MagiDesk.Shared.DTOs.Settings;
using System.Text.Json;

namespace SettingsApi.Tests.Helpers;

public static class TestDataHelper
{
    public static HierarchicalSettings CreateValidSettings()
    {
        return new HierarchicalSettings
        {
            General = new GeneralSettings
            {
                BusinessName = "Test Business",
                BusinessAddress = "123 Test St",
                BusinessPhone = "555-1234",
                BusinessEmail = "test@business.com",
                Theme = "Light",
                Language = "en-US",
                TimeZone = "UTC",
                Currency = "USD"
            },
            Pos = new PosSettings
            {
                DefaultTaxRate = 8.5m,
                AutoOpenCashDrawer = true,
                RequireCustomerForOrders = false,
                DefaultOrderType = "DineIn",
                MaxTableCapacity = 8,
                EnableTableService = true,
                AutoAssignTables = false
            },
            Inventory = new InventorySettings
            {
                LowStockThreshold = 10,
                AutoReorderEnabled = true,
                DefaultVendor = "Test Vendor",
                TrackInventory = true,
                AllowNegativeStock = false,
                ReorderPoint = 5,
                MaxStockLevel = 100
            },
            Customers = new CustomersSettings
            {
                EnableMembershipTiers = true,
                DefaultTier = "Bronze",
                EnableWalletSystem = true,
                WalletMinBalance = 0m,
                WalletMaxBalance = 1000m,
                EnableLoyaltyProgram = true,
                PointsPerDollar = 1,
                PointsRedemptionRate = 100
            },
            Payments = new PaymentsSettings
            {
                EnableCashPayments = true,
                EnableCardPayments = true,
                EnableDigitalWallets = true,
                EnableSplitPayments = true,
                MaxSplitCount = 4,
                DefaultTipPercentage = 18m,
                EnableCustomTips = true,
                RequireReceiptPrint = true
            },
            Printers = new PrinterSettings
            {
                DefaultReceiptPrinter = "Receipt Printer",
                DefaultKitchenPrinter = "Kitchen Printer",
                ReceiptPaperSize = "80mm",
                AutoPrintReceipts = true,
                PrintOrderCopies = 2,
                EnablePrintPreview = true
            },
            Notifications = new NotificationsSettings
            {
                EnableEmailNotifications = true,
                EnableSmsNotifications = false,
                EnablePushNotifications = true,
                EmailFromAddress = "noreply@business.com",
                SmsProvider = "Twilio",
                PushNotificationService = "Firebase"
            },
            Security = new SecuritySettings
            {
                EnableRbac = true,
                SessionTimeoutMinutes = 30,
                MaxLoginAttempts = 5,
                PasswordMinLength = 8,
                RequirePasswordComplexity = true,
                EnableAuditLogging = true,
                EnableTwoFactorAuth = false
            },
            Integrations = new IntegrationsSettings
            {
                EnablePaymentGateways = true,
                DefaultPaymentGateway = "Stripe",
                EnableWebhooks = true,
                WebhookSecret = "test-secret",
                EnableCrmSync = false,
                CrmProvider = "Salesforce",
                ApiRateLimit = 1000
            },
            System = new SystemSettings
            {
                LogLevel = "Information",
                EnablePerformanceMonitoring = true,
                BackupRetentionDays = 30,
                AutoBackupEnabled = true,
                BackupSchedule = "Daily",
                EnableSystemMaintenance = true,
                MaintenanceWindow = "02:00-04:00"
            }
        };
    }

    public static HierarchicalSettings CreateInvalidSettings()
    {
        var settings = CreateValidSettings();
        
        // Make settings invalid
        settings.General.BusinessName = ""; // Empty business name
        settings.General.BusinessEmail = "invalid-email"; // Invalid email format
        settings.Pos.DefaultTaxRate = -1; // Negative tax rate
        settings.Inventory.LowStockThreshold = -5; // Negative threshold
        settings.Customers.WalletMinBalance = -100; // Negative balance
        settings.Payments.MaxSplitCount = 0; // Zero split count
        settings.Security.PasswordMinLength = 0; // Zero password length
        settings.System.BackupRetentionDays = -1; // Negative retention days
        
        return settings;
    }

    public static GeneralSettings CreateValidGeneralSettings()
    {
        return new GeneralSettings
        {
            BusinessName = "Test Business",
            BusinessAddress = "123 Test St",
            BusinessPhone = "555-1234",
            BusinessEmail = "test@business.com",
            Theme = "Light",
            Language = "en-US",
            TimeZone = "UTC",
            Currency = "USD"
        };
    }

    public static PosSettings CreateValidPosSettings()
    {
        return new PosSettings
        {
            DefaultTaxRate = 8.5m,
            AutoOpenCashDrawer = true,
            RequireCustomerForOrders = false,
            DefaultOrderType = "DineIn",
            MaxTableCapacity = 8,
            EnableTableService = true,
            AutoAssignTables = false
        };
    }

    public static string SerializeSettings(HierarchicalSettings settings)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        return JsonSerializer.Serialize(settings, options);
    }

    public static HierarchicalSettings DeserializeSettings(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.Deserialize<HierarchicalSettings>(json, options) ?? new HierarchicalSettings();
    }

    public static List<string> GetValidCategories()
    {
        return new List<string>
        {
            "general",
            "pos",
            "inventory",
            "customers",
            "payments",
            "printers",
            "notifications",
            "security",
            "integrations",
            "system"
        };
    }

    public static List<string> GetInvalidCategories()
    {
        return new List<string>
        {
            "invalid",
            "nonexistent",
            "test"
        };
    }

    public static SettingsAuditEntry CreateTestAuditEntry()
    {
        return new SettingsAuditEntry
        {
            Id = 1,
            HostKey = "test-host",
            Action = "settings_updated",
            Description = "Test settings update",
            Category = "general",
            ChangedBy = "test-user",
            Timestamp = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent",
            Changes = new Dictionary<string, object>
            {
                { "businessName", "Old Name -> New Name" },
                { "theme", "Dark -> Light" }
            }
        };
    }
}

