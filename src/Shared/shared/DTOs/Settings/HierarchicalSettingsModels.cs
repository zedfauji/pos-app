using System.ComponentModel.DataAnnotations;

namespace MagiDesk.Shared.DTOs.Settings;

/// <summary>
/// Root settings container with hierarchical organization
/// </summary>
public class HierarchicalSettings
{
    public GeneralSettings General { get; set; } = new();
    public PosSettings Pos { get; set; } = new();
    public InventorySettings Inventory { get; set; } = new();
    public CustomerSettings Customers { get; set; } = new();
    public PaymentSettings Payments { get; set; } = new();
    public PrinterSettings Printers { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public IntegrationSettings Integrations { get; set; } = new();
    public SystemSettings System { get; set; } = new();
}

/// <summary>
/// General application settings
/// </summary>
public class GeneralSettings
{
    [Required, StringLength(100)]
    public string BusinessName { get; set; } = "MagiDesk POS";
    
    [Required, StringLength(200)]
    public string BusinessAddress { get; set; } = "123 Business Street, City, State 12345";
    
    [Required, StringLength(20)]
    public string BusinessPhone { get; set; } = "+1-555-0123";
    
    [EmailAddress, StringLength(100)]
    public string? BusinessEmail { get; set; }
    
    [Url, StringLength(100)]
    public string? BusinessWebsite { get; set; }
    
    [Required, StringLength(10)]
    public string Theme { get; set; } = "System"; // System, Light, Dark
    
    [Required, StringLength(10)]
    public string Language { get; set; } = "en-US";
    
    [Required, StringLength(50)]
    public string Timezone { get; set; } = "America/New_York";
    
    [StringLength(50)]
    public string? HostKey { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Point-of-Sale specific settings
/// </summary>
public class PosSettings
{
    public CashDrawerSettings CashDrawer { get; set; } = new();
    public TableLayoutSettings TableLayout { get; set; } = new();
    public ShiftSettings Shifts { get; set; } = new();
    public TaxSettings Tax { get; set; } = new();
}

public class CashDrawerSettings
{
    public bool AutoOpenOnSale { get; set; } = true;
    public bool AutoOpenOnRefund { get; set; } = true;
    public string? ComPort { get; set; }
    public int BaudRate { get; set; } = 9600;
    public bool RequireManagerOverride { get; set; } = false;
}

public class TableLayoutSettings
{
    [Range(0.01, 999.99)]
    public decimal RatePerMinute { get; set; } = 0.50m;
    
    [Range(1, 1440)]
    public int WarnAfterMinutes { get; set; } = 30;
    
    [Range(1, 1440)]
    public int AutoStopAfterMinutes { get; set; } = 120;
    
    public bool EnableAutoStop { get; set; } = false;
    public bool ShowTimerOnTables { get; set; } = true;
}

public class ShiftSettings
{
    public TimeSpan ShiftStartTime { get; set; } = new(9, 0, 0);
    public TimeSpan ShiftEndTime { get; set; } = new(21, 0, 0);
    public bool RequireShiftReports { get; set; } = true;
    public bool AutoCloseShift { get; set; } = false;
}

public class TaxSettings
{
    [Range(0, 100)]
    public decimal DefaultTaxRate { get; set; } = 8.5m;
    
    public bool TaxInclusivePricing { get; set; } = false;
    public string TaxDisplayName { get; set; } = "Tax";
    public List<TaxRate> AdditionalTaxRates { get; set; } = new();
}

public class TaxRate
{
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Inventory management settings
/// </summary>
public class InventorySettings
{
    public StockSettings Stock { get; set; } = new();
    public ReorderSettings Reorder { get; set; } = new();
    public VendorSettings Vendors { get; set; } = new();
}

public class StockSettings
{
    [Range(1, 1000)]
    public int LowStockThreshold { get; set; } = 10;
    
    [Range(1, 1000)]
    public int CriticalStockThreshold { get; set; } = 5;
    
    public bool EnableStockAlerts { get; set; } = true;
    public bool AutoDeductStock { get; set; } = true;
    public bool AllowNegativeStock { get; set; } = false;
}

public class ReorderSettings
{
    public bool EnableAutoReorder { get; set; } = false;
    
    [Range(1, 365)]
    public int ReorderLeadTimeDays { get; set; } = 7;
    
    [Range(1.0, 10.0)]
    public decimal SafetyStockMultiplier { get; set; } = 1.5m;
}

public class VendorSettings
{
    [Range(1, 90)]
    public int DefaultPaymentTerms { get; set; } = 30;
    
    public string DefaultCurrency { get; set; } = "USD";
    public bool RequireApprovalForOrders { get; set; } = true;
}

/// <summary>
/// Customer and membership settings
/// </summary>
public class CustomerSettings
{
    public MembershipSettings Membership { get; set; } = new();
    public WalletSettings Wallet { get; set; } = new();
    public LoyaltySettings Loyalty { get; set; } = new();
}

public class MembershipSettings
{
    public List<MembershipTier> Tiers { get; set; } = new();
    
    [Range(1, 365)]
    public int DefaultExpiryDays { get; set; } = 365;
    
    public bool AutoRenewMemberships { get; set; } = false;
    public bool RequireEmailForMembership { get; set; } = true;
}

public class MembershipTier
{
    public string Name { get; set; } = "";
    public decimal DiscountPercentage { get; set; }
    public decimal MinimumSpend { get; set; }
    public string Color { get; set; } = "#007ACC";
}

public class WalletSettings
{
    public bool EnableWalletSystem { get; set; } = true;
    
    [Range(0, 10000)]
    public decimal MaxWalletBalance { get; set; } = 1000m;
    
    [Range(1, 100)]
    public decimal MinTopUpAmount { get; set; } = 10m;
    
    public bool AllowNegativeBalance { get; set; } = false;
    public bool RequireIdForWalletUse { get; set; } = false;
}

public class LoyaltySettings
{
    public bool EnableLoyaltyProgram { get; set; } = true;
    
    [Range(0.01, 10.0)]
    public decimal PointsPerDollar { get; set; } = 1.0m;
    
    [Range(0.01, 1.0)]
    public decimal PointValue { get; set; } = 0.01m; // $0.01 per point
    
    [Range(1, 10000)]
    public int MinPointsForRedemption { get; set; } = 100;
}

/// <summary>
/// Payment processing settings
/// </summary>
public class PaymentSettings
{
    public List<PaymentMethod> EnabledMethods { get; set; } = new();
    public DiscountSettings Discounts { get; set; } = new();
    public SurchargeSettings Surcharges { get; set; } = new();
    public SplitPaymentSettings SplitPayments { get; set; } = new();
}

public class PaymentMethod
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // Cash, Card, Digital, etc.
    public bool IsEnabled { get; set; } = true;
    public decimal? SurchargePercentage { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public class DiscountSettings
{
    [Range(0, 100)]
    public decimal MaxDiscountPercentage { get; set; } = 50m;
    
    public bool RequireManagerApproval { get; set; } = true;
    public bool AllowStackingDiscounts { get; set; } = false;
    public List<string> PresetDiscounts { get; set; } = new() { "5%", "10%", "15%", "20%" };
}

public class SurchargeSettings
{
    public bool EnableSurcharges { get; set; } = false;
    
    [Range(0, 10)]
    public decimal CardSurchargePercentage { get; set; } = 2.5m;
    
    public bool ShowSurchargeOnReceipt { get; set; } = true;
}

public class SplitPaymentSettings
{
    public bool EnableSplitPayments { get; set; } = true;
    
    [Range(2, 10)]
    public int MaxSplitCount { get; set; } = 4;
    
    public bool AllowUnevenSplits { get; set; } = true;
}

/// <summary>
/// Comprehensive printer and device settings
/// </summary>
public class PrinterSettings
{
    public ReceiptPrinterSettings Receipt { get; set; } = new();
    public KitchenPrinterSettings Kitchen { get; set; } = new();
    public PrinterDeviceSettings Devices { get; set; } = new();
    public PrintJobSettings Jobs { get; set; } = new();
}

public class ReceiptPrinterSettings
{
    [StringLength(100)]
    public string DefaultPrinter { get; set; } = "";
    
    public string FallbackPrinter { get; set; } = "";
    
    [Required]
    public string PaperSize { get; set; } = "80mm"; // 58mm, 80mm, A4
    
    public bool AutoPrintOnPayment { get; set; } = true;
    public bool PreviewBeforePrint { get; set; } = true;
    public bool PrintProForma { get; set; } = true;
    public bool PrintFinalReceipt { get; set; } = true;
    
    [Range(1, 5)]
    public int CopiesForFinalReceipt { get; set; } = 2;
    
    public bool PrintCustomerCopy { get; set; } = true;
    public bool PrintMerchantCopy { get; set; } = true;
    
    public ReceiptTemplate Template { get; set; } = new();
}

public class ReceiptTemplate
{
    public bool ShowLogo { get; set; } = true;
    public bool ShowBusinessInfo { get; set; } = true;
    public bool ShowItemDetails { get; set; } = true;
    public bool ShowTaxBreakdown { get; set; } = true;
    public bool ShowPaymentMethod { get; set; } = true;
    public string FooterMessage { get; set; } = "Thank you for your business!";
    public string HeaderMessage { get; set; } = "";
    
    // Margins in millimeters
    public int TopMargin { get; set; } = 5;
    public int BottomMargin { get; set; } = 5;
    public int LeftMargin { get; set; } = 2;
    public int RightMargin { get; set; } = 2;
}

public class KitchenPrinterSettings
{
    public List<KitchenPrinter> Printers { get; set; } = new();
    public bool PrintOrderNumbers { get; set; } = true;
    public bool PrintTimestamps { get; set; } = true;
    public bool PrintSpecialInstructions { get; set; } = true;
    
    [Range(1, 5)]
    public int CopiesPerOrder { get; set; } = 1;
}

public class KitchenPrinter
{
    public string Name { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public List<string> Categories { get; set; } = new(); // Which menu categories to print
    public bool IsEnabled { get; set; } = true;
}

public class PrinterDeviceSettings
{
    public List<PrinterDevice> AvailablePrinters { get; set; } = new();
    public bool AutoDetectPrinters { get; set; } = true;
    public string DefaultComPort { get; set; } = "COM1";
    public int DefaultBaudRate { get; set; } = 9600;
}

public class PrinterDevice
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // USB, Serial, Network, Bluetooth
    public string ConnectionString { get; set; } = "";
    public bool IsOnline { get; set; } = false;
    public DateTime LastTestPrint { get; set; }
}

public class PrintJobSettings
{
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;
    
    [Range(1000, 30000)]
    public int TimeoutMs { get; set; } = 5000;
    
    public bool LogPrintJobs { get; set; } = true;
    public bool QueueFailedJobs { get; set; } = true;
    
    [Range(1, 100)]
    public int MaxQueueSize { get; set; } = 50;
}

/// <summary>
/// Notification system settings
/// </summary>
public class NotificationSettings
{
    public EmailSettings Email { get; set; } = new();
    public SmsSettings Sms { get; set; } = new();
    public PushSettings Push { get; set; } = new();
    public AlertSettings Alerts { get; set; } = new();
}

public class EmailSettings
{
    public bool EnableEmail { get; set; } = false;
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = ""; // Should be encrypted
    public bool UseSsl { get; set; } = true;
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
}

public class SmsSettings
{
    public bool EnableSms { get; set; } = false;
    public string Provider { get; set; } = ""; // Twilio, AWS SNS, etc.
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string FromNumber { get; set; } = "";
}

public class PushSettings
{
    public bool EnablePush { get; set; } = true;
    public bool ShowStockAlerts { get; set; } = true;
    public bool ShowOrderAlerts { get; set; } = true;
    public bool ShowPaymentAlerts { get; set; } = true;
    public bool ShowSystemAlerts { get; set; } = true;
}

public class AlertSettings
{
    public List<ThresholdAlert> ThresholdAlerts { get; set; } = new();
    public bool EnableSoundAlerts { get; set; } = true;
    public string AlertSoundPath { get; set; } = "";
    
    [Range(1, 100)]
    public int AlertVolume { get; set; } = 50;
}

public class ThresholdAlert
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // Stock, Revenue, Orders, etc.
    public decimal Threshold { get; set; }
    public string Condition { get; set; } = ""; // Below, Above, Equal
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Security and role-based access settings
/// </summary>
public class SecuritySettings
{
    public RbacSettings Rbac { get; set; } = new();
    public LoginSettings Login { get; set; } = new();
    public SessionSettings Sessions { get; set; } = new();
    public AuditSettings Audit { get; set; } = new();
}

public class RbacSettings
{
    public bool EnforceRolePermissions { get; set; } = true;
    public bool AllowRoleInheritance { get; set; } = true;
    public bool RequireManagerOverride { get; set; } = true;
    public List<string> RestrictedOperations { get; set; } = new();
}

public class LoginSettings
{
    [Range(3, 10)]
    public int MaxLoginAttempts { get; set; } = 5;
    
    [Range(1, 60)]
    public int LockoutDurationMinutes { get; set; } = 15;
    
    public bool RequireStrongPasswords { get; set; } = true;
    
    [Range(1, 365)]
    public int PasswordExpiryDays { get; set; } = 90;
    
    public bool EnableTwoFactor { get; set; } = false;
}

public class SessionSettings
{
    [Range(5, 480)]
    public int SessionTimeoutMinutes { get; set; } = 60;
    
    public bool EnableAutoLogout { get; set; } = true;
    public bool RequireReauthForSensitive { get; set; } = true;
    
    [Range(1, 10)]
    public int MaxConcurrentSessions { get; set; } = 3;
}

public class AuditSettings
{
    public bool EnableAuditLogging { get; set; } = true;
    public bool LogUserActions { get; set; } = true;
    public bool LogSystemEvents { get; set; } = true;
    public bool LogDataChanges { get; set; } = true;
    
    [Range(30, 2555)]
    public int RetentionDays { get; set; } = 365;
}

/// <summary>
/// External integration settings
/// </summary>
public class IntegrationSettings
{
    public PaymentGatewaySettings PaymentGateways { get; set; } = new();
    public WebhookSettings Webhooks { get; set; } = new();
    public CrmSettings Crm { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
}

public class PaymentGatewaySettings
{
    public List<PaymentGateway> Gateways { get; set; } = new();
    public string DefaultGateway { get; set; } = "";
    public bool EnableTestMode { get; set; } = false;
}

public class PaymentGateway
{
    public string Name { get; set; } = "";
    public string Provider { get; set; } = ""; // Stripe, Square, PayPal, etc.
    public string ApiKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public bool IsEnabled { get; set; } = false;
    public bool IsTestMode { get; set; } = true;
}

public class WebhookSettings
{
    public List<WebhookEndpoint> Endpoints { get; set; } = new();
    public bool EnableWebhooks { get; set; } = false;
    
    [Range(1000, 30000)]
    public int TimeoutMs { get; set; } = 10000;
    
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;
}

public class WebhookEndpoint
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public List<string> Events { get; set; } = new();
    public string Secret { get; set; } = "";
    public bool IsEnabled { get; set; } = false;
}

public class CrmSettings
{
    public bool EnableCrmSync { get; set; } = false;
    public string CrmProvider { get; set; } = ""; // Salesforce, HubSpot, etc.
    public string ApiEndpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool SyncCustomers { get; set; } = true;
    public bool SyncOrders { get; set; } = true;
    
    [Range(5, 1440)]
    public int SyncIntervalMinutes { get; set; } = 60;
}

public class ApiSettings
{
    public List<ApiEndpoint> Endpoints { get; set; } = new();
    
    [Range(1000, 60000)]
    public int DefaultTimeoutMs { get; set; } = 10000;
    
    [Range(1, 10)]
    public int DefaultRetries { get; set; } = 3;
    
    public bool EnableApiLogging { get; set; } = true;
}

public class ApiEndpoint
{
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public int TimeoutMs { get; set; } = 10000;
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// System-level settings
/// </summary>
public class SystemSettings
{
    public LoggingSettings Logging { get; set; } = new();
    public TracingSettings Tracing { get; set; } = new();
    public BackgroundJobSettings BackgroundJobs { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information"; // Trace, Debug, Information, Warning, Error, Critical
    public bool EnableFileLogging { get; set; } = true;
    public bool EnableConsoleLogging { get; set; } = true;
    public string LogFilePath { get; set; } = "logs/magidesk.log";
    
    [Range(1, 100)]
    public int MaxLogFileSizeMB { get; set; } = 10;
    
    [Range(1, 100)]
    public int MaxLogFiles { get; set; } = 10;
    
    [Range(1, 365)]
    public int LogRetentionDays { get; set; } = 30;
}

public class TracingSettings
{
    public bool EnableTracing { get; set; } = false;
    public bool TraceApiCalls { get; set; } = true;
    public bool TraceDbQueries { get; set; } = false;
    public bool TraceUserActions { get; set; } = true;
    public string TracingEndpoint { get; set; } = "";
}

public class BackgroundJobSettings
{
    public bool EnableBackgroundJobs { get; set; } = true;
    
    [Range(1, 60)]
    public int HeartbeatIntervalMinutes { get; set; } = 5;
    
    [Range(1, 1440)]
    public int CleanupIntervalMinutes { get; set; } = 60;
    
    [Range(1, 1440)]
    public int BackupIntervalMinutes { get; set; } = 240;
}

public class PerformanceSettings
{
    [Range(10, 1000)]
    public int DatabaseConnectionPoolSize { get; set; } = 50;
    
    [Range(1, 60)]
    public int CacheExpirationMinutes { get; set; } = 15;
    
    public bool EnableResponseCompression { get; set; } = true;
    public bool EnableResponseCaching { get; set; } = true;
    
    [Range(100, 10000)]
    public int MaxConcurrentRequests { get; set; } = 1000;
}

/// <summary>
/// Setting metadata for UI generation
/// </summary>
public class SettingMetadata
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string SubCategory { get; set; } = "";
    public SettingType Type { get; set; } = SettingType.Text;
    public object? DefaultValue { get; set; }
    public List<string>? Options { get; set; } // For dropdowns
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public bool RequiresRestart { get; set; } = false;
    public string? ValidationRegex { get; set; }
    public string? HelpText { get; set; }
}

public enum SettingType
{
    Text,
    Number,
    Decimal,
    Boolean,
    Dropdown,
    MultiSelect,
    Password,
    Email,
    Url,
    FilePath,
    Color,
    DateTime,
    TimeSpan
}
